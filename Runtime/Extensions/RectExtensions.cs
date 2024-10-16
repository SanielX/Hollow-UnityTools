using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Extensions
{
    public static class RectExtensions
    {
        public static bool OverlapInclusive(this Rect r, Rect other)
        {
            return other.xMax >= r.xMin && other.xMin <= r.xMax && other.yMax >= r.yMin && other.yMin <= r.yMax;
        }

        public static Rect Combine(this Rect r, Rect other)
        {
            return new()
            {
                xMin = Mathf.Min(r.xMin, other.xMin),
                yMin = Mathf.Min(r.yMin, other.yMin),

                xMax = Mathf.Max(r.xMax, other.xMax),
                yMax = Mathf.Max(r.yMax, other.yMax),
            };
        }

        public static Rect WithHeight(this Rect r, float height)
        {
            r.height = height;
            return r;
        }
    }
}