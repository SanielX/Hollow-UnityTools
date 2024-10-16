using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Hollow
{
    /// <summary>
    /// Totally identical to <see cref="System.Guid"/> except unity can serialize it. 
    /// You can use SGuid.AsSystemGuid to cast value to system guid 
    /// </summary>
    [System.Serializable]
    public unsafe struct SGuid : IEquatable<SGuid>,  IEquatable<Guid>, IComparable, 
                                 IComparable<SGuid>, IComparable<Guid>, IFormattable
    {
        public SGuid(Guid guid)
        {
            ref SGuid sguid = ref UnsafeUtility.As<Guid, SGuid>(ref guid);

            _a = sguid._a;
            _b = sguid._b;
            _c = sguid._c;
            _d = sguid._d;
            _e = sguid._e;
            _f = sguid._f;
            _g = sguid._g;
            _h = sguid._h;
            _i = sguid._i;
            _j = sguid._j;
            _k = sguid._k;
        }

        public static readonly SGuid Null = System.Guid.Empty;

        // Implemented it this way because it allows to not worry about byte array difference
        // Also makes type totally no-gc and unmanaged, don't have to worry about ISerializedCallbackReciever being called or not. Nice stuff
        // 16 bytes in total
        [SerializeField] internal int   _a;
        [SerializeField] internal short _b;
        [SerializeField] internal short _c;
        [SerializeField] internal byte  _d;
        [SerializeField] internal byte  _e;
        [SerializeField] internal byte  _f;
        [SerializeField] internal byte  _g;
        [SerializeField] internal byte  _h;
        [SerializeField] internal byte  _i;
        [SerializeField] internal byte  _j;
        [SerializeField] internal byte  _k;

        public Guid SystemGuid => AsSystemGuid(ref this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Guid AsSystemGuid(ref SGuid sguid) => ref UnsafeUtility.As<SGuid, Guid>(ref sguid);

        public override int GetHashCode()
        {
            return AsSystemGuid(ref this).GetHashCode();
        }

        public override string ToString()
        {
            return AsSystemGuid(ref this).ToString();
        }

        public string ToString(string format) => AsSystemGuid(ref this).ToString(format);

        public override bool Equals(object obj)
        {
            if (obj is SGuid sguid)
                return this.Equals(sguid);

            if (obj is Guid guid)
                return AsSystemGuid(ref this).Equals(guid);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Guid other)
        {
            return AsSystemGuid(ref this).Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(SGuid other)
        {
            return this == other;
        }

        public int CompareTo(object obj)
        {
            if (obj is Guid guid)
                return this.CompareTo(guid);

            if (obj is SGuid sguid)
                return this.CompareTo(sguid.SystemGuid);

            return 1;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return AsSystemGuid(ref this).ToString(format, formatProvider);
        }

        public byte[] ToByteArray() => AsSystemGuid(ref this).ToByteArray();

        public bool IsNull() => this == Null;

        // Reference source code of System.Guid is SO BAD here like REALLY BAD
        // It casts every value us uint and compares it one by one
        // It's basically the same as using memcmp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(SGuid otherValue)
        {
            void* self  = UnsafeUtility.AddressOf(ref this);
            void* other = UnsafeUtility.AddressOf(ref otherValue);

            return UnsafeUtility.MemCmp(self, other, sizeof(SGuid));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Guid otherValue)
        {
            void* self  = UnsafeUtility.AddressOf(ref this);
            void* other = UnsafeUtility.AddressOf(ref otherValue);

            return UnsafeUtility.MemCmp(self, other, sizeof(SGuid));
        }

        public static implicit operator SGuid(Guid guid) => new SGuid(guid);
        public static implicit operator Guid(SGuid sguid) => AsSystemGuid(ref sguid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SGuid a, SGuid b)
        {
            return UnsafeUtility.MemCmp(&a, &b, sizeof(SGuid)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(SGuid a, SGuid b) => !(a == b);
    }
}
