using UnityEngine;

namespace Hollow
{
    public class FloatRangeMinAttribute : PropertyAttribute
    {
        public FloatRangeMinAttribute(float min = 0.0f)
        {
            Min = min;
        }

        public float Min { get; }
    }

    [System.Serializable]
    public struct FloatRange
    {
        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        [SerializeField] float min;
        [SerializeField] float max;

        public float Min
        {
            readonly get => min;
            set 
            {
                min = Mathf.Min(max, value);
            }
        }
        public float Max
        {
            readonly get => max;
            set
            {
                max = Mathf.Max(min, value);
            }
        }

        public readonly float Lerp(float t) => Mathf.Lerp(min, max, t);
        
        /// <summary>
        /// Returns normalized [0;1] value according to min/max values
        /// </summary>
        /// <remarks>Result is clamped to 0-1!</remarks>
        public readonly float InverseLerp(float x) => Mathf.InverseLerp(min, max, x);
        public readonly float Random() => UnityEngine.Random.Range(min, max);

        public readonly float Clamp(float value) => Mathf.Clamp(value, Min, Max);

        public static implicit operator Vector2(FloatRange range) => new Vector2(range.min, range.max);

        public static implicit operator FloatRange((float x0, float x1) range) => new(Mathf.Min(range.x0, range.x1), Mathf.Max(range.x0, range.x1));
        public static implicit operator FloatRange(float x) => new(x, x);

        public override string ToString()
        {
            return $"({min}:{max})";
        }
    }
}
