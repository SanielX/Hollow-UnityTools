using System.Runtime.CompilerServices;
using Hollow.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Hollow
{
    public static class UBoundsUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect ToRect(this UBounds bounds)
        {
            return new(bounds.min.xz, bounds.size.xz);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps(this in UBounds b0, in UBounds b1)
        {
            return (b0.min.x <= b1.max.x && b0.max.x >= b1.min.x) &&
                   (b0.min.y <= b1.max.y && b0.max.y >= b1.min.y) &&
                   (b0.min.z <= b1.max.z && b0.max.z >= b1.min.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InteresectsSphere(this in UBounds b, in float4 sphere)
        {
            var closestPoint = ClosestPoint(b, sphere.xyz);
            var diff         = sphere.xyz - closestPoint;

            float distSqr   = math.lengthsq(diff);
            float radiusSqr = sphere.w * sphere.w;

            return distSqr < radiusSqr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ClosestPoint(this in UBounds b, in float3 point)
        {
            return math.clamp(point, b.min, b.max);
        }

        public static float DistanceTo(this in UBounds b, float3 point)
        {
            return ClosestPoint(b, point).DistanceTo(point);
        }

        /// <summary>
        /// Add point, so bound volume will extend if needed
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Encapsulate(this ref UBounds b, in float3 point)
        {
            b.min = math.select(b.min, point, point < b.min);
            b.max = math.select(b.max, point, point > b.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBounds Scale(this UBounds b, float scale)
        {
            b.min *= scale;
            b.max *= scale;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBounds Scale(this UBounds b, float3 scale)
        {
            b.extents *= scale;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UBounds Combine(this UBounds b0, in UBounds b1)
        {
            b0.Encapsulate(b1.min);
            b0.Encapsulate(b1.max);
            return b0;
        }
    }
}