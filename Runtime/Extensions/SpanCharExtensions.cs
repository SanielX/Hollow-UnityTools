using System;

namespace Hollow.Extensions
{
    public static class SpanCharExtensions
    {
        public static Span<char> Concat(this Span<char> span, ReadOnlySpan<char> s0, int count0, ReadOnlySpan<char> s1, int count1)
        {
            s0[0..count0].CopyTo(span);
            s1[0..count1].CopyTo(span[s0.Length..]);

            return span;
        }

        public static Span<char> Concat(this Span<char>    span,
                                        ReadOnlySpan<char> s0, int count0,
                                        ReadOnlySpan<char> s1, int count1,
                                        ReadOnlySpan<char> s2, int count2)
        {
            s0[0..count0].CopyTo(span);
            s1[0..count1].CopyTo(span[count0..]);
            s2[0..count2].CopyTo(span[(count0 + count1)..]);

            return span;
        }

        public static int Join(this Span<char> span, char separator, string[] array, int startIndex, int count)
        {
            int pos   = 0;
            int iters = startIndex + count;
            for (int i = startIndex; i < iters; i++)
            {
                var part = array[i].AsSpan();

                part.CopyTo(span[pos..]);
                pos += part.Length;

                if (i != iters - 1)
                {
                    span[pos] = separator;
                    pos++;
                }
            }

            return pos;
        }
    }
}
