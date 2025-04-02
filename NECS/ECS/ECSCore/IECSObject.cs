using NECS.Core.Logging;
using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using System.Threading.Tasks;
using NECS.ECS.Types.AtomicType;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(1)]
    public class IECSObject
    {
        static public long Id { get; set; } = 0;
        static public long GId<T>() => EntitySerialization.TypeIdStorage[typeof(T)];
        public long instanceId = Guid.NewGuid().GuidToLongR();
        [System.NonSerialized]
        public List<IManager> connectPoints = new List<IManager>();
        public List<T> GetConnectPoints<T>() where T : IManager
        {
            var result = new List<T>();
            this.connectPoints.Where(x => x.GetType() == typeof(T)).ForEach(x => result.Add((T)x));
            return result;
        }
        [System.NonSerialized]
        public Type ObjectType;
        [System.NonSerialized]
        protected long ReflectionId = 0;
        [System.NonSerialized]
        public object SerialLocker = new object();
        [System.NonSerialized]
        public IECSObjectSerializedStateMode ChangesState = IECSObjectSerializedStateMode.NoData;
        public bool HasChildChanges = true; //after creation = yes
        public long ownerECSObjectId;

        [System.NonSerialized]
        RWLock NodeLock = new RWLock();
        [System.NonSerialized]
        public bool ChildDispose = false; //for db component may be true

        [System.NonSerialized]
        private IECSObject ownerECSObjectStorage = null;
        public IECSObject ownerECSObject {
            get{
                return ownerECSObjectStorage;
            }
            set{
                ownerECSObjectStorage = value;
                ownerECSObjectId = value.instanceId;
                OnUpdateOwner(value);
                ChangesState = IECSObjectSerializedStateMode.Changed;
            }
        }

        /// <summary>
        /// serialization container where dictionary key is child ECSObject instanceId and value is array of id path with types to real IECSObject, example idlong;cmp / idlong;ent where cmp - component, ent - entity
        /// </summary>
        public Dictionary<long, IECSObjectPathContainer> childECSObjectsId = new Dictionary<long, IECSObjectPathContainer>();
        [System.NonSerialized]
        private LockedDictionary<long, IECSObject> storagechildECSObjects;
        private LockedDictionary<long, IECSObject> childECSObjects
        {
            get
            {
                if (storagechildECSObjects == null)
                {
                    storagechildECSObjects = new LockedDictionary<long, IECSObject>();
                }
                return storagechildECSObjects;
            }
            set
            {
                storagechildECSObjects = value;
            }
        }
        
        protected virtual void OnUpdateOwner(IECSObject newOwner)
        {
            
        }

        private bool CompareChildsWithNew(Dictionary<long, List<string>> dict1, Dictionary<long, List<string>> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var key in dict1.Keys)
            {
                if (!dict2.ContainsKey(key))
                    return false;

                var list1 = dict1[key];
                var list2 = dict2[key];

                if (list1.Count != list2.Count)
                    return false;

                for (int i = 0; i < list1.Count; i++)
                {
                    if (list1[i] != list2[i])
                        return false;
                }
            }

            return true;
        }

        public void AddChildObject(IECSObject value, bool updateOwner = true)
        {
            bool isChanged = false;
            childECSObjects.ExecuteOnAddLocked(value.instanceId, value, (key, component) =>
            {
                childECSObjects[value.instanceId] = value;
                isChanged = true;
                ChangesState = IECSObjectSerializedStateMode.Changed;
            });
            if (isChanged)
            {
                if (updateOwner)
                    value.ownerECSObject = this;
                OnAddChildObject(value);
            }
            else
                NLogger.Error($"IECSObject '{instanceId}: {this.GetType().Name}': childECSObjects.ContainsKey({value.instanceId} - {value.GetType().Name})");
        }

        protected virtual void OnAddChildObject(IECSObject value)
        {
            
        }

        public bool RemoveChildObject(long key, bool updateOwner = true)
        {
            IECSObject removed = null;
            var result = childECSObjects.Remove(key, out removed);
            if(!result)
            {
                NLogger.Error($"IECSObject '{instanceId}: {this.GetType().Name}': childECSObjects.TryRemove({key})");
            }
            else
            {
                if(removed != null)
                {
                    if(updateOwner)
                        removed.ownerECSObject = null;
                    OnRemoveChildObject(removed);
                }
                ChangesState = IECSObjectSerializedStateMode.Changed;
            }
            return result;
        }

        protected virtual void OnRemoveChildObject(IECSObject value)
        {
            
        }

        public bool ContainsChildObject(long key)
        {
            return childECSObjects.ContainsKey(key);
        }

        public void ClearChildObjects()
        {
            childECSObjects.Clear();
        }

        public bool TryGetChildObject(long key, out IECSObject value)
        {
            return childECSObjects.TryGetValue(key, out value);
        }

        public bool TryGetChildObjectReadLocked(long key, out IECSObject value, out RWLock.LockToken lockToken)
        {
            return childECSObjects.TryGetLockedElement(key, out value, out lockToken);
        }

        public bool TryGetChildObjectWriteLocked(long key, out IECSObject value, out RWLock.LockToken lockToken)
        {
            return childECSObjects.TryGetLockedElement(key, out value, out lockToken, true);
        }

        public RWLock.LockToken GetLockedStorage()
        {
            return childECSObjects.LockStorage();
        }

        public void IECSDispose()
        {
            if(ChildDispose)
            {
                foreach (var childpair in childECSObjects)
                {
                    childpair.Value.ChainedIECSDispose();
                }
                ClearChildObjects();
            }
            else
            {
                foreach (var childpair in childECSObjects)
                {
                    childpair.Value.ownerECSObject = this.ownerECSObject;
                }
                ClearChildObjects();
            }
        }

        public virtual void ChainedIECSDispose()
        {
            
        }

        private void SerializationProcess()
        {
            if(ChangesState == IECSObjectSerializedStateMode.Freezed)
            {
                ChangesState = IECSObjectSerializedStateMode.NoData;
                HasChildChanges = false;
            }
            if(ChangesState == IECSObjectSerializedStateMode.Changed)
            {
                childECSObjectsId.Clear();
                foreach (var childpair in childECSObjects)
                {
                    childECSObjectsId[childpair.Key] = new IECSObjectPathContainer(){ECSObject = childpair.Value};
                }
                ChangesState = IECSObjectSerializedStateMode.Freezed;
                HasChildChanges = true;
            }
        }

        private void DeserializationProcess()
        {
            var newchildECSObjects = new ConcurrentDictionary<long, IECSObject>();
            foreach (var entry in childECSObjectsId)
            {   
                if(childECSObjects.ContainsKey(entry.Key))
                    continue;
                
                newchildECSObjects[entry.Key] = entry.Value.ECSObject;
                if(entry.Value.ECSObject != null)
                {
                    this.AddChildObject(entry.Value.ECSObject);
                }
            }
            foreach(var entry in childECSObjects)
            {
                if(!newchildECSObjects.ContainsKey(entry.Key))
                {
                    this.RemoveChildObject(entry.Key);
                }
            }
        }

        /// <summary>
        /// s
        /// </summary>
        private void AfterDeserializationChildChanges()
        {
            
        }

        /// <summary>
        /// signalise IECSObject for starting process serialization
        /// </summary>
        public void EnterToSerialization()
        {
            SerializationProcess();
            //lock(SerialLocker)
            {
                EnterToSerializationImpl();
            }
        }

        /// <summary>
        /// override this method for store property values to serializable fields
        /// </summary>
        protected virtual void EnterToSerializationImpl()
        {

        }

        public void AfterSerialization()
        {
            //lock(SerialLocker)
            {
                AfterSerializationImpl();
            }
        }

        protected virtual void AfterSerializationImpl()
        {

        }

        public void AfterDeserialization()
        {
            lock (SerialLocker)
            {
                if(HasChildChanges)
                    DeserializationProcess();
                AfterDeserializationImpl();
            }
        }
        protected virtual void AfterDeserializationImpl()
        {

        }

        public long GetId()
        {
            if (Id == 0)
                try
                {
                    if (ObjectType == null)
                    {
                        ObjectType = GetType();
                    }
                    if (ReflectionId == 0)
                        ReflectionId = ObjectType.GetCustomAttribute<TypeUidAttribute>().Id;
                    return ReflectionId;
                }
                catch
                {
                    NLogger.Error(this.GetType().ToString() + "Could not find Id field");
                    return 0;
                }
            else
                return Id;
        }

        public enum IECSObjectSerializedStateMode
        {
            NoData,
            Changed,
            Freezed
        }
    }
}
