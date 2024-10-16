using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Hollow.Extensions
{
    public static class Compare
    {
        /// <summary>
        /// It's 2 to the power of -13
        /// </summary>
        public const float REASONABLE_EPSILON     = 0.0001220703125f;
        public const float REASONABLE_EPSILON_SQR = REASONABLE_EPSILON * REASONABLE_EPSILON;

        /// <summary>
        /// Compares 2 floats based on given delta
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEquals(this float Num1, float Num2, float delta = REASONABLE_EPSILON)
        {
            return Mathf.Abs(Num1 - Num2) <= delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEqualsOrLess(this float Num1, float Num2, float delta = REASONABLE_EPSILON)
        {
            return Num1 < Num2 || Mathf.Abs(Num1 - Num2) <= delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEqualsOrBiggerThan(this float Num1, float Num2, float delta = REASONABLE_EPSILON)
        {
            return Num1 > Num2 || Mathf.Abs(Num1 - Num2) <= delta;
        }

        /// <summary>
        /// Compares 2 floats based on given delta
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEquals (this float Num1, float Num2, float delta = REASONABLE_EPSILON)
        {
            return !RoughlyEquals(Num1, Num2, delta);
        }

        /// <summary>
        /// Compares 2 ints based on given delta
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEquals(this int Num1, int Num2, int delta = 1)
        {
            return Mathf.Abs(Num1 - Num2) <= delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEquals(this ref Vector3 vec1, in Vector3 vec2, in float delta = REASONABLE_EPSILON)
        {
            return vec1.x.RoughlyEquals(vec2.x, delta)
                && vec1.y.RoughlyEquals(vec2.y, delta)
                && vec1.z.RoughlyEquals(vec2.z, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEquals(this ref Quaternion vec1, in Quaternion vec2, in float angleDelta = 0.01f)  // Higher epsilon for quaternions
        {
            return Mathf.Abs(Quaternion.Angle(vec1, vec2)) < angleDelta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool roughlyEquals(this ref float3 vec1, in float3 vec2, in float delta = REASONABLE_EPSILON)
        {
            return vec1.x.RoughlyEquals(vec2.x, delta)
                && vec1.y.RoughlyEquals(vec2.y, delta)
                && vec1.z.RoughlyEquals(vec2.z, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool roughlyEquals(this ref float2 vec1, in float2 vec2, in float delta = REASONABLE_EPSILON)
        {
            return vec1.x.RoughlyEquals(vec2.x, delta)
                && vec1.y.RoughlyEquals(vec2.y, delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RoughlyEquals(this ref Vector2 vec1, in Vector2 vec2, in float delta = REASONABLE_EPSILON)
        {
            return vec1.x.RoughlyEquals(vec2.x, delta)
                && vec1.y.RoughlyEquals(vec2.y, delta);
        }
    }
}