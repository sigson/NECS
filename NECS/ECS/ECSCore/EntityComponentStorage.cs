
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using NECS.Harness.Serialization;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    public class EntityComponentStorage
    {
        private ECSEntity entity;
        public static Type StorageType;
        public int ChangedComponent => changedComponents.Count;
        private readonly IDictionary<Type, ECSComponent> components = new ConcurrentDictionary<Type, ECSComponent>();
        private readonly IDictionary<Type, int> changedComponents = new ConcurrentDictionary<Type, int>();
        public readonly IDictionary<long, Type> IdToTypeComponent = new ConcurrentDictionary<long, Type>();
        public ConcurrentDictionary<long, object> SerializationContainer = new ConcurrentDictionary<long, object>();
        public List<long> RemovedComponents = new List<long>();
        public object serializationLocker = new object();
        public object operationLocker = new object();
        public EntityComponentStorage(ECSEntity entity)
        {
            this.entity = entity;
            if(StorageType != null)
                StorageType = SerializationContainer.GetType();
        }

        #region serialization

        public Dictionary<long, byte[]> SlicedSerializeStorage(bool serializeOnlyChanged, bool clearChanged)
        {
            if (serializeOnlyChanged)
            {
                lock (this.serializationLocker)
                {
                    ConcurrentDictionary<long, ECSComponent> serializeContainer = new ConcurrentDictionary<long, ECSComponent>();
                    Dictionary<long, byte[]> slicedComponents = new Dictionary<long, byte[]>();
                    var cachedChangedComponents = changedComponents.Keys.ToList();
                    List<Type> errorList = new List<Type>();
                    foreach (var changedComponent in cachedChangedComponents)
                    {
                        if(Defines.LogECSEntitySerializationComponents)
                        {
                            NLogger.Log($"Will serialized changed component {changedComponent} in {this.entity.AliasName}:{this.entity.instanceId}");
                        }
                        try
                        {
                            var component = components[changedComponent];
                            serializeContainer[component.GetId()] = component;
                        }
                        catch (Exception ex)
                        {
                            errorList.Add(changedComponent);
                        }
                    }
                    foreach (var pairComponent in serializeContainer)
                    {
                        using (MemoryStream writer = new MemoryStream())
                        {
                            var component = pairComponent.Value;
                            byte[] serializedData = null;
                            lock (component.SerialLocker)
                            {
                                component.EnterToSerialization();

                                DBComponent dBComponent = null;
                                if (component is DBComponent)
                                {
                                    dBComponent = (component as DBComponent);
                                }
                                if (dBComponent != null)
                                {
                                    dBComponent.SerializeDB(serializeOnlyChanged, clearChanged);
                                }

                                //NetSerializer.Serializer.Default.Serialize(writer, component);
                                serializedData = SerializationAdapter.SerializeECSComponent(component);

                                if (dBComponent != null)
                                {
                                    dBComponent.AfterSerializationDB();
                                }
                                component.AfterSerialization();
                            }
                            slicedComponents[pairComponent.Key] = serializedData;//writer.ToArray();
                        }
                    }
                    if (clearChanged)
                    {
                        changedComponents.Clear();
                        errorList.ForEach((errorType) => changedComponents.Add(errorType, 0));
                        if (errorList.Count > 0)
                            NLogger.LogError("serialization error");
                    }

                    return slicedComponents;
                }
            }
            else
            {
                lock (this.serializationLocker)
                {
                    Dictionary<long, byte[]> slicedComponents = new Dictionary<long, byte[]>();
                    var cacheSerializationContainerKeys = SerializationContainer.Keys.ToList();
                    foreach (var pairComponentKey in cacheSerializationContainerKeys)
                    {
                        object pairComponent;
                        if (SerializationContainer.TryGetValue(pairComponentKey, out pairComponent))
                        {
                            using (MemoryStream writer = new MemoryStream())
                            {
                                if (!(pairComponent as ECSComponent).Unregistered)
                                {
                                    DBComponent dbComp = null;

                                    if(Defines.LogECSEntitySerializationComponents)
                                    {
                                        NLogger.Log($"Will serialized component {pairComponent.GetType()} in {this.entity.AliasName}:{this.entity.instanceId}");
                                    }


                                    if (pairComponent is DBComponent)
                                    {
                                        dbComp = (pairComponent as DBComponent);
                                        dbComp.SerializeDB(serializeOnlyChanged, clearChanged);
                                    }

                                    //NetSerializer.Serializer.Default.Serialize(writer, pairComponent);
                                    var serializedData = SerializationAdapter.SerializeECSComponent((pairComponent as ECSComponent));

                                    slicedComponents[pairComponentKey] = serializedData;//writer.ToArray();
                                    if (dbComp != null)
                                    {
                                        dbComp.AfterSerializationDB();
                                    }
                                }
                            }
                        }
                    }
                    if (clearChanged)
                        changedComponents.Clear();
                    return slicedComponents;
                }
                return null;
            }
        }

        public Dictionary<long, byte[]> SerializeStorage(bool serializeOnlyChanged, bool clearChanged)
        {
            Dictionary<long, byte[]> serializeContainer = new Dictionary<long, byte[]>();
            if (serializeOnlyChanged)
            {
                foreach (var changedComponent in changedComponents)
                {
                    var component = components[changedComponent.Key];
                    if(Defines.LogECSEntitySerializationComponents)
                    {
                        NLogger.Log($"Will serialized component {component.GetType()} in {this.entity.AliasName}:{this.entity.instanceId}");
                    }
                    serializeContainer[component.GetId()] = SerializationAdapter.SerializeECSComponent(component);
                    //using (MemoryStream writer = new MemoryStream())
                    //{
                    //    var component = components[changedComponent.Key];
                    //    NetSerializer.Serializer.Default.Serialize(writer, component);
                    //    serializeContainer[component.GetId()] = writer.ToArray();
                    //}
                }
            }
            else
            {
                foreach (var changedComponent in SerializationContainer)
                {
                    serializeContainer[changedComponent.Key] = SerializationAdapter.SerializeECSComponent(changedComponent.Value as ECSComponent);
                    //using (MemoryStream writer = new MemoryStream())
                    //{
                    //    NetSerializer.Serializer.Default.Serialize(writer, changedComponent.Value);
                    //    serializeContainer[changedComponent.Key] = writer.ToArray();
                    //}
                }
            }
            if (clearChanged)
                changedComponents.Clear();
            return serializeContainer;
        }

        public void DeserializeStorage(Dictionary<long, byte[]> serializedComponents)
        {
            foreach(var serComponent in serializedComponents)
            {
                this.SerializationContainer[serComponent.Key] = (ECSComponent)SerializationAdapter.DeserializeECSComponent(serComponent.Value, serComponent.Key);
                //using (var memoryStream = new MemoryStream())
                //{
                //    memoryStream.Write(serComponent.Value, 0, serComponent.Value.Length);
                //    memoryStream.Position = 0;
                //    this.SerializationContainer[serComponent.Key] =  (ECSComponent)ReflectionCopy.MakeReverseShallowCopy(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                //}
            }
        }

        public Dictionary<long, string> SlicedSerializeStorageJSON(JsonSerializer serializer, bool serializeOnlyChanged, bool clearChanged)
        {

            {
                if (serializeOnlyChanged)
                {
                    lock (this.serializationLocker)
                    {
                        ConcurrentDictionary<long, object> serializeContainer = new ConcurrentDictionary<long, object>();
                        Dictionary<long, string> slicedComponents = new Dictionary<long, string>();
                        var cachedChangedComponents = changedComponents.Keys.ToList();
                        List<Type> errorList = new List<Type>();
                        foreach (var changedComponent in cachedChangedComponents)
                        {
                            try
                            {
                                var component = components[changedComponent];
                                if (component is DBComponent)
                                {
                                    (component as DBComponent).SerializeDB(serializeOnlyChanged, clearChanged);
                                }
                                else
                                {
                                    serializeContainer[component.GetId()] = component;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorList.Add(changedComponent);
                            }
                        }
                        foreach (var pairComponent in serializeContainer)
                        {
                            using (StringWriter writer = new StringWriter())
                            {
                                serializer.Serialize(writer, pairComponent.Value);
                                slicedComponents[pairComponent.Key] = writer.ToString();
                                if (pairComponent.Value is DBComponent)
                                {
                                    (pairComponent.Value as DBComponent).AfterSerializationDB();
                                }
                            }
                        }
                        if (clearChanged)
                        {
                            changedComponents.Clear();
                            errorList.ForEach((errorType) => changedComponents.Add(errorType, 0));
                            if (errorList.Count > 0)
                                NLogger.LogError("serialization error");
                        }

                        return slicedComponents;
                    }
                }
                else
                {
                    lock (this.serializationLocker)
                    {
                        Dictionary<long, string> slicedComponents = new Dictionary<long, string>();
                        var cacheSerializationContainerKeys = SerializationContainer.Keys.ToList();
                        foreach (var pairComponentKey in cacheSerializationContainerKeys)
                        {
                            object pairComponent;
                            if (SerializationContainer.TryGetValue(pairComponentKey, out pairComponent))
                            {
                                using (StringWriter writer = new StringWriter())
                                {
                                    if (!(pairComponent as ECSComponent).Unregistered)
                                    {
                                        DBComponent dbComp = null;
                                        if (pairComponent is DBComponent)
                                        {
                                            dbComp = (pairComponent as DBComponent);
                                            dbComp.SerializeDB(serializeOnlyChanged, clearChanged);
                                        }
                                        serializer.Serialize(writer, pairComponent);
                                        slicedComponents[pairComponentKey] = writer.ToString();
                                        if (dbComp != null)
                                        {
                                            dbComp.AfterSerializationDB();
                                        }
                                    }
                                }
                            }
                        }
                        if (clearChanged)
                            changedComponents.Clear();
                        return slicedComponents;
                    }
                    return null;
                }
            }
        }

        public string SerializeStorageJSON(JsonSerializer serializer, bool serializeOnlyChanged, bool clearChanged)
        {
            using (StringWriter writer = new StringWriter())
            {
                if (serializeOnlyChanged)
                {
                    ConcurrentDictionary<long, object> serializeContainer = new ConcurrentDictionary<long, object>();
                    foreach (var changedComponent in changedComponents)
                    {
                        var component = components[changedComponent.Key];
                        serializeContainer[component.GetId()] = component;
                    }
                    serializer.Serialize(writer, serializeContainer);
                }
                else
                    serializer.Serialize(writer, SerializationContainer);
                if (clearChanged)
                    changedComponents.Clear();
                return writer.ToString();
            }
        }



        public void RestoreComponentsAfterSerialization(ECSEntity entity)
        {
            this.entity = entity;
            if (components.Count == 0)
            {
                foreach (var objPair in SerializationContainer)
                {
                    ECSComponent objComponent = (ECSComponent)objPair.Value;
                    ECSComponent component;
                    if (ECSComponentManager.AllComponents.TryGetValue(objPair.Key, out component))
                    {
                        var typedComponent = (ECSComponent)Convert.ChangeType(objPair.Value, component.GetTypeFast());
                        if (typedComponent is DBComponent)
                            TaskEx.RunAsync(() =>
                            {
                                (typedComponent as DBComponent).UnserializeDB();
                            });
                        AddComponentImmediately(component.GetTypeFast(), typedComponent, true, true);
                        typedComponent.AfterDeserialization();
                    }
                }
            }
        }


        #endregion

        public bool CheckChanged(Type typeComponent) => changedComponents.Keys.Contains(typeComponent);
        public void DirectiveChange(Type typeComponent)
        {
            lock (this.serializationLocker)
            {
                lock (this.operationLocker)
                {
                    if (components.Keys.Contains(typeComponent))
                    {
                        changedComponents[typeComponent] = 1;
                    }

                }

            }
        }
        
        public void AddComponentImmediately(Type comType, ECSComponent component, bool restoringMode = false, bool silent = false)
        {
            bool exception = false;
            lock (this.serializationLocker)
            {
                lock(this.operationLocker)
                {
                    if (this.components.Keys.Contains(comType))
                    {
                        exception = true;
                    }
                    else
                    {
                        component.ownerEntity = this.entity;
                        this.components.Add(comType, component);
                        if (this.entity != null)
                            this.entity.fastEntityComponentsId.AddI(component.instanceId, 0, this.entity.SerialLocker);
                        else
                            NLogger.LogError("null owner entity");
                        if (restoringMode)
                            this.SerializationContainer.TryAdd(component.GetId(), component);
                        else
                            this.SerializationContainer[component.GetId()] = component;
                        this.IdToTypeComponent.TryAdd(component.GetId(), component.GetTypeFast());
                    }
                }
            }
            if(exception)
            {
                NLogger.Error("try add presented component");
                throw new Exception("try add presented component");
            }
            else
            {
                if (!silent)
                {
                    component.Unregistered = false;
                    component.OnAdded(this.entity);
                }
            }
            
        }

        public void RegisterAllComponents()
        {
            foreach(var component in components)
            {
                if(component.Value.Unregistered)
                {
                    component.Value.MarkAsChanged(false, true);
                }
            }
            foreach (var component in components)
            {
                if(component.Value.Unregistered)
                {
                    component.Value.Unregistered = false;
                    component.Value.OnAdded(entity);
                    this.entity.manager.OnAddComponent(this.entity, component.Value);
                }
            }
            this.entity.Alive = true;
        }

        public void AddComponentsImmediately(IList<ECSComponent> addedComponents)
        {
            addedComponents.ForEach<ECSComponent>(component => this.AddComponentImmediately(component.GetTypeFast(), component));
        }

        public void MarkComponentChanged(ECSComponent component, bool serializationSilent = false, bool eventSilent = false)
        {
            lock (this.serializationLocker)
            {
                lock (this.operationLocker)
                {
                    Type componentClass = component.GetTypeFast();
                    if (!serializationSilent)
                    {
                        changedComponents[componentClass] = 1;
                    }
                }

            }
            if(!eventSilent)
            {
                TaskEx.RunAsync(() =>
                {
                    component.RunOnChangeCallbacks(this.entity);
                });
            }
        }

        public void ChangeComponent(ECSComponent component, bool silent = false, ECSEntity restoringOwner = null)
        {
            Type componentClass = component.GetTypeFast();
            lock (this.serializationLocker)
            {
                lock (this.operationLocker)
                {
                    if (restoringOwner != null)
                        component.ownerEntity = restoringOwner;
                    component.Unregistered = false;//for afterserialize changing
                    this.components[componentClass] = component;
                }
            }
            this.MarkComponentChanged(component, silent);
        }

        public ECSComponent GetComponent(Type componentClass)
        {
            ECSComponent component = null;
            try
            {
                component = this.components[componentClass];
            }
            catch (Exception ex)
            {
                if (ex is KeyNotFoundException && Defines.HiddenKeyNotFoundLog)
                    NLogger.Error(ex.Message + "  \n" + ex.StackTrace);
            }
            return component;
        }

        public ECSComponent GetComponent(long componentTypeId)
        {
            ECSComponent component = null;
            try
            {
                component = this.components[this.IdToTypeComponent[componentTypeId]];
            }
            catch (Exception ex)
            {
                if (ex is KeyNotFoundException && Defines.HiddenKeyNotFoundLog)
                    NLogger.Error(ex.Message + "  \n" + ex.StackTrace);
            }
            return component;
        }

        public ECSComponent GetComponentUnsafe(Type componentType)
        {
            ECSComponent component;
            return (!this.components.TryGetValue(componentType, out component) ? null : component);
        }

        public ECSComponent GetComponentUnsafe(long componentTypeId)
        {
            ECSComponent component;
            return (!this.components.TryGetValue(this.IdToTypeComponent[componentTypeId], out component) ? null : component);
        }

        public bool HasComponent(Type componentClass) =>
            this.components.ContainsKey(componentClass);

        public bool HasComponent(long componentClassId) =>
            this.IdToTypeComponent.ContainsKey(componentClassId);

        public void OnEntityDelete()
        {
            lock (this.serializationLocker)
            {
                lock (this.operationLocker)
                {
                    foreach (var component in this.components.Values)
                    {
                        component.OnRemove();
                    }
                    this.components.Clear();
                    this.SerializationContainer.Clear();
                    this.IdToTypeComponent.Clear();
                    this.changedComponents.Clear();
                    this.RemovedComponents.Clear();
                    this.IdToTypeComponent.Clear();
                }
            }
        }

        public ECSComponent RemoveComponentImmediately(long componentTypeId)
        {
            return RemoveComponentImmediately(this.IdToTypeComponent[componentTypeId]);
        }

        public void RemoveComponentsWithGroup(long componentGroup)
        {
            List<ECSComponent> toRemoveComponent = new List<ECSComponent>();
            List<ECSComponent> notRemovedComponent = new List<ECSComponent>();
            bool exception = false;
            lock (this.serializationLocker)
            {     
                foreach (var component in components)
                {
                    if (component.Value.ComponentGroups.TryGetValueI(componentGroup, out _, component.Value.SerialLocker))
                    {
                        toRemoveComponent.Add(component.Value);
                    }
                }
                lock (this.operationLocker)
                {
                    toRemoveComponent.ForEach((removedComponent) =>
                    {
                        if (!this.components.Keys.Contains(removedComponent.GetTypeFast()))
                        {
                            exception = true;
                            notRemovedComponent.Add(removedComponent);
                        }
                        else
                        {
                            this.changedComponents.Remove(removedComponent.GetTypeFast(), out _);
                            this.entity.fastEntityComponentsId.RemoveI(removedComponent.instanceId, this.entity.SerialLocker);
                            this.components.Remove(removedComponent.GetTypeFast());
                            this.SerializationContainer.Remove(removedComponent.GetId(), out _);
                            this.IdToTypeComponent.Remove(removedComponent.GetId(), out _);
                            this.RemovedComponents.Add(removedComponent.GetId());
                            ManagerScope.instance.entityManager.OnRemoveComponent(this.entity, removedComponent);
                        }
                    });
                }
            }
            if(exception)
            {
                NLogger.Error("try to remove non present component in group removing");
            }
            toRemoveComponent.ForEach((removedComponent) =>
            {
                if(!notRemovedComponent.Contains(removedComponent))
                    removedComponent.OnRemoving(this.entity);
            });
        }

        public void FilterRemovedComponents(List<long> filterList, List<long> filteringOnlyGroups)
        {
            var bufFilterList = new List<long>(filterList);
            foreach(var component in this.components)
            {
                if(filteringOnlyGroups.Count == 0)
                {
                    var id = component.Value.instanceId;
                    bool finded = false;
                    int i;
                    for (i = 0; i < bufFilterList.Count; i++)
                    {
                        if (id == bufFilterList[i])
                        {
                            finded = true;
                        }
                    }
                    if (!finded)
                    {
                        this.RemoveComponentImmediately(component.Key);
                    }
                }
                else
                {
                    foreach (var group in filteringOnlyGroups)
                    {
                        foreach(var componentGroup in component.Value.ComponentGroups.SnapshotI(component.Value.SerialLocker))
                        {
                            if (componentGroup.Key == group)
                            {
                                var id = component.Value.instanceId;
                                bool finded = false;
                                int i;
                                for (i = 0; i < bufFilterList.Count; i++)
                                {
                                    if (id == bufFilterList[i])
                                    {
                                        finded = true;
                                    }
                                }
                                if (!finded)
                                {
                                    this.RemoveComponentImmediately(component.Key);
                                }
                            }
                        }
                    }
                }
            }
        }

        public ECSComponent RemoveComponentImmediately(Type componentClass)
        {
            ECSComponent component2 = null;
            bool exception = false;
            lock (this.serializationLocker)
            {
                lock (this.operationLocker)
                {
                    if (!this.components.Keys.Contains(componentClass))
                    {
                        exception = true;
                    }
                    else
                    {
                        ECSComponent component = this.components[componentClass];
                        this.changedComponents.Remove(componentClass, out _);
                        this.components.Remove(componentClass);
                        this.SerializationContainer.Remove(component.GetId(), out _);
                        this.IdToTypeComponent.Remove(component.GetId(), out _);
                        this.entity.fastEntityComponentsId.RemoveI(component.instanceId, this.entity.SerialLocker);
                        this.RemovedComponents.Add(component.GetId());
                        ManagerScope.instance.entityManager.OnRemoveComponent(this.entity, component);
                        component2 = component;
                    }
                    
                }
                
            }
            if(exception)
            {
                NLogger.LogError("try to remove non present component");
                throw new Exception("try to remove non present component");
            }
            else
            {
                component2.OnRemoving(this.entity);
            }
            return component2;
        }


        public ICollection<Type> ComponentClasses =>
            this.components.Keys;

        public ICollection<ECSComponent> Components =>
            this.components.Values;
    }
}
