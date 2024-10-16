using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Linq;

namespace Hollow
{
    public static class JsonHelper
    {
        [Serializable]
        private struct Wrapper<T>
        {
            public T Value;
        }

        public static string ToJson<T>(T array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Value = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T thing, bool prettyPrint = false)
        {
            Wrapper<T> t = new Wrapper<T>()
            {
                Value = thing
            };

            return JsonUtility.ToJson(t, prettyPrint);
        }

        public static T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<Wrapper<T>>(json).Value;
        }

        private const string INDENT_STRING = "    ";

        /// <summary>
        /// Editor only method
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        private static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
}