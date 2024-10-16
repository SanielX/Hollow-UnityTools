using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hollow
{
    /// <summary>
    /// 62bit encoded value that can be used as lighter version of Guid. Provides initialization with random (<see cref="UniqueTag.New()"/>), similar to GUID but
    /// also provides ability to initialize as string using corrsponsing constructor. 
    /// </summary>
    /// <remarks>
    /// Hashed strings information persist only during editor/player session. When editor is reloaded, 
    /// you can no longer get string from UniqueTag without hashing text first. Use <see cref="RememberString(string)"/>
    /// to store hash for a given string.
    /// </remarks>
    [System.Serializable]
    public unsafe struct UniqueTag : IEquatable<UniqueTag>, IComparable<UniqueTag>
    {
        private static ulong s_deviceID;
        [ThreadStatic] // UnityEngine.Random can be messed with by setting seed which is what we do not want by any means
        private static System.Random t_random;

        [SerializeField, HideInInspector] internal int _a;
        [SerializeField, HideInInspector] internal int _b;

        public static UniqueTag Null => default;

        public UniqueTag(int id)
        {
            _a = id;
            _b = (int)UniqueTagType.Int32;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitStaticValues()
        {
            // Fucking not allowed to call from constructor bullshit god damn it
            // Also it seems like if RuntimeInitalizeOnLoadMethod guarded by #if may not be called in builds
            // So we'll have to leave this empty method
#if !UNITY_EDITOR
            var hash   = xxHashUtility.HashString(SystemInfo.deviceUniqueIdentifier);
            s_deviceID = UnsafeUtility.As<uint2, ulong>(ref hash);
#endif 
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitHashMap()
        {
            // Hash storage object serializes all hashes so they can survive domain reload
            s_hashStorage           =  ScriptableObject.CreateInstance<StringHashStorage>();
            s_hashStorage.hideFlags |= HideFlags.DontSave;
            
            var hash   = xxHashUtility.HashString(SystemInfo.deviceUniqueIdentifier);
            s_deviceID = UnsafeUtility.As<uint2, ulong>(ref hash);
        }

        private static StringHashStorage                       s_hashStorage = null!;
        private static ConcurrentDictionary<UniqueTag, string> hashMap => StringHashStorage.hashMap;
#else // Don't need to keep dictionary between domain reloads, therefore can just use field
        private static ConcurrentDictionary<UniqueTag, string> hashMap = new();
#endif

        internal const bool REMEMBER_HASHES_DEFAULT =
#if UNITY_ASSERTIONS
            true;
#else
            false;
#endif

        private static bool RememberStringHashes { get; set; } = REMEMBER_HASHES_DEFAULT;

        public static void RememberString(string text)
        {
            new UniqueTag(text);
        }

        public static string TryFindString(UniqueTag tag)
        {
            if (hashMap.TryGetValue(tag, out var val))
                return val;

            return null;
        }

        public UniqueTag(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _a = 0;
                _b = 0;
                return;
            }

            var hash = xxHashUtility.HashString(text);

            _a = (int)hash.x;
            _b = (int)(hash.y & ~0b11u) | (int)UniqueTagType.Text;

            if (RememberStringHashes)
                hashMap.TryAdd(this, text);
        }

        public UniqueTag(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < sizeof(UniqueTag))
                throw new System.ArgumentException("ReadOnlySpan must have size of " + sizeof(UniqueTag) + " bytes to initialize unique tag");

            _a = BitConverter.ToInt32(bytes);
            _b = BitConverter.ToInt32(bytes[4..]);

            if (_a != 0 &&  _b != 0 && GetTagType() == UniqueTagType.Null)
                throw new System.InvalidOperationException("Span contains invalid unique tag. Contents are non null but tag type is null");
        }

        /// <summary>
        /// Attempts to generate semi random unique value based on device id, 
        /// current date time and just random values
        /// </summary>
        public static UniqueTag New()
        {
            if (t_random is null) t_random = new System.Random();
            UniqueTag res                  = default;
            var       utc                  = DateTime.UtcNow;

            int* r = stackalloc int[1];
            t_random.NextBytes(new Span<byte>((byte*)r, sizeof(int)));

            // Tested on creating 1_000_000 instances at once, no collisions
            res._a = (*r & ~0xFFFF) | ((int)s_deviceID & 0xFFFF);
            res._b = ((int)utc.Ticks & 0xFFFF) ^ (((int)(s_deviceID >> 32) ^ *r) << 16);

            res._b = (res._b & ~0b11) | (int)UniqueTagType.Default;

            return res;
        }

        public readonly UniqueTagType GetTagType() => (UniqueTagType)(_b & 0b11);

        public readonly void TryWriteBytes(Span<byte> bytes)
        {
            Assert.IsTrue(bytes.Length >= sizeof(UniqueTag), "Span must be long enough to contain unique tag");
            BitConverter.TryWriteBytes(bytes,      _a);
            BitConverter.TryWriteBytes(bytes[4..], _b);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is UniqueTag tag &&
                   this.Equals(tag);
        }

        public override readonly string ToString()
        {
            var type = GetTagType();

            switch (type)
            {
            default:
                return hexString();

            case UniqueTagType.Null:
                return "Null";

            case UniqueTagType.Text:
                if (hashMap.TryGetValue(this, out var value))
                    return value;
                else
                    return $"UNKNOWN:{hexString()}";
            }
        }

        public readonly string ToString(string format)
        {
            if(string.IsNullOrEmpty(format) || format != "X")
                return ToString();
            
            return hexString();
        }

        readonly string hexString()
        {
            return $"{_a:X8}-{_b:X8}";
        }

        public override readonly int GetHashCode()
        {
            return _a ^ ((_b & ~0b11) << 4);
        }

        public readonly int CompareTo(UniqueTag other)
        {
            int c0 = (int)((uint)_a - (uint)other._a);
            int c1 = (int)((uint)_b - (uint)other._b);

            return c0 == 0 ? c1 : c0;
        }

        public readonly bool Equals(UniqueTag other) => this == other;

        public static explicit operator UniqueTag(int    nid)  => new UniqueTag(nid);
        public static implicit operator UniqueTag(string text) => new UniqueTag(text);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UniqueTag t0, UniqueTag t1)
            => t0._a == t1._a & t0._b == t1._b; // Logical && would introduce branch, which we don't want to have

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UniqueTag t0, UniqueTag t1) => !(t0 == t1);
    }

    public enum UniqueTagType : byte
    {
        Null = 0b00,
        /// <summary>
        /// Created with <see cref="UniqueTag.New"/>
        /// </summary>
        Default = 0b01,
        /// <summary>
        /// Created with <see cref="UniqueTag.UniqueTag(int)"/>
        /// </summary>
        Int32 = 0b10,
        /// <summary>
        /// Created with <see cref="UniqueTag.UniqueTag(string)"/>
        /// </summary>
        Text = 0b11,
    }

#if UNITY_EDITOR
    internal class StringHashStorage : ScriptableObject
    {
        internal static ConcurrentDictionary<UniqueTag, string> hashMap = new();

        private void OnEnable()
        {
            if (hashMap is null) hashMap = new();

            string keysJson = UnityEditor.SessionState.GetString("SAVEHASH_KEYS", null);
            if (string.IsNullOrEmpty(keysJson))
                return;

            string valuesJson = UnityEditor.SessionState.GetString("SAVEHASH_VALUES", null);
            if (string.IsNullOrEmpty(valuesJson))
                return;

            var keys   = JsonHelper.FromJson<UniqueTag[]>(keysJson);
            var values = JsonHelper.FromJson<string[]>(valuesJson);

            if (keys is null || values is null || keys.Length == 0 || values.Length == 0)
                return;

            for (int i = 0; i < values.Length; i++)
                hashMap.TryAdd(keys[i], values[i]);
        }

        private void OnDisable()
        {
            if (hashMap is null) // I have literally 0 idea how is this possible but unity throws exceptions sometimes
                return;

            var keys   = hashMap.Keys.ToArray();
            var values = hashMap.Values.ToArray();

            var keysJson   = JsonHelper.ToJson(keys);
            var valuesJson = JsonHelper.ToJson(values);

            UnityEditor.SessionState.SetString("SAVEHASH_KEYS",   keysJson);
            UnityEditor.SessionState.SetString("SAVEHASH_VALUES", valuesJson);
        }
    }
#endif 
}