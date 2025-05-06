using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.Extensions;

//namespace NECS.Extensions
//{
public class Collections
{
    public static readonly object[] EmptyArray = new object[0];

    public static List<T> AsList<T>(params T[] values) =>
        new List<T>(values);

    //public static IList<T> EmptyList<T>() =>
    //    EmptyList<T>.Instance;

    public static void ForEach<T>(IEnumerable<T> coll, Action<T> action)
    {
        Enumerator<T> enumerator = GetEnumerator<T>(coll);
        while (enumerator.MoveNext())
        {
            action(enumerator.Current);
        }
    }

    public static IEnumerable<TSource> IntersectEnum<TSource>(HashSet<TSource> first, IEnumerable<TSource> second)
    {
        foreach (TSource element in first)
        {
            if (second.Contains(element)) yield return element;
        }
    }

    public static bool FirstIntersect<TSource, TNull>(ConcurrentDictionaryEx<TSource, TNull> first, IEnumerable<TSource> second)
    {
        foreach (KeyValuePair<TSource, TNull> element in first)
        {
            if (second.Contains(element.Key)) return true;
        }
        return false;
    }

    public static IEnumerable<TSource> IntersectEnum<TSource, TNull>(ConcurrentDictionaryEx<TSource, TNull> first, IEnumerable<TSource> second)
    {
        foreach (KeyValuePair<TSource, TNull> element in first)
        {
            if (second.Contains(element.Key)) yield return element.Key;
        }
    }

    public static bool FirstIntersect<TSource, TNull>(IDictionary<TSource, TNull> first, IEnumerable<TSource> second)
    {
        foreach (KeyValuePair<TSource, TNull> element in first)
        {
            if (second.Contains(element.Key)) return true;
        }
        return false;
    }

    public static IEnumerable<TSource> IntersectEnum<TSource, TNull>(IDictionary<TSource, TNull> first, IEnumerable<TSource> second)
    {
        foreach (KeyValuePair<TSource, TNull> element in first)
        {
            if (second.Contains(element.Key)) yield return element.Key;
        }
    }

    public static bool FirstIntersect<TSource>(HashSet<TSource> first, IEnumerable<TSource> second)
    {
        foreach (TSource element in first)
        {
            if (second.Contains(element)) return true;
        }
        return false;
    }

    public static Enumerator<T> GetEnumerator<T>(IEnumerable<T> collection) =>
        new Enumerator<T>(collection);

    public static T GetOnlyElement<T>(ICollection<T> coll)
    {
        if (coll.Count != 1)
        {
            throw new InvalidOperationException("Count: " + coll.Count);
        }
        List<T> list = coll as List<T>;
        if (list != null)
        {
            return list[0];
        }
        HashSet<T> set = coll as HashSet<T>;
        if (set != null)
        {
            HashSet<T>.Enumerator enumerator = set.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
        IEnumerator<T> enumerator2 = coll.GetEnumerator();
        enumerator2.MoveNext();
        return enumerator2.Current;
    }

    //public static IList<T> SingletonList<T>(T value) =>
    //    new SingletonList<T>(value);


    public struct Enumerator<T>
    {
        private IEnumerable<T> collection;
        private HashSet<T>.Enumerator hashSetEnumerator;
        private List<T>.Enumerator ListEnumerator;
        private IEnumerator<T> enumerator;
        public Enumerator(IEnumerable<T> collection)
        {
            this.collection = collection;
            this.enumerator = null;
            List<T> list = collection as List<T>;
            if (list != null)
            {
                this.ListEnumerator = list.GetEnumerator();
                HashSet<T>.Enumerator enumerator = new HashSet<T>.Enumerator();
                this.hashSetEnumerator = enumerator;
            }
            else
            {
                HashSet<T> set = collection as HashSet<T>;
                if (set != null)
                {
                    this.hashSetEnumerator = set.GetEnumerator();
                    List<T>.Enumerator enumerator2 = new List<T>.Enumerator();
                    this.ListEnumerator = enumerator2;
                }
                else
                {
                    HashSet<T>.Enumerator enumerator3 = new HashSet<T>.Enumerator();
                    this.hashSetEnumerator = enumerator3;
                    List<T>.Enumerator enumerator4 = new List<T>.Enumerator();
                    this.ListEnumerator = enumerator4;
                    this.enumerator = collection.GetEnumerator();
                }
            }
        }

        public bool MoveNext() =>
            !(this.collection is List<T>) ? (!(this.collection is HashSet<T>) ? this.enumerator.MoveNext() : this.hashSetEnumerator.MoveNext()) : this.ListEnumerator.MoveNext();

        public T Current =>
            !(this.collection is List<T>) ? (!(this.collection is HashSet<T>) ? this.enumerator.Current : this.hashSetEnumerator.Current) : this.ListEnumerator.Current;
    }
}
public static class EnumerableExtension
{
    public static string ToStringListing<T>(this IEnumerable<T> source, string delimiter = ", ")
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (delimiter == null)
            throw new ArgumentNullException(nameof(delimiter));

        return string.Join(delimiter, source.Select(x => x?.ToString() ?? "null"));
    }
    private static readonly Random rand1 = new Random();
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count = -1)
    {
        if (count == -1)
        {
            count = rand1.Next(1, source.Count());
        }
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");

        var list = source.ToList();
        int availableCount = list.Count;
        if (availableCount == 0)
            return Enumerable.Empty<T>();

        int actualCount = Math.Min(count, availableCount);
        return list.OrderBy(_ => rand1.Next()).Take(actualCount);
    }
    public static void ForEach<TKey>(this IEnumerable<TKey> enumerable, Action<TKey> compute)
    {
        Collections.ForEach<TKey>(enumerable, compute);
    }

    public static void ForEach<TKey>(this IList<TKey> list, Action<TKey> compute)
    {
        if (list == null)
            return;
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            compute(list[i]);
        }
    }

    public static void ForEachWithIndex<TKey>(this IEnumerable<TKey> list, Action<int> compute)
    {
        if (list == null)
            return;
        var rlist = list.ToList();
        int count = rlist.Count;
        for (int i = 0; i < count; i++)
        {
            compute(i);
        }
    }

    public static void ForEachWithIndex<TKey>(this IEnumerable<TKey> list, Action<TKey, int> compute)
    {
        if (list == null)
            return;
        var rlist = list.ToList();
        int count = rlist.Count;
        for (int i = 0; i < count; i++)
        {
            compute(rlist[i], i);
        }
    }

    public static T Fill<T>(this T fillObject, System.Collections.IEnumerable fillInput, Action<T, object> fillAction) where T : System.Collections.IEnumerable
    {
        foreach (var fillObj in fillInput)
            fillAction(fillObject, fillObj);
        return fillObject;
    }

    public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
    {
        if (dictionary.TryGetValue(key, out value))
            return dictionary.Remove(key);
        return false;
    }

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.TryGetValue(key, out _))
            return false;
        else
        {
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) return;
        ArrayTraverse walker = new ArrayTraverse(array);
        do action(array, walker.Position);
        while (walker.Step());
    }
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}

public class ArrayTraverse
{
    public int[] Position;
    private int[] maxLengths;

    public ArrayTraverse(Array array)
    {
        maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            maxLengths[i] = array.GetLength(i) - 1;
        }
        Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < Position.Length; ++i)
        {
            if (Position[i] < maxLengths[i])
            {
                Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    Position[j] = 0;
                }
                return true;
            }
        }
        return false;
    }
}
//}
