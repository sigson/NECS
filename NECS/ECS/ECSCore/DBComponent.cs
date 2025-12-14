using NECS.Core.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static NECS.ECS.ECSCore.ComponentsDBComponent;
using NECS.ECS.Types.AtomicType;
using NECS.Extensions.ThreadingSync;
using NECS.Harness.Services;
using System.Diagnostics;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(11)]
    public class DBComponent : ECSComponent
    {
        static public new long Id { get; set; } = 11;
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }

        // Logging configuration
        [NonSerialized]
        [JsonIgnore]
        public DBLoggingLevel LoggingLevel = DBLoggingLevel.None;

        public Dictionary<IECSObjectPathContainer, List<dbRow>> serializedDB = new Dictionary<IECSObjectPathContainer, List<dbRow>>();

        //public bool fullSerialized = true;

        public virtual SharedLock.LockToken SerializeDB(bool serializeOnlyChanged = false, bool clearChanged = true)
        {
            return null;
        }
        
        public virtual void AfterSerializationDB(bool clearAfterSerializaion = true)
        {

        }
        
        public virtual void UnserializeDB(bool retryNullEntityOwner = false)
        {

        }

        public virtual void AfterDeserializeDB()
        { }
    }

    [System.Serializable]
    public class dbRow
    {
        //[System.NonSerialized]
        public long componentInstanceId;
        public long componentId;
        public object component;
        public ComponentState componentState;
    }

    // Logging level enum
    public enum DBLoggingLevel
    {
        None = 0,           // No logging
        CountOnly = 1,      // Only element counts
        CountAndTypes = 2,  // Counts and component types
        Full = 3           // Counts, types, and operation results
    }

    [System.Serializable]
    [TypeUid(12)]
    public class ComponentsDBComponent : DBComponent
    {
        static public new long Id { get; set; } = 12;
        static public new System.Collections.Generic.List<System.Action> StaticOnChangeHandlers { get; set; }

        public enum ComponentState
        {
            Created,
            Changed,
            Removed,
            Null
        }

        [System.NonSerialized]
        public Dictionary<long, Dictionary<long, (ECSComponent, ComponentState)>> DB = new Dictionary<long, Dictionary<long, (ECSComponent, ComponentState)>>();
        [System.NonSerialized]
        public Dictionary<long, long> ComponentOwners = new Dictionary<long, long>();
        [System.NonSerialized]
        public Dictionary<long, IECSObjectPathContainer> OwnerPaths = new Dictionary<long, IECSObjectPathContainer>();
        [System.NonSerialized]
        public Dictionary<long, int> ChangedComponents = new Dictionary<long, int>();

        #region Logging Helper Methods

        private void LogDBState(string operation, List<(ECSComponent component, ComponentState state, string action)> changes = null)
        {
            if (LoggingLevel == DBLoggingLevel.None) return;

            int totalComponents = 0;
            Dictionary<Type, int> componentCounts = new Dictionary<Type, int>();
            Dictionary<Type, Dictionary<ComponentState, int>> statesByType = new Dictionary<Type, Dictionary<ComponentState, int>>();

            // Count all components and their states
            foreach (var owner in DB)
            {
                foreach (var comp in owner.Value)
                {
                    if (comp.Value.Item2 != ComponentState.Removed)
                    {
                        totalComponents++;
                        Type compType = comp.Value.Item1.GetType();
                        
                        if (!componentCounts.ContainsKey(compType))
                            componentCounts[compType] = 0;
                        componentCounts[compType]++;

                        if (!statesByType.ContainsKey(compType))
                            statesByType[compType] = new Dictionary<ComponentState, int>();
                        
                        if (!statesByType[compType].ContainsKey(comp.Value.Item2))
                            statesByType[compType][comp.Value.Item2] = 0;
                        statesByType[compType][comp.Value.Item2]++;
                    }
                }
            }

            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"[DB Operation: {operation}]");

            // Level 1: Count only
            logMessage.AppendLine($"Total Components: {totalComponents}");
            logMessage.AppendLine($"Total Owners: {DB.Count}");
            logMessage.AppendLine($"Changed Components: {ChangedComponents.Count}");

            // Level 2: Count and types
            if (LoggingLevel >= DBLoggingLevel.CountAndTypes)
            {
                logMessage.AppendLine("Components by Type:");
                foreach (var kvp in componentCounts.OrderBy(x => x.Key.Name))
                {
                    logMessage.AppendLine($"  - {kvp.Key.Name}: {kvp.Value}");
                }
            }

            // Level 3: Full details with operation results
            if (LoggingLevel >= DBLoggingLevel.Full)
            {
                if (changes != null && changes.Count > 0)
                {
                    logMessage.AppendLine("Operation Results:");
                    
                    var grouped = changes.GroupBy(x => new { Type = x.component.GetType(), Action = x.action });
                    foreach (var group in grouped)
                    {
                        logMessage.AppendLine($"  - {group.Key.Action} {group.Key.Type.Name} <{group.Count()}>");
                    }
                }

                logMessage.AppendLine("Component States by Type:");
                foreach (var typeStates in statesByType.OrderBy(x => x.Key.Name))
                {
                    logMessage.Append($"  - {typeStates.Key.Name}: ");
                    var states = typeStates.Value.Select(x => $"{x.Key}={x.Value}");
                    logMessage.AppendLine(string.Join(", ", states));
                }
            }

            NLogger.Log(logMessage.ToString());
        }

        private string GetComponentTypeName(ECSComponent component)
        {
            return component?.GetType().Name ?? "Unknown";
        }

        #endregion

        #region add methods

        public virtual void AddComponent(IECSObject ownerComponent, ECSComponent component)
        {
            Dictionary<long, (ECSComponent, ComponentState)> components = new Dictionary<long, (ECSComponent, ComponentState)>();
            ECSComponent addedComponent = null;
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    DB.TryGetValue(ownerComponent.instanceId, out components);
                    if (components == null)
                        components = new Dictionary<long, (ECSComponent, ComponentState)>();
                    if (ownerComponent is ECSEntity eCSEntity)
                    {
                        component.ownerEntity = eCSEntity;
                    }
                    else if (ownerComponent is ECSComponent eCSComponent)
                    {
                        component.ownerEntity = eCSComponent.ownerEntity;
                    }
                    component.ownerDB = this;
                    components[component.instanceId] = (component, ComponentState.Created);
                    DB[ownerComponent.instanceId] = components;
                    ComponentOwners[component.instanceId] = ownerComponent.instanceId;
                    if(!OwnerPaths.ContainsKey(ownerComponent.instanceId))
                    {
                        OwnerPaths[ownerComponent.instanceId] = new IECSObjectPathContainer(true){ECSObject = ownerComponent};
                    }
                    ChangedComponents[component.instanceId] = 1;
                    addedComponent = component;
                    changes.Add((component, ComponentState.Created, "Added"));
                    
                    LogDBState($"AddComponent({GetComponentTypeName(component)})", changes);
                }
            }
            addedComponent.AddedReaction(addedComponent.ownerEntity);
        }

        public virtual void AddOrChangeComponent(IECSObject ownerComponent, ECSComponent component)
        {
            Dictionary<long, (ECSComponent, ComponentState)> components = new Dictionary<long, (ECSComponent, ComponentState)>();
            bool change = false;
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    DB.TryGetValue(ownerComponent.instanceId, out components);
                    if (components == null)
                        components = new Dictionary<long, (ECSComponent, ComponentState)>();
                    if (components.ContainsKey(component.instanceId))
                    {
                        change = true;
                        changes.Add((component, ComponentState.Changed, "Changed"));
                    }
                    else
                    {
                        if(ownerComponent is ECSEntity eCSEntity)
                        {
                            component.ownerEntity = eCSEntity;
                        }
                        else if (ownerComponent is ECSComponent eCSComponent)
                        {
                            component.ownerEntity = eCSComponent.ownerEntity;
                        }
                        component.ownerDB = this;
                        components[component.instanceId] = (component, ComponentState.Created);
                        ComponentOwners[component.instanceId] = ownerComponent.instanceId;
                        if(!OwnerPaths.ContainsKey(ownerComponent.instanceId))
                        {
                            OwnerPaths[ownerComponent.instanceId] = new IECSObjectPathContainer(true){ECSObject = ownerComponent};
                        }
                        ChangedComponents[component.instanceId] = 1;
                        changes.Add((component, ComponentState.Created, "Added"));
                    }
                    DB[ownerComponent.instanceId] = components;
                    
                    LogDBState($"AddOrChangeComponent({GetComponentTypeName(component)})", changes);
                }
            }
            if(change)
                ChangeComponent(component, ownerComponent);
            else
            {
                if (ownerComponent is ECSEntity eCSEntity)
                {
                    component.AddedReaction(eCSEntity);
                }
                else if (ownerComponent is ECSComponent eCSComponent)
                {
                    component.AddedReaction(eCSComponent.ownerEntity);
                }
            }
        }

        public virtual void AddComponents(IECSObject ownerComponent, params ECSComponent[] component)
        {
            var changes = new List<(ECSComponent, ComponentState, string)>();
            foreach(var comp in component)
            {
                AddComponent(ownerComponent, comp);
            }
        }

        public virtual void AddComponents(IECSObject ownerComponent, List<ECSComponent> component)
        {
            foreach (var comp in component)
            {
                AddComponent(ownerComponent, comp);
            }
        }

        public virtual void AddComponent(IECSObjectPathContainer ownerComponentId, ECSComponent component)
        {
            AddComponent(ownerComponentId.ECSObject, component);
        }

        public virtual void AddOrChangeComponent(IECSObjectPathContainer ownerComponentId, ECSComponent component)
        {
            AddOrChangeComponent(ownerComponentId.ECSObject, component);
        }

        public virtual void AddComponents(IECSObjectPathContainer ownerComponentId, params ECSComponent[] component)
        {
            foreach (var comp in component)
            {
                AddComponent(ownerComponentId.ECSObject, comp);
            }
        }

        public virtual void AddComponents(IECSObjectPathContainer ownerComponentId, List<ECSComponent> component)
        {
            foreach (var comp in component)
            {
                AddComponent(ownerComponentId.ECSObject, comp);
            }
        }

        #endregion

        #region edit methods
        
        public virtual (ECSComponent, ComponentState) GetComponent(long componentId, IECSObject ownerComponent = null)
        {
            using (this.monoLocker.Lock())
            {
                long owner = 0;
                if (ownerComponent == null)
                {
                    if (!ComponentOwners.TryGetValue(componentId, out owner))
                    {
                        if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client && new StackTrace().ToString().Contains("UnserializeDB"))
                        {
                            NLogger.Log("SETUP_UNSERIALIZE error get component from db");
                        }
                        else
                        {
                            NLogger.LogErrorDB("error get component from db");
                        }
                        return (null, ComponentState.Null);
                    }
                }
                else
                {
                    owner = ownerComponent.instanceId;
                }
                (ECSComponent, ComponentState) comp;
                if (DB[owner].TryGetValue(componentId, out comp) && comp.Item2 != ComponentState.Removed)
                {
                    if (LoggingLevel >= DBLoggingLevel.Full)
                    {
                        NLogger.Log($"[DB GetComponent] Retrieved {GetComponentTypeName(comp.Item1)} (State: {comp.Item2})");
                    }
                    return comp;
                }
                else
                {
                    NLogger.LogErrorDB("error get component from db");
                    return (null, ComponentState.Null);
                }
            }
        }

        public virtual List<(ECSComponent, ComponentState)> GetComponentsByType<T>(IECSObject ownerComponent = null)
        {
            return this.GetComponentsByType(new List<long>() { typeof(T).IdToECSType() }, ownerComponent);
        }

        public virtual List<(ECSComponent, ComponentState)> GetComponentsByType(List<long> componentTypeId, IECSObject ownerComponent = null)
        {
            List<(ECSComponent, ComponentState)> result = new List<(ECSComponent, ComponentState)>();
            using (this.monoLocker.Lock())
            {
                List<long> owners = new List<long>();
                if (ownerComponent == null)
                {
                    owners = DB.Keys.ToList();
                }
                else
                {
                    if (DB.ContainsKey(ownerComponent.instanceId))
                        owners.Add(ownerComponent.instanceId);
                }
                foreach (var dbOwner in owners)
                {
                    var components = DB[dbOwner];
                    foreach(var comp in components)
                    {
                        if(comp.Value.Item2 != ComponentState.Removed && componentTypeId.Contains(comp.Value.Item1.GetId()))
                        {
                            result.Add(comp.Value);
                        }
                    }
                }
                
                if (LoggingLevel >= DBLoggingLevel.Full)
                {
                    NLogger.Log($"[DB GetComponentsByType] Found {result.Count} components");
                }
            }
            return result;
        }

        public virtual void ChangeComponent(ECSComponent component, IECSObject ownerComponent = null)
        {
            if(!ComponentOwners.ContainsKey(component.instanceId))
            {
                NLogger.LogErrorDB("error change component from db");
                return;
            }
            
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    long owner = 0;
                    if (ownerComponent == null)
                    {
                        if (!ComponentOwners.TryGetValue(component.instanceId, out owner))
                        {
                            NLogger.LogErrorDB("error change component from db");
                        }
                    }
                    else
                        owner = ownerComponent.instanceId;
                    DB[owner][component.instanceId] = (component, ComponentState.Changed);
                    ChangedComponents[component.instanceId] = 1;
                    changes.Add((component, ComponentState.Changed, "Changed"));
                    
                    LogDBState($"ChangeComponent({GetComponentTypeName(component)})", changes);
                }
            }
            
        }
        
        #endregion

        #region remove methods

        public virtual void RemoveComponent(long componentId, IECSObject ownerComponent = null)
        {
            if (!ComponentOwners.ContainsKey(componentId))
            {
                NLogger.LogErrorDB("error remove component from db");
                return;
            }
            ECSComponent removedComponent = null;
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    long owner = 0;
                    if (ownerComponent == null)
                    {
                        if (!ComponentOwners.TryGetValue(componentId, out owner))
                        {
                            NLogger.LogErrorDB("error remove component from db");
                        }
                    }
                    else
                        owner = ownerComponent.instanceId;
                    (ECSComponent, ComponentState) comp;
                    if (DB[owner].TryGetValue(componentId, out comp))
                    {
                        DB[owner][componentId] = (comp.Item1, ComponentState.Removed);
                        ChangedComponents[componentId] = 1;
                        removedComponent = comp.Item1;
                        changes.Add((comp.Item1, ComponentState.Removed, "Removed"));
                        
                        LogDBState($"RemoveComponent({GetComponentTypeName(comp.Item1)})", changes);
                    }
                    else
                    {
                        NLogger.LogErrorDB("error remove component from db");
                    }
                }
            }
            if(removedComponent != null)
            {
                removedComponent.RemovingReaction(removedComponent.ownerEntity);
            }
        }

        public virtual void RemoveComponent(params long[] componentsId)
        {
            foreach(var comp in componentsId)
            {
                RemoveComponent(comp);
            }
        }

        public virtual void RemoveComponent(List<long> componentsId, IECSObject ownerComponent = null)
        {
            foreach (var comp in componentsId)
            {
                RemoveComponent(comp);
            }
        }

        public virtual void RemoveComponent(List<ECSComponent> components, IECSObject ownerComponent = null)
        {
            foreach (var comp in components)
            {
                RemoveComponent(comp.instanceId);
            }
        }

        public virtual void RemoveComponentsByType(List<Type> componentTypeId, bool includeInherit, List<IECSObject> ownerComponent = null)
        {
            List<ECSComponent> removedComponents = new List<ECSComponent>();
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    List<long> owners = new List<long>();
                    if (ownerComponent == null)
                    {
                        owners = DB.Keys.ToList();
                    }
                    else
                    {
                        ownerComponent.ForEach(x =>
                        {
                            if (DB.ContainsKey(x.instanceId))
                                owners.Add(x.instanceId);
                        });
                    }
                    foreach (var dbOwner in owners)
                    {
                        var components = DB[dbOwner];
                        List<(ECSComponent, ComponentState)> removeList = new List<(ECSComponent, ComponentState)>();
                        foreach (var comp in components)
                        {
                            foreach (var removableType in componentTypeId)
                            {
                                if (comp.Value.Item1.GetType() == removableType || (includeInherit && comp.Value.Item1.GetType().IsSubclassOf(removableType)))
                                {
                                    removeList.Add(comp.Value);
                                }
                            }
                        }
                        foreach (var removedComp in removeList)
                        {
                            components[removedComp.Item1.instanceId] = (removedComp.Item1, ComponentState.Removed);
                            ChangedComponents[removedComp.Item1.instanceId] = 1;
                            removedComponents.Add(removedComp.Item1);
                            changes.Add((removedComp.Item1, ComponentState.Removed, "Removed"));
                        }
                        DB[dbOwner] = components;
                    }
                    
                    LogDBState($"RemoveComponentsByType({string.Join(", ", componentTypeId.Select(t => t.Name))})", changes);
                }
            }
            removedComponents.ForEach(x => {
                x.RemovingReaction(x.ownerEntity);
            });
        }

        public virtual void RemoveComponentsByType(List<long> componentTypeId, List<IECSObject> ownerComponent = null)
        {
            List<ECSComponent> removedComponents = new List<ECSComponent>();
            var changes = new List<(ECSComponent, ComponentState, string)>();
            
            using(ownerEntity.entityComponents.StabilizationLocker.WriteLock())
            {
                using (this.monoLocker.Lock())
                {
                    List<long> owners = new List<long>();
                    if (ownerComponent == null)
                    {
                        owners = DB.Keys.ToList();
                    }
                    else
                    {
                        ownerComponent.ForEach(x =>
                        {
                            if (DB.ContainsKey(x.instanceId))
                                owners.Add(x.instanceId);
                        });
                    }
                    foreach (var dbOwner in owners)
                    {
                        var components = DB[dbOwner];
                        List<(ECSComponent, ComponentState)> removeList = new List<(ECSComponent, ComponentState)>();
                        foreach (var comp in components)
                        {
                            if (componentTypeId.Contains(comp.Value.Item1.GetId()))
                            {
                                removeList.Add(comp.Value);
                            }
                        }
                        foreach (var removedComp in removeList)
                        {
                            components[removedComp.Item1.instanceId] = (removedComp.Item1, ComponentState.Removed);
                            ChangedComponents[removedComp.Item1.instanceId] = 1;
                            removedComponents.Add(removedComp.Item1);
                            changes.Add((removedComp.Item1, ComponentState.Removed, "Removed"));
                        }
                        DB[dbOwner] = components;
                    }
                    
                    LogDBState($"RemoveComponentsByType(IDs: {string.Join(", ", componentTypeId)})", changes);
                }
            }
            removedComponents.ForEach(x => {
                x.RemovingReaction(x.ownerEntity);
            });
        }

        public void RemoveComponentsByOwner(long instanceId)
        {
            try
            {
                var changes = new List<(ECSComponent, ComponentState, string)>();
                //var dbsnap = this.DB.SnapshotI(this.SerialLocker)[instanceId];
                if(this.DB.SnapshotI(this.SerialLocker).TryGetValue(instanceId, out var dbsnap))
                {
                    foreach (var inter in dbsnap.ToList())
                    {
                        changes.Add((inter.Value.Item1, ComponentState.Removed, "Removed"));
                        this.RemoveComponent(inter.Value.Item1.instanceId);
                    }
                    
                    if (LoggingLevel >= DBLoggingLevel.CountAndTypes)
                    {
                        LogDBState($"RemoveComponentsByOwner(Owner: {instanceId})", changes);
                    }
                }
            }
            catch (Exception ex)
            {
                NLogger.LogErrorDB($"error remove components from db by owner {ex.Message} \n [[[[[[[[[{ex.StackTrace}]]]]]]]]]");
            }
        }

        public virtual void ClearDB()
        {
            try
            {
                var changes = new List<(ECSComponent, ComponentState, string)>();
                var dbsnap = this.DB.SnapshotI(this.SerialLocker);
                
                foreach (var dbinter in dbsnap)
                {
                    foreach (var inter in dbinter.Value.SnapshotI(this.SerialLocker))
                    {
                        changes.Add((inter.Value.Item1, ComponentState.Removed, "Cleared"));
                        this.RemoveComponent(inter.Value.Item1.instanceId);
                    }
                }
                
                LogDBState("ClearDB", changes);
            }
            catch (Exception e)
            {
                NLogger.LogErrorDB("error remove components from db by owner");
            }
        }

        #endregion

        public override SharedLock.LockToken SerializeDB(bool serializeOnlyChanged = false, bool clearChanged = true)
        {
            Dictionary<IECSObjectPathContainer, List<dbRow>> newSerializedDB = new Dictionary<IECSObjectPathContainer, List<dbRow>>();
            var sharelock = this.monoLocker.Lock();
            //using (this.monoLocker.Lock())
            {
                serializedDB.Clear();
                List<long> errorChanged = new List<long>();
                
                if (LoggingLevel >= DBLoggingLevel.CountOnly)
                {
                    NLogger.Log($"[DB SerializeDB] Starting serialization (OnlyChanged: {serializeOnlyChanged}, ClearChanged: {clearChanged})");
                }
                
                if (serializeOnlyChanged)
                {
                    Dictionary<long, List<dbRow>> serializedComp = new Dictionary<long, List<dbRow>>();
                    Dictionary<IECSObjectPathContainer, List<dbRow>> serializedCompPath = new Dictionary<IECSObjectPathContainer, List<dbRow>>();

                    foreach (var changedComponent in ChangedComponents)
                    {
                        try
                        {
                            var ownerId = ComponentOwners[changedComponent.Key];
                            var component = DB[ownerId][changedComponent.Key];

                            List<dbRow> components = null;
                            serializedComp.TryGetValue(ownerId, out components);
                            if (components == null)
                                components = new List<dbRow>();
                            component.Item1.EnterToSerialization();
                            components.Add(new dbRow()
                            {
                                component = component.Item1,
                                componentInstanceId = component.Item1.instanceId,
                                componentId = component.Item1.GetId(),
                                componentState = component.Item2
                            });
                            serializedComp[ownerId] = components;
                            serializedCompPath[this.OwnerPaths[ownerId]] = components;
                        }
                        catch (Exception ex)
                        {
                            errorChanged.Add(changedComponent.Key);
                        }
                    }
                    newSerializedDB = serializedCompPath;
                }
                else
                {
                    foreach (var entityRow in DB)
                    {
                        if (entityRow.Value == null)
                            continue;
                        List<dbRow> components = new List<dbRow>();
                        var entityRowValues = entityRow.Value.Values.ToList();
                        for (int i = 0; i < entityRowValues.Count; i++)
                        {
                            var ecsComponent = entityRowValues[i];
                            ecsComponent.Item1.EnterToSerialization();
                            components.Add(new dbRow()
                            {
                                component = ecsComponent.Item1,
                                componentInstanceId = ecsComponent.Item1.instanceId,
                                componentId = ecsComponent.Item1.GetId(),
                                componentState = ecsComponent.Item2
                            });

                        }
                        newSerializedDB[this.OwnerPaths[entityRow.Key]] = components;
                    }
                }
                
                if (LoggingLevel >= DBLoggingLevel.CountOnly)
                {
                    NLogger.Log($"[DB SerializeDB] Serialized {newSerializedDB.Count} owners, {errorChanged.Count} errors");
                }
                
                if (clearChanged)
                    ChangedComponents.Clear();
                errorChanged.ForEach(x => ChangedComponents[x] = 1);

                serializedDB = newSerializedDB;
            }
            if (LoggingLevel >= DBLoggingLevel.CountOnly)
            {
                var elementsOwners = new StringBuilder();
                foreach (var serializedRow in newSerializedDB)
                {
                    elementsOwners.AppendLine($"{serializedRow.Key.serializableInstanceId} " + "{");
                    foreach (var dbrow in serializedRow.Value)
                    {
                        elementsOwners.AppendLine($"        {dbrow.componentId}++{dbrow.componentInstanceId}++{dbrow.componentState}, ");
                    }
                    elementsOwners.AppendLine("}");
                }

                var elementsOwnersEO = new StringBuilder();
                foreach (var serializedRow in serializedDBNonEO)
                {
                    elementsOwners.AppendLine($"{serializedRow.Key.serializableInstanceId} " + "{");
                    foreach (var dbrow in serializedRow.Value.Item1)
                    {
                        elementsOwners.AppendLine($"        {dbrow.componentId}++{dbrow.componentInstanceId}++{dbrow.componentState}, ");
                    }
                    elementsOwners.AppendLine("}");
                }
                NLogger.Log($"[DB UnserializeDB] Starting deserialization of {newSerializedDB.Count} owners with elements:\n {elementsOwners} \n AND HAS NullEntityOwner:\n {elementsOwnersEO}");
            }

            return sharelock;
        }

        public override void AfterSerializationDB(bool clearAfterSerializaion = true)
        {
            using (this.monoLocker.Lock())
            {
                if (clearAfterSerializaion)
                {
                    int removedCount = 0;

                    HashSet<long> removedOwners = new HashSet<long>();

                    foreach (var entityRow in new Dictionary<IECSObjectPathContainer, List<dbRow>>(serializedDB))
                    {
                        var entityRowValues = entityRow.Value.ToList();
                        for (int i = 0; i < entityRowValues.Count; i++)
                        {
                            var ownerList = DB[entityRow.Key.ECSObject.instanceId];
                            var ecsComponent = ownerList[entityRowValues[i].componentInstanceId];
                            if (ecsComponent.Item2 == ComponentState.Removed)
                            {
                                ecsComponent.Item1.RemovingReaction(ecsComponent.Item1.ownerEntity);
                                ownerList.Remove(ecsComponent.Item1.instanceId);
                                removedCount++;
                            }
                            if(ownerList.Count == 0)
                            {
                                removedOwners.Add(entityRow.Key.ECSObject.instanceId);
                            }
                        }
                    }

                    removedOwners.ForEach(x => DB.Remove(x));
                    
                    if (LoggingLevel >= DBLoggingLevel.CountOnly)
                    {
                        NLogger.Log($"[DB AfterSerializationDB] Cleaned up {removedCount} removed components");
                    }
                }
            }
        }

        [System.NonSerialized]
        private int unserializeCheckCount = 0;
        
        [System.NonSerialized]
        public DictionaryWrapper<IECSObjectPathContainer, (List<dbRow>, int)> serializedDBNonEO = new DictionaryWrapper<IECSObjectPathContainer, (List<dbRow>, int)>();

        [System.NonSerialized]
        public Dictionary<IECSObjectPathContainer, List<dbRow>> afterDeserializedDB = new Dictionary<IECSObjectPathContainer, List<dbRow>>();

        public override void UnserializeDB(bool retryNullEntityOwner = false)
        {
            lock (serializedDB)
            {
                if (LoggingLevel >= DBLoggingLevel.CountOnly)
                {
                    var elementsOwners = new StringBuilder();
                    foreach (var serializedRow in serializedDB)
                    {
                        elementsOwners.AppendLine($"{serializedRow.Key.serializableInstanceId} " + "{");
                        foreach (var dbrow in serializedRow.Value)
                        {
                            elementsOwners.AppendLine($"        {dbrow.componentId}++{dbrow.componentInstanceId}++{dbrow.componentState}, ");
                        }
                        elementsOwners.AppendLine("}");
                    }

                    var elementsOwnersEO = new StringBuilder();
                    foreach (var serializedRow in serializedDBNonEO)
                    {
                        elementsOwners.AppendLine($"{serializedRow.Key.serializableInstanceId} " + "{");
                        foreach (var dbrow in serializedRow.Value.Item1)
                        {
                            elementsOwners.AppendLine($"        {dbrow.componentId}++{dbrow.componentInstanceId}++{dbrow.componentState}, ");
                        }
                        elementsOwners.AppendLine("}");
                    }
                    NLogger.Log($"[DB UnserializeDB] Starting deserialization of {serializedDB.Count} owners with elements:\n {elementsOwners} \n AND HAS NullEntityOwner:\n {elementsOwnersEO}");
                }

                if (retryNullEntityOwner)
                {
                    serializedDBNonEO.ForEach(x => serializedDB[x.Key] = x.Value.Item1);

                    foreach (var serializedRow in serializedDB)
                    {
                        Dictionary<long, (ECSComponent, ComponentState)> components = new Dictionary<long, (ECSComponent, ComponentState)>();
                        DB.TryGetValue(serializedRow.Key.CacheInstanceId, out components);
                        if (components == null)
                            components = new Dictionary<long, (ECSComponent, ComponentState)>();
                        IECSObject entityOwner = serializedRow.Key.ECSObject;
                        if (entityOwner == null)
                        {
                            if (!serializedDBNonEO.ContainsKey(serializedRow.Key))
                            {
                                serializedDBNonEO[serializedRow.Key] = (serializedRow.Value, 0);
                            }

                            if (serializedDBNonEO[serializedRow.Key].Item2 >= 10)
                            {
                                NLogger.Log("client: error unserialize: no entity");
                                var lostInstanceId = serializedRow.Key.serializableInstanceId;
                                if (DB.ContainsKey(lostInstanceId))
                                {
                                    this.RemoveComponentsByOwner(lostInstanceId);
                                }
                                NLogger.Log("lost components destroyed");
                                serializedDBNonEO.Remove(serializedRow.Key);
                                continue;
                            }

                            serializedDBNonEO[serializedRow.Key] = (serializedRow.Value, serializedDBNonEO[serializedRow.Key].Item2 + 1);

                        }
                        else
                        {
                            if (serializedDBNonEO.ContainsKey(serializedRow.Key))
                            {
                                serializedDBNonEO.Remove(serializedRow.Key);
                            }
                        }
                    }
                    if (serializedDBNonEO.Count > 0)
                    {
                        serializedDBNonEO.ForEach(x => serializedDB.Remove(x.Key));
                        serializedDBNonEO.Where(x => x.Value.Item2 > 10).ToList().ForEach(x => serializedDBNonEO.Remove(x.Key));

                        var timer = new TimerCompat();
                        timer.TimerCompatInit(200, (obj, arg) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            UnserializeDB(true);
                        }, false);
                        timer.Start();
                    }
                }

                ChangedComponents.Clear();
                int addedCount = 0;
                int updatedCount = 0;

                foreach (var serializedRow in serializedDB)
                {
                    Dictionary<long, (ECSComponent, ComponentState)> components = new Dictionary<long, (ECSComponent, ComponentState)>();
                    DB.TryGetValue(serializedRow.Key.CacheInstanceId, out components);
                    if (components == null)
                        components = new Dictionary<long, (ECSComponent, ComponentState)>();
                    IECSObject entityOwner = serializedRow.Key.ECSObject;
                    if (entityOwner != null)
                    {
                        foreach (var component in serializedRow.Value)
                        {
                            var unserComp = (ECSComponent)ReflectionCopy.MakeReverseShallowCopy(component.component);
                            component.componentInstanceId = unserComp.instanceId;
                            if (!OwnerPaths.ContainsKey(entityOwner.instanceId))
                            {
                                OwnerPaths[entityOwner.instanceId] = new IECSObjectPathContainer(true) { ECSObject = entityOwner };
                            }
                            if (entityOwner is ECSEntity eCSEntity)
                            {
                                unserComp.ownerEntity = eCSEntity;
                            }
                            else if (entityOwner is ECSComponent eCSComponent)
                            {
                                unserComp.ownerEntity = eCSComponent.ownerEntity;
                            }
                            unserComp.ownerDB = this;
                            if (!components.ContainsKey(unserComp.instanceId))
                            {
                                //unserComp.componentManagers.ownerComponent = unserComp;
                                components[unserComp.instanceId] = (unserComp, component.componentState);
                                ComponentOwners[unserComp.instanceId] = entityOwner.instanceId;
                                unserComp.AfterDeserialization();
                                if (component.componentState != ComponentState.Created)
                                {
                                    unserComp.AddedReaction(unserComp.ownerEntity);
                                }
                                addedCount++;
                            }
                            else
                            {
                                //unserComp.componentManagers = components[unserComp.instanceId].Item1.componentManagers;
                                components[unserComp.instanceId] = (unserComp, component.componentState);
                                unserComp.componentManagers.ForEach(x => x.Value.ConnectPoint = unserComp);
                                unserComp.AfterDeserialization();
                                updatedCount++;
                            }
                            ChangedComponents[unserComp.instanceId] = 1;
                        }
                        DB[serializedRow.Key.CacheInstanceId] = components;
                    }
                    else
                    {
                        NLogger.Error("error unserialize: no entity");
                    }
                }

                if (LoggingLevel >= DBLoggingLevel.CountOnly)
                {
                    NLogger.Log($"[DB UnserializeDB] Deserialized - Added: {addedCount}, Updated: {updatedCount}");
                }

                AfterDeserializeDB();
                afterDeserializedDB = serializedDB;
                serializedDB = new Dictionary<IECSObjectPathContainer, List<dbRow>>();
            }
        }

        public override void AfterDeserializeDB()
        {
            int createdCount = 0;
            int changedCount = 0;
            int removedCount = 0;
            
            foreach (var entityRow in serializedDB)
            {
                var entityRowValues = entityRow.Value.ToList();
                for (int i = 0; i < entityRowValues.Count; i++)
                {
                    var ownerList = DB[entityRow.Key.CacheInstanceId];
                    if (entityRowValues[i].componentState == ComponentState.Removed && !ownerList.ContainsKey(entityRowValues[i].componentInstanceId))
                    {
                        NLogger.LogErrorDB("remove db component duplicate");
                        continue;
                    }
                    var ecsComponent = ownerList[entityRowValues[i].componentInstanceId];
                    if (ecsComponent.Item2 == ComponentState.Created)
                    {
                        ecsComponent.Item1.AddedReaction(ecsComponent.Item1.ownerEntity);
                        createdCount++;
                    }
                    if (ecsComponent.Item2 == ComponentState.Changed)
                    {
                        //ecsComponent.Item1.OnAdded(ecsComponent.Item1.ownerEntity);
                        TaskEx.RunAsync(() =>
                        {
                            ecsComponent.Item1.ChangeReaction(ecsComponent.Item1.ownerEntity);
                        });
                        changedCount++;
                    }
                    if (ecsComponent.Item2 == ComponentState.Removed)
                    {
                        ecsComponent.Item1.RemovingReaction(ecsComponent.Item1.ownerEntity);
                        ownerList.Remove(ecsComponent.Item1.instanceId);
                        ComponentOwners.Remove(ecsComponent.Item1.instanceId);
                        removedCount++;
                    }
                }
            }
            
            if (LoggingLevel >= DBLoggingLevel.CountOnly)
            {
                NLogger.Log($"[DB AfterDeserializeDB] Processed - Created: {createdCount}, Changed: {changedCount}, Removed: {removedCount}");
                LogDBState("AfterDeserializeDB Complete");
            }
        }
    }
}
