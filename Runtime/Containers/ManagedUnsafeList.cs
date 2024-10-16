using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Hollow
{
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
    public class ManagedUnsafeList<T> : IEnumerable<T>
    {
        public ManagedUnsafeList(int capacity = 64)
        {
            _array = new T[capacity];
        }
        
        public ManagedUnsafeList(IEnumerable<T> items)
        {
            _array = items.ToArray();
            _count = _array.Length;
        }
        
        private static readonly EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        internal int _count;
        internal T[] _array;

        public int Capacity => _array.Length;

        public int Count
        {
            get => _count;
            set => _count = value;
        }

        public ref T GetArrayRef() => ref _array[0]; // When .NET7 comes replace this with MemoryMarshal.GetArrayDataReference
                                                     // It basically allows to remove bound checks in both mono and IL2CPP

        public T GetItemWithoutChecks(int index) => _array[index];
        
        public ref T this[int index]
        {
            get
            {
#if UNITY_ASSERTIONS
                if(index < 0 || index >= _count)
                    throw new System.IndexOutOfRangeException($"Index '{index}' is out of range of collection (Length: {_count})");
#endif 
                
                return ref _array[index];
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacityForItem(1);
            _array[_count++] = item;
        }

        public int IndexOf(T item)
        {
            return System.Array.IndexOf(_array, item, 0, _count);
        }

        /// <inheritdoc cref="RemoveAtBySwapping(int)"/>
        public void RemoveBySwapping(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
                RemoveAtBySwapping(index);
        }

        /// <summary>
        /// Removes item from list very fast but doesn't preserve ordering
        /// </summary>
        public void RemoveAtBySwapping(int index)
        {
            // [8, 2, 42, 7] _count = 4
            // removing at index 3
            // will assign 7 to itself
            // then clear it because _count is now equals to index
            //
            // if removing at index 0
            // then we'll first assign 7 to [0] position getting list like this
            // [7, 2, 42, 7]
            // then we can set 7 to 0, because reference is stored there and we don't want to hold this pointer any longer
            _array[index]  = _array[--_count];
            _array[_count] = default;
        }

        public void Clear()
        {
            if (_count > 0)
            {
                Array.Clear(_array, 0, _count);
                _count = 0;
            }
        }

        void EnsureCapacityForItem(int itemCount)
        {
            if (_count + itemCount > Capacity)
                Resize(Capacity * 2);
        }

        void Resize(int newSize)
        {
            var oldArray = _array;

            _array = new T[newSize];

            Array.Copy(oldArray, _array, _count);
        }

        public T[] ToArray()
        {
            var cloned = new T[_count];
            Array.Copy(_array, cloned, _count);
            return cloned;
        }

        public struct ListEnumerator : IEnumerator<T>
        {
            public ListEnumerator(ManagedUnsafeList<T> list) : this()
            {
                _list = list;
                _index = -1;
            }

            private ManagedUnsafeList<T> _list;
            private int _index;
            
            public bool MoveNext() => ++_index < _list.Count;
            public T Current => _list[_index];
            
            object IEnumerator.Current => _list[_index];

            public void Dispose() { }
            public void Reset() { }
        }
        
        public ListEnumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ListEnumerator(this);
        }

        public void RemoveAt(int i)
        {
            Array.Copy(_array, i+1, _array, i, _count-i-1);
            _count--;
        }
    }
    
    public static class ManagedUnsafeListExt
    {
        public static Span<T> AsSpan<T>(this ManagedUnsafeList<T> list) where T : unmanaged
        {
            return new Span<T>(list._array)[0..list._count];
        }
    }
}