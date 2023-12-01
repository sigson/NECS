using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using NECS.ECS.Components.ECSComponentsGroup;
using NECS.Harness.Services;
using NECS.Core.Logging;
using NECS.ECS.DefaultsDB.ECSComponents;
using NECS.Extensions;
using System.Data;
using System.IO;

namespace NECS.ECS.ECSCore
{
    public class EntitySerialization
    {
        #region setupData
        public static ConcurrentDictionary<long, Type> TypeStorage = new ConcurrentDictionary<long, Type>();

        public static void InitSerialize()
        {
            var nonSerializedSet = new HashSet<Type>() { typeof(EntityManagersComponent) };

            var ecsObjects = ECSAssemblyExtensions.GetAllSubclassOf(typeof(IECSObject)).Where(x => !x.IsAbstract).Where(x => !nonSerializedSet.Contains(x)).ToList();
            ecsObjects.Select(x => Activator.CreateInstance(x)).Cast<IECSObject>().ForEach(x => TypeStorage[x.GetId()] = x.GetType());

            ecsObjects.Add(typeof(SerializedEntity));

            NetSerializer.Serializer.Default = new NetSerializer.Serializer(ecsObjects);
        }
        [System.Serializable]
        public class SerializedEntity
        {
            public byte[] Entity;
            [System.NonSerialized]
            public ECSEntity desEntity = null;
            [System.NonSerialized]
            public ConcurrentDictionary<long, ECSComponent> SerializationContainer = new ConcurrentDictionary<long, ECSComponent>();
            public Dictionary<long, byte[]> Components = new Dictionary<long, byte[]>();

            public void DeserializeEntity()
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(this.Entity, 0, this.Entity.Length);
                    memoryStream.Position = 0;
                    desEntity = (ECSEntity)ReflectionCopy.MakeReverseShallowCopy(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }

            public void DeserializeComponents()
            {
                foreach(var sComp in Components)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(sComp.Value, 0, sComp.Value.Length);
                        memoryStream.Position = 0;
                        SerializationContainer[sComp.Key] = (ECSComponent)ReflectionCopy.MakeReverseShallowCopy(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                    }
                }
                
            }
        }
        #endregion

        /// <summary>
        /// OBSOLETE, not has in use
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="serializeOnlyChanged"></param>
        /// <returns></returns>
        private static byte[] FullSerialize(ECSEntity entity, bool serializeOnlyChanged = false)
        {
            var resultObject = new SerializedEntity();
            using (var memoryStream = new MemoryStream())
            {
                NetSerializer.Serializer.Default.Serialize(memoryStream, entity);
                resultObject.Entity = memoryStream.ToArray();
                resultObject.Components = entity.entityComponents.SerializeStorage(serializeOnlyChanged, true);
            }
            using (var memoryStream = new MemoryStream())
            {
                NetSerializer.Serializer.Default.Serialize(memoryStream, resultObject);
                return memoryStream.ToArray();
            }
        }
        /// <summary>
        /// inner method of entity serialization, need for build serialized atomic data
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="serializeOnlyChanged"></param>
        /// <param name="clearChanged"></param>
        /// <returns></returns>
        private static Dictionary<long, byte[]> SlicedSerialize(ECSEntity entity, bool serializeOnlyChanged = false, bool clearChanged = false)
        {
            var resultObject = new SerializedEntity();

            lock (entity.entityComponents.serializationLocker)//wtf double locking is work
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, entity);
                    resultObject.Entity = memoryStream.ToArray();
                    resultObject.Components = entity.entityComponents.SlicedSerializeStorage(serializeOnlyChanged, clearChanged);
                }
                resultObject.Components[ECSEntity.Id] = resultObject.Entity;
            }
            return resultObject.Components;
        }

        /// <summary>
        /// Setup method of entity serialization, freezes the serialized state of an object for next gdap building manipulation.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="serializeOnlyChanged">set true only if you have fully serialized the object at least once before</param>
        public static void SerializeEntity(ECSEntity entity, bool serializeOnlyChanged = false)
        {
            var serializedData = SlicedSerialize(entity, serializeOnlyChanged, true);
            bool emptyData = true;
            foreach (var GDAP in entity.dataAccessPolicies)
            {
                GDAP.JsonAvailableComponents = "";
                GDAP.BinAvailableComponents.Clear();
                GDAP.JsonRestrictedComponents = "";
                GDAP.BinRestrictedComponents.Clear();
                GDAP.IncludeRemovedAvailable = false;
                GDAP.IncludeRemovedRestricted = false;
                foreach (var availableComp in GDAP.AvailableComponents)
                {
                    byte[] serialData = null;
                    if (entity.entityComponents.RemovedComponents.Contains(availableComp))
                    {
                        GDAP.IncludeRemovedAvailable = true;
                        emptyData = false;
                    }
                    if (!serializedData.TryGetValue(availableComp, out serialData))
                        continue;
                    GDAP.BinAvailableComponents[availableComp] = serialData;
                    emptyData = false;
                }
                foreach (var availableComp in GDAP.RestrictedComponents)
                {
                    byte[] serialData = null;
                    if (entity.entityComponents.RemovedComponents.Contains(availableComp))
                    {
                        GDAP.IncludeRemovedRestricted = true;
                        emptyData = false;
                    }
                    if (!serializedData.TryGetValue(availableComp, out serialData))
                        continue;
                    GDAP.BinRestrictedComponents[availableComp] = serialData;
                    emptyData = false;
                }
            }
            entity.entityComponents.RemovedComponents.Clear();
            entity.binSerializedEntity = serializedData[ECSEntity.Id];
            entity.emptySerialized = emptyData;
        }
        /// <summary>
        /// high perfomance data exchange building method, building component exchange storage based on cached serialized in SerializeEntity components (no use serialization)
        /// </summary>
        /// <param name="toEntity"></param>
        /// <param name="fromEntity"></param>
        /// <param name="ignoreNullData"></param>
        /// <returns></returns>
        public static byte[] BuildSerializedEntityWithGDAP(ECSEntity toEntity, ECSEntity fromEntity, bool ignoreNullData = false)
        {
            var data = GroupDataAccessPolicy.ComponentsFilter(toEntity, fromEntity);
            var resultObject = new SerializedEntity();
            if (data.Item1 == "" && data.Item2.Count() == 0 && !ignoreNullData)
            {
                return new byte[0];
            }
            resultObject.Entity = fromEntity.binSerializedEntity;
            if (!(data.Item1 == "#INCLUDEREMOVED#" || ignoreNullData))
            {
                data.Item1 = "";
                resultObject.Components = data.Item2;
            }
            using (var memoryStream = new MemoryStream())
            {
                NetSerializer.Serializer.Default.Serialize(memoryStream, resultObject);
                return memoryStream.ToArray();
            }
        }
        /// <summary>
        /// gdap data exchange building with full entity serialization, mainly needed for setup data exchange, because slower then BuildSerializedEntityWithGDAP and takes up too much space in network traffic
        /// </summary>
        /// <param name="toEntity"></param>
        /// <param name="fromEntity"></param>
        /// <returns></returns>
        public static byte[] BuildFullSerializedEntityWithGDAP(ECSEntity toEntity, ECSEntity fromEntity)
        {
            var componentData = GroupDataAccessPolicy.RawComponentsFilter(toEntity, fromEntity);
            var resultObject = new SerializedEntity();
            if (componentData.Count == 0)
            {
                return new byte[0];
            }
            var serializedData = SlicedSerialize(fromEntity);
            foreach (var comp in componentData)
            {
                byte[] serialData = null;
                if (!serializedData.TryGetValue(comp, out serialData))
                    continue;
                resultObject.Components[comp] = serialData;
            }
            resultObject.Entity = fromEntity.binSerializedEntity;
            using (var memoryStream = new MemoryStream())
            {
                NetSerializer.Serializer.Default.Serialize(memoryStream, resultObject);
                return memoryStream.ToArray();
            }
        }
        /// <summary>
        /// OBSOLETE, not has in use
        /// </summary>
        /// <param name="Entity"></param>
        /// <returns></returns>
        private static byte[] BuildFullSerializedEntity(ECSEntity Entity)
        {
            var serializedData = FullSerialize(Entity, false);
            return serializedData;
        }
        /// <summary>
        /// OBSOLETE, for tests reason, use UpdateDeserialize
        /// </summary>
        /// <param name="serializedData"></param>
        /// <returns></returns>
        public static ECSEntity Deserialize(byte[] serializedData)
        {
            SerializedEntity bufEntity;
            EntityComponentStorage storage;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(serializedData, 0, serializedData.Length);
                memoryStream.Position = 0;
                bufEntity = (SerializedEntity)NetSerializer.Serializer.Default.Deserialize(memoryStream);
            }
            bufEntity.DeserializeEntity();


            storage = bufEntity.desEntity.entityComponents;
            storage.DeserializeStorage(bufEntity.Components);
            storage.RestoreComponentsAfterSerialization(bufEntity.desEntity);
            return bufEntity.desEntity;
        }
        /// <summary>
        /// Deserialization with adding new entity or update exist with add|update|remove components
        /// </summary>
        /// <param name="serializedData"></param>
        public static void UpdateDeserialize(byte[] serializedData)
        {
            ECSEntity entity;
            SerializedEntity bufEntity;
            EntityComponentStorage storage;

            lock (ManagerScope.instance)
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(serializedData, 0, serializedData.Length);
                    memoryStream.Position = 0;
                    bufEntity = (SerializedEntity)NetSerializer.Serializer.Default.Deserialize(memoryStream);
                }
                bufEntity.DeserializeEntity();

                if (!ManagerScope.instance.entityManager.EntityStorage.TryGetValue(bufEntity.desEntity.instanceId, out entity))
                {
                    NLogger.Log(bufEntity.desEntity.instanceId.ToString() + " new entity");
                    entity = bufEntity.desEntity;
                    storage = bufEntity.desEntity.entityComponents;
                    storage.DeserializeStorage(bufEntity.Components);
                    storage.RestoreComponentsAfterSerialization(entity);
                    entity.AddComponentSilent(new EntityManagersComponent());
                    entity.fastEntityComponentsId = new Dictionary<long, int>(entity.entityComponents.Components.ToDictionary(k => k.instanceId, t => 0));
                    ManagerScope.instance.entityManager.OnAddNewEntity(entity);
                    return;
                }


                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                {
                    entity.entityComponents.FilterRemovedComponents(bufEntity.desEntity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ServerComponentGroup.Id });
                }
                else if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                {
                    entity.entityComponents.FilterRemovedComponents(bufEntity.desEntity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ClientComponentGroup.Id });
                }
                entity.entityComponents.RegisterAllComponents();

                foreach (var component in bufEntity.SerializationContainer)
                {
                    var tComponent = (ECSComponent)component.Value;
                    entity.AddOrChangeComponentSilentWithOwnerRestoring(tComponent);
                    if (tComponent is DBComponent)
                        TaskEx.RunAsync(() => (entity.GetComponent<DBComponent>(tComponent.GetId())).UnserializeDB());
                    tComponent.AfterDeserialization();
                }
                entity.entityComponents.RegisterAllComponents();
            }
        }


        #region oldest JSON serialization implementation

        private static string[] FullSerializeJSON(ECSEntity entity, bool serializeOnlyChanged = false)//tr
        {
            string result;
            string strEntity;
            using (StringWriter writer = new StringWriter())
            {
                GlobalCachingSerialization.cachingSerializer.Serialize(writer, entity);
                strEntity = writer.ToString();
            }
            string strComponents = entity.entityComponents.SerializeStorageJSON(GlobalCachingSerialization.cachingSerializer, serializeOnlyChanged, true);
            if(Defines.SerializationResultPrint)
                result = "{\"Entity\":\"" + strEntity + "\",\"Components\":\"" + strComponents + "\"}";
            return new string[] { strEntity, strComponents };
        }

        private static Dictionary<long, string> SlicedSerializeJSON(ECSEntity entity, bool serializeOnlyChanged = false, bool clearChanged = false)
        {
            string result;
            string strEntity;
            
            lock(entity.entityComponents.serializationLocker)//wtf double locking is work
            {
                var strComponents = entity.entityComponents.SlicedSerializeStorageJSON(GlobalCachingSerialization.cachingSerializer, serializeOnlyChanged, clearChanged);
                using (StringWriter writer = new StringWriter())
                {
                    GlobalCachingSerialization.cachingSerializer.Serialize(writer, entity);
                    strEntity = writer.ToString();
                }
                strComponents[ECSEntity.Id] = strEntity;
                if (Defines.SerializationResultPrint)
                    result = "{\"Entity\":\"" + strEntity + "\",\"Components\":\"" + strComponents + "\"}";
                return strComponents;
            }
            
        }

        private static void SerializeEntityJSON(ECSEntity entity, bool serializeOnlyChanged = false)
        {
            var serializedData = SlicedSerializeJSON(entity, serializeOnlyChanged, true);
            bool emptyData = true;
            foreach(var GDAP in entity.dataAccessPolicies)
            {
                GDAP.JsonAvailableComponents = "";
                GDAP.JsonRestrictedComponents = "";
                GDAP.IncludeRemovedAvailable = false;
                GDAP.IncludeRemovedRestricted = false;
                foreach (var availableComp in GDAP.AvailableComponents)
                {
                    string serialData = "";
                    if (entity.entityComponents.RemovedComponents.Contains(availableComp))
                    {
                        GDAP.IncludeRemovedAvailable = true;
                        emptyData = false;
                    }
                    if (!serializedData.TryGetValue(availableComp, out serialData))
                        continue;
                    GDAP.JsonAvailableComponents += "\"" + availableComp + "\":" + serialData + ",";
                    emptyData = false;
                }
                foreach (var availableComp in GDAP.RestrictedComponents)
                {
                    string serialData = "";
                    if (entity.entityComponents.RemovedComponents.Contains(availableComp))
                    {
                        GDAP.IncludeRemovedRestricted = true;
                        emptyData = false;
                    }
                    if (!serializedData.TryGetValue(availableComp, out serialData))
                        continue;
                    GDAP.JsonRestrictedComponents += "\"" + availableComp + "\":" + serialData + ",";
                    emptyData = false;
                }
            }
            entity.entityComponents.RemovedComponents.Clear();
            entity.serializedEntity = serializedData[ECSEntity.Id];
            entity.emptySerialized = emptyData;
        }

        private static string BuildSerializedEntityWithGDAPJSON(ECSEntity toEntity, ECSEntity fromEntity, bool ignoreNullData = false)
        {
            var data = GroupDataAccessPolicy.ComponentsFilter(toEntity, fromEntity);
            if(data.Item1 == "" && !ignoreNullData)
            {
                return "";
            }
            if(data.Item1 == "#INCLUDEREMOVED#" || ignoreNullData)
            {
                data.Item1 = "";
                return "{\"entity\":" + fromEntity.serializedEntity + ",\"SerializationContainer\":{" + data.Item1 + "}}";
            }
            return "{\"entity\":" + fromEntity.serializedEntity + ",\"SerializationContainer\":{" + data.Item1.Substring(0, data.Item1.Length - 1) + "}}";
        }

        private static string BuildFullSerializedEntityWithGDAPJSON(ECSEntity toEntity, ECSEntity fromEntity)
        {
            var componentData = GroupDataAccessPolicy.RawComponentsFilter(toEntity, fromEntity);
            if (componentData.Count == 0)
            {
                return "";
            }
            var serializedData = SlicedSerializeJSON(fromEntity);
            string data = "";
            foreach(var comp in componentData)
            {
                string serialData = "";
                if (!serializedData.TryGetValue(comp, out serialData))
                    continue;
                data += "\"" + comp + "\":" + serialData + ",";
            }
            return "{\"entity\":" + fromEntity.serializedEntity + ",\"SerializationContainer\":{" + data.Substring(0, data.Length - 1) + "}}";
        }

        private static string BuildFullSerializedEntityJSON(ECSEntity Entity)
        {
            var serializedData = FullSerializeJSON(Entity, false);
            if(serializedData[1] == "")
            {
                return "";
            }
            return "{\"entity\":" + serializedData[0] + ",\"SerializationContainer\":{" + serializedData[1] + "}}";
        }

        private static ECSEntity DeserializeJSON(string serializedData)
        {
            ECSEntity entity;
            UnserializedEntity bufEntity;
            EntityComponentStorage storage;

            using (StringReader reader = new StringReader(serializedData))
            {
                JsonTextReader jreader = new JsonTextReader(reader);
                bufEntity = (UnserializedEntity)GlobalCachingSerialization.standartSerializer.Deserialize(jreader, typeof(UnserializedEntity));
                entity = bufEntity.entity;
                bufEntity.ReworkDictionary();
                storage = new EntityComponentStorage(entity);
                storage.SerializationContainer = bufEntity.SerializationContainer;
                storage.RestoreComponentsAfterSerialization(entity);
            }   
            entity.entityComponents = storage;
            return entity;
        }
        private static void UpdateDeserializeJSON(string serializedData)
        {
            ECSEntity entity;
            UnserializedEntity bufEntity;
            EntityComponentStorage storage;

            lock (ManagerScope.instance)
            {
                using (StringReader reader = new StringReader(serializedData))
                {
                    JsonTextReader jreader = new JsonTextReader(reader);
                    bufEntity = (UnserializedEntity)GlobalCachingSerialization.standartSerializer.Deserialize(jreader, typeof(UnserializedEntity));
                    entity = null;
                    
                    if (!ManagerScope.instance.entityManager.EntityStorage.TryGetValue(bufEntity.entity.instanceId, out entity))
                    {
                        NLogger.Log(bufEntity.entity.instanceId.ToString() + " new entity");
                        entity = bufEntity.entity;
                        bufEntity.ReworkDictionary();
                        storage = new EntityComponentStorage(entity);
                        storage.SerializationContainer = bufEntity.SerializationContainer;
                        storage.RestoreComponentsAfterSerialization(entity);
                        entity.entityComponents = storage;
                        entity.AddComponentSilent(new EntityManagersComponent());
                        entity.fastEntityComponentsId = new Dictionary<long, int>(entity.entityComponents.Components.ToDictionary(k => k.instanceId, t => 0));
                        ManagerScope.instance.entityManager.OnAddNewEntity(entity);
                        return;
                    }
                    bufEntity.ReworkDictionary();

                    if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    {
                        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.SnapshotI(bufEntity.entity.SerialLocker).Keys.ToList(), new List<long>() { ServerComponentGroup.Id });
                    }
                    else if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                    {
                        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.SnapshotI(bufEntity.entity.SerialLocker).Keys.ToList(), new List<long>() { ClientComponentGroup.Id });
                    }
                    entity.entityComponents.RegisterAllComponents();

                    foreach (var component in bufEntity.SerializationContainer)
                    {
                        var tComponent = (ECSComponent)component.Value;
                        entity.AddOrChangeComponentSilentWithOwnerRestoring(tComponent);
                        if (tComponent is DBComponent)
                            TaskEx.RunAsync(() => (entity.GetComponent<DBComponent>(tComponent.GetId())).UnserializeDB());
                    }
                    entity.entityComponents.RegisterAllComponents();

                }
            }

            #region oldest
            //using (StringReader reader = new StringReader(serializedData))
            //{
            //    JsonTextReader jreader = new JsonTextReader(reader);
            //    bufEntity = (UnserializedEntity)GlobalCachingSerialization.standartSerializer.Deserialize(jreader, typeof(UnserializedEntity));
            //    entity = null;
            //    if (!ManagerScope.instance.entityManager.EntityStorage.TryGetValue(bufEntity.entity.instanceId, out entity))
            //    {
            //        entity = bufEntity.entity;
            //        bufEntity.ReworkDictionary();
            //        storage = new EntityComponentStorage(entity);
            //        storage.SerializationContainer = bufEntity.SerializationContainer;
            //        storage.RestoreComponentsAfterSerialization(entity);
            //        entity.entityComponents = storage;
            //        entity.fastEntityComponentsId = new ConcurrentDictionary<long, int>(entity.entityComponents.Components.ToDictionary(k => k.instanceId, t => 0));
            //        ManagerScope.instance.entityManager.OnAddNewEntity(entity);
            //        return;
            //    }
            //    bufEntity.ReworkDictionary();
            //    if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            //    {
            //        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ServerComponentGroup.Id });
            //    }
            //    else if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            //    {
            //        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ClientComponentGroup.Id });
            //    }
            //    entity.entityComponents.RegisterAllComponents();
            //    foreach (var component in bufEntity.SerializationContainer)
            //    {
            //        entity.AddOrChangeComponentSilent((ECSComponent)component.Value);
            //    }
            //    entity.entityComponents.RegisterAllComponents();
            //}
            #endregion
        }
        #endregion
    }
    public class UnserializedEntity
    {
        public ECSEntity entity { get; set; }
        public ConcurrentDictionary<long, object> SerializationContainer { get; set; }

        public void ReworkDictionary()
        {
            foreach(var keypair in SerializationContainer)
            {
                SerializationContainer[keypair.Key] = (object)(keypair.Value as JObject).ToObject(ECSComponentManager.AllComponents[keypair.Key].GetTypeFast());
            }
        }
    }

    public static class CachingSettings
    {
        public static JsonSerializerSettings Default = new JsonSerializerSettings();
    }

    public static class GlobalCachingSerialization
    {
        public static JsonSerializer cachingSerializer => standartSerializer;
        public static JsonSerializer standartSerializer = new JsonSerializer();
    }

}
