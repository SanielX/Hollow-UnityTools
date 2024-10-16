using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hollow.Extensions
{
    public static class ReflectionExtensions
    {
        public static bool ImplementsInterface<T>(this Type t) where T : class
        {
            return t.GetInterfaces().Contains(typeof(T));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetCustomAttribute<T>(this Type t, out T attribute) where T : Attribute
        {
            attribute = t.GetCustomAttribute<T>();
            return attribute is not null;
        }

        public static Delegate GetDelegate(this MethodInfo info)
        {
            var parmTypes          = info.GetParameters().Select(parm => parm.ParameterType);
            var parmAndReturnTypes = parmTypes.Append(info.ReturnType).ToArray();
            var delegateType       = System.Linq.Expressions.Expression.GetDelegateType(parmAndReturnTypes);
            
            return info.CreateDelegate(delegateType);
        }
        
        public static bool IsAutoProperty(this PropertyInfo prop)
        {
            return prop.DeclaringType != null && prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                                     .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }
    }
}
