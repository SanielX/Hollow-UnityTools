using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Extensions
{
    public static class Extensions
    {
        private static List<Collider> _collidersPooled = new();

        public static int[] GetAllColliders(this Component cmp, bool includeInactive = false)
        {
            _collidersPooled.Clear();
            cmp.GetComponentsInChildren<Collider>(includeInactive, _collidersPooled);

            int[] res = new int[_collidersPooled.Count];
            for (int i = 0; i < _collidersPooled.Count; i++)
            {
                res[i] = _collidersPooled[i].GetInstanceID();
            }

            return res;
        }

        /// <summary>
        /// If a is true then b must be true as well. If a is false then result of b doesn't matter
        /// </summary>
        public static bool Means(this bool a, bool b) => !a | b;

        public static Transform[] GetChildren(this Transform tr)
        {
            var c = new Transform[tr.childCount];
            for (int i = 0; i < tr.childCount; i++)
            {
                c[i] = tr.GetChild(i);
            }

            return c;
        }

        public static bool TryGetComponentInParent<T>(this UnityEngine.Component o, out T obj)
        {
            obj = o.GetComponentInParent<T>();
            return obj != null;
        }

        public static Transform FindRecoursive(this Transform from, string name)
        {
            var child = from.Find(name);
            if (!child)
            {
                for (int i = 0; i < from.childCount; i++)
                {
                    var r = FindRecoursive(from.GetChild(i), name);
                    if (r)
                        return r;
                }
            }

            return child;
        }
        
        public static void FindChildrenWithTagRecoursive(this Transform from, string tag, ref List<Transform> results)
        {
            for (int i = 0; i < from.childCount; i++)
            {
                var c = from.GetChild(i);
                if(c.gameObject.CompareTag(tag))
                    results.Add(c);
                else 
                    FindChildrenWithTagRecoursive(from, tag, ref results);
            }
        }

        public static HashSet<int> ToInstanceIDHashSet(this Collider[] colliders)
        {
            if (colliders is null)
                return null;

            HashSet<int> result = new(colliders.Length);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i])
                    result.Add(colliders[i].GetInstanceID());
            }

            return result;
        }

        /// <summary>
        /// Determines if <paramref name="x"/> is in [<paramref name="min"/>; <paramref name="max"/>] range
        /// </summary>
        /// <remarks>Requires both <paramref name="min"/> and <paramref name="max"/> to be positive</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInPositiveRange(this float x, float min, float max) => ((x - min) <= (max - min));

        /// <summary>
        /// Determines if <paramref name="x"/> is in [<paramref name="range"/>.Min; <paramref name="range"/>.Max] range
        /// </summary>
        /// <remarks>Requires both min and max to be positive</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInPositiveRange(this float x, FloatRange range) => ((x - range.Min) <= (range.Max - range.Min));

        public static bool IsValid(this in RaycastHit hit) => hit.distance > 0 && hit.point != Vector3.zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Find<T>(this T[] array, in T element) where T : class
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (element == array[i])
                    return true;
            }

            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Find(this int[] array, int element)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (element == array[i])
                    return true;
            }

            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Find(this string[] array, string element, StringComparison comparison)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(element, comparison))
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt<T>(this T @enum) where T : struct, IConvertible
        {
            return UnsafeUtility.EnumToInt(@enum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToEnum<T>(this int i) where T : struct, IConvertible
        {
            return UnsafeUtility.As<int, T>(ref i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAlpha<T>(this T i, float alpha) where T : Graphic
        {
            Color c = i.color;
            c.a = alpha;

            i.color = c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClampThis(this ref float a, float min, float max)
        {
            a = math.max(min, math.min(max, a));
        }
    }
}