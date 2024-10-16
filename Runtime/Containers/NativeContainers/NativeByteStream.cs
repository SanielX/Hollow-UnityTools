using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Hollow
{
    /// <summary>
    /// A set of untyped, append-only buffers. Allows for concurrent reading and concurrent writing without synchronization.
    /// </summary>
    /// <remarks>
    /// As long as each individual buffer is written in one thread and read in one thread, multiple
    /// threads can read and write the stream concurrently, *e.g.*
    /// while thread *A* reads from buffer *X* of a stream, thread *B* can read from
    /// buffer *Y* of the same stream.
    ///
    /// Each buffer is stored as a chain of blocks. When a write exceeds a buffer's current capacity, another block
    /// is allocated and added to the end of the chain. Effectively, expanding the buffer never requires copying the existing
    /// data (unlike with <see cref="NativeList{T}"/>, for example).
    ///
    /// **All writing to a stream should be completed before the stream is first read.**
    /// Violating these rules won't *necessarily* cause any problems, but they are the intended usage pattern.
    ///
    /// Writing is done with <see cref="NativeByteStream.Writer"/>, and reading is done with <see cref="NativeByteStream.Reader"/>.
    /// An individual reader or writer cannot be used concurrently across threads: each thread must use its own.
    ///
    /// The data written to an individual buffer can be heterogeneous in type, and the data written
    /// to different buffers of a stream can be entirely different in type, number, and order. Just make sure
    /// that the code reading from a particular buffer knows what to expect to read from it.
    /// </remarks>
    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct NativeByteStream : IDisposable
    {
        // Okay so it turned out that NativeStream from Unity.Collections is kinda useless
        // It makes this weird assumption that you only ever want to write/read N elements of same size/type
        // It is possible to write wrapper that does proper untyped writing/reading, but it would be limited
        // because unlike UnsafeList, UnsafeStream is encapsulated for some reason
        // so here you go, custom NativeCollection

        /// <param name="bufferCount">Number of separate buffers you can write or read from different threads</param>
        /// <param name="blockSize">Must be bigger than 0 and multiple of 8</param>
        public NativeByteStream(int bufferCount, Allocator allocator, 
                                int blockSize = UnsafeByteStream.DEFAULT_BLOCK_SIZE)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (bufferCount <= 0)
                throw new System.ArgumentException
                    ($"Buffer count must be greater than 0 (Given: '{bufferCount}')", nameof(bufferCount));

            if (allocator <= Allocator.None)
                throw new System.ArgumentException($"Invalid allocator {allocator}");

            if (blockSize <= 0 || blockSize % 8 != 0)
                throw new System.ArgumentException
                    ($"Block size must be bigger than 0 and multiple of 8 (Given: '{blockSize}')", nameof(blockSize));

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
            m_BufferStates = (long*)UnsafeUtility.Malloc(bufferCount  * sizeof(long),
                                                         UnsafeUtility.AlignOf<long>(),
                                                         allocator);
            UnsafeUtility.MemClear(m_BufferStates, bufferCount * sizeof(long));
#endif 

            stream = new UnsafeByteStream(bufferCount, allocator, blockSize);
        }

        internal UnsafeByteStream   stream;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private long*               m_BufferStates; // "Is Writing" flag for each buffer, set atomically
        private AtomicSafetyHandle  m_Safety;
        private DisposeSentinel     m_DisposeSentinel;
#endif 

        /// <inheritdoc cref="NativeStream.IsCreated"/>
        public bool IsCreated => stream.IsCreated;

        public int BufferCount => stream.BufferCount;
        
        /// <inheritdoc cref="UnsafeByteStream.AddBuffers(int)"/>
        public void AddBuffers(int count = 1)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (count < 1)
                throw new System.ArgumentException($"Can't add less than 1 buffer (Given: '{count}')", nameof(count));

            for (int i = 0; i < BufferCount; i++)
            {
                if (IsWriting(m_BufferStates + i))
                    throw new System.AccessViolationException("Can't add buffers to stream while it is being written to");
            }

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            var newCount = BufferCount + count;
            UnsafeUtility.Free(m_BufferStates, stream.AllocatorLabel);
            m_BufferStates = (long*)UnsafeUtility.Malloc(newCount * sizeof(long),
                                                         UnsafeUtility.AlignOf<long>(),
                                                         stream.AllocatorLabel);
            UnsafeUtility.MemClear(m_BufferStates, newCount * sizeof(long));
#endif 

            stream.AddBuffers(count);
        }

        /// <inheritdoc cref="NativeStream.AsWriter"/>
        /// <remarks>Returned struct must always be passed by reference</remarks>
        public Writer AsWriter(int bufferIndex)
        {
            CheckWriteAccess(bufferIndex);
            long* bufferState = null;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            bufferState = m_BufferStates + bufferIndex;
#endif 
            return new Writer(ref stream, bufferIndex, bufferState);
        }

        /// <inheritdoc cref="NativeStream.AsReader"/>
        public Reader AsReader(int bufferIndex)
        {
            CheckReadAccess(bufferIndex);
            return new Reader(ref stream, bufferIndex);
        }

        public void ClearBuffer(int bufferIndex)
        {
            CheckWriteAccess(bufferIndex);
            stream.ClearBuffer(bufferIndex);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess(int bufferIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            BoundsCheckAndThrow(bufferIndex);
            if (IsWriting(m_BufferStates + bufferIndex))
                throw new System.AccessViolationException("Can not start reading from buffer that is being written to");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess(int bufferIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            BoundsCheckAndThrow(bufferIndex);
            if (IsWriting(m_BufferStates + bufferIndex))
                throw new System.AccessViolationException("Can not start writing to buffer that is being written to already");

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        internal long* GetBufferStatePtr(int bufferIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return m_BufferStates + bufferIndex;
#else
            return null;
#endif 
        }
        internal static bool IsWriting (long* bufferState) 
            => Interlocked.Read(ref *bufferState) > 0;
        internal static void SetWriting(long* bufferState, bool isWriting) 
            => Interlocked.Exchange(ref *bufferState, isWriting? 1 : 0);

        private void BoundsCheckAndThrow(int bufferIndex)
        {
            if (bufferIndex < 0 || bufferIndex >= stream.BufferCount)
                throw new System.IndexOutOfRangeException
                    ($"Index '{bufferIndex}' is out of range (BufferCount: {stream.BufferCount})");
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
            UnsafeUtility.Free(m_BufferStates, stream.AllocatorLabel);
#endif
            stream.Dispose();
        }

        /// <summary>
        /// This struct must always be passed by reference
        /// </summary>
        [DebuggerDisplay("Bytes Written = {WrittenBytesCount}")]
        [GenerateTestsForBurstCompatibility]
        public struct Writer : IWriterStream
        {
            internal Writer(ref UnsafeByteStream stream, int foreachIndex, long* bufferState)
            {
                this.writer = stream.AsWriter(foreachIndex); // can't copy it btw
                
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_PassByRefCheckPtr  = null;
                m_BufferState = bufferState;
#endif
            }

            internal UnsafeByteStream.Writer writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [NativeDisableUnsafePtrRestriction]
            private long* m_BufferState; // Points to safety handle from stream itself
            [NativeDisableUnsafePtrRestriction]
            private void* m_PassByRefCheckPtr;
#endif
            
            /// <summary>
            /// Count of bytes that were written since last <see cref="BeginWrite"/> call
            /// </summary>
            public long WrittenBytesCount => writer.writtenCount;

            public int Offset => writer.offset;

            /// <summary>
            /// Starts appending data to the end of stream, you need to apply changes by calling EndWrite
            /// </summary>
            public byte* BeginWrite()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_PassByRefCheckPtr is not null)
                    throw new System.InvalidOperationException("Writer is already writing into stream");

                if (IsWriting(m_BufferState))
                    throw new System.AccessViolationException("Someone is already writing to requested buffer");

                SetWriting(m_BufferState, true); 
                
                fixed(Writer* self = &this) // This is ref struct but C# still wants fixed, why the hell
                    m_PassByRefCheckPtr = self;
#endif

                return writer.BeginWrite();
            }

            /// <summary>
            /// Applies written data into stream
            /// </summary>
            public int EndWrite()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_PassByRefCheckPtr is null)
                    throw new System.AccessViolationException("You must call BeginWrite before EndWrite");

                m_PassByRefCheckPtr = null;

                if (!IsWriting(m_BufferState))
                    return 0;

                SetWriting(m_BufferState, false);
#endif

                return writer.EndWrite();
            }

            /// <summary>
            /// Discards any content that was written into it since last BeginWrite call
            /// </summary>
            public void Discard()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                CheckWriteAndThrow();
                if (m_PassByRefCheckPtr is null)
                    throw new System.Exception("You must call BeginWrite before Discarding any contents");
#endif
                writer.BeginWrite();
            }

            /// <summary>
            /// Treats struct as raw bytes and writes them directly into the stream
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteValue<T>(T value) where T : unmanaged
            {
                Write((byte*)&value, sizeof(T));
            }

            /// <summary>
            /// Writes string directly into buffer if encoding is Unicode, otherwise creates temporary 
            /// unmanaged buffer and uses System.Text.Encoding.ENCODER.GetBytes
            /// </summary>
            public void Write(ReadOnlySpan<char> text, TextEncodingType encoding = TextEncodingType.Unicode,
                              bool insertLength = false)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!StringEncodingUtility.IsValidEncodingType(encoding))
                    throw new System.ArgumentException("Invalid encoding type", nameof(encoding));

                CheckWriteAndThrow();
#endif
                writer.Write(text, encoding, insertLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write(byte[] bytes)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (bytes == null)
                    throw new System.ArgumentNullException(nameof(bytes));
#endif 
                fixed (byte* ptr = &bytes[0])
                {
                    Write(ptr, bytes.Length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write<T>(Span<T> bytes) where T : unmanaged
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (bytes == null)
                    throw new System.ArgumentNullException(nameof(bytes));
#endif 
                fixed(T* ptr = &bytes.GetPinnableReference())
                {
                    Write((byte*)ptr, bytes.Length * sizeof(T));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write<T>(ReadOnlySpan<T> bytes) where T : unmanaged
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (bytes == null)
                    throw new System.ArgumentNullException(nameof(bytes));
#endif 
                fixed (T* ptr = &MemoryMarshal.GetReference(bytes))
                {
                    Write((byte*)ptr, bytes.Length * sizeof(T));
                }
            }

            public void Write(byte* ptr, int size)
            {
                CheckWriteAndThrow();
                writer.Write(ptr, size);
            }

            /// <inheritdoc cref="UnsafeByteStream.Writer.TryAllocSpan"/>
            public bool TryAllocSpan(int size, out Span<byte> span)
            {
                CheckWriteAndThrow();
                return writer.TryAllocSpan(size, out span);
            }

            private readonly void CheckWriteAndThrow()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!IsWriting(m_BufferState))
                    throw new System.Exception("You must call BeginWrite before writing any data into stream");

                // fixed(Writer* self = &this)
                // {
                //     if (self != m_PassByRefCheckPtr)
                //         throw new System.Exception("NativeByteStream.Writer must always be passed by reference");
                // }

                if (writer.buffer is null)
                    throw new Exception("You must call BeginWrite on NativeByteStream.Writer before writing anything");
#endif
            }

            void IWriterStream.Write(ReadOnlySpan<byte> bytes)
                            => Write<byte>(bytes);
        }

        [GenerateTestsForBurstCompatibility]
        public ref struct Reader
        {
            internal Reader(ref UnsafeByteStream stream, int bufferIndex)
            {
                reader = stream.AsReader(bufferIndex);
            }

            UnsafeByteStream.Reader reader;
            
            public int Length => reader.Length;
            public int Position
            {
                get => reader.Position;
                set
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (value < 0 || value >= Length)
                        throw new System.ArgumentOutOfRangeException($"Value '{value}' is out of bounds (Length: {Length})");
#endif
                    reader.Position = value;
                }
            }

            /// <summary>
            /// Advances stream by <paramref name="size"/> bytes right
            /// </summary>
            public void Move(int size) => reader.Move(size);

            /// <summary>
            /// Returns new value read from stream and advances it by sizeof(T) bytes
            /// In case stream can't read sizeof(T) amount of bytes 
            /// <see cref="System.IndexOutOfRangeException"/> is thrown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T ReadAs<T>() where T : unmanaged
            {
                T value = default;
                ReadTo((byte*)UnsafeUtility.AddressOf(ref value), sizeof(T), true);

                return value;
            }

            /// <summary>
            /// Replaces value bytes with bytes from stream and advances by sizeof(T) bytes.
            /// In case stream can't read sizeof(T) amount of bytes <see cref="System.OutOfMemoryException"/> is thrown
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReadTo<T>(ref T value) where T : unmanaged
            {
                var ptr = (byte*)UnsafeUtility.AddressOf(ref value);
                ReadTo(ptr, sizeof(T), true);
            }

            /// <summary>
            /// Copies data from stream to span
            /// </summary>
            /// <param name="span">Target span to read data to</param>
            /// <returns>Count of bytes that were read. May be less than span.Length * sizeof(T)</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReadTo<T>(Span<T> span) where T : unmanaged
            {
                reader.ReadTo(span);
            }

            /// <summary>
            /// Copies data from stream to pointer
            /// </summary>
            /// <param name="ptr">Pointer to copy data to</param>
            /// <param name="size">Amount of bytes to read</param>
            /// <param name="throwIfAnyLeft">Allows to throw an exception if stream couldn't read required amount of bytes</param>
            /// <returns>Number of bytes that were read</returns>
            public void ReadTo(byte* ptr, int size, bool throwIfAnyLeft = false)
            {
                CheckReadAndThrow(size);
                reader.ReadTo(ptr, size, throwIfAnyLeft);
            }

            public ReadOnlySpan<byte> GetReadSpan(int desiredSize)
            {
                var size = reader.GetUnsafeReadPointer(desiredSize, out var ptr);
                return new(ptr, size); // FIXME: Where is constructor with long geez
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckReadAndThrow(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((Length - Position) < size)
                    throw new System.IndexOutOfRangeException
                    ($"Buffer has only '{(Length - Position)}' bytes left and you are trying to read '{size}'");
   
                if(reader.block.buffer is null)
                {
                    throw new System.Exception("Buffer pointer is null");
                }
#endif
            }
        }
    }
}
