using Hollow;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Hollow
{
    [GenerateTestsForBurstCompatibility]
    public unsafe struct StreamBlock
    {
        public StreamBlock(byte* start, int size)
        {
            buffer = start;
            next   = (byte**)(start + size);
        }

        // Smart pants part here is leaving pointer to the next block 
        // at the end of current
        // therefore when reading, pointer will most probably be prefetched already
        [NativeDisableUnsafePtrRestriction] public byte*  buffer;
        [NativeDisableUnsafePtrRestriction] public byte** next;

        public int bytesLeft => (int)((byte*)next - buffer);
    }

    [GenerateTestsForBurstCompatibility]
    public unsafe struct StreamBuffer : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        public byte*       first;
        public StreamBlock current;
        public Allocator   allocator;
        public int         blockSize;
        public int         bytesWritten;

        public bool IsInitialized => first is not null;

        public StreamBlock At(int position)
        {
            StreamBlock b = new(first, blockSize);

            while (position > 0)
            {
                if (position > blockSize)
                {
                    b = new(*b.next, blockSize);
                    position -= blockSize;
                }
                else
                    b.buffer += position;
            }

            return b;
        }

        public void AllocFirstBlock()
        {
            current = AllocateBlock();
            first   = current.buffer;
        }

        public void Init(Allocator allocator, int blockSize)
        {
            Assert.IsTrue(UnsafeUtility.IsValidAllocator(allocator), "Allocator must be valid");
            Assert.IsTrue(first is null,      "Buffer was already initialized");
            Assert.IsTrue(blockSize % 8 == 0, "Block size must be multiple of 8");

            this.blockSize = blockSize;
            this.allocator = allocator;
        }

        public StreamBlock AllocateBlock()
        {
            Assert.IsTrue(blockSize % 8 == 0);
            var block = (byte*)UnsafeUtility.Malloc(blockSize + sizeof(void*), 
                                                    UnsafeUtility.AlignOf<long>(), allocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS // When viewing bytes in debug it's helpful to know where zeroes are.
                                    // Not useful at all when just writing/reading in release
            UnsafeUtility.MemClear(block, blockSize + sizeof(void*));
#endif 

            var current  = new StreamBlock(block, blockSize);
            *current.next = null;

            return current;
        }

        public void Clear()
        {
            bytesWritten = 0;
            current      = new StreamBlock(first, blockSize);
        }

        public void Dispose()
        {
            byte* current = first;
            
            while(current is not null)
            {
                byte** next = (byte**)(current + blockSize);
                UnsafeUtility.Free(current, allocator);

                current = *next;
            }
        }
    }

    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeByteStream : IDisposable
    {
        public const int DEFAULT_BLOCK_SIZE = 4096 - 8 /*== sizeof(void*) for most cases */;

        public UnsafeByteStream(int bufferCount, Allocator allocator, int blockSize = DEFAULT_BLOCK_SIZE)
        {
            this.bufferCount = bufferCount;
            buffers = (StreamBuffer*)UnsafeUtility.Malloc(bufferCount * sizeof(StreamBuffer),
                                                          UnsafeUtility.AlignOf<StreamBuffer>(),
                                                          allocator);
            UnsafeUtility.MemClear(buffers, bufferCount * sizeof(StreamBuffer));

            for (int i = 0; i < bufferCount; i++)
            {
                (buffers + i)->Init(allocator, blockSize);
            }
        }

        private int           bufferCount;
        [NativeDisableUnsafePtrRestriction]
        private StreamBuffer* buffers;

        public int  BufferCount => bufferCount;
        public bool IsCreated   => buffers is not null;
        public Allocator AllocatorLabel => buffers->allocator;
        public int       BlockSize      => buffers->blockSize;

        /// <summary>
        /// Allocates new stream buffer you can write to
        /// </summary>
        /// <remarks>
        /// Do not use this method if anyone is using the stream
        /// </remarks>
        public void AddBuffers(int count)
        {
            int newCount   = (bufferCount + count);
            var newBuffers = (StreamBuffer*)UnsafeUtility.Malloc(newCount * sizeof(StreamBuffer),
                                                                  UnsafeUtility.AlignOf<StreamBuffer>(),
                                                                  AllocatorLabel);
            // Clear only stuff that is new, copy all old buffers
            UnsafeUtility.MemCpy  (newBuffers,  buffers,     bufferCount * sizeof(StreamBuffer));
            UnsafeUtility.MemClear(newBuffers + bufferCount, count * sizeof(StreamBuffer));

            for (int i = bufferCount; i < count; i++)
            {
                (newBuffers + i)->Init(AllocatorLabel, BlockSize);
            }

            buffers     = newBuffers;
            bufferCount = newCount;  
        }

        public Writer AsWriter(int bufferIndex) => new Writer(buffers + bufferIndex);
        public Reader AsReader(int bufferIndex) => new Reader(buffers + bufferIndex);

        public void ClearBuffer(int bufferIndex)
        {
            var buffer = buffers + bufferIndex;
            buffer->Clear();
        }

        public void Dispose()
        {
            var allocator = buffers->allocator;
            for (int i = 0; i < bufferCount; i++)
            {
                (buffers + i)->Dispose();
            }
            UnsafeUtility.Free(buffers, allocator);

            buffers     = null;
            bufferCount = 0;
        }

        [Unity.Burst.BurstCompile]
        public struct Writer : IWriterStream
        {
            internal Writer(StreamBuffer* streamBuffer)
            {
                buffer = streamBuffer;
                block  = default;
                writtenCount = 0;

                if (!streamBuffer->IsInitialized)
                    streamBuffer ->AllocFirstBlock();
            }

            [NativeDisableUnsafePtrRestriction]
            public StreamBuffer* buffer;
            public StreamBlock   block;
            public int           writtenCount;
            public int           offset => buffer->bytesWritten + writtenCount;

            public byte* BeginWrite()
            {
                block        = buffer->current;
                writtenCount = 0;
                
                return block.buffer;
            }

            public int EndWrite()
            {
                if (writtenCount == 0)
                    return 0;

                buffer->current       = block;
                buffer->bytesWritten += writtenCount;
                int w = writtenCount;
                writtenCount = 0;

                return w;
            }
            
            /// <summary>
            /// Treats struct as raw bytes and writes them directly into the stream
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteValue<T>(T value) where T : unmanaged
            {
                Write((byte*)&value, sizeof(T));
            }

            [Unity.Burst.BurstDiscard]
            public void Write(ReadOnlySpan<char> text, TextEncodingType encoding = TextEncodingType.Unicode,
                              bool insertLength = false)
            {
                if (text == null || text.Length == 0)
                    return;

                // Otherwise we need buffer to store bytes into
                int bytesCount = StringEncodingUtility.GetByteCount(text, encoding);
                if (bytesCount == 0)
                    return;

                int _insertLength = insertLength? 4 : 0;
                bytesCount += _insertLength;

                // This avoids copying by writing bytes directly into stream if possible  (most of the time)
                if (TryAllocSpan(bytesCount, out var bytesSpan))
                {
                    BitConverter.TryWriteBytes(bytesSpan[0.._insertLength], text.Length);
                    StringEncodingUtility.WriteBytes(text, bytesSpan[_insertLength..], encoding);
                }
                else // can't alloc span, then making temp buffer to copy data
                {
                    byte* byteBuffer = (byte*)UnsafeUtility.Malloc(bytesCount,
                                              UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
                    bytesSpan = new Span<byte>(byteBuffer, bytesCount);

                    BitConverter.TryWriteBytes(bytesSpan[0.._insertLength], text.Length);
                    StringEncodingUtility.WriteBytes(text, bytesSpan[_insertLength..], encoding);

                    Write(byteBuffer, bytesSpan.Length);
                    UnsafeUtility.Free(byteBuffer, Allocator.Temp);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write<T>(Span<T> bytes) where T : unmanaged
            {
                fixed (T* ptr = &bytes.GetPinnableReference())
                {
                    Write((byte*)ptr, bytes.Length * sizeof(T));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write<T>(ReadOnlySpan<T> bytes) where T : unmanaged
            {
                fixed (T* ptr = &System.Runtime.InteropServices.MemoryMarshal.GetReference(bytes))
                {
                    Write((byte*)ptr, bytes.Length * sizeof(T));
                }
            }

            public void Write(byte* ptr, int size)
            {
                if (size <= 0)
                    return;

                WriteInternal(ref this, ptr, size);
            }

            delegate void writeInternalDelegate(ref Writer writer, byte* ptr, int size);

            [Unity.Burst.BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(writeInternalDelegate))] // This is for IL2CPP I believe
            static void WriteInternal(ref Writer writer, byte* ptr, int size)
            {
                if (writer.block.bytesLeft == 0)
                    writer.MoveToNextBlock();

                int bytesLeft = size;
                while (bytesLeft > 0)
                {
                    int blockSize = Math.Min(writer.block.bytesLeft, bytesLeft);
                    UnsafeUtility.MemCpy(writer.block.buffer, ptr, blockSize);

                    bytesLeft           -= blockSize;
                    writer.block.buffer += blockSize;
                    ptr                 += blockSize;

                    if (bytesLeft > 0 && writer.block.bytesLeft == 0)
                        writer.MoveToNextBlock();
                }

                writer.writtenCount += size;
            }

            /// <summary>
            /// Tries to acquire span of bytes you can write to directly without copying data. 
            /// Downside is this method may randomly fail if current block does not have enough space left
            /// to hold entire span
            /// </summary>
            /// <remarks>
            /// If span is returned successfully, writer also moves by <paramref name="size"/> bytes
            /// </remarks>
            /// <returns>True if span was successfull allocated</returns>
            public unsafe bool TryAllocSpan(int size, out Span<byte> span)
            {
                if (block.bytesLeft == 0)
                    MoveToNextBlock();

                if(block.bytesLeft < size)
                {
                    span = null;
                    return false;
                }

                span = new(block.buffer, size);
                block.buffer += size;
                writtenCount += size;
                return true;
            }

            public unsafe bool TryAllocUnsafePtr(int size, out byte* ptr)
            {
                if (block.bytesLeft == 0)
                    MoveToNextBlock();

                if (block.bytesLeft < size)
                {
                    ptr = null;
                    return false;
                }

                ptr = block.buffer;
                block.buffer += size;
                writtenCount += size;
                return true;
            }

            internal void MoveToNextBlock()
            {
                if (*block.next is null)
                {
                    var newBlock = buffer->AllocateBlock();
                    *block.next = newBlock.buffer;
                     block      = newBlock;
                }
                else
                {
                    block = new StreamBlock(*block.next, buffer->blockSize); 
                }
            }

            void IWriterStream.Write(ReadOnlySpan<byte> bytes)
             => Write<byte>(bytes);
        }
    
        public struct Reader
        {
            public Reader(StreamBuffer* buffer) 
            {
                this.buffer = buffer;
                block = new StreamBlock(buffer->first, buffer->blockSize);
                position = 0;
            }
            
            public int Position
            {
                get => position;
                set 
                {
                    block = buffer->At(value);
                    position = value;
                }
            }

            public int Length => buffer->bytesWritten;

            public StreamBuffer* buffer;
            public StreamBlock   block;
            
            private int position;

            /// <summary>
            /// Advances stream by <paramref name="size"/> bytes
            /// </summary>
            public void Move(int size)
            {
                while (size > 0)
                {
                    int read = GetUnsafeReadPointer(size, out _);
                    if (read == 0) // Infinite loop can happen otherwise
                        return;

                    size -= read;
                }
            }

            internal int GetUnsafeReadPointer(int size, out byte* ptr)
            {
                if (block.bytesLeft == 0)
                    ReadNextBlock();

                int maxToRead = Math.Min(block.bytesLeft, size);

                ptr = block.buffer;
                block.buffer += maxToRead; // This may go out of bounds, but we never read it
                position     += maxToRead;

                return maxToRead;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReadTo<T>(Span<T> span) where T : unmanaged
            {
                // var ptr = (byte*)UnsafeUtility.AddressOf(ref span.GetPinnableReference());
                fixed(void* ptr = &span.GetPinnableReference())
                    ReadTo((byte*)ptr, span.Length * sizeof(T));
            }

            [Unity.Burst.BurstDiscard]
            public int CopyToStream(System.IO.Stream stream, int size)
            {
                var streamWrapper = new ManagedStreamWriter(stream);
                return CopyToStream(ref streamWrapper, size);
            }

            public int CopyToStream<T>(ref T stream, int size) where T : IWriterStream
            {
                int bytesLeft = size;
                while(bytesLeft > 0)
                {
                    int readSize = GetUnsafeReadPointer(bytesLeft, out var ptr);
                    if (readSize == 0)
                        break;

                    stream.Write(ptr, readSize);
                    bytesLeft -= readSize;
                }

                return size - bytesLeft; 
            }

            public void ReadTo(byte* ptr, int size, bool throwIfAnyLeft = false)
            {
                int bytesLeft = size;
                while (bytesLeft > 0)
                {
                    int blockSize = GetUnsafeReadPointer(bytesLeft, out var readPtr);
                    if (blockSize == 0)
                        break;

                    UnsafeUtility.MemCpy(ptr, readPtr, blockSize);
                    ptr       += blockSize;
                    bytesLeft -= blockSize;
                }

                if (throwIfAnyLeft && bytesLeft > 0)
                    throw new System.IndexOutOfRangeException($"Stream was not big enough to read " +
                                                              $"requested {size} bytes (read {size - bytesLeft})");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ReadNextBlock()
            {
                if (*block.next is not null)
                    block = new StreamBlock(*this.block.next, buffer->blockSize);
                else
                    block = default;
            }
        }
    }
}
