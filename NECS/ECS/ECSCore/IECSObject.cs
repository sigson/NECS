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

        private ConcurrentDictionary<long, IECSObject> childECSObjects = new ConcurrentDictionary<long, IECSObject>();
        
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

        public void AddChildObject(long key, IECSObject value, bool updateOwner = true)
        {
            lock(childECSObjects)
            {
                if(childECSObjects.ContainsKey(key))
                {
                    NLogger.Error($"IECSObject '{instanceId}: {this.GetType().Name}': childECSObjects.ContainsKey({key} - {value.GetType().Name})");
                }
                else
                {
                    childECSObjects[key] = value;
                    if(updateOwner)
                        value.ownerECSObject = this;
                    OnAddChildObject(key, value);
                    ChangesState = IECSObjectSerializedStateMode.Changed;
                }
            }
        }

        protected virtual void OnAddChildObject(long key, IECSObject value)
        {
            
        }

        public bool RemoveChildObject(long key, bool updateOwner = true)
        {
            IECSObject removed = null;
            var result = childECSObjects.TryRemove(key, out removed);
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
                    OnRemoveChildObject(key, removed);
                }
                ChangesState = IECSObjectSerializedStateMode.Changed;
            }
            return result;
        }

        protected virtual void OnRemoveChildObject(long key, IECSObject value)
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
                    this.AddChildObject(entry.Key, entry.Value.ECSObject);
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
