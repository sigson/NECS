using NECS.ECS.ECSCore;
using NECS.Harness.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NECS.Extensions
{

    public static class InterlockedCollection
    {
        //private static HashSet <object> lockDB = new HashSet <object> ();
        #region dictionary
        public static bool AddI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, object externalLockerObject = null)
        {
            if(externalLockerObject == null)
            {
                if(value is IECSObject)
                {
                    externalLockerObject = (value as IECSObject).SerialLocker;
                }
            }
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    if (!dictionary.ContainsKey(key))
                        dictionary[key] = value;
                    else
                        return false;
                }
            }
            return true;
        }

        public static TValue GetI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    if (dictionary.ContainsKey(key))
                        return dictionary[key];
                    else
                        return default(TValue);
                }
            }
        }

        public static bool TryGetValueI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    if (dictionary.ContainsKey(key))
                        value = dictionary[key];
                    else
                    {
                        value = default(TValue);
                        return false;
                    }
                }
            }
            return true;
        }

        public static void SetI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, object externalLockerObject = null)
        {
            if (externalLockerObject == null)
            {
                if (value is IECSObject)
                {
                    externalLockerObject = (value as IECSObject).SerialLocker;
                }
            }
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    dictionary[key]=value;
                }
            }
        }

        public static bool RemoveI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    return dictionary.Remove(key);
                }
            }
        }

        public static bool ClearI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    dictionary.Clear();
                }
            }
            return true;
        }

        public static IDictionary<TKey, TValue> SnapshotI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (dictionary)
                {
                    return new Dictionary<TKey, TValue>(dictionary);
                }
            }
        }

        public static bool ContainsKeyI<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, object externalLockerObject)
        {
            lock(externalLockerObject)
            {
                lock (dictionary)
                {
                    return dictionary.ContainsKey(key);
                }
            }
        }
        #endregion

        #region list
        public static void AddI<TValue>(this ICollection<TValue> list, TValue value, object externalLockerObject = null)
        {
            if (externalLockerObject == null)
            {
                if (value is IECSObject)
                {
                    externalLockerObject = (value as IECSObject).SerialLocker;
                }
            }
            lock (externalLockerObject)
            {
                lock (list)
                {
                    list.Add(value);
                }
            }
        }

        public static void ClearI<TValue>(this ICollection<TValue> list, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    list.Clear();
                }
            }
        }

        public static List<TValue> SnapshotI<TValue>(this ICollection<TValue> list, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    return new List<TValue>(list);
                }
            }
        }

        public static bool RemoveI<TValue>(this ICollection<TValue> list, TValue value, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    return list.Remove(value);
                }
            }
        }

        public static void InsertI<TValue>(this IList<TValue> list, int index, TValue insValue, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    list.Insert(index, insValue);
                }
            }
        }

        public static void RemoveAtI<TValue>(this IList<TValue> list, int index, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    list.RemoveAt(index);
                }
            }
        }

        public static bool ContainsI<TValue>(this ICollection<TValue> list, TValue value, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    return list.Contains( value);
                }
            }
        }

        public static void SetI<TValue>(this IList<TValue> list, int index, TValue newValue, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    list[index] = newValue;
                }
            }
        }


        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {

            if (collection is List<T> list)
            {

                list.AddRange(items);

            }
            else
            {

                foreach (T item in items)
                    collection.Add(item);

            }

        }
        public static void SetI<TValue>(this ICollection<TValue> list, int index, TValue newValue, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    if (index < 0 || index > list.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");

                    if (list is IList<TValue> ilist)
                    {
                        ilist.Insert(index, newValue);
                    }
                    else
                    {
                        List<TValue> temp = new List<TValue>(list);

                        list.Clear();

                        list.AddRange(temp.Take(index));
                        list.Add(newValue);
                        list.AddRange(temp.Skip(index));
                    }
                }
            }
        }

        public static TValue GetI<TValue>(this IList<TValue> list, int index, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    return list[index];
                }
            }
        }

        public static TValue GetI<TValue>(this ICollection<TValue> list, int index, object externalLockerObject)
        {
            lock (externalLockerObject)
            {
                lock (list)
                {
                    return list.ElementAt(index);
                }
            }
        }
        #endregion
    }

    public class TupleList<T1, T2> : List<Tuple<T1, T2>> where T1 : IComparable
    {
        public void Add(T1 item, T2 item2)
        {
            Add(new Tuple<T1, T2>(item, item2));
        }

        public new void Sort()
        {
            Comparison<Tuple<T1, T2>> c = (a, b) => a.Item1.CompareTo(b.Item1);
            base.Sort(c);
        }
        public void ReverseSort()
        {
            Comparison<Tuple<T1, T2>> c = (a, b) => b.Item1.CompareTo(a.Item1);
            base.Sort(c);
        }

    }
    public class DescComparer<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            if (x == null) return -1;
            if (y == null) return 1;
            return Comparer<T>.Default.Compare(y, x);
        }
    }

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

    public class SynchronizedList<T> : IList<T>, IList
    {
        List<T> items;
        object sync;

        public SynchronizedList()
        {
            this.items = new List<T>();
            this.sync = new Object();
        }

        public SynchronizedList(object syncRoot)
        {
            if (syncRoot == null)
                throw (new ArgumentNullException("syncRoot"));

            this.items = new List<T>();
            this.sync = syncRoot;
        }

        public SynchronizedList(object syncRoot, IEnumerable<T> list)
        {
            if (syncRoot == null)
                throw (new ArgumentNullException("syncRoot"));
            if (list == null)
                throw (new ArgumentNullException("list"));

            this.items = new List<T>(list);
            this.sync = syncRoot;
        }

        public SynchronizedList(object syncRoot, params T[] list)
        {
            if (syncRoot == null)
                throw new ArgumentNullException("syncRoot");
            if (list == null)
                throw (new ArgumentNullException("list"));

            this.items = new List<T>(list.Length);
            for (int i = 0; i < list.Length; i++)
                this.items.Add(list[i]);

            this.sync = syncRoot;
        }

        public int Count
        {
            get { lock (this.sync) { return this.items.Count; } }
        }

        protected List<T> Items
        {
            get { return this.items; }
        }

        public object SyncRoot
        {
            get { return this.sync; }
        }

        public T this[int index]
        {
            get
            {
                lock (this.sync)
                {
                    return this.items[index];
                }
            }
            set
            {
                lock (this.sync)
                {
                    if (index < 0 || index >= this.items.Count)
                        throw (new ArgumentOutOfRangeException("index", index, "Out of range"));

                    this.SetItem(index, value);
                }
            }
        }

        public void Add(T item)
        {
            lock (this.sync)
            {
                int index = this.items.Count;
                this.InsertItem(index, item);
            }
        }

        public void Clear()
        {
            lock (this.sync)
            {
                this.ClearItems();
            }
        }

        public void CopyTo(T[] array, int index)
        {
            lock (this.sync)
            {
                this.items.CopyTo(array, index);
            }
        }

        public bool Contains(T item)
        {
            lock (this.sync)
            {
                return this.items.Contains(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (this.sync)
            {
                return new List<T>(this.items).GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (this.sync)
            {
                return this.InternalIndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (this.sync)
            {
                if (index < 0 || index > this.items.Count)
                    throw (new ArgumentOutOfRangeException("index", index,"Insert error range"));

                this.InsertItem(index, item);
            }
        }

        int InternalIndexOf(T item)
        {
            int count = items.Count;

            for (int i = 0; i < count; i++)
            {
                if (object.Equals(items[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool Remove(T item)
        {
            lock (this.sync)
            {
                int index = this.InternalIndexOf(item);
                if (index < 0)
                    return false;

                this.RemoveItem(index);
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            lock (this.sync)
            {
                if (index < 0 || index >= this.items.Count)
                    throw (new ArgumentOutOfRangeException("index", index,"Out of range of removing"));


                this.RemoveItem(index);
            }
        }

        protected virtual void ClearItems()
        {
            this.items.Clear();
        }

        protected virtual void InsertItem(int index, T item)
        {
            this.items.Insert(index, item);
        }

        protected virtual void RemoveItem(int index)
        {
            this.items.RemoveAt(index);
        }

        protected virtual void SetItem(int index, T item)
        {
            this.items[index] = item;
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList) new List<T>(this.items)).GetEnumerator();
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        object ICollection.SyncRoot
        {
            get { return this.sync; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            lock (this.sync)
            {
                ((IList)this.items).CopyTo(array, index);
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                VerifyValueType(value);
                this[index] = (T)value;
            }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        int IList.Add(object value)
        {
            VerifyValueType(value);

            lock (this.sync)
            {
                this.Add((T)value);
                return this.Count - 1;
            }
        }

        bool IList.Contains(object value)
        {
            VerifyValueType(value);
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            VerifyValueType(value);
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            VerifyValueType(value);
            this.Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            VerifyValueType(value);
            this.Remove((T)value);
        }

        static void VerifyValueType(object value)
        {
            if (value == null)
            {
                if (typeof(T).IsValueType)
                {
                    throw (new ArgumentException("Error value type"));
                }
            }
            else if (!(value is T))
            {
                throw (new ArgumentException("Error value type"));
            }
        }
    }

    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private List<TKey> keys = new List<TKey>();
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        private void AddImpl(TKey key, TValue value)
        {
            //lock (dictionary)
            {
                dictionary.Add(key, value);
                keys.Add(key);
            }
        }

        private TValue GetImpl(TKey key)
        {
            //lock (dictionary)
            return dictionary[key];
        }

        public TValue Get(int index)
        {
            //lock (dictionary)
            return dictionary[keys[index]];
        }

        private void SetImpl(TKey key, TValue value)
        {
            //lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                    dictionary[key] = value;
                else
                {
                    dictionary.Add(key, value);
                    keys.Add(key);
                }
            }
        }

        private bool RemoveImpl(TKey key)
        {
            //lock (dictionary)
            {
                keys.Remove(key);
                return dictionary.Remove(key);
            }
        }

        public bool Remove(int index)
        {
            //lock(dictionary)
            {
                var ret = dictionary.Remove(keys[index]);
                keys.RemoveAt(index);
                return ret;
            }
        }

        private void ClearImpl()
        {
            //lock (dictionary)
            {
                dictionary.Clear();
                keys.Clear();
            }
        }

        public TValue this[TKey key] { get => GetImpl(key); set => SetImpl(key, value); }

        public ICollection<TKey> Keys => keys;

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>();
                keys.ForEach(x => values.Add(dictionary[x]));
                return values;
            }
        }

        public int Count => dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            AddImpl(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddImpl(item.Key, item.Value);
        }

        public void Clear()
        {
            ClearImpl();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.ContainsKey(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return RemoveImpl(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return RemoveImpl(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
    }

    public class LockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public class LockedValue
        {
            public TValue Value;
            public RWLock lockValue;
        }
        private readonly ConcurrentDictionary<TKey, LockedValue> dictionary = new ConcurrentDictionary<TKey, LockedValue>();
        //private readonly ConcurrentDictionary<TKey, RWLock> keyLocks = new ConcurrentDictionary<TKey, RWLock>();
        public bool LockValue = true;
        private readonly RWLock GlobalLocker = new RWLock();
        private readonly RWLock RemoveChangeLocker = new RWLock();
        private readonly RWLock RemoveLocker = new RWLock();
        private RWLock Remlocker => LockValue ? RemoveChangeLocker : RemoveLocker;

        public bool TryAddOrChange(TKey key, TValue value)
        {
            var result = false;
            using (GlobalLocker.ReadLock())
            {
                RWLock.LockToken token = null;
                LockedValue dvalue;
                bool added = false;
                //using(this.Remlocker.ReadLock())
                {
                    if(!dictionary.ContainsKey(key))
                    {
                        dictionary.TryAdd(key, new LockedValue() { Value = value, lockValue = new RWLock() });
                        added = true;
                        result = true;
                    }
                    if (dictionary.TryGetValue(key, out dvalue) && !added)
                    {
                        if (LockValue)
                            token = dvalue.lockValue.WriteLock();
                        else
                            token = dvalue.lockValue.ReadLock();
                    }
                }
                if(!added && dvalue != null)
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        dvalue.Value = value;
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                    token.Dispose();
                }
            }
            return result;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            bool result = false;
            using (GlobalLocker.ReadLock())
            {
                RWLock.LockToken token = null;
                LockedValue dvalue;
                //using(this.Remlocker.ReadLock())
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        token = dvalue.lockValue.WriteLock();
                    }
                }
                if(dvalue != null)
                {
                    LockedValue outValue = null;
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        dictionary.Remove(key, out outValue);
                        value = outValue.Value;
                        result = true;
                    }
                    else
                    {
                        value = default(TValue);
                        result = false;
                    }
                    token.Dispose();
                }
                else
                {
                    value = default(TValue);
                    result = false;
                }
            }
            return result;
        }

        public bool TryGetLockedElement(TKey key, out TValue value, out RWLock.LockToken lockToken, bool? overrideLockValue = null)
        {
            RWLock.LockToken token = null;
            bool result = false;
            using (GlobalLocker.ReadLock())
            {
                LockedValue dvalue;
                //using(this.Remlocker.ReadLock())
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        if (overrideLockValue != null ? (bool)overrideLockValue : LockValue)
                            token = dvalue.lockValue.WriteLock();
                        else
                            token = dvalue.lockValue.ReadLock();
                    }
                }
                if(dvalue != null)
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        value = dvalue.Value;
                        result = true;
                    }
                    else
                    {
                        value = default(TValue);
                        token.Dispose();
                        result = false;
                    }
                }
                else
                {
                    value = default(TValue);
                    result = false;
                }
            }
            lockToken = token;
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            using (GlobalLocker.ReadLock())
            {
                if (dictionary.TryGetValue(key, out var keylock))
                {
                    value = keylock.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            using (GlobalLocker.ReadLock())
            {
                return dictionary.ContainsKey(key);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                using (GlobalLocker.ReadLock())
                    return dictionary.Keys;
            }
        }

        public ICollection<TValue> Values{
            get
            {
                using (GlobalLocker.ReadLock())
                    return dictionary.Values.Select(x => x.Value).ToList();
            }
        }

        public int Count
        {
            get
            {
                using (GlobalLocker.ReadLock())
                    return dictionary.Count;
            }
        }

        public bool IsReadOnly => false;

        public TValue this[TKey key]
        {
            get
            {
                TryGetValue(key, out var value);
                return value;
            }
            set
            {
                TryAddOrChange(key, value);
            }
        }

        public void ExecuteReadLocked(TKey key, Action<TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, false))
            {
                action(value);
            }
        }

        public void ExecuteWriteLocked(TKey key, Action<TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, true))
            {
                action(value);
            }
        }

        public void Clear()
        {
            using (GlobalLocker.ReadLock())
            {
                dictionary.Clear();
            }
        }

        public void Add(TKey key, TValue value)
        {
            this.TryAdd(key, value);
        }

        public bool Remove(TKey key)
        {
            return this.TryRemove(key, out _);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.TryAdd(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.TryRemove(item.Key, out _);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }


    public class ConcurrentDictionaryEx<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        public int FastCount;

        public IList<TKey> IKeys = new List<TKey>();
        public IList<TValue> IValues = new List<TValue>();


        public ConcurrentDictionaryEx() : base()
        {

        }

        public ConcurrentDictionaryEx(ConcurrentDictionary<TKey, TValue> keyValuePairs) : base()
        {
            foreach(var keyval in keyValuePairs)
            {
                this.TryAdd(keyval.Key, keyval.Value);
            }
        }

        public ConcurrentDictionaryEx<TKey, TValue> Upd()
        {
            for (int i = 0; i < IKeys.Count; i++)
            {
                this.TryAdd(IKeys[i], IValues[i]);
            }
            return this;
        }

        public ConcurrentDictionaryEx(IDictionary<TKey, TValue> dictionary)
        {
            foreach(var row in dictionary)
            {
                this.TryAdd(row.Key, row.Value);
            }
        }

    }

    public class ConcurrentHashSet<T> : ICollection<T>, IEnumerable<T>, System.Collections.IEnumerable, IReadOnlyCollection<T>, ISet<T>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        private ConcurrentDictionary<T, int> storage = new ConcurrentDictionary<T, int>();

        public ConcurrentHashSet() { }

        public ConcurrentHashSet(ICollection<T> collection)
        {
            collection.ForEach(x => this.Add(x));
        }

        public int Count => storage.Count;

        public bool IsReadOnly => storage.Keys.IsReadOnly;

        public void Add(T item)
        {
            storage[item] = 0;
        }

        public void Clear()
        {
            storage.Clear();
        }

        public bool Contains(T item)
        {
            return storage.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return storage.Keys.GetEnumerator();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void OnDeserialization(object sender)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            return storage.TryRemove(item, out _);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.Add(T item)
        {
            return storage.TryAdd(item, 0);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return storage.Keys.GetEnumerator();
        }
    }

    public class ConcurrentList<T> : IList<T> where T : class
    {
        private readonly ConcurrentDictionary<long, T> _store;

        public ConcurrentList(IEnumerable<T> items = null)
        {
            var prime = (items ?? Enumerable.Empty<T>()).Select(x => new KeyValuePair<long, T>(Guid.NewGuid().GuidToLongR(), x));
            _store = new ConcurrentDictionary<long, T>(prime);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _store.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_store.TryAdd(Guid.NewGuid().GuidToLongR(), item) == false)
                throw new ApplicationException("Unable to concurrently add item to list");
        }

        public void Clear()
        {
            _store.Clear();
        }

        public bool Contains(T item)
        {
            return _store.Values.Where(x => item == x).Count() > 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _store.Values.CopyTo(array, arrayIndex);
        }

        public T[] ToArray()
        {
            return _store.Values.ToArray();
        }

        public bool Remove(T item)
        {
            foreach (var key in _store.Keys)
            {
                if (_store.TryGetValue(key, out var value) && value == item)
                    return _store.TryRemove(key, out _);
            }
            return false;
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _store.Count; }
        }

        public bool IsReadOnly
        {
            get { return _store.Keys.IsReadOnly; }
        }

        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
