using System;
using System.IO;

namespace Hollow
{
    public interface IWriterStream
    {
        public void Write(ReadOnlySpan<byte> bytes);

        public unsafe void Write(byte* ptr, int size) => Write(new(ptr, size));
    }

    public struct ManagedStreamWriter : IWriterStream
    {
        public ManagedStreamWriter(Stream stream)
        {
#if UNITY_ASSERTIONS
            if (stream is null)
                throw new System.ArgumentNullException(nameof(stream));

            if (!stream.CanWrite)
                throw new System.ArgumentException("Stream must be able to write", nameof(stream));
#endif 

            this.stream = stream;
        }

        private Stream stream;

        public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes);

        public static implicit operator ManagedStreamWriter(Stream stream) => new(stream);
    }
}
