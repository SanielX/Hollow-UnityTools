using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hollow
{
    /// <summary>
    /// These bounds store min and max instead of center and extends like <see cref="Bounds"/>
    /// </summary>
    [System.Serializable]
    public struct UBounds : IEquatable<UBounds>
    {
        public UBounds(in float3 min, in float3 max)
        {
            Assert.IsTrue(math.all(max >= min), "math.all(max >= min)");
            
            this.min = min;
            this.max = max;
        }

        public float3 min;
        public float3 max;

        public float3 center 
        {
            readonly get => (min + max) * 0.5f;

            set
            {
                var extends = this.extents;
                
                min = value - extends;
                max = value + extends;
            }
        }

        /// <summary>
        /// <code>
        /// center + extends = max; 
        /// center - extends = min;
        /// </code>
        /// </summary>
        public float3 extents
        {
            readonly get => (max - min) * 0.5f;

            set
            {
                var ctr = center;

                min = ctr - value;
                max = ctr + value;
            }
        }

        /// <summary>
        /// Twice as big as <see cref="extents"/>
        /// </summary>
        public float3 size
        {
            readonly get => max - min;
            set 
            {
                var ctr = center;
                value *= .5f;

                min = ctr - value;
                max = ctr + value;
            }
        }

        public readonly float volume()
        {
            float3 _size = size;
            return _size.x * _size.y * _size.z;
        }

        public override readonly string ToString()
        {
            return $"(min: ({min}); max: ({max}))";
        }

        public static implicit operator Bounds(in  UBounds b) => new Bounds { min  = b.min, max = b.max };
        public static implicit operator UBounds(in Bounds  b) => new UBounds { min = b.min, max = b.max };

        public UBounds WithSize(float3 size1)
        {
            return new()
            {
                center = this.center,
                size   = size1
            };
        }

        public bool Equals(UBounds other)
        {
            return min.Equals(other.min) && max.Equals(other.max);
        }

        public override bool Equals(object obj)
        {
            return obj is UBounds other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(min, max);
        }
    }
}