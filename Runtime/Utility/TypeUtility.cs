using System;
using System.Text;

namespace Hollow.Utility
{
    public class TypeUtility
    {
        public static string GetNiceTypeName(Type type)
        {
            if(type.IsPointer)
            {
                return $"{GetNiceTypeName(type.GetElementType())}*";
            }

            if (type.IsPrimitive)
            {
                if (type == typeof(int))
                    return "int";
                if (type == typeof(float))
                    return "float";
                if (type == typeof(char))
                    return "char";
                if (type == typeof(byte))
                    return "byte";
                if (type == typeof(short))
                    return "short";
                if (type == typeof(long))
                    return "long";
                if (type == typeof(double))
                    return "double";
                if (type == typeof(uint))
                    return "uint";
                if (type == typeof(sbyte))
                    return "sbyte";
                if (type == typeof(ushort))
                    return "ushort";
                if (type == typeof(ulong))
                    return "ulong";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index < 0)
                return type.Name;

            builder.Append(type.Name.Substring(0, index));
            //builder.AppendFormat("{0}.{1}", type.Name, name.Substring(0, index));
            builder.Append('<');
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                builder.Append(GetNiceTypeName(arg));
                first = false;
            }
            builder.Append('>');
            return builder.ToString();
        }
    }
}