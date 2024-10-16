using System;

namespace Hollow
{
    public static class StringEncodingUtility
    {
        public static int GetByteCount(ReadOnlySpan<char> text, TextEncodingType type) => type switch
        {
            TextEncodingType.ASCII   => System.Text.Encoding.ASCII  .GetByteCount(text),
            TextEncodingType.UTF7    => System.Text.Encoding.UTF7   .GetByteCount(text),
            TextEncodingType.UTF8    => System.Text.Encoding.UTF8   .GetByteCount(text),
            TextEncodingType.UTF32   => System.Text.Encoding.UTF32  .GetByteCount(text),
            TextEncodingType.Unicode => System.Text.Encoding.Unicode.GetByteCount(text),
            _ => throw new System.ArgumentException(nameof(type)),
        };

        public static void WriteBytes(ReadOnlySpan<char> text, Span<byte> span, TextEncodingType type)
        {
            switch (type)
            {
            default:
                throw new System.ArgumentException(nameof(type));

            case TextEncodingType.ASCII:
                System.Text.Encoding.ASCII.GetBytes(text, span);
                break;

            case TextEncodingType.UTF7:
                System.Text.Encoding.UTF7.GetBytes(text, span);
                break;

            case TextEncodingType.UTF8:
                System.Text.Encoding.UTF8.GetBytes(text, span);
                break;

            case TextEncodingType.UTF32:
                System.Text.Encoding.UTF32.GetBytes(text, span);
                break;

            case TextEncodingType.Unicode:
                System.Text.Encoding.Unicode.GetBytes(text, span);
                break;
            }
        }

        public static string Decode(this ReadOnlySpan<byte> bytes, TextEncodingType encoding) => encoding switch
        {
            TextEncodingType.ASCII   => System.Text.Encoding.ASCII  .GetString(bytes),
            TextEncodingType.UTF7    => System.Text.Encoding.UTF7   .GetString(bytes),
            TextEncodingType.UTF8    => System.Text.Encoding.UTF8   .GetString(bytes),
            TextEncodingType.UTF32   => System.Text.Encoding.UTF32  .GetString(bytes),
            TextEncodingType.Unicode => System.Text.Encoding.Unicode.GetString(bytes),
            _ => throw new System.ArgumentException("Invalid encoding type", nameof(encoding)),
        };

        public static bool IsValidEncodingType(TextEncodingType type)
        {
            return (type) switch
            {
                TextEncodingType.ASCII   => true,
                TextEncodingType.UTF7    => true,
                TextEncodingType.UTF8    => true,
                TextEncodingType.UTF32   => true,
                TextEncodingType.Unicode => true,
                _ => false
            };
        }
    }
}
