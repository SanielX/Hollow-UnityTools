using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hollow.Extensions
{
    public static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNullOrEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNullOrWhiteSpace(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        public static string ToFullString(this string[] arr)
        {
            StringBuilder builder = new StringBuilder(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                builder.Append(arr[i]);
            }

            return builder.ToString();
        }

        public static string ToFullString(this string[] arr, string insert)
        {
            StringBuilder builder = new StringBuilder(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                builder.Append(arr[i]);
                if (i != arr.Length - 1)
                    builder.Append(insert);
            }

            return builder.ToString();
        }

        public static string ToFullString(this List<string> arr, string insert)
        {
            StringBuilder builder = new StringBuilder(arr.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                builder.Append(arr[i]);
                if (i != arr.Count - 1)
                    builder.Append(insert);
            }

            return builder.ToString();
        }

        public static string ToFullString(this List<List<string>> arr, string insert, string newLine = "\n")
        {
            StringBuilder builder = new StringBuilder(arr.Count);

            for (int i = 0; i < arr.Count; i++)
            {
                for (int j = 0; j < arr[i].Count; j++)
                {
                    builder.Append(arr[i][j]);
                    if (j != arr[i].Count - 1)
                        builder.Append(insert);
                }

                builder.Append(newLine);
            }

            return builder.ToString();
        }

        public static bool StartsWithAny(this string str, string[] strs, StringComparison cmp)
        {
            foreach (var s in strs)
                if (str.StartsWith(s, cmp))
                    return true;

            return false;
        }
    }
}