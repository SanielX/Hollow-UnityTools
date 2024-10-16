using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Hollow
{
    internal static unsafe class xxHashUtility
    {
        public static unsafe uint2 HashString(ReadOnlySpan<char> text)
        {
            fixed (char* chars = &MemoryMarshal.GetReference(text))
                return xxHash3.Hash64((byte*)chars, sizeof(char) * text.Length, 0);
        }
    }
}
