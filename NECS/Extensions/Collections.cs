using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions.ThreadingSync;
using NECS.Harness.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private LockedDictionary<TKey, bool> KeysHoldingStorage = null;
        private ConcurrentDictionary<TKey, bool> KeysHoldingLockdownCache = new ConcurrentDictionary<TKey, bool>();
        public bool HoldKeys = false;
        public bool HoldKeyStorage = false;
        private readonly ConcurrentDictionary<TKey, LockedValue> dictionary = new ConcurrentDictionary<TKey, LockedValue>();
        public bool LockValue = false;
        private readonly RWLock GlobalLocker = new RWLock();

        public LockedDictionary(bool preserveLockingKeys = false)
        {
            HoldKeys = preserveLockingKeys;
            if(HoldKeys)
            {
                KeysHoldingStorage = new LockedDictionary<TKey, bool>();
                KeysHoldingStorage.HoldKeyStorage = true;
            }
        }

        #region Base functions
        private bool TryAddOrChange(TKey key, TValue value, out TValue oldValue, out RWLock.LockToken lockToken, bool lockedMode = false, bool? overrideLockingMode = false)
        {
            var result = false;
            lockToken = null;
            oldValue = default(TValue);
            using (GlobalLocker.ReadLock())
            {
                RWLock.LockToken token = null;
                LockedValue dvalue;
                bool added = false;
                //using(this.Remlocker.ReadLock())
                {
                    int raceChecker = 0;
                    recheckRaceOfStates:
                    bool noncontainsDetected = false;
                    if(!dictionary.ContainsKey(key))
                    {
                        RWLock.LockToken holdToken = null;
                        if(HoldKeys)
                        {
                            recheckHolded:
                            KeysHoldingStorage.TryAddChangeLockedElement(key, false, true, out holdToken, true);
                            if(this.dictionary.ContainsKey(key))
                            {
                                holdToken.Dispose();
                                goto recheckRaceOfStates;
                            }
                            else if(KeysHoldingLockdownCache.ContainsKey(key))
                            {
                                holdToken.Dispose();
                                goto recheckHolded;
                            }
                        }
                        var newLockedValue = new LockedValue() { Value = value, lockValue = new RWLock() };
                        if(lockedMode) 
                        {
                            if((overrideLockingMode != null ? (bool)overrideLockingMode : LockValue))
                            {
                                lockToken = newLockedValue.lockValue.WriteLock();
                            }
                            else 
                            {
                                lockToken = newLockedValue.lockValue.ReadLock();
                            }
                        }
                        if(raceChecker > 5)
                            Monitor.Enter(dictionary);
                        if(dictionary.TryAdd(key, newLockedValue))
                        {
                            added = true;
                            result = true;
                            if(raceChecker > 5)
                                Monitor.Exit(dictionary);
                            if(HoldKeys)
                                holdToken.Dispose();
                            return result;
                        }
                        else
                        {
                            noncontainsDetected = true;
                        }
                        if(HoldKeys && holdToken != null)
                            holdToken.Dispose();
                    }
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        if (!added)
                        {
                            if((overrideLockingMode != null ? (bool)overrideLockingMode : LockValue))
                            {
                                token = dvalue.lockValue.WriteLock();
                            }
                            else 
                            {
                                token = dvalue.lockValue.ReadLock();
                            }
                        }
                    }
                    else if(noncontainsDetected)
                    {
                        ///we got a race of states when it was discovered that the element was present at the stage of trying to add it, but became absent at the stage of trying to get it. Based on the statistically small chance of such a situation, I repeat the check until the situation is resolved.
                        ///
                        raceChecker++;
                        goto recheckRaceOfStates;
                    }
                    if(raceChecker > 5)
                        Monitor.Exit(dictionary);
                }
                if(!added && dvalue != null)
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        oldValue = dvalue.Value;
                        dvalue.Value = value;
                        result = false;
                        if(lockedMode)
                            lockToken = token;
                        else
                            token.Dispose();
                    }
                    else
                    {
                        result = false;
                        token.Dispose();
                    }
                }
            }
            return result;
        }

        private bool TryRemove(TKey key, out TValue value, Action<TKey, TValue> action = null)
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
                        if(action != null)
                        {
                            action(key, dvalue.Value);
                        }
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

        /// <summary>
        /// IMPORTANT!!! HALT!!! if you will trying to remove or change value on selected key - YOU ENTER TO DEADLOCK!!! USE Async* or Unsafe* operations for this element, and THINK about you doing!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="lockToken"></param>
        /// <param name="overrideLockValue"></param>
        /// <returns></returns>
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

        public bool HoldKey(TKey key, out RWLock.LockToken lockToken, bool holdMode = true)
        {
            lockToken = null;
            if (HoldKeys)
            {
                KeysHoldingStorage.TryAddChangeLockedElement(key, false, holdMode, out var wrlockToken, true);
                if(wrlockToken != null)
                {
                    if (!this.ContainsKey(key))
                    {
                        if (KeysHoldingLockdownCache.TryAdd(key, false))
                        {
                            wrlockToken.Dispose();
                            KeysHoldingStorage.TryAddChangeLockedElement(key, false, holdMode, out lockToken);
                            KeysHoldingLockdownCache.Remove(key, out _);
                            return true;
                        }
                    }
                    wrlockToken.Dispose();
                }
                return false;
            }
            else
                return false;
        }

        public bool ExecuteOnKeyHolded(TKey key, Action action)
        {
            if(HoldKey(key, out var lockToken))
            {
                action();
                lockToken.Dispose();
                return true;
            }
            return false;
        }

        public bool TryAddChangeLockedElement(TKey key, TValue value, bool writeLocked, out RWLock.LockToken lockToken, bool LockingMode = false)
        {
            return this.TryAddOrChange(key, value, out _, out lockToken, writeLocked, LockingMode);
        }

        public void ExecuteOnAddLocked(TKey key, TValue value, Action<TKey,TValue> action)
        {
            if(this.TryAddOrChange(key, value, out _, out var lockToken, true) && lockToken != null)
            {
                action(key, value);
                lockToken.Dispose();
            }
        }
        /// <summary>
        /// input change action has key, value, oldvalue params
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="action">key, value, oldvalue</param>
        /// <returns></returns>
        public void ExecuteOnChangeLocked(TKey key, TValue value, Action<TKey,TValue,TValue> action)
        {
            if(this.dictionary.ContainsKey(key) && !this.TryAddOrChange(key, value, out var oldvalue, out var lockToken, true) && lockToken != null)
            {
                action(key, value, oldvalue);
                lockToken.Dispose();
            }
        }

        /// <summary>
        /// input change action has key, value, oldvalue params
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="action">key, value, oldvalue</param>
        /// <returns></returns>
        public void ExecuteOnAddOrChangeLocked(TKey key, TValue value, Action<TKey,TValue> onAddaction, Action<TKey,TValue,TValue> onChangeaction)
        {
            if(this.TryAddOrChange(key, value, out var oldvalue, out var lockToken, true) && lockToken != null)
            {
                onAddaction(key, value);
                lockToken.Dispose();
            }
            else if(lockToken != null)
            {
                onChangeaction(key, value, oldvalue);
                lockToken.Dispose();
            }
        }

        public void ExecuteOnRemoveLocked(TKey key, out TValue value, Action<TKey,TValue> action)
        {
            TryRemove(key, out value, action);
        }
        /// <summary>
        /// input change action has key, value, oldvalue params
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="action">key, value, oldvalue</param>
        /// <returns></returns>
        public bool ExecuteOnAddChangeLocked(TKey key, TValue value, Action<TKey,TValue,TValue> action)
        {
            var result = this.TryAddOrChange(key, value, out var oldValue, out var lockToken, true);
            if(lockToken != null)
            {
                action(key, value, oldValue);
                lockToken.Dispose();
            }
            return result;
        }

        public void ExecuteReadLocked(TKey key, Action<TKey,TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, false))
            {
                action(key, value);
                token.Dispose();
            }
        }

        public void ExecuteWriteLocked(TKey key, Action<TKey,TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, true))
            {
                action(key, value);
                token.Dispose();
            }
        }

        public void ExecuteReadLockedContinuously(TKey key, Action<TKey,TValue> action, out RWLock.LockToken token)
        {
            if(this.TryGetLockedElement(key, out var value, out token, false))
            {
                action(key, value);
                //token.Dispose();
            }
        }

        public void ExecuteWriteLockedContinuously(TKey key, Action<TKey,TValue> action, out RWLock.LockToken token)
        {
            if(this.TryGetLockedElement(key, out var value, out token, true))
            {
                action(key, value);
                //token.Dispose();
            }
        }

        public RWLock.LockToken LockStorage()
        {
            return this.GlobalLocker.WriteLock();
        }

        public void Clear()
        {
            using (GlobalLocker.WriteLock())
            {
                dictionary.Clear();
            }
        }

        public IDictionary<TKey, TValue> ClearSnapshot()
        {
            IDictionary<TKey, TValue> result = null;
            using (GlobalLocker.WriteLock())
            {
                result = dictionary.ToDictionary(x => x.Key, x => x.Value.Value);
                dictionary.Clear();
            }
            return result;
        }

        #endregion

        #region Async functions
        public void AsyncAdd(TKey key, TValue value)
        {
            TaskEx.RunAsync(() => this.Add(key, value));
        }

        public void AsyncRemove(TKey key)
        {
            TaskEx.RunAsync(() => this.Remove(key));
        }

        public void AsyncRemove(KeyValuePair<TKey, TValue> item)
        {
            TaskEx.RunAsync(() => this.Remove(item));
        }

        public void AsyncAdd(KeyValuePair<TKey, TValue> item)
        {
            TaskEx.RunAsync(() => this.Add(item));
        }
        #endregion

        #region Unsafe functions

        public bool UnsafeAdd(TKey key, TValue value)
        {
            if(this.dictionary.ContainsKey(key)) return false;
            return this.dictionary.TryAdd(key, new LockedValue(){Value = value, lockValue = new RWLock()});
        }

        public bool UnsafeRemove(TKey key, out TValue value)
        {
            if(this.dictionary.Remove(key, out var dicvalue))
            {
                value = dicvalue.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public bool UnsafeChange(TKey key, TValue value)
        {
            if(this.dictionary.TryGetValue(key, out var oldvalue))
            {
                oldvalue.Value = value;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool UnsafeRemove(KeyValuePair<TKey, TValue> item)
        {
            return this.dictionary.Remove(item.Key, out _);
        }

        public void UnsafeAdd(KeyValuePair<TKey, TValue> item)
        {
            this.dictionary.TryAdd(item.Key, new LockedValue(){Value = item.Value, lockValue = new RWLock()});
        }

        #endregion

        #region Default functions

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
                TryAddOrChange(key, value, out _, out _);
            }
        }

        public void Add(TKey key, TValue value)
        {
            this.TryAddOrChange(key, value, out _, out _);
        }

        public bool Remove(TKey key)
        {
            return this.TryRemove(key, out _);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.TryRemove(item.Key, out _);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.TryAddOrChange(item.Key, item.Value, out _, out _);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
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
            // Validate input parameters
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index must be non-negative and within the bounds of the array.");

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("The array does not have enough space to copy all elements starting at the specified index.");

            // Copy elements to the array
            int index = arrayIndex;
            foreach (T item in this)
            {
                array[index] = item;
                index++;
            }
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

    public class PriorityEventQueue<TKey, TEvent> where TEvent : System.Delegate
    {
        private struct ActionWrapper
        {
            public Guid actionId;
            public TEvent actionEvent;
            public bool inAction;
        }

        private class PriorityWrapper
        {
            public TKey priorityValue;
            public bool GateOpened;
        }
        private int OpenedDownGates;
        private Func<int, int> GatesCounter;
        private readonly Dictionary<TKey, SynchronizedList<ActionWrapper>> _eventLists;
        private readonly List<PriorityWrapper> _priorityOrder;
        private readonly object _lock = new object();
        private StackTrace creationStackTrace;
        private Type ownerType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priorityOrder"></param>
        /// <param name="gatesOpened">minimal gates value = 1, gates opened on first event</param>
        /// <param name="gatesCounter"> may be + 2</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public PriorityEventQueue(IEnumerable<TKey> priorityOrder, int gatesOpened = Int32.MaxValue, Func<int, int> gatesCounter = null, Type ownerType = null)
        {
            if (priorityOrder == null)
                throw new ArgumentNullException(nameof(priorityOrder));

            creationStackTrace = new StackTrace();
            this.ownerType = ownerType;
            OpenedDownGates = gatesOpened;
            if(gatesCounter == null)
            {
                GatesCounter = x => x + 1;
            }
            else
            {
                GatesCounter = gatesCounter;
            }
            _priorityOrder = new List<PriorityWrapper>();
            if (priorityOrder.Count() == 0)
                throw new ArgumentException("Priority order must not be empty", nameof(priorityOrder));
            // if (priorityOrder.Distinct().Count() != _priorityOrder.Count)
            //     throw new ArgumentException("Priority order must contain unique keys", nameof(priorityOrder));

            _eventLists = new Dictionary<TKey, SynchronizedList<ActionWrapper>>();
            foreach (var key in priorityOrder)
            {
                _priorityOrder.Add(new PriorityWrapper() { priorityValue = key, GateOpened = false });
                _eventLists[key] = new SynchronizedList<ActionWrapper>();
            }
        }

        public void AddEvent(TKey key, TEvent eventItem)
        {
            lock (_lock)
            {
                if (!_eventLists.ContainsKey(key))
                    throw new ArgumentException("Key is not part of the priority order", nameof(key));
                var newAction = new ActionWrapper(){actionId = Guid.NewGuid(), actionEvent = eventItem, inAction = false};
                _eventLists[key].Add(newAction);
                IncludeEvent(key, newAction); 
            }
        }

        private void IncludeEvent(TKey key, ActionWrapper eventItem)
        {
            for (int i = 0; i < OpenedDownGates; i++)
            {
                if(i >= _priorityOrder.Count)
                    break;
                var prioritynow = _priorityOrder[i];
                if (_eventLists.TryGetValue(prioritynow.priorityValue, out var prioritystorage))
                {
                    if (prioritystorage.Count > 0)
                    {
                        if (prioritystorage[0].actionId == eventItem.actionId)
                        {
                            eventItem.inAction = true;
                            TaskEx.RunAsync(() =>
                            {
                                lock (prioritynow)
                                {
                                    eventItem.actionEvent.DynamicInvoke();
                                    if (!prioritynow.GateOpened)
                                    {
                                        prioritynow.GateOpened = true;
                                        OpenedDownGates = GatesCounter(OpenedDownGates);
                                    }
                                    lock (_lock)
                                    {
                                        try
                                        {
                                            _eventLists[prioritynow.priorityValue].RemoveAt(0);
                                        }
                                        catch (Exception ex)
                                        {
                                            NLogger.Log($"Error in priority event queue (Maybe you start already executed only one execution method (OnAdded as example)) - {ex.Message}\n in type {this.ownerType} \n{this.creationStackTrace}");
                                        }
                                    }
                                    ProcessEvents();
                                }
                            });
                        }
                    }
                }
            }
        }

        private void ProcessEvents()
        {
            lock (_lock)
            {
                for (int i = 0; i < OpenedDownGates; i++)
                {
                    if(i >= _priorityOrder.Count)
                        break;
                    var prioritynow = _priorityOrder[i];
                    if (_eventLists.TryGetValue(prioritynow.priorityValue, out var prioritystorage))
                    {
                        if (prioritystorage.Count > 0)
                        {
                            if (!prioritystorage[0].inAction)
                            {
                                var priorevent = prioritystorage[0];
                                priorevent.inAction = true;
                                TaskEx.RunAsync(() =>
                                {
                                    lock (prioritynow)
                                    {
                                        priorevent.actionEvent.DynamicInvoke();
                                        if (!prioritynow.GateOpened)
                                        {
                                            prioritynow.GateOpened = true;
                                            OpenedDownGates = GatesCounter(OpenedDownGates);
                                        }
                                        lock (_lock)
                                        {
                                            //_eventLists[prioritynow.priorityValue].RemoveAt(0);
                                            try
                                            {
                                                _eventLists[prioritynow.priorityValue].RemoveAt(0);
                                            }
                                            catch (Exception ex)
                                            {
                                                NLogger.Log($"Error in priority event queue (Maybe you start already executed only one execution method (OnAdded as example)) - {ex.Message}\n in type {this.ownerType} \n{this.creationStackTrace}");
                                            }
                                        }

                                        ProcessEvents();
                                    }
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
