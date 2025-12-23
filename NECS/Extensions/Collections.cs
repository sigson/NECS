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

        public IList<T> GetAndClear()
        {
            IList<T> result = null;
            lock (this.sync)
            {
                result = new List<T>(this.items);
                this.ClearItems();
            }
            return result;
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
                // dictionary.LogAction = (operation, dict, key, value) =>
                // {
                //     if(key != null && key is Type keyType && keyType.IdToECSType() == 3129)
                //     {
                //         NLogger.Log($"{operation}+{dict.InstanceId}+Elements count: {dict.Count}\nStack Trace:\n{new StackTrace(true)}");
                //     }
                // };
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
                checkagain:
                RWLock.LockToken token = null;
                LockedValue dvalue = null;
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
                            // else if(KeysHoldingLockdownCache.ContainsKey(key))
                            // {
                            //     holdToken.Dispose();
                            //     goto recheckHolded;
                            // }
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
                    LockedValue checkdvalue = null;
                    if (!dictionary.TryGetValue(key, out checkdvalue))
                    {
                        if(token != null)
                            token.Dispose();
                        if(lockToken != null)
                            lockToken.Dispose();
                        //raceChecker++;
                        goto checkagain;
                    }
                    if (checkdvalue.lockValue != dvalue.lockValue)
                    {
                        if(token != null)
                            token.Dispose();
                        if(lockToken != null)
                            lockToken.Dispose();
                        //raceChecker++;
                        goto checkagain;
                    }

                    if (dvalue != null)
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
                checkagain:
                RWLock.LockToken token = null;
                LockedValue dvalue = null;
                //using(this.Remlocker.ReadLock())
                {
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        //в этот момент обьекта уже может не существовать в словаре
                        token = dvalue.lockValue.WriteLock();
                    }
                }
                
                if(dvalue != null)
                {
                    LockedValue checkdvalue;
                    if (!dictionary.TryGetValue(key, out checkdvalue))
                    {
                        if(token != null)
                            token.Dispose();
                        goto checkagain;
                    }
                    if (checkdvalue.lockValue != dvalue.lockValue)
                    {
                        if(token != null)
                            token.Dispose();
                        goto checkagain;
                    }
                    LockedValue outValue = null;
                    if (dictionary.TryGetValue(key, out dvalue))
                    {
                        if(action != null)
                        {
                            action(key, dvalue.Value);
                        }
                        tryremoveagain:
                        dictionary.Remove(key, out outValue);

                        LockedValue checkdeletedvalue;
                        if (dictionary.TryGetValue(key, out checkdeletedvalue) && checkdeletedvalue.lockValue == dvalue.lockValue)
                        {
                            NLogger.Error("Dothet shiet detected on TryRemove LockedDictionary, retrying remove...");
                            goto tryremoveagain;
                        }

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
                checkagain:
                LockedValue dvalue = null;
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
                    LockedValue checkdvalue;
                    if (!dictionary.TryGetValue(key, out checkdvalue))
                    {
                        if(token != null)
                            token.Dispose();
                        goto checkagain;
                    }
                    if (checkdvalue.lockValue != dvalue.lockValue)
                    {
                        if(token != null)
                            token.Dispose();
                        goto checkagain;
                    }
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
                KeysHoldingStorage.TryAddChangeLockedElement(key, false, true, out var rdlockToken, false);
                if(rdlockToken != null)
                {
                    if (!this.ContainsKey(key))
                    {
                        lockToken = rdlockToken;
                        return true;
                    }
                    //rdlockToken?.Dispose();
                }
                rdlockToken?.Dispose();
                return false;
            }
            else
                return false;
        }

        public bool ExecuteOnKeyHolded(TKey key, Action action)
        {
            if(HoldKey(key, out var lockToken))
            {
                try
                {
                    action();
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
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
            var result = this.TryAddOrChange(key, value, out _, out var lockToken, true);
            if (result && lockToken != null)
            {
                try
                {
                    action(key, value);
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
                lockToken.Dispose();
            }
            else if(lockToken != null)
            {
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
            //var result = this.TryAddOrChange(key, value, out var oldvalue, out var lockToken, true);

            if(this.TryGetLockedElement(key, out var oldvalue, out var token, true))
            {
                if(this.UnsafeChange(key, value))
                {
                    try
                    {
                        action(key, value, oldvalue);
                    }
                    catch(Exception ex)
                    {
                        NLogger.Error(ex);
                    }
                }
                //token.Dispose();
                if(token != null)
                    token?.Dispose();
            }
            

            // if (this.dictionary.ContainsKey(key) && !result && lockToken != null)
            // {
            //     action(key, value, oldvalue);
            //     lockToken.Dispose();
            // }
            // else if(lockToken != null)
            // {
            //     lockToken.Dispose();
            // }
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
            if (this.TryAddOrChange(key, value, out var oldvalue, out var lockToken, true) && lockToken != null)
            {
                try
                {
                    onAddaction(key, value);
                }
                catch (Exception ex)
                {
                    NLogger.Error(ex);
                }
                lockToken.Dispose();
            }
            else if (lockToken != null)
            {
                try
                {
                    onChangeaction(key, value, oldvalue);
                }
                catch (Exception ex)
                {
                    NLogger.Error(ex);
                }
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
                try
                {
                    action(key, value, oldValue);
                }
                catch (Exception ex)
                {
                    NLogger.Error(ex);
                }
                lockToken.Dispose();
            }
            return result;
        }

        public void ExecuteReadLocked(TKey key, Action<TKey,TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, false))
            {
                try
                {
                    action(key, value);
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
                token.Dispose();
            }
        }

        public void ExecuteWriteLocked(TKey key, Action<TKey,TValue> action)
        {
            if(this.TryGetLockedElement(key, out var value, out var token, true))
            {
                try
                {
                    action(key, value);
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
                token.Dispose();
            }
        }

        public void ExecuteReadLockedContinuously(TKey key, Action<TKey,TValue> action, out RWLock.LockToken token)
        {
            if(this.TryGetLockedElement(key, out var value, out token, false))
            {
                try
                {
                    action(key, value);
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
                //token.Dispose();
            }
        }

        public void ExecuteWriteLockedContinuously(TKey key, Action<TKey,TValue> action, out RWLock.LockToken token)
        {
            if(this.TryGetLockedElement(key, out var value, out token, true))
            {
                try
                {
                    action(key, value);
                }
                catch(Exception ex)
                {
                    NLogger.Error(ex);
                }
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
                var tokens = dictionary.Select(x => x.Value.lockValue.WriteLock());
                dictionary.Clear();
                tokens.ForEach(x => x.Dispose());
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

    public class LoggingConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

        // Сделали публичным, чтобы можно было использовать в LogAction при формировании сообщения
        public string InstanceId { get; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        // ИЗМЕНЕНИЕ: Теперь Action принимает название операции, Ключ и Значение.
        // Используем TKey? и TValue?, так как в некоторых операциях (Clear, Ctor) их может не быть.
        public Action<string, LoggingConcurrentDictionary<TKey, TValue>, TKey, TValue> LogAction { get; set; }

        // ИЗМЕНЕНИЕ: Метод Log теперь принимает типизированные аргументы
        private void Log(string operation, TKey key = default, TValue value = default)
        {
            if (LogAction == null) return;

            // Мы передаем сырые объекты. Форматирование строки и фильтрация теперь 
            // лежат на плечах того, кто задает LogAction.
            LogAction.Invoke(operation, this, key, value);
        }

        #region Constructors

        public LoggingConcurrentDictionary()
        {
            _dictionary = new ConcurrentDictionary<TKey, TValue>();
            Log("Ctor");
        }

        public LoggingConcurrentDictionary(int capacity)
        {
            int concurrencyLevel = Environment.ProcessorCount * 4;
            _dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity);
            Log("Ctor(capacity)");
        }

        public LoggingConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
            Log("Ctor(comparer)");
        }

        public LoggingConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(collection ?? throw new ArgumentNullException(nameof(collection)));
            Log("Ctor(collection)");
        }

        public LoggingConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(collection, comparer ?? EqualityComparer<TKey>.Default);
            Log("Ctor(collection, comparer)");
        }

        public LoggingConcurrentDictionary(int concurrencyLevel, int capacity)
        {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity);
            Log("Ctor(concurrencyLevel, capacity)");
        }

        #endregion

        #region ConcurrentDictionary Specific Methods

        public bool IsEmpty => _dictionary.IsEmpty;

        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            var result = _dictionary.GetOrAdd(key, value);
            // Передаем и ключ, и итоговое значение
            Log("GetOrAdd(Value)", key, result);
            return result;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

            var result = _dictionary.GetOrAdd(key, (k) => 
            {
                var val = valueFactory(k);
                Log("GetOrAdd (Factory Executed)", k, val);
                return val;
            });
            
            Log("GetOrAdd(Factory) Result", key, result);
            return result;
        }

        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

            var result = _dictionary.GetOrAdd(key, (k, arg) =>
            {
                var val = valueFactory(k, arg);
                Log("GetOrAdd (FactoryArg Executed)", k, val);
                return val;
            }, factoryArgument);

            Log("GetOrAdd(FactoryArg) Result", key, result);
            return result;
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (addValueFactory == null) throw new ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            var result = _dictionary.AddOrUpdate(key, 
                addValueFactory, 
                (k, v) => updateValueFactory(k, v));
            
            Log("AddOrUpdate(Factory)", key, result);
            return result;
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            var result = _dictionary.AddOrUpdate(key, addValue, updateValueFactory);
            Log("AddOrUpdate(Value)", key, result);
            return result;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            bool result = _dictionary.TryAdd(key, value);
            Log(result ? "TryAdd (Success)" : "TryAdd (Fail)", key, value);
            return result;
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            bool result = _dictionary.TryUpdate(key, newValue, comparisonValue);
            // Логируем новый value, который мы пытались установить
            Log(result ? "TryUpdate (Success)" : "TryUpdate (Fail)", key, newValue);
            return result;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            bool result = _dictionary.TryRemove(key, out value);
            // Если удаление успешно, value будет содержать удаленный объект, иначе default
            Log(result ? "TryRemove (Success)" : "TryRemove (Fail)", key, value);
            return result;
        }

        public bool TryRemove(KeyValuePair<TKey, TValue> item)
        {
            bool result = ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);
            Log(result ? "TryRemove(KVP) (Success)" : "TryRemove(KVP) (Fail)", item.Key, item.Value);
            return result;
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            Log("ToArray");
            return _dictionary.ToArray();
        }

        #endregion

        #region IDictionary<TKey, TValue> Implementation

        public TValue this[TKey key]
        {
            get
            {
                var val = _dictionary[key];
                Log("Indexer[Get]", key, val);
                return val;
            }
            set
            {
                _dictionary[key] = value;
                Log("Indexer[Set]", key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                Log("Keys[Get]");
                return _dictionary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                Log("Values[Get]");
                return _dictionary.Values;
            }
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            if (!_dictionary.TryAdd(key, value))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
            }
            Log("Add", key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
            Log("Clear");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Log("CopyTo");
            ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(TKey key)
        {
            bool result = _dictionary.TryRemove(key, out var val);
            Log(result ? "Remove (Success)" : "Remove (Fail)", key, val);
            return result;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool result = ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item);
            Log(result ? "Remove(KVP) (Success)" : "Remove(KVP) (Fail)", item.Key, item.Value);
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Log("GetEnumerator");
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDictionary Implementation (Non-generic)

        void IDictionary.Add(object key, object value)
        {
            if (key is TKey k && (value is TValue || value == null))
            {
                Add(k, (TValue)value);
            }
            else
            {
                throw new ArgumentException("Invalid key or value type");
            }
        }

        void IDictionary.Clear() => Clear();

        bool IDictionary.Contains(object key)
        {
            if (key is TKey k) return ContainsKey(k);
            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)_dictionary).GetEnumerator();
        }

        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => (ICollection)Keys;
        ICollection IDictionary.Values => (ICollection)Values;

        void IDictionary.Remove(object key)
        {
            if (key is TKey k) Remove(k);
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key is TKey k && TryGetValue(k, out var val)) return val;
                return null;
            }
            set
            {
                if (key is TKey k && (value is TValue || value == null))
                {
                    this[k] = (TValue)value;
                }
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            Log("CopyTo (Non-generic)");
            ((ICollection)_dictionary).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized => ((ICollection)_dictionary).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

        #endregion
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

    public class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> SimpleDictionary = null;
        private ConcurrentDictionary<TKey, TValue> ConcurrentDictionary = null;

        private IDictionary<TKey, TValue> dictionary
        {
            get
            {
                if (Defines.OneThreadMode)
                {
                    if (SimpleDictionary == null)
                    {
                        SimpleDictionary = new Dictionary<TKey, TValue>();
                    }
                    return SimpleDictionary;
                }
                else
                {
                    if (ConcurrentDictionary == null)
                    {
                        ConcurrentDictionary = new ConcurrentDictionary<TKey, TValue>();
                    }
                    return ConcurrentDictionary;
                }
            }
        }

        private void AddImpl(TKey key, TValue value)
        {
            //lock (dictionary)
            {
                dictionary.Add(key, value);
            }
        }

        private TValue GetImpl(TKey key)
        {
            //lock (dictionary)
            return dictionary[key];
        }

        private void SetImpl(TKey key, TValue value)
        {
            try
            {
                dictionary[key] = value;
            }
            catch
            {
                //ignore addition error
            }
        }

        private bool RemoveImpl(TKey key)
        {
            try
            {
                return dictionary.Remove(key);
            }
            catch
            {
                //ignore addition error
                return false;
            }
        }

        private void ClearImpl()
        {
            //lock (dictionary)
            {
                dictionary.Clear();
            }
        }

        public TValue this[TKey key] { get => GetImpl(key); set => SetImpl(key, value); }

        public ICollection<TKey> Keys => dictionary.Keys;

        public ICollection<TValue> Values => dictionary.Values;

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
            dictionary.CopyTo(array, arrayIndex);
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

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> value)
        {
            if (dictionary is ConcurrentDictionary<TKey, TValue> concurrentDictionary)
            {
                return concurrentDictionary.GetOrAdd(key, value);
            }
            else if (!dictionary.TryGetValue(key, out var invalue))
            {
                invalue = value(key);
                dictionary.Add(key, invalue);
                return invalue;
            }
            return default(TValue);
        }
    }

    public class ActionGatewayInTime
    {
        // Используем ConcurrentQueue для потокобезопасного хранения действий в очереди.
        private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        private ConcurrentHashSet<TimerCompat> timerCache = new ConcurrentHashSet<TimerCompat>();

        // Приватное поле для хранения состояния переключателя.
        private bool _actionSwitch = true;
        
        // Объект для синхронизации потоков, чтобы избежать состояний гонки.
        private readonly object _lock = new object();

        /// <summary>
        /// Переключатель выполнения.
        /// true: Действия выполняются немедленно. При переключении с false на true
        ///       выполняются все накопленные действия.
        /// false: Действия кешируются в очередь для последующего выполнения.
        /// </summary>
        public bool ActionSwitch
        {
            get
            {
                lock (_lock)
                {
                    return _actionSwitch;
                }
            }
            private set // Сеттер сделан приватным, чтобы управление шло только через метод SetTimeAwaitSwitchValue
            {
                List<Action> cachedActionsToRun = null;

                lock (_lock)
                {
                    // Если новое значение совпадает с текущим, ничего не делаем.
                    if (_actionSwitch == value) return;

                    _actionSwitch = value;

                    // Если мы только что ВКЛЮЧИЛИ переключатель,
                    // нужно забрать все действия из кеша для выполнения.
                    if (_actionSwitch)
                    {
                        cachedActionsToRun = new List<Action>();
                        while (_actions.TryDequeue(out Action cachedAction))
                        {
                            cachedActionsToRun.Add(cachedAction);
                        }
                    }
                }

                // Выполняем кеш за пределами lock, чтобы не блокировать
                // другие потоки на время выполнения потенциально долгих действий.
                if (cachedActionsToRun != null)
                {
                    ExecuteCachedActions(cachedActionsToRun);
                }
            }
        }

        /// <summary>
        /// Пытается выполнить действие или кеширует его в зависимости от состояния ActionSwitch.
        /// </summary>
        /// <param name="action">Действие, которое нужно выполнить.</param>
        public void ExecuteAction(Action action)
        {
            bool executeImmediately;
            
            // Блокируем, чтобы проверка и добавление в очередь были атомарной операцией.
            lock (_lock)
            {
                executeImmediately = _actionSwitch;
                if (!executeImmediately)
                {
                    _actions.Enqueue(action);
                }
            }

            if (executeImmediately)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // Рекомендуется логировать ошибки, чтобы выполнение одного действия
                    // не прервало всю программу.
                    NLogger.Log($"Ошибка при выполнении действия: {ex.Message} : {new StackTrace(ex).ToString()}");
                }
            }
        }

        /// <summary>
        /// Устанавливает значение переключателя и запускает таймер, 
        /// который по истечению времени вернет переключатель в ОБРАТНОЕ состояние.
        /// </summary>
        /// <param name="milliseconds">Время в миллисекундах, через которое сработает таймер.</param>
        /// <param name="switchTo">Значение, в которое нужно установить переключатель сейчас.</param>
        public void SetTimeAwaitSwitchValue(int milliseconds, bool switchTo)
        {
            // Сначала устанавливаем текущее значение.
            // Это вызовет приватный сеттер, который может запустить выполнение кеша, если switchTo = true.
            ActionSwitch = switchTo;

            // Используем Task.Delay как современную и удобную замену таймеру для одноразовой операции.
            // ContinueWith гарантирует, что следующий код выполнится после завершения задержки.
            lock (_lock)
            {
                var timer = new TimerCompat();
                timer.TimerCompatInit(milliseconds, (obj, arg) =>
                {
                    // По истечению времени инвертируем значение, которое было установлено изначально.
                    ActionSwitch = !switchTo;
                    timerCache.Remove(timer);
                }).Start();
                timerCache.Add(timer);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                // 1. Остановить и освободить таймер
                timerCache.ForEach(x => x.Stop().Dispose());
                timerCache.Clear();

                // 2. Очистить очередь кешированных действий
                // ConcurrentQueue не имеет метода Clear(), поэтому очищаем через Dequeue
                while (_actions.TryDequeue(out _)) { }

                // 3. Установить переключатель в состояние 'true' напрямую,
                // чтобы избежать логики сеттера, которая пытается ВЫПОЛНИТЬ кеш 
                // (который мы только что очистили).
                _actionSwitch = true;
            }
        }

        /// <summary>
        /// Выполняет список кешированных действий.
        /// </summary>
        private void ExecuteCachedActions(List<Action> actionsToExecute)
        {
            foreach (var action in actionsToExecute)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // Важно обрабатывать исключения здесь, чтобы сбой одного
                    // действия из кеша не остановил выполнение остальных.
                    NLogger.Log($"Ошибка при выполнении кешированного действия: {ex.Message} : {new StackTrace(ex).ToString()}");
                }
            }
        }
    }

    /// <summary>
    /// Представляет Kafka-подобную очередь событий с индивидуальным отслеживанием для каждого обработчика.
    /// Работа с состоянием очереди синхронизируется через внутренний цикл событий (Event Loop) для обеспечения потокобезопасности.
    /// Добавлена функциональность TTL (Time-To-Live) для автоматического удаления устаревших событий.
    /// </summary>
    /// <typeparam name="TEvent">Тип событий, хранимых в очереди.</typeparam>
    public class TimeSequencedEventBus<TEvent>
    {
        /// <summary>
        /// Определяет результат обработки события обработчиком.
        /// </summary>
        public enum ProcessingResult
        {
            /// <summary>
            /// Событие успешно обработано. Счетчик обработанных событий будет увеличен.
            /// </summary>
            Processed,
            /// <summary>
            /// Событие было пропущено. Оно будет добавлено в список пропущенных для этого обработчика.
            /// </summary>
            Skipped
        }

        #region Внутренние классы для Event Loop

        /// <summary>
        /// Обертка для хранения события вместе с его временной меткой.
        /// </summary>
        private class EventWrapper
        {
            public TEvent Payload { get; }
            public long TimestampTicks { get; }
            public StackTrace sTrace;

            public EventWrapper(TEvent payload, StackTrace creationTrace)
            {
                Payload = payload;
                sTrace = creationTrace;
                TimestampTicks = DateTime.UtcNow.Ticks;
            }
        }

        /// <summary>
        /// Базовый класс для всех событий, обрабатываемых внутренней шиной.
        /// </summary>
        private abstract class BusEvent { }

        /// <summary>
        /// Событие для добавления нового события в общую очередь.
        /// </summary>
        private class PublishEventCommand : BusEvent
        {
            public TEvent EventPayload { get; }
            public StackTrace sTrace;
            public PublishEventCommand(TEvent eventPayload)
            {
                sTrace = new StackTrace();
                EventPayload = eventPayload;
            }
        }

        /// <summary>
        /// Событие для регистрации нового обработчика (подписчика).
        /// </summary>
        private class SubscribeHandlerCommand : BusEvent
        {
            public string Key { get; }
            public Func<TEvent, ProcessingResult> Handler { get; }
            public SubscribeHandlerCommand(string key, Func<TEvent, ProcessingResult> handler)
            {
                Key = key;
                Handler = handler;
            }
        }

        #endregion

        /// <summary>
        /// Хранит индивидуальное состояние для каждого подключенного обработчика.
        /// </summary>
        public class HandlerState
        {
            public string Key { get; }
            public Func<TEvent, ProcessingResult> Handler { get; }
            public int ProcessedEventsCount { get; internal set; }
            public List<TEvent> SkippedEvents { get; }

            internal HandlerState(string key, Func<TEvent, ProcessingResult> handler)
            {
                Key = key;
                Handler = handler;
                ProcessedEventsCount = 0;
                SkippedEvents = new List<TEvent>();
            }
        }

        // --- Основное состояние ---

        /// <summary>
        /// Мастер-лог всех событий в том порядке, в котором они были получены.
        /// Хранит обертки с временными метками.
        /// </summary>
        private readonly List<EventWrapper> _masterEventLog = new List<EventWrapper>();

        /// <summary>
        /// Словарь состояний для каждого обработчика, индексированный по уникальному ключу.
        /// </summary>
        private readonly Dictionary<string, HandlerState> _handlerStates = new Dictionary<string, HandlerState>();

        /// <summary>
        /// Потокобезопасная очередь команд для цикла событий.
        /// </summary>
        private readonly ConcurrentQueue<BusEvent> _eventQueue = new ConcurrentQueue<BusEvent>();
        
        /// <summary>
        /// Время жизни события в тиках. Если 0 или меньше, TTL отключен.
        /// </summary>
        private readonly long _ttlTicks;

        /// <summary>
        /// Флаг, предотвращающий одновременный запуск нескольких циклов обработки.
        /// 0 - не в обработке, 1 - в обработке.
        /// </summary>
        private int _isProcessing = 0;

        public bool Logging = false;

        /// <summary>
        /// Инициализирует новый экземпляр шины событий с указанным временем жизни событий.
        /// </summary>
        /// <param name="ttl">Время жизни для событий в очереди. События старше этого значения будут удалены при вызове Update.</param>
        public TimeSequencedEventBus(TimeSpan ttl)
        {
            _ttlTicks = ttl.Ticks;
        }

        /// <summary>
        /// Выполняет один цикл обработки событий. Этот метод должен вызываться циклически извне.
        /// Сначала удаляет устаревшие события, затем обрабатывает новые.
        /// Если предыдущий вызов еще не завершился, метод немедленно выйдет.
        /// </summary>
        public void Update()
        {
            // Проверяем, не выполняется ли уже обработка.
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                // 1. Удаляем все протухшие события
                CleanupExpiredEvents();

                // 2. Обрабатываем все накопившиеся команды (публикации и подписки)
                while (_eventQueue.TryDequeue(out var busEvent))
                {
                    ProcessCommand(busEvent);
                }
            }
            finally
            {
                // Гарантированно сбрасываем флаг.
                Volatile.Write(ref _isProcessing, 0);
            }
        }

        /// <summary>
        /// Публикует новое событие в шину. Метод является потокобезопасным.
        /// </summary>
        /// <param name="eventPayload">Событие для добавления.</param>
        public void Publish(TEvent eventPayload)
        {
            _eventQueue.Enqueue(new PublishEventCommand(eventPayload));
        }

        /// <summary>
        /// Регистрирует новый обработчик событий. Метод является потокобезопасным.
        /// Если обработчик с таким ключом уже существует, он будет заменен.
        /// </summary>
        /// <param name="key">Уникальный ключ для идентификации обработчика.</param>
        /// <param name="handler">Лямбда-функция для обработки событий.</param>
        public void Subscribe(string key, Func<TEvent, ProcessingResult> handler)
        {
            _eventQueue.Enqueue(new SubscribeHandlerCommand(key, handler));
        }

        /// <summary>
        /// Возвращает снимок текущего состояния указанного обработчика.
        /// </summary>
        public HandlerState GetHandlerState(string key)
        {
            _handlerStates.TryGetValue(key, out var state);
            return state;
        }
        
        /// <summary>
        /// Удаляет устаревшие события из начала мастер-лога и корректирует счетчики обработчиков.
        /// </summary>
        private void CleanupExpiredEvents()
        {
            if (_ttlTicks <= 0 || _masterEventLog.Count == 0) return;

            var nowTicks = DateTime.UtcNow.Ticks;
            int expiredCount = 0;

            foreach (var evtWrapper in _masterEventLog)
            {
                if (nowTicks - evtWrapper.TimestampTicks > _ttlTicks)
                {
                    expiredCount++;
                }
                else
                {
                    // События отсортированы по времени, так что можно остановиться на первом "свежем".
                    break;
                }
            }

            if (expiredCount > 0)
            {
                _masterEventLog.RemoveRange(0, expiredCount);
                if(Logging)
                    NLogger.Log($"[Bus TTL Cleaner] Cleared {expiredCount} expired events.");

                // Корректируем состояние всех обработчиков
                foreach (var handlerState in _handlerStates.Values)
                {
                    // Сдвигаем "окно" обработанных событий.
                    // Math.Max гарантирует, что счетчик не станет отрицательным.
                    handlerState.ProcessedEventsCount = Math.Max(0, handlerState.ProcessedEventsCount - expiredCount);
                }
            }
        }

        /// <summary>
        /// Маршрутизатор команд для внутренней обработки (выполняется синхронно в Update).
        /// </summary>
        private void ProcessCommand(BusEvent busEvent)
        {
            switch (busEvent)
            {
                case PublishEventCommand cmd:
                    ProcessNewEvent(cmd);
                    break;
                case SubscribeHandlerCommand cmd:
                    ProcessNewSubscription(cmd);
                    break;
            }
        }

        /// <summary>
        /// Обрабатывает регистрацию нового обработчика.
        /// </summary>
        private void ProcessNewSubscription(SubscribeHandlerCommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.Key) || cmd.Handler == null) return;

            var newState = new HandlerState(cmd.Key, cmd.Handler);
            _handlerStates[cmd.Key] = newState;
            if(Logging)
                NLogger.Log($"[Bus] Subscriber '{cmd.Key}' registered.");

            // Сразу же пытаемся обработать все уже существующие события для нового подписчика
            ProcessEventsForHandler(newState);
        }

        /// <summary>
        /// Обрабатывает добавление нового события в мастер-лог.
        /// </summary>
        private void ProcessNewEvent(PublishEventCommand cmd)
        {
            _masterEventLog.Add(new EventWrapper(cmd.EventPayload, cmd.sTrace));
            if(Logging)
                NLogger.Log($"[Bus] Published new event: '{cmd.EventPayload}'. All events: {_masterEventLog.Count}.");

            // После добавления нового события, запускаем обработку для ВСЕХ подписчиков.
            foreach (var handlerState in _handlerStates.Values)
            {
                ProcessEventsForHandler(handlerState);
            }
        }

        /// <summary>
        /// Основная логика: запускает лямбду обработчика для всех событий, которые он еще не видел.
        /// </summary>
        private void ProcessEventsForHandler(HandlerState state)
        {
            // Начинаем с первого необработанного события
            int startFromIndex = state.ProcessedEventsCount;

            if (startFromIndex >= _masterEventLog.Count)
            {
                return; // Все события уже обработаны
            }
            if(Logging)
                NLogger.Log($"[Bus] Start process for '{state.Key}' with event index #{startFromIndex + 1}...");

            for (int i = startFromIndex; i < _masterEventLog.Count; i++)
            {
                var currentEventWrapper = _masterEventLog[i];
                var currentEventPayload = currentEventWrapper.Payload;
                try
                {
                    var result = state.Handler(currentEventPayload);
                    switch (result)
                    {
                        case ProcessingResult.Processed:
                            // Только в случае успешной обработки мы двигаем счетчик вперед
                            state.ProcessedEventsCount++;
                            break;
                        case ProcessingResult.Skipped:
                            state.SkippedEvents.Add(currentEventPayload);
                            if(Logging)
                                NLogger.Log($"[Handler: {state.Key}]  Event '{currentEventPayload}' skipped.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Если лямбда бросает исключение, считаем событие пропущенным.
                    state.SkippedEvents.Add(currentEventPayload);
                    if(Logging)
                        NLogger.Log($"[Handler: {state.Key}] Error processing event '{currentEventPayload}': {ex.Message}. Event skipped. Stacktrace: {ex.StackTrace} \n -=-=-=-=-Creation trace=-=-=-=-=-\n{currentEventWrapper.sTrace}\n -=-=-=-=-=-=-=-=-=-=-=-");
                }
            }
            if(Logging)
                NLogger.Log($"[Bus] Process for '{state.Key}' finalized. Proceed: {state.ProcessedEventsCount}, Skipped: {state.SkippedEvents.Count}.");
        }
    }

    public class LoggingDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        // Внутренний, "настоящий" словарь, который мы оборачиваем
        private long instanceId = Guid.NewGuid().GuidToLong();
        private readonly IDictionary<TKey, TValue> _dictionary;

        // Приватный метод для логирования текущего состояния
        private void LogState(string prefix = "")
        {
            // Environment.StackTrace дает более полную информацию, чем new StackTrace()
            string stackTrace = Environment.StackTrace;
            
            NLogger.Log($"{prefix}+{instanceId}+Elements count: {_dictionary.Count}\nStack Trace:\n{stackTrace}");
        }

        #region Constructors
        // Повторяем конструкторы оригинального Dictionary
        public LoggingDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
            LogState("Ctor()");
        }

        public LoggingDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
            LogState("Ctor(capacity)");
        }

        public LoggingDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
            LogState("Ctor(comparer)");
        }

        public LoggingDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
            LogState("Ctor(dictionary)");
        }
        #endregion

        #region IDictionary<TKey, TValue> Implementation

        public TValue this[TKey key]
        {
            get
            {
                var value = _dictionary[key];
                LogState("Indexer[get]");
                return value;
            }
            set
            {
                _dictionary[key] = value;
                LogState("Indexer[set]");
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                var keys = _dictionary.Keys;
                LogState("Keys[get]");
                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                var values = _dictionary.Values;
                LogState("Values[get]");
                return values;
            }
        }

        public int Count
        {
            get
            {
                var count = _dictionary.Count;
                LogState("Count[get]");
                return count;
            }
        }

        public bool IsReadOnly => _dictionary.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            LogState("Add(key, value)");
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item);
            LogState("Add(item)");
        }

        public void Clear()
        {
            _dictionary.Clear();
            LogState("Clear()");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            bool result = _dictionary.Contains(item);
            LogState("Contains(item)");
            return result;
        }

        public bool ContainsKey(TKey key)
        {
            bool result = _dictionary.ContainsKey(key);
            LogState("ContainsKey(key)");
            return result;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
            LogState("CopyTo()");
        }

        public bool Remove(TKey key)
        {
            bool result = _dictionary.Remove(key);
            LogState("Remove(key)");
            return result;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool result = _dictionary.Remove(item);
            LogState("Remove(item)");
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool result = _dictionary.TryGetValue(key, out value);
            LogState("TryGetValue()");
            return result;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            // Логируем сам факт получения итератора. 
            // Логирование каждого шага итерации (MoveNext) потребовало бы создания
            // обертки и для IEnumerator, что усложнило бы код.
            var enumerator = _dictionary.GetEnumerator();
            LogState("GetEnumerator()");
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }

    public class PriorityEventQueue<TKey, TEvent> where TEvent : System.Delegate
    {
        private PriorityEventQueueOneTread<TKey, TEvent> onethreadqueue;
        private PriorityEventQueueMultiThread<TKey, TEvent> multithreadqueue;
        public PriorityEventQueue(IEnumerable<TKey> priorityOrder, int gatesOpened = Int32.MaxValue, Func<int, int> gatesCounter = null, Type ownerType = null)
        {
            if (Defines.OneThreadMode)
            {
                onethreadqueue = new PriorityEventQueueOneTread<TKey, TEvent>(priorityOrder, gatesOpened, gatesCounter, ownerType);
            }
            else
            {
                multithreadqueue = new PriorityEventQueueMultiThread<TKey, TEvent>(priorityOrder, gatesOpened, gatesCounter, ownerType);
            }
        }

        public void AddEvent(TKey key, TEvent eventItem)
        {
            if (Defines.OneThreadMode)
            {
                onethreadqueue.AddEvent(key, eventItem);
            }
            else
            {
                multithreadqueue.AddEvent(key, eventItem);
            }
        }
    }

    public class PriorityEventQueueOneTread<TKey, TEvent> where TEvent : System.Delegate
    {
        // ... все поля и конструктор остаются прежними ...
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
        private readonly Func<int, int> GatesCounter;
        private readonly ConcurrentDictionary<TKey, SynchronizedList<ActionWrapper>> _eventLists;
        private readonly List<PriorityWrapper> _priorityOrder;
        private readonly object _lock = new object();

        private int _processing = 0;

        private readonly StackTrace creationStackTrace;
        private readonly Type ownerType;

        public PriorityEventQueueOneTread(IEnumerable<TKey> priorityOrder, int gatesOpened = Int32.MaxValue, Func<int, int> gatesCounter = null, Type ownerType = null)
        {
            if (priorityOrder == null)
                throw new ArgumentNullException(nameof(priorityOrder));

            creationStackTrace = new StackTrace();
            this.ownerType = ownerType;
            OpenedDownGates = gatesOpened;
            GatesCounter = gatesCounter ?? (x => x + 1);

            _priorityOrder = new List<PriorityWrapper>();
            if (!priorityOrder.Any())
                throw new ArgumentException("Priority order must not be empty", nameof(priorityOrder));

            _eventLists = new ConcurrentDictionary<TKey, SynchronizedList<ActionWrapper>>();
            foreach (var key in priorityOrder)
            {
                _priorityOrder.Add(new PriorityWrapper() { priorityValue = key, GateOpened = false });
                _eventLists[key] = new SynchronizedList<ActionWrapper>();
            }
        }

        public void AddEvent(TKey key, TEvent eventItem)
        {
            if (!_eventLists.ContainsKey(key))
                throw new ArgumentException("Key is not part of the priority order", nameof(key));

            var newAction = new ActionWrapper() { actionId = Guid.NewGuid(), actionEvent = eventItem, inAction = false };

            lock (_lock)
            {
                _eventLists[key].Add(newAction);
            }

            ProcessQueue();
        }


        // --- ИСПРАВЛЕННЫЙ МЕТОД ОБРАБОТКИ С ГАРАНТИЕЙ ВЫЗОВА ---
        private void ProcessQueue()
        {
            // 1. Пытаемся захватить "право на обработку". Если кто-то уже работает, выходим.
            if (Interlocked.CompareExchange(ref _processing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                // 2. Вводим внешний цикл, который будет работать до тех пор,
                // пока внутренний цикл находит и обрабатывает события.
                bool wasWorkDoneInLastPass;
                do
                {
                    wasWorkDoneInLastPass = false;

                    // Внутренний цикл для поиска и обработки одного события
                    while (true)
                    {
                        ActionWrapper? eventToProcess = null;
                        PriorityWrapper priorityOfEvent = null;

                        // Блокируем, чтобы безопасно найти следующее событие
                        lock (_lock)
                        {
                            for (int i = 0; i < OpenedDownGates && i < _priorityOrder.Count; i++)
                            {
                                var currentPriority = _priorityOrder[i];
                                if (_eventLists.TryGetValue(currentPriority.priorityValue, out var eventList) && eventList.Count > 0)
                                {
                                    var wrapper = eventList[0];
                                    if (!wrapper.inAction)
                                    {
                                        wrapper.inAction = true;
                                        eventList[0] = wrapper; // Важно для struct

                                        eventToProcess = wrapper;
                                        priorityOfEvent = currentPriority;
                                        break;
                                    }
                                }
                            }
                        }

                        // Если доступных событий не найдено, выходим из внутреннего цикла
                        if (eventToProcess == null)
                        {
                            break;
                        }

                        // Нашли событие - значит, работа была проделана
                        wasWorkDoneInLastPass = true;

                        // Запускаем выполнение задачи
                        // ВАЖНО: TaskEx.Run должен быть синхронным в однопоточном режиме,
                        // иначе эта логика не будет работать так, как задумано.
                        TaskEx.RunAsync(() =>
                        {
                            try
                            {
                                eventToProcess.Value.actionEvent.DynamicInvoke();

                                // Этот код должен выполняться атомарно с удалением
                                lock (_lock)
                                {
                                    if (!priorityOfEvent.GateOpened)
                                    {
                                        priorityOfEvent.GateOpened = true;
                                        OpenedDownGates = GatesCounter(OpenedDownGates);
                                    }
                                }
                            }
                            finally
                            {
                                // Гарантированное удаление события из очереди
                                lock (_lock)
                                {
                                    try
                                    {
                                        // Проверяем, что удаляем именно то событие, которое обработали
                                        if (_eventLists[priorityOfEvent.priorityValue].Count > 0 &&
                                            _eventLists[priorityOfEvent.priorityValue][0].actionId == eventToProcess.Value.actionId)
                                        {
                                            _eventLists[priorityOfEvent.priorityValue].RemoveAt(0);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        NLogger.Log($"Error removing event from queue - {ex.Message}\n in type {this.ownerType} \n{this.creationStackTrace}");
                                    }
                                }
                            }
                        });
                    }

                    // 3. Если в последнем полном проходе была проделана работа,
                    // повторяем внешний цикл, чтобы проверить наличие новых событий,
                    // которые могли быть добавлены во время выполнения `TaskEx.Run`.
                } while (wasWorkDoneInLastPass);
            }
            finally
            {
                // 4. Только когда очередь действительно пуста, сбрасываем флаг.
                _processing = 0;
            }
        }
    }

    public class PriorityEventQueueMultiThread<TKey, TEvent> where TEvent : System.Delegate
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
        private readonly ConcurrentDictionary<TKey, SynchronizedList<ActionWrapper>> _eventLists;
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
        public PriorityEventQueueMultiThread(IEnumerable<TKey> priorityOrder, int gatesOpened = Int32.MaxValue, Func<int, int> gatesCounter = null, Type ownerType = null)
        {
            if (priorityOrder == null)
                throw new ArgumentNullException(nameof(priorityOrder));

            creationStackTrace = new StackTrace();
            this.ownerType = ownerType;
            OpenedDownGates = gatesOpened;
            if (gatesCounter == null)
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

            _eventLists = new ConcurrentDictionary<TKey, SynchronizedList<ActionWrapper>>();
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
                var newAction = new ActionWrapper() { actionId = Guid.NewGuid(), actionEvent = eventItem, inAction = false };
                _eventLists[key].Add(newAction);
                IncludeEvent(key, newAction);
            }
        }

        private void IncludeEvent(TKey key, ActionWrapper eventItem)
        {
            for (int i = 0; i < OpenedDownGates; i++)
            {
                if (i >= _priorityOrder.Count)
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
                                LockEx.Lock(prioritynow, () => !Defines.OneThreadMode, () =>
                                {
                                    eventItem.actionEvent.DynamicInvoke();
                                    if (!prioritynow.GateOpened)
                                    {
                                        prioritynow.GateOpened = true;
                                        OpenedDownGates = GatesCounter(OpenedDownGates);
                                    }
                                    LockEx.Lock(_lock, () => !Defines.OneThreadMode, () =>
                                    {
                                        try
                                        {
                                            _eventLists[prioritynow.priorityValue].RemoveAt(0);
                                        }
                                        catch (Exception ex)
                                        {
                                            NLogger.Log($"Error in priority event queue (Maybe you start already executed only one execution method (OnAdded as example)) - {ex.Message}\n in type {this.ownerType} \n{this.creationStackTrace}");
                                        }
                                    });
                                    ProcessEvents();
                                });
                            });
                        }
                    }
                }
            }
        }

        private void ProcessEvents()
        {
            LockEx.Lock(_lock, () => !Defines.OneThreadMode, () =>
            {
                for (int i = 0; i < OpenedDownGates; i++)
                {
                    if (i >= _priorityOrder.Count)
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
                                    LockEx.Lock(prioritynow, () => !Defines.OneThreadMode, () =>
                                    {
                                        priorevent.actionEvent.DynamicInvoke();
                                        if (!prioritynow.GateOpened)
                                        {
                                            prioritynow.GateOpened = true;
                                            OpenedDownGates = GatesCounter(OpenedDownGates);
                                        }
                                        LockEx.Lock(_lock, () => !Defines.OneThreadMode, () =>
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
                                        });

                                        ProcessEvents();
                                    });
                                });
                            }
                        }
                    }
                }
            });
        }
    }
}
