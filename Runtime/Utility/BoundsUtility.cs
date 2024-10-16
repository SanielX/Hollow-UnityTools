using System;
using Hollow;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Hollow.EdRenderPipeline
{
    public static class BoundsUtility
    {
        // https://iquilezles.org/articles/diskbbox/
        public static UBounds ConeBounds(float3 pa, float3 pb, float ra, float rb)
        {
            float3 a = pb - pa;
            float3 e = sqrt( 1.0f - a*a/dot(a,a) );
            return new( min( pa - e*ra, pb - e*rb ),
                        max( pa + e*ra, pb + e*rb ) );
        }

        public static void DebugDraw(UBounds bounds, Color color) => DebugDraw(bounds, color, Matrix4x4.identity);
        public static void DebugDraw(UBounds bounds, Color color, Matrix4x4 mat)
        {
            Span<float3> points = stackalloc float3[8];
            points[0] = mat.MultiplyPoint3x4(bounds.min);
            points[1] = mat.MultiplyPoint3x4(float3(points[0].x, points[0].y, points[1].z));
            points[2] = mat.MultiplyPoint3x4(float3(points[1].x, points[0].y, points[0].z));
            points[3] = mat.MultiplyPoint3x4(float3(points[0].x, points[1].y, points[0].z));
            points[4] = mat.MultiplyPoint3x4(bounds.max);
            points[5] = mat.MultiplyPoint3x4(float3(points[1].x, points[1].y, points[0].z));
            points[6] = mat.MultiplyPoint3x4(float3(points[0].x, points[1].y, points[1].z));
            points[7] = mat.MultiplyPoint3x4(float3(points[1].x, points[0].y, points[1].z));
            
            Debug.DrawLine(points[0], points[1], color);
            Debug.DrawLine(points[0], points[2], color);
            Debug.DrawLine(points[0], points[3], color);
            
            
            Debug.DrawLine(points[4], points[5], color);
            Debug.DrawLine(points[4], points[6], color);
            Debug.DrawLine(points[4], points[7], color);
        }
    }
}