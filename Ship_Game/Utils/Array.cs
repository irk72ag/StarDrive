﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    /// <summary>
    /// The empty object pattern. Provides safe to use immutable empty objects
    /// </summary>
    public static class Empty<T>
    {
        /// <summary>This is safe to reference everywhere, because an empty array is fully immutable</summary>
        public static readonly T[] Array = new T[0];
    }

    /// <summary>
    /// This is a custom version of List, to make debugging easier
    /// and optimize for game relate performance requirements
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}  Capacity = {Capacity}")]
    [Serializable]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    public class Array<T> : IList<T>, IReadOnlyList<T>
    {
        protected static readonly int SizeOf = typeof(T).SizeOfRef();
        protected T[] Items;
        public int Count { get; protected set; }

        public bool IsReadOnly => false;
        public bool IsEmpty    => Count == 0;
        public bool NotEmpty   => Count != 0;

        public Array()
        {
            Items = Empty<T>.Array;
        }
        public Array(int capacity)
        {
            Items = new T[capacity];
        }
        /// <summary> Fastest method to copying an ArrayT.</summary>
        public Array(Array<T> list)
        {
            Items = Empty<T>.Array;
            if ((Count = list.Count) > 0)
                list.CopyTo(Items = new T[Count]);
        }
        /// <summary>Array capacity is reserved exactly, CopyTo is used so the speed will vary
        /// on the container implementation. ArrayT CopyTo uses an extremly fast internal C++ copy routine
        /// which is the best case scenario. Lists that use Array.Copy() are about 2 times slower.</summary>
        public Array(ICollection<T> collection)
        {
            Items = Empty<T>.Array;
            if ((Count = collection.Count) > 0)
                collection.CopyTo(Items = new T[Count], 0);
        }
        /// <summary>Array capacity is reserved exactly, CopyTo is used if list is ICollection and indexing operator is used for element access (kinda slow, but ok)</summary>
        public Array(IReadOnlyList<T> list)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                int count = list.Count;
                if ((Count = count) <= 0) return;
                Items = new T[count];
                if (list is ICollection<T> c)
                    c.CopyTo(Items, 0);
                else for (int i = 0; i < count; ++i)
                    Items[i] = list[i];
            }
        }
        /// <summary>Array capacity is reserved exactly, but dynamic enumeration is used if collection is not an ICollection (very slow)</summary>
        public Array(IReadOnlyCollection<T> collection)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                int count = collection.Count;
                if ((Count = count) <= 0)
                    return;
                Items = new T[count];
                if (collection is ICollection<T> c)
                    c.CopyTo(Items, 0);
                else using (IEnumerator<T> e = collection.GetEnumerator())
                    for (int i = 0; i < count && e.MoveNext(); ++i)
                        Items[i] = e.Current;
            }
        }
        /// <summary>The slowest way to construct an new ArrayT.
        /// This will check for multiple subtypes to try and optimize creation, dynamic enumeration would be too slow
        /// Going from fastest implementations to the slowest:
        /// ICollection, IReadOnlyList, IReadOnlyCollection, IEnumerable
        /// If </summary>
        public Array(IEnumerable<T> sequence)
        {
            unchecked
            {
                Items = Empty<T>.Array;
                if (sequence is ICollection<T> c) // might also call this.CopyTo(), best case scenario
                {
                    if ((Count = c.Count) > 0)
                        c.CopyTo(Items = new T[Count], 0);
                }
                else if (sequence is IReadOnlyList<T> rl)
                {
                    int count = rl.Count;
                    if ((Count = count) <= 0) return;
                    Items = new T[count];
                    for (int i = 0; i < count; ++i)
                        Items[i] = rl[i];
                }
                else if (sequence is IReadOnlyCollection<T> rc)
                {
                    int count = rc.Count;
                    if ((Count = count) <= 0) return;
                    Items = new T[count];
                    using (IEnumerator<T> e = rc.GetEnumerator())
                        for (int i = 0; i < count && e.MoveNext(); ++i)
                            Items[i] = e.Current;
                }
                else // fall back to epicly slow enumeration
                {
                    using (IEnumerator<T> e = sequence.GetEnumerator())
                        while (e.MoveNext()) Add(e.Current);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array<T> Clone() => new Array<T>(this);

        // If you KNOW what you are doing, I will allow you to access internal items for optimized looping
        // But don't blame me if you mess something up
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetInternalArrayItems() => Items;

        // Separated throw from this[] to enable MSIL inlining
        private void ThrowIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public T this[int index]
        {
            get
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        ThrowIndexOutOfBounds(index);
                    return Items[index];
                }
            }
            set
            {
                unchecked
                {
                    if ((uint)index >= (uint)Count)
                        ThrowIndexOutOfBounds(index);
                    Items[index] = value;
                }
            }
        }

        // First element in the list
        public T First => this[0];

        // Last element in the list
        public T Last  => this[Count - 1];

        // Get/Set the exact capacity of this Array<T>
        public int Capacity
        {
            get { return Items.Length; }
            set
            {
                if (value > Items.Length) // manually inlined to improve performance
                {
                    var newArray = new T[value];
                    Array.Copy(Items, 0, newArray, 0, Items.Length);
                    Items = newArray;
                }
            }
        }

        // If TRUE, Array<T> will grow by 2.0x during Add/Insert
        // else, growth is 1.5x
        // 1.5x may use less memory, but can potentially cause more reallocations
        // This should be tested to measure memory usage and GC pressure
        private const bool AgressiveGrowth = true;

        private void Grow(int capacity)
        {
            if (capacity >= 4)
            {
                capacity = AgressiveGrowth ? capacity * 2 : (capacity * 3) / 2;

                int rem = capacity & 3; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;
            }
            else capacity = 4;

            var newArray = new T[capacity];
            Array.Copy(Items, 0, newArray, 0, Items.Length);
            Items = newArray;
        }

        public void Resize(int newSize)
        {
            if (newSize > Items.Length) // grow requires realloc
            {
                int capacity = newSize;
                int rem = capacity & 3; // align capacity to a multiple of 4
                if (rem != 0) capacity += 4 - rem;

                var newArray = new T[capacity];
                Array.Copy(Items, 0, newArray, 0, Items.Length);
                Items = newArray;
            }
            else // shrink, clear the upper part of the array to avoid ref leaks
            {
                Array.Clear(Items, newSize, Items.Length - newSize);
            }
            Count = newSize;
        }

        public void Add(T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                int count    = Count;
                if (count == capacity)
                    Grow(capacity);
                Items[count] = item;
                Count = count + 1;
            }
        }

        public void AddUnique(T item)
        {
            if (!Contains(item))
                Add(item);
        }

        public void Insert(int index, T item)
        {
            unchecked
            {
                int capacity = Items.Length;
                int count    = Count;
                if (count == capacity)
                    Grow(capacity);
                if (index < count) Array.Copy(Items, index, Items, index + 1, count - index);
                Items[index] = item;
                Count = count + 1;
            }
        }

        public void Clear()
        {
            int count = Count;
            if (count == 0)
                return;
            // nulls all references/structfields to avoid GC leaks
            Array.Clear(Items, 0, count); 
            Count = 0;
        }

        public void ClearAndDispose()
        {
            int count = Count;
            if (count == 0)
                return;
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                for (int i = 0; i < count; ++i)
                    (Items[i] as IDisposable)?.Dispose();
            Array.Clear(Items, 0, count);
            Count = 0;
        }

        // This is slower than ContainsRef if T is a class
        public bool Contains(T item)
        {
            unchecked
            {
                int count = Count;
                if (count == 0)
                    return false;

                T[] items = Items;
                if (item == null)
                {
                    for (int i = 0; i < count; ++i)
                        if (items[i] == null) return true;
                    return false;
                }
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < count; ++i)
                    if (c.Equals(items[i], item)) return true;
                return false;
            }
        }

        public void CopyTo(T[] array, int arrayIndex = 0)
        {
            int count = Count;
            if (count != 0) Memory.HybridCopy(array, arrayIndex, Items, count);
        }

        public bool Remove(T item)
        {
            int i = IndexOf(item);
            if (i < 0) return false;
            RemoveAt(i);
            return true;
        }

        // does a fast removal by swapping the item with the last element in the array
        public bool RemoveSwapLast(T item)
        {
            unchecked
            {
                int i = IndexOf(item);
                if (i < 0) return false;

                int last = --Count;
                Items[i]    = Items[last];
                Items[last] = default(T);
                return true;
            }
        }

        // This is slower than IndexOfRef if T is a class
        public int IndexOf(T item)
        {
            unchecked
            {
                int count = Count;
                T[] items = Items;
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < count; ++i)
                    if (c.Equals(items[i], item)) return i;
                return -1;
            }
        }

        public void RemoveAt(int index)
        {
            unchecked
            {
                int count = Count;
                if ((uint)index >= (uint)count)
                    ThrowIndexOutOfBounds(index);
                Count = --count;
                if (index < count) Array.Copy(Items, index + 1, Items, index, count - index);
                Items[count] = default(T);
            }
        }

        // does a fast removal by swapping the item at index with the last element in the array
        // if index is outside bounds, an OOB exception will throw
        public void RemoveAtSwapLast(int index)
        {
            unchecked
            {
                if ((uint)index >= (uint)Count)
                    ThrowIndexOutOfBounds(index);
                int last = --Count;
                Items[index] = Items[last];
                Items[last]  = default(T);
            }
        }

        public T PopLast()
        {
            if (Count == 0)
                ThrowIndexOutOfBounds(0);
            T item       = Items[--Count];
            Items[Count] = default(T);
            return item;
        }

        // This is slower than RemoveDuplicateRefs if T is a class
        public int RemoveDuplicates()
        {
            int removed = 0;
            int count = Count;
            T[] items = Items;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                for (int j = count - 1; j >= 0; --j)
                {
                    if (i == j || !c.Equals(items[j], item)) continue;
                    int last = --count; // RemoveAtSwapLast():
                    Count    = last;
                    Items[j]    = Items[last];
                    Items[last] = default(T);
                    ++removed;
                }
            }
            return removed;
        }

        // A quite memory efficient filtering function to replace Where clauses
        public T[] FilterBy(Func<T, bool> predicate) => Items.FilterBy(Count, predicate);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        public IEnumerator<T> GetEnumerator()   => new Enumerator(this);

        /// <summary>Get a subslice enumerator from this ArrayT</summary>
        /// <param name="start">Start of range (inclusive)</param>
        /// <param name="end">End of range (exclusive)</param>
        public ArrayView<T> SubRange(int start, int end)
        {
            unchecked
            {
                int count = Count;
                if ((uint)start >= (uint)count) ThrowIndexOutOfBounds(start);
                if ((uint)end   >  (uint)count) ThrowIndexOutOfBounds(end);
                return new ArrayView<T>(start, end, Items);
            }
        }

        public override string ToString()
        {
            return GetType().GenericName();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private int Index;
            private readonly int Count;
            private readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(Array<T> arr)
            {
                Index = 0;
                Count = arr.Count;
                Items = arr.Items;
                Current = default(T);
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                unchecked
                {
                    if (Index >= Count)
                        return false;
                    Current = Items[Index++];
                    return true;
                }
            }
            public void Reset()
            {
                Index = 0;
            }
        }

        ////////////////// Collection Utilities ////////////////////

        public T Find(Predicate<T> match)
        {
            for (int i = 0; i < Count; i++)
            {
                T item = Items[i];
                if (match(item))
                    return item;
            }
            return default(T);
        }

        public void AddRange(ICollection<T> collection)
        {
            unchecked
            {
                int n = collection.Count;
                Capacity = Count + n;
                collection.CopyTo(Items, Count);
                Count += n;
            }
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            using (var en = enumerable.GetEnumerator())
                while (en.MoveNext()) Add(en.Current);
        }

        public void Sort()
        {
            Array.Sort(Items, 0, Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(Items, 0, Count, comparer);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            Array.Sort(Items, index, count, comparer);
        }

        internal struct Comparer : IComparer<T>
        {
            private readonly Comparison<T> Comparison;
            public Comparer(Comparison<T> comparison)
            {
                Comparison = comparison;
            }
            public int Compare(T x, T y)
            {
                return Comparison(x, y);
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            Array.Sort(Items, 0, Count, new Comparer(comparison));
        }

        // sorts the array by using the keyvalues generated by keyPredicate
        public void Sort<TKey>(Func<T, TKey> keyPredicate)
        {
            int count = Count;
            if (count <= 1)
                return;

            var items = Items;
            var keys = new TKey[count];
            for (int i = 0; i < count; ++i)
                keys[i] = keyPredicate(items[i]);

            Array.Sort(keys, items, 0, count);
        }

        public T[] SortedBy<TKey>(Func<T, TKey> keyPredicate)
        {
            int count = Count;
            if (count <= 1)
                return Empty<T>.Array;


            var items = new T[count];
            Memory.HybridCopy(items, 0, Items, count);

            var keys = new TKey[items.Length];
            for (int i = 0; i < items.Length; ++i) // items.Length allows CLR to optimize the loop
                keys[i] = keyPredicate(items[i]);

            Array.Sort(keys, items, 0, items.Length);
            return items;
        }

        public void RemoveAll(Predicate<T> match)
        {
            unchecked
            {
                int oldCount = Count;
                int newCount = 0;
                for (int i = 0; i < oldCount; ++i) // stable erasure, O(n)
                {
                    T item = Items[i];
                    if (!match(item)) // rebuild list from non-removing items
                        Items[newCount++] = item;
                }
                // throw away any remaining references
                Array.Clear(Items, newCount, oldCount - newCount);
                Count = newCount;
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
                return;

            Count -= count;
            if (index < Count)
            {
                Array.Copy(Items, index + count, Items, index, Count - index);
            }
            Array.Clear(Items, Count, count);
        }

        public int IndexOf(Predicate<T> match)
        {
            unchecked
            {
                int count = Count;
                for (int i = 0; i < count; ++i)
                    if (match(Items[i])) return i;
                return -1;
            }
        }

        public void Reverse()
        {
            unchecked
            {
                for (int i = 0, j = Count - 1; i < j; ++i, --j)
                {
                    T temp = Items[i];
                    Items[i] = Items[j];
                    Items[j] = temp;
                }
            }
        }

        public void ForEach(Action<T> action)
        {
            unchecked
            {
                int count = Count;
                for (int i = 0; i < count; ++i)
                    action(Items[i]);
            }
        }

        public T[] ToArray()
        {
            int count = Count;
            if (count == 0)
                return Empty<T>.Array;

            var arr = new T[count];
            Memory.HybridCopy(arr, 0, Items, count);
            return arr;
        }

        // So you accidentally called ToArrayList() which is an Array<T> as well, are you trying to clone the Array<T> ?
        public Array<T> ToArrayList()
        {
            throw new InvalidOperationException("You are trying to convert Array<T> to Array<T>. Are you trying to Clone() the Array<T>?");
        }
    }

    // Optimized specializations for speeding up reference based lookup
    public static class ArrayOptimizations
    {
        public static bool ContainsRef<T>(this Array<T> list, T item) where T : class
        {
            unchecked
            {
                int count = list.Count;
                if (count == 0)
                    return false;

                T[] items = list.GetInternalArrayItems();
                if (item == null)
                {
                    for (int i = 0; i < count; ++i)
                        if (items[i] == null) return true;
                    return false;
                }
                for (int i = 0; i < count; ++i)
                    if (items[i] == item) return true;
                return false;
            }
        }

        public static void AddUniqueRef<T>(this Array<T> list, T item) where T : class
        {
            if (!list.ContainsRef(item))
                list.Add(item);
        }

        public static int IndexOfRef<T>(this Array<T> list, T item) where T : class
        {
            unchecked
            {
                int count = list.Count;
                T[] items = list.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                    if (items[i] == item) return i;
                return -1;
            }
        }

        public static void RemoveRef<T>(this Array<T> list, T item) where T : class
        {
            int index = list.IndexOfRef(item);
            if (index != -1) list.RemoveAtSwapLast(index);
        }

        public static int RemoveDuplicateRefs<T>(this Array<T> list) where T : class
        {
            int removed = 0;
            int count = list.Count;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                for (int j = count - 1; j >= 0; --j)
                {
                    if (i == j || items[j] != item) continue;
                    list.RemoveAtSwapLast(j);
                    --count;
                    ++removed;
                }
            }
            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] UniqueGameObjects<T>(this Array<T> list) where T : GameplayObject
            => list.GetInternalArrayItems().UniqueGameObjects(list.Count);
    }

    internal sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> Collection;

        public CollectionDebugView(ICollection<T> collection)
        {
            Collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[Collection.Count];
                Collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    public struct ArrayView<T> : IReadOnlyList<T>
    {
        private readonly int Start;
        public           int Count { get; }
        private readonly T[] Items;
        // start (inclusive), end (exclusive)
        public ArrayView(int start, int end, T[] items)
        {
            Start = start;
            Count = end - start;
            Items = items;
        }
        public IEnumerator<T> GetEnumerator()   => new Enumerator(Start, Start + Count, Items);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(Start, Start + Count, Items);

        public T this[int index]
        {
            get
            {
                unchecked
                {
                    int idx = Start + index;
                    if ((uint)index >= (uint)Count)
                        ThrowIndexOutOfRange(idx);
                    return Items[idx];
                }
            }
        }

        private void ThrowIndexOutOfRange(int index)
        {
            throw new IndexOutOfRangeException($"Index [{index}] out of range({Count}) {ToString()}");
        }

        public struct Enumerator : IEnumerator<T>
        {
            private int Index;
            private readonly int End;
            private readonly T[] Items;
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(int start, int end, T[] arr)
            {
                Index = start;
                End   = end;
                Items = arr;
                Current = default(T);
            }
            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                unchecked
                {
                    if (Index >= End) // end index is considered invalid, since it's exclusive
                        return false;
                    Current = Items[Index++];
                    return true;
                }
            }
            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
