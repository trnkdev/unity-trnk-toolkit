using System;
using System.Collections.Generic;
using System.Text;
using Random = UnityEngine.Random;

namespace TRnK.Extensions
{
    public static class CollectionExtensions
    {
        #region Array
        /// <summary>Returns a random element from the array.</summary>
        public static T Rand<T>(this T[] arr)
        {
            if (arr.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random element from null or empty array");
            return arr[Random.Range(0, arr.Length)];
        }

        /// <summary>Returns a random index from the array.</summary>
        public static int RandIndex<T>(this T[] arr)
        {
            if (arr.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random index from null or empty array");
            return Random.Range(0, arr.Length);
        }

        /// <summary>Shuffles the elements of the array.</summary>
        public static T[] Shuffle<T>(this T[] arr)
        {
            T[] result = new T[arr.Length];
            arr.CopyTo(result, 0);

            for (int i = 0; i < result.Length; i++)
            {
                T temp = result[i];
                int randomIndex = Random.Range(i, result.Length);
                result[i] = result[randomIndex];
                result[randomIndex] = temp;
            }

            return result;
        }

        /// <summary>Swaps two elements in the array by index, in place.</summary>
        public static void SwapAt<T>(this T[] arr, int aIndex, int bIndex)
        {
            if (aIndex < 0 || aIndex >= arr.Length || bIndex < 0 || bIndex >= arr.Length)
                throw new IndexOutOfRangeException($"Index {aIndex} or {bIndex} is out of bounds of array of length {arr.Length}");

            if (aIndex == bIndex) return;

            (arr[bIndex], arr[aIndex]) = (arr[aIndex], arr[bIndex]);
        }

        /// <summary>Swaps two elements in the array by value, in place.</summary>
        public static void Swap<T>(this T[] arr, T a, T b)
        {
            var eq = EqualityComparer<T>.Default;
            if (eq.Equals(a, default) && a is null) throw new ArgumentNullException(nameof(a));
            if (eq.Equals(b, default) && b is null) throw new ArgumentNullException(nameof(b));

            int aIndex = Array.IndexOf(arr, a);
            int bIndex = Array.IndexOf(arr, b);

            if (aIndex == -1)
                throw new ArgumentException($"Element '{a}' not found in array", nameof(a));
            if (bIndex == -1)
                throw new ArgumentException($"Element '{b}' not found in array", nameof(b));

            arr.SwapAt(aIndex, bIndex);
        }

        /// <summary>Checks if the array contains any null elements.</summary>
        public static bool ContainsNull<T>(this T[] arr) where T : class
        {
            if (arr.IsNullOrEmpty()) return false;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) return true;
            }
            return false;
        }

        /// <summary>Checks if the array is null or empty.</summary>
        public static bool IsNullOrEmpty<T>(this T[] arr)
        {
            return arr == null || arr.Length == 0;
        }

        /// <summary>Returns a flat "[a, b, c]" representation for debug logging (not nested-aware).</summary>
        public static string ToLiteral<T>(this T[] arr)
        {
            return arr == null ? "null" : JoinLiteral(arr, "[", "]");
        }

        /// <summary>Gets a sub-array from the specified start index to the end.</summary>
        public static T[] Slice<T>(this T[] arr, int startIndex)
        {
            return arr.Slice(startIndex, arr.Length - startIndex);
        }

        /// <summary>Gets a sub-array from the specified start index with the specified length.</summary>
        public static T[] Slice<T>(this T[] arr, int startIndex, int length)
        {
            if (arr.IsNullOrEmpty())
                return new T[0];
            if (startIndex < 0 || startIndex > arr.Length)
                throw new IndexOutOfRangeException($"Start index {startIndex} is out of bounds of array of length {arr.Length}");
            if (length < 0 || startIndex + length > arr.Length)
                throw new ArgumentException($"Length {length} would exceed array bounds from start index {startIndex}");

            if (length == 0)
                return new T[0];

            T[] result = new T[length];
            Array.Copy(arr, startIndex, result, 0, length);
            return result;
        }

        /// <summary>Checks if the array contains the specified element.</summary>
        public static bool Contains<T>(this T[] arr, T item)
        {
            return Array.IndexOf(arr, item) != -1;
        }

        /// <summary>Reverses the array and returns a new array.</summary>
        public static T[] Reverse<T>(this T[] arr)
        {
            if (arr.IsNullOrEmpty())
                return arr?.Clone() as T[] ?? new T[0];

            T[] result = arr.Clone() as T[];
            Array.Reverse(result);
            return result;
        }

        /// <summary>Returns multiple random elements from the array without replacement.</summary>
        public static T[] RandMultiple<T>(this T[] arr, int count)
        {
            if (arr.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random elements from null or empty array");
            if (count < 0)
                throw new ArgumentException("Count cannot be negative");
            if (count > arr.Length)
                throw new ArgumentException($"Cannot get {count} elements from array of length {arr.Length}");

            if (count == 0)
                return new T[0];

            var shuffled = arr.Shuffle();
            return shuffled.Slice(0, count);
        }

        /// <summary>Returns a random element from the collection using weighted selection.</summary>
        public static T RandWeighted<T>(this T[] arr, Func<T, float> weightSelector)
        {
            if (arr.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random element from null or empty array");
            return PickWeighted(arr, weightSelector);
        }

        /// <summary>Checks if the 2D grid is null or has zero total elements.</summary>
        public static bool IsNullOrEmpty<T>(this T[,] grid)
        {
            return grid == null || grid.Length == 0;
        }

        /// <summary>Returns a row-major string representation of a 2D grid for debug logging.</summary>
        public static string ToLiteral<T>(this T[,] grid)
        {
            if (grid == null)
                return "null";

            int height = grid.GetLength(0);
            int width = grid.GetLength(1);
            var sb = new StringBuilder();
            sb.Append("{");
            for (int y = 0; y < height; y++)
            {
                if (y > 0) sb.Append(", ");
                sb.Append(y);
                sb.Append(": [");
                for (int x = 0; x < width; x++)
                {
                    if (x > 0) sb.Append(", ");
                    sb.Append(Literal(grid[y, x]));
                }
                sb.Append("]");
            }
            sb.Append("}");
            return sb.ToString();
        }
        #endregion

        #region List
        /// <summary>Returns a random element from the list.</summary>
        public static T Rand<T>(this List<T> list)
        {
            if (list.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random element from null or empty list");
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>Returns a random index from the list.</summary>
        public static int RandIndex<T>(this List<T> list)
        {
            if (list.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random index from null or empty list");
            return Random.Range(0, list.Count);
        }

        /// <summary>Shuffles the elements of the list.</summary>
        public static List<T> Shuffle<T>(this List<T> list)
        {
            List<T> result = new(list);
            for (int i = 0; i < result.Count; i++)
            {
                T temp = result[i];
                int randomIndex = Random.Range(i, result.Count);
                result[i] = result[randomIndex];
                result[randomIndex] = temp;
            }
            return result;
        }

        /// <summary>Swaps two elements in the list by index, in place.</summary>
        public static void SwapAt<T>(this List<T> list, int aIndex, int bIndex)
        {
            if (aIndex < 0 || aIndex >= list.Count || bIndex < 0 || bIndex >= list.Count)
                throw new IndexOutOfRangeException($"Index {aIndex} or {bIndex} is out of bounds of list of length {list.Count}");

            if (aIndex == bIndex) return;

            (list[bIndex], list[aIndex]) = (list[aIndex], list[bIndex]);
        }

        /// <summary>Swaps two elements in the list by value, in place.</summary>
        public static void Swap<T>(this List<T> list, T a, T b)
        {
            var eq = EqualityComparer<T>.Default;
            if (eq.Equals(a, default) && a is null) throw new ArgumentNullException(nameof(a));
            if (eq.Equals(b, default) && b is null) throw new ArgumentNullException(nameof(b));

            int aIndex = list.IndexOf(a);
            int bIndex = list.IndexOf(b);

            if (aIndex == -1)
                throw new ArgumentException($"Element '{a}' not found in list", nameof(a));
            if (bIndex == -1)
                throw new ArgumentException($"Element '{b}' not found in list", nameof(b));

            list.SwapAt(aIndex, bIndex);
        }

        /// <summary>Checks if the list contains any null elements.</summary>
        public static bool ContainsNull<T>(this List<T> list) where T : class
        {
            if (list.IsNullOrEmpty()) return false;

            foreach (var item in list)
            {
                if (item == null) return true;
            }
            return false;
        }

        /// <summary>Checks if the list is null or empty.</summary>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>Checks if the read-only list is null or empty.</summary>
        public static bool IsNullOrEmpty<T>(this IReadOnlyList<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>Checks if the collection is null or empty.</summary>
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }


        /// <summary>Returns a flat "[a, b, c]" representation for debug logging (not nested-aware).</summary>
        public static string ToLiteral<T>(this List<T> list)
        {
            return list == null ? "null" : JoinLiteral(list, "[", "]");
        }

        /// <summary>Returns multiple random elements from the list without replacement.</summary>
        public static List<T> RandMultiple<T>(this List<T> list, int count)
        {
            if (list.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random elements from null or empty list");
            if (count < 0)
                throw new ArgumentException("Count cannot be negative");
            if (count > list.Count)
                throw new ArgumentException($"Cannot get {count} elements from list of length {list.Count}");

            if (count == 0)
                return new List<T>();

            var shuffled = list.Shuffle();
            return shuffled.GetRange(0, count);
        }

        /// <summary>Returns a random element from the list using weighted selection.</summary>
        public static T RandWeighted<T>(this List<T> list, Func<T, float> weightSelector)
        {
            if (list.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random element from null or empty list");
            return PickWeighted(list, weightSelector);
        }
        #endregion

        #region Dictionary
        /// <summary>Returns a random value from a dictionary.</summary>
        public static V RandV<K, V>(this Dictionary<K, V> dict) where K : notnull
        {
            if (dict.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random value from null or empty dictionary");
            int target = Random.Range(0, dict.Count);
            int i = 0;
            foreach (var v in dict.Values) { if (i++ == target) return v; }
            return default;
        }

        /// <summary>Returns a random key from a dictionary.</summary>
        public static K RandK<K, V>(this Dictionary<K, V> dict) where K : notnull
        {
            if (dict.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot get random key from null or empty dictionary");
            int target = Random.Range(0, dict.Count);
            int i = 0;
            foreach (var k in dict.Keys) { if (i++ == target) return k; }
            return default;
        }

        /// <summary>Checks if the dictionary contains any null values.</summary>
        public static bool ContainsNullValues<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TValue : class
        {
            if (dictionary.IsNullOrEmpty()) return false;

            foreach (var kvp in dictionary)
            {
                if (kvp.Value == null) return true;
            }
            return false;
        }

        /// <summary>Returns true if the dictionary is null or empty.</summary>
        public static bool IsNullOrEmpty<K, V>(this Dictionary<K, V> dict) where K : notnull
        {
            return dict == null || dict.Count == 0;
        }

        /// <summary>Returns a flat "{k: v, ...}" representation for debug logging (not nested-aware).</summary>
        public static string ToLiteral<K, V>(this Dictionary<K, V> dict) where K : notnull
        {
            if (dict == null)
                return "null";

            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(", ");
                sb.Append(Literal(kvp.Key)).Append(": ").Append(Literal(kvp.Value));
                first = false;
            }
            return sb.Append("}").ToString();
        }
        #endregion

        /// <summary>Returns a flat FIFO-order "[a, b, c]" representation for debug logging.</summary>
        public static string ToLiteral<T>(this Queue<T> queue)
        {
            return queue == null ? "null" : JoinLiteral(queue, "[", "]");
        }

        /// <summary>Returns a flat LIFO-order "[a, b, c]" representation for debug logging.</summary>
        public static string ToLiteral<T>(this Stack<T> stack)
        {
            return stack == null ? "null" : JoinLiteral(stack, "[", "]");
        }

        /// <summary>Returns a flat "{a, b, c}" representation for debug logging.</summary>
        public static string ToLiteral<T>(this HashSet<T> set)
        {
            return set == null ? "null" : JoinLiteral(set, "{", "}");
        }

        /// <summary>Picks an item with probability proportional to its weight; zero-weight items are never picked.</summary>
        private static T PickWeighted<T>(IReadOnlyList<T> items, Func<T, float> weightSelector)
        {
            float totalWeight = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                float weight = weightSelector(items[i]);
                if (weight < 0f)
                    throw new ArgumentException($"Weight at index {i} is negative: {weight}");
                totalWeight += weight;
            }
            if (totalWeight <= 0f)
                throw new InvalidOperationException("Total weight must be greater than 0");

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            int lastWeighted = 0;
            for (int i = 0; i < items.Count; i++)
            {
                float weight = weightSelector(items[i]);
                if (weight <= 0f) continue;
                cumulative += weight;
                lastWeighted = i;
                if (roll < cumulative) return items[i];
            }

            // Random.Range includes totalWeight itself, which no item's range covers.
            return items[lastWeighted];
        }

        private static string JoinLiteral<T>(IEnumerable<T> items, string open, string close)
        {
            var sb = new StringBuilder(open);
            bool first = true;
            foreach (T item in items)
            {
                if (!first) sb.Append(", ");
                sb.Append(Literal(item));
                first = false;
            }
            return sb.Append(close).ToString();
        }

        private static string Literal<T>(T value)
        {
            if (value is null) return "null";
            if (value is string text) return $"'{text}'";
            return value.ToString();
        }
    }
}

