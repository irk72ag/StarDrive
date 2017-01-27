﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
    public static class CollectionExtensions
    {
        public static TValue ConsumeValue<TKey,TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.TryGetValue(key, out TValue value)) return default(TValue);
            dict[key] = default(TValue);
            return value;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item) where T : class
        {
            if (list is IList<T> ilist)
                return ilist.IndexOf(item);

            for (int i = 0, n = list.Count; i < n; ++i)
                if (item == list[i])
                    return i;
            return -1;
        }


        // Return the element with the greatest selector value, or null if empty
        public static T FindMax<T>(this T[] items, int count, Func<T, float> selector) where T : class
        {
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this T[] items, Func<T, float> selector) where T : class
            => items.FindMax(items.Length, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMax<T>(this Array<T> list, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMax(list.Count, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMax<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
            => (elem = FindMax(list, selector)) != null;


        public static T FindMaxFiltered<T>(this T[] items, int count, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            float max = float.MinValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                if (!filter(item)) continue;
                float value = selector(item);
                if (value <= max) continue;
                max   = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this T[] items, Predicate<T> filter, Func<T, float> selector) where T : class
            => items.FindMaxFiltered(items.Length, filter, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMaxFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMaxFiltered(list.Count, filter, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMaxFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
            => (elem = FindMaxFiltered(list, filter, selector)) != null;


        // Return the element with the smallest selector value, or null if empty
        public static T FindMin<T>(this T[] items, int count, Func<T, float> selector) where T : class
        {
            T found = null;
            float min = float.MaxValue;
            for (int i = 0; i < count; ++i)
            {
                T item = items[i];
                float value = selector(item);
                if (value > min) continue;
                min = value;
                found = item;
            }
            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this T[] items, Func<T, float> selector) where T : class
            => items.FindMin(items.Length, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindMin<T>(this Array<T> list, Func<T, float> selector) where T : class
            => list.GetInternalArrayItems().FindMin(list.Count, selector);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMin<T>(this Array<T> list, out T elem, Func<T, float> selector) where T : class
            => (elem = FindMin(list, selector)) != null;


        public static T FindMinFiltered<T>(this Array<T> list, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            T found = null;
            int n = list.Count;
            float min = float.MaxValue;
            T[] items = list.GetInternalArrayItems();
            for (int i = 0; i < n; ++i)
            {
                T item = items[i];
                if (!filter(item)) continue;     
                
                float value = selector(item);
                if (value > min) continue;
                min   = value;
                found = item;
            }
            return found;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FindMinFiltered<T>(this Array<T> list, out T elem, Predicate<T> filter, Func<T, float> selector) where T : class
        {
            return (elem = FindMinFiltered(list, filter, selector)) != null;
        }

        public static bool Any<T>(this Array<T> list, Predicate<T> match)
        {
            int n = list.Count;
            for (int i = 0; i < n; ++i)
                if (match(list[i]))
                    return true;
            return false;
        }

        public static int Count<T>(this Array<T> list, Predicate<T> match)
        {
            int count = 0;
            int n = list.Count;
            for (int i = 0; i < n; ++i)
                if (match(list[i]))
                    ++count;
            return count;
        }

        // warning, this is O(n*m), worst case O(n^2)
        public static bool ContainsAny<T>(this T[] arr1, T[] arr2)
        {
            var c = EqualityComparer<T>.Default;
            for (int i = 0; i < arr1.Length; ++i)
            {
                for (int j = 0; j < arr2.Length; ++j)
                {
                    if (c.Equals(arr1[i], arr2[j]))
                        return true;
                }
            }
            return false;
        }

        // The following methods are all specific implementations
        // of ToArray() and ToList() as ToArrayList(); Main goal is to improve performance
        // compared to generic .NET ToList() which doesn't reserve capacity etc.
        // ToArrayList() will return an Array<T> as opposed to .NET ToList() which returns List<T>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this ICollection<T> source) => new Array<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this IReadOnlyList<T> source) => new Array<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this IReadOnlyCollection<T> source) => new Array<T>(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array<T> ToArrayList<T>(this IEnumerable<T> source) => new Array<T>(source);

        public static T[] ToArray<T>(this ICollection<T> source)
        {
            int count = source.Count;
            if (count == 0) return Empty<T>.Array;
            var items = new T[count];
            source.CopyTo(items, 0);
            return items;
        }

        public static T[] ToArray<T>(this IReadOnlyList<T> source)
        {
            int count = source.Count;
            if (count == 0) return Empty<T>.Array;
            var items = new T[count];
            if (source is ICollection<T> c)
                c.CopyTo(items, 0);
            else for (int i = 0; i < count; ++i)
                items[i] = source[i];
            return items;
        }

        public static T[] ToArray<T>(this IReadOnlyCollection<T> source)
        {
            unchecked
            {
                int count = source.Count;
                if (count == 0) return Empty<T>.Array;
                var items = new T[count];
                if (source is ICollection<T> c)
                    c.CopyTo(items, 0);
                else using (var e = source.GetEnumerator())
                    for (int i = 0; i < count && e.MoveNext(); ++i)
                        items[i] = e.Current;
                return items;
            }
        }

        public static T[] ToArray<T>(this IEnumerable<T> source)
        {
            if (source is ICollection<T> c)          return c.ToArray();
            if (source is IReadOnlyList<T> rl)       return rl.ToArray();
            if (source is IReadOnlyCollection<T> rc) return rc.ToArray();

            // fall back to epicly slow enumeration
            T[] items = Empty<T>.Array;
            int count = 0;
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (items.Length == count)
                    {
                        int len = count == 0 ? 4 : count * 2; // aggressive growth
                        Array.Resize(ref items, len);
                    }
                    items[count++] = e.Current;
                }
            }
            if (items.Length != count)
                Array.Resize(ref items, count);
            return items;
        }
    }
}
