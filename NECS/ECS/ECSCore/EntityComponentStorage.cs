
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
using NECS.Extensions.ThreadingSync;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    public class EntityComponentStorage
    {
        private ECSEntity entity;
        public static Type StorageType;
        public int ChangedComponent => changedComponents.Count;
        private readonly LockedDictionary<Type, ECSComponent> components = new LockedDictionary<Type, ECSComponent>(true);
        private readonly IDictionary<Type, int> changedComponents = new ConcurrentDictionary<Type, int>();
        public readonly IDictionary<long, Type> IdToTypeComponent = new ConcurrentDictionary<long, Type>();
        public LockedDictionary<long, object> SerializationContainer = new LockedDictionary<long, object>();
        public List<long> RemovedComponents = new List<long>();
        //public object serializationLocker = new object();
        //public object operationLocker = new object();
        [System.NonSerialized]
        public RWLock StabilizationLocker = new RWLock();
        public EntityComponentStorage(ECSEntity entity)
        {
            this.entity = entity;
            if (StorageType != null)
                StorageType = SerializationContainer.GetType();
        }

        #region serialization

        public Dictionary<long, byte[]> SlicedSerializeStorage(bool serializeOnlyChanged, bool clearChanged)
        {
            if (serializeOnlyChanged)
            {
                //using (this.StabilizationLocker.ReadLock())//lock (this.serializationLocker)
                {
                    ConcurrentDictionary<Type, ECSComponent> serializedContainer = new ConcurrentDictionary<Type, ECSComponent>();
                    Dictionary<long, byte[]> slicedComponents = new Dictionary<long, byte[]>();
                    var cachedChangedComponents = changedComponents.Keys.ToList();
                    List<Type> errorList = new List<Type>();
                    foreach (var changedComponent in cachedChangedComponents)
                    {
                        if (Defines.LogECSEntitySerializationComponents)
                        {
                            NLogger.Log($"Will serialized changed component {changedComponent} in {this.entity.AliasName}:{this.entity.instanceId}");
                        }
                        components.ExecuteReadLocked(changedComponent, (key, component) =>
                        {
                            using (MemoryStream writer = new MemoryStream())
                            {
                                var pairComponent = new KeyValuePair<long, ECSComponent>(component.GetId(), component);
                                //var component = pairComponent.Value;
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
                                if (clearChanged)
                                    changedComponents.Remove(component.GetTypeFast(), out _);
                            }
                        });
                    }
                    return slicedComponents;
                }
            }
            else
            {
                //using (this.StabilizationLocker.ReadLock())//lock (this.serializationLocker)
                {
                    Dictionary<long, byte[]> slicedComponents = new Dictionary<long, byte[]>();
                    var cacheSerializationContainerKeys = SerializationContainer.Keys.ToList();
                    foreach (var pairComponentKey in cacheSerializationContainerKeys)
                    {
                        SerializationContainer.ExecuteReadLocked(pairComponentKey, (key, pairComponent) => { 
                            using (MemoryStream writer = new MemoryStream())
                            {
                                if (!(pairComponent as ECSComponent).Unregistered)
                                {
                                    DBComponent dbComp = null;

                                    if (Defines.LogECSEntitySerializationComponents)
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
                                    if (clearChanged)
                                        changedComponents.Remove((pairComponent as ECSComponent).GetTypeFast(), out _);
                                }
                            }
                            });
                    }
                    return slicedComponents;
                }
                return null;
            }
        }

        public Dictionary<long, byte[]> SerializeStorage(bool serializeOnlyChanged, bool clearChanged) // OBSOLETE
        {
            Dictionary<long, byte[]> serializeContainer = new Dictionary<long, byte[]>();
            if (serializeOnlyChanged)
            {
                foreach (var changedComponent in changedComponents)
                {
                    var component = components[changedComponent.Key];
                    if (Defines.LogECSEntitySerializationComponents)
                    {
                        NLogger.Log($"Will serialized component {component.GetType()} in {this.entity.AliasName}:{this.entity.instanceId}");
                    }
                    serializeContainer[component.GetId()] = SerializationAdapter.SerializeECSComponent(component);
                }
            }
            else
            {
                foreach (var changedComponent in SerializationContainer)
                {
                    serializeContainer[changedComponent.Key] = SerializationAdapter.SerializeECSComponent(changedComponent.Value as ECSComponent);
                }
            }
            if (clearChanged)
                changedComponents.Clear();
            return serializeContainer;
        }

        public void DeserializeStorage(Dictionary<long, byte[]> serializedComponents)
        {
            foreach (var serComponent in serializedComponents)
            {
                this.SerializationContainer[serComponent.Key] = (ECSComponent)SerializationAdapter.DeserializeECSComponent(serComponent.Value, serComponent.Key);
            }
        }

        public Dictionary<long, string> SlicedSerializeStorageJSON(JsonSerializer serializer, bool serializeOnlyChanged, bool clearChanged)
        {

            {
                if (serializeOnlyChanged)
                {
                    using (this.StabilizationLocker.ReadLock())//lock (this.serializationLocker)
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
                    using (this.StabilizationLocker.ReadLock())//lock (this.serializationLocker)
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
                List<ECSComponent> afterDeser = new List<ECSComponent>();
                foreach (var objPair in SerializationContainer)
                {
                    ECSComponent objComponent = (ECSComponent)objPair.Value;
                    ECSComponent component;
                    if (ECSComponentManager.AllComponents.TryGetValue(objPair.Key, out component))
                    {
                        var typedComponent = (ECSComponent)Convert.ChangeType(objPair.Value, component.GetTypeFast());
                        
                        AddComponentImmediately(component.GetTypeFast(), typedComponent, true, true);
                        afterDeser.Add(typedComponent);
                    }
                }
                afterDeser.ForEach(typedComponent =>
                {
                    if (typedComponent is DBComponent)
                    {
                        //TaskEx.RunAsync(() =>
                        //{
                            (typedComponent as DBComponent).UnserializeDB();
                        //});
                    }
                    typedComponent.AfterDeserialization();
                });
            }
        }


        #endregion

        public bool CheckChanged(Type typeComponent) => changedComponents.Keys.Contains(typeComponent);
        public void DirectiveChange(Type typeComponent)
        {
            components.ExecuteReadLocked(typeComponent, (key, component) =>
            {
                changedComponents[typeComponent] = 1;
            });
        }

        #region Base functions

        public bool AddOrChangeComponentImmediately(Type comType, ECSComponent component, bool restoringMode = false, bool silent = false)
        {
            bool added = false;
            bool changed = false;
            components.ExecuteOnAddOrChangeLocked(comType, component, (key, newcomponent) => {
                AddComponentProcess(comType, newcomponent, restoringMode);
                    added = true;
            }, (key, newcomponent, oldcomponent) => {
                changed = ChangeComponentProcess(newcomponent, oldcomponent, silent, restoringMode ? this.entity : null);
                if (restoringMode)
				{
					if (newcomponent is DBComponent dBComponent)
                    {
                        this.components.UnsafeChange(key, oldcomponent);
                        (oldcomponent as DBComponent).serializedDB = (newcomponent as DBComponent).serializedDB;
                    }
				}
            });
            if (added)
            {
                if (!silent)
                {
                    components.ExecuteReadLocked(comType, (key, addedcomponent) =>
                    {
                        addedcomponent.Unregistered = false;
                        component.AddedReaction(this.entity);
                    });
                }
            }
            // else
            //     NLogger.Error("try add presented component");
            
            if (!silent && changed)
            {
                component.ChangeReaction(this.entity);
            }
            return added;
        }


        public bool AddComponentImmediately(Type comType, ECSComponent component, bool restoringMode = false, bool silent = false)
        {
            bool added = false;
            if (!this.components.Keys.Contains(comType))
            {
                components.ExecuteOnAddLocked(comType, component, (key, newcomponent) =>
                {
                    AddComponentProcess(comType, newcomponent, restoringMode);
                    added = true;
                });
            }
            if (added)
            {
                if (!silent)
                {
                    components.ExecuteReadLocked(comType, (key, addedcomponent) =>
                    {
                        addedcomponent.Unregistered = false;
                        component.AddedReaction(this.entity);
                    });
                }
            }
            else
                NLogger.Error($"try add presented component {comType.Name} into {this.entity.AliasName}:{this.entity.instanceId}");
            return added;
        }

        private void AddComponentProcess(Type comType, ECSComponent component, bool restoringMode = false)
        {
            component.ownerEntity = this.entity;
            if (this.entity != null)
                this.entity.fastEntityComponentsId.AddI(component.instanceId, 0, this.entity.SerialLocker);
            else
                NLogger.LogError("null owner entity");
            if (restoringMode)
                this.SerializationContainer.TryAdd(component.GetId(), component);
            else
                this.SerializationContainer[component.GetId()] = component;
            this.IdToTypeComponent.TryAdd(component.GetId(), component.GetTypeFast());
            //this.entity.manager.OnAddComponent(this.entity, component);
            ManagerScope.instance.entityManager.OnAddComponent(this.entity, component);
        }

        public bool ChangeComponent(ECSComponent component, bool silent = false, ECSEntity restoringOwner = null)
        {
            bool changed = false;
            components.ExecuteOnChangeLocked(component.GetTypeFast(), component, (key, chcomponent, oldcomponent) =>
                {
                    changed = ChangeComponentProcess(chcomponent,oldcomponent, silent, restoringOwner);
                }
            );
            if (!silent && changed)
            {
                component.ChangeReaction(this.entity);
            }
            return changed;
            //this.MarkComponentChanged(component, silent);
        }

        private bool ChangeComponentProcess(ECSComponent component, ECSComponent oldcomponent, bool silent = false, ECSEntity restoringOwner = null)
        {
            bool changed = false;
            if (restoringOwner != null)
                component.ownerEntity = restoringOwner;
            component.Unregistered = false;
            component.StateReactionQueue = oldcomponent.StateReactionQueue;
            if (!silent)
            {
                Type componentClass = component.GetTypeFast();
                changedComponents[componentClass] = 1;
                changed = true;
            }
            return changed;
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

        public bool MarkComponentChanged(ECSComponent component, bool serializationSilent = false, bool eventSilent = false)
        {
            bool changed = false;
            components.ExecuteOnChangeLocked(component.GetType(), component, (key, chcomponent, oldcomponent) =>
                {
                    Type componentClass = chcomponent.GetTypeFast();
                    if (!serializationSilent)
                    {
                        changedComponents[componentClass] = 1;
                        changed = true;
                    }
                }
            );
            if (!eventSilent && changed)
            {
                component.ChangeReaction(this.entity);
            }
            return changed;
        }

        public ECSComponent RemoveComponentImmediately(Type componentClass)
        {
            ECSComponent component2 = null;
            bool removed = false;

            components.ExecuteOnRemoveLocked(componentClass, out component2, (key, component) =>
            {
                RemoveComponentProcess(componentClass, component);
                removed = true;
            });
            if (removed)
            {
                component2.RemovingReaction(this.entity);
            }
            else
            {
                NLogger.LogError("try to remove non present component");
            }
            return component2;
        }

        private void RemoveComponentProcess(Type componentClass, ECSComponent component)
        {
            this.changedComponents.Remove(componentClass, out _);
            this.SerializationContainer.Remove(component.GetId(), out _);
            this.IdToTypeComponent.Remove(component.GetId(), out _);
            this.entity.fastEntityComponentsId.RemoveI(component.instanceId, this.entity.SerialLocker);
            this.RemovedComponents.Add(component.GetId());
            ManagerScope.instance.entityManager.OnRemoveComponent(this.entity, component);
        }




        public void RemoveComponentsWithGroup(long componentGroup)
        {
            List<ECSComponent> toRemoveComponent = new List<ECSComponent>();
            List<ECSComponent> notRemovedComponent = new List<ECSComponent>();
            bool exception = false;
            foreach (var component in components)
            {
                if (component.Value.ComponentGroups.TryGetValueI(componentGroup, out _, component.Value.SerialLocker))
                {
                    toRemoveComponent.Add(component.Value);
                }
            }
            toRemoveComponent.ForEach((removedComponent) =>
                {
                    this.ExecuteWriteLockedComponent(removedComponent.GetTypeFast(), (key, component) =>
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
                            removedComponent.RemovingReaction(this.entity);
                        }
                    });
                });
            if (exception)
            {
                NLogger.Error("try to remove non present component in group removing");
            }
        }

        public void FilterRemovedComponents(List<long> filterList, List<long> filteringOnlyGroups)
        {
            var bufFilterList = new List<long>(filterList);
            foreach (var component in this.components)
            {
                if (filteringOnlyGroups.Count == 0)
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
                        foreach (var componentGroup in component.Value.ComponentGroups.SnapshotI(component.Value.SerialLocker))
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




        #endregion

        #region Unsafe component functions

        public bool AddComponentUnsafe(Type componentType, ECSComponent component, bool restoringMode = false, bool silent = false)
        {
            if (this.components.UnsafeAdd(componentType, component))
            {
                AddComponentProcess(componentType, component, restoringMode);
                if (!silent)
                {
                    component.AddedReaction(this.entity);
                }
                return true;
            }
            return false;
        }

        public bool ChangeComponentUnsafe(ECSComponent component, bool silent = false, ECSEntity restoringOwner = null)
        {
            var oldcomponent = GetComponentUnsafe(component.GetTypeFast());
            if (this.components.UnsafeChange(component.GetTypeFast(), component))
            {
                ChangeComponentProcess(component, oldcomponent, silent, restoringOwner);
                if (!silent)
                {
                    component.ChangeReaction(this.entity);
                }
                return true;
            }
            return false;
        }

        public bool RemoveComponentUnsafeSilent(Type componentType)
        {
            if (this.components.UnsafeRemove(componentType, out var component))
            {
                RemoveComponentProcess(componentType, component);
                return true;
            }
            return false;
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
        #endregion

        #region Async component functions
        public void AddComponentAsync(Type componentType, ECSComponent component)
        {
            TaskEx.RunAsync(() => AddComponentImmediately(componentType, component));
        }

        public void AddComponentsAsync(IList<ECSComponent> addedComponents)
        {
            TaskEx.RunAsync(() => AddComponentsImmediately(addedComponents));
        }

        public void ChangeComponentAsync(ECSComponent component)
        {
            TaskEx.RunAsync(() => ChangeComponent(component));
        }

        public void RemoveComponentAsync(Type componentType)
        {
            TaskEx.RunAsync(() => RemoveComponentImmediately(componentType));
        }
        #endregion

        #region Additional Base functions

        public ECSComponent RemoveComponentImmediately(long componentTypeId)
        {
            return RemoveComponentImmediately(this.IdToTypeComponent[componentTypeId]);
        }

        public void AddComponentsImmediately(IList<ECSComponent> addedComponents)
        {
            addedComponents.ForEach<ECSComponent>(component => this.AddComponentImmediately(component.GetTypeFast(), component));
        }

        public void RemoveComponentsImmediately(IList<ECSComponent> removedComponents)
        {
            removedComponents.ForEach(component => this.RemoveComponentImmediately(component.GetTypeFast()));
        }

        public void RegisterAllComponents(bool previous_changed = false)
        {
            if (previous_changed)//bullshit from oldest version, need to check, but better been deleted
            {
                List<ECSComponent> changed_components = new List<ECSComponent>();
                foreach (var component in components)
                {
                    if (component.Value.Unregistered)
                    {
                        if (MarkComponentChanged(component.Value, false, true))
                        {
                            changed_components.Add(component.Value);
                        }
                    }
                }
                foreach (var component in changed_components)
                {
                    component.Unregistered = false;
                    component.AddedReaction(entity);
                    //this.entity.manager.OnAddComponent(this.entity, component);
                    ManagerScope.instance.entityManager.OnAddComponent(this.entity, component);
                }
            }
            else
            {
                foreach (var component1 in components)
                {
                    components.ExecuteReadLocked(component1.Key, (key, component) =>
                    {
                        if (component.Unregistered)
                        {
                            component.Unregistered = false;
                            component.AddedReaction(entity);
                            //this.entity.manager.OnAddComponent(this.entity, component);
                            ManagerScope.instance.entityManager.OnAddComponent(this.entity, component);
                        }
                    });
                }
            }
            this.entity.Alive = true;
        }

        public bool HasComponent(Type componentClass) =>
            this.components.ContainsKey(componentClass);

        public bool HasComponent(long componentClassId) =>
            this.IdToTypeComponent.ContainsKey(componentClassId);

        public void OnEntityDelete()
        {
            var removedComponents = this.components.ClearSnapshot();
            this.SerializationContainer.Clear();
            this.IdToTypeComponent.Clear();
            this.changedComponents.Clear();
            this.RemovedComponents.Clear();
            this.IdToTypeComponent.Clear();
            foreach (var component in removedComponents)
            {
                component.Value.OnRemove();
            }
        }

        public bool ExecuteOnNotHasComponent(Type componentType, Action action)
        {
            ECSComponent component;
            if (this.components.ExecuteOnKeyHolded(componentType, action))
            {
                return true;
            }
            return false;
        }

        public bool HoldComponentAddition(Type componentType, out RWLock.LockToken token, bool holdMode = true)
        {
            return this.components.HoldKey(componentType, out token, holdMode);
        }

        public void ExecuteReadLockedComponent(Type componentType, Action<Type, ECSComponent> action)
        {
            components.ExecuteReadLocked(componentType, action);
        }

        public void ExecuteWriteLockedComponent(Type componentType, Action<Type, ECSComponent> action)
        {
            components.ExecuteWriteLocked(componentType, action);
        }

        public bool GetReadLockedComponent(Type componentType, out ECSComponent component, out RWLock.LockToken token)
        {
            return components.TryGetLockedElement(componentType, out component, out token, false);
        }

        public bool GetWriteLockedComponent(Type componentType, out ECSComponent component, out RWLock.LockToken token)
        {
            return components.TryGetLockedElement(componentType, out component, out token, true);
        }

        public RWLock.LockToken GetWriteLockedComponentStorage()
        {
            return components.LockStorage();
        }

        #endregion

        public ICollection<Type> ComponentClasses =>
            this.components.Keys;

        public ICollection<ECSComponent> Components =>
            this.components.Values;
    }
}
