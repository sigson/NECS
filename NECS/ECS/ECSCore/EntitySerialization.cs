using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.ECS.Components;
using NECS.ECS.Components.ECSComponentsGroup;
using NECS.Network.Simple.Net;
using NECS.Harness.Services;
using NECS.Core.Logging;
using NECS.ECS.DefaultsDB.ECSComponents;

namespace NECS.ECS.ECSCore
{
    public class EntitySerialization
    {
        public static string[] FullSerialize(ECSEntity entity, bool serializeOnlyChanged = false)
        {
            string result;
            string strEntity;
            using (StringWriter writer = new StringWriter())
            {
                GlobalCachingSerialization.cachingSerializer.Serialize(writer, entity);
                strEntity = writer.ToString();
            }
            string strComponents = entity.entityComponents.SerializeStorage(GlobalCachingSerialization.cachingSerializer, serializeOnlyChanged, true);
            if(Defines.SerializationResultPrint)
                result = "{\"Entity\":\"" + strEntity + "\",\"Components\":\"" + strComponents + "\"}";
            return new string[] { strEntity, strComponents };
        }

        public static Dictionary<long, string> SlicedSerialize(ECSEntity entity, bool serializeOnlyChanged = false, bool clearChanged = false)
        {
            string result;
            string strEntity;
            
            lock(entity.entityComponents.serializationLocker)//wtf double locking is work
            {
                var strComponents = entity.entityComponents.SlicedSerializeStorage(GlobalCachingSerialization.cachingSerializer, serializeOnlyChanged, clearChanged);
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

        public static void SerializeEntity(ECSEntity entity, bool serializeOnlyChanged = false)
        {
            var serializedData = SlicedSerialize(entity, serializeOnlyChanged, true);
            bool emptyData = true;
            foreach(var GDAP in entity.dataAccessPolicies)
            {
                GDAP.JsonAvailableComponents = "";
                GDAP.rawUpdateAvailableComponents.Clear();
                GDAP.JsonRestrictedComponents = "";
                GDAP.rawUpdateRestrictedComponents.Clear();
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
                    if (entity.entityComponents.directSerialized.Keys.Contains(availableComp))   
                    {
                        GDAP.rawUpdateAvailableComponents.Add(availableComp, entity.entityComponents.directSerialized[availableComp]);
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
                    if (entity.entityComponents.directSerialized.Keys.Contains(availableComp))
                    {
                        GDAP.rawUpdateAvailableComponents.Add(availableComp, entity.entityComponents.directSerialized[availableComp]);
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

        public static (string, List<INetSerializable>) BuildSerializedEntityWithGDAP(ECSEntity toEntity, ECSEntity fromEntity, bool ignoreNullData = false)
        {
            var data = GroupDataAccessPolicy.ComponentsFilter(toEntity, fromEntity);
            if(data.Item1 == "" && !ignoreNullData)
            {
                return ("", data.Item2);
            }
            if(data.Item1 == "#INCLUDEREMOVED#" || ignoreNullData)
            {
                data.Item1 = "";
                return ("{\"entity\":" + fromEntity.serializedEntity + ",\"SerializationContainer\":{" + data.Item1 + "}}", data.Item2);
            }
            return ("{\"entity\":" + fromEntity.serializedEntity + ",\"SerializationContainer\":{" + data.Item1.Substring(0, data.Item1.Length - 1) + "}}", data.Item2);
        }

        public static string BuildFullSerializedEntityWithGDAP(ECSEntity toEntity, ECSEntity fromEntity)
        {
            var componentData = GroupDataAccessPolicy.RawComponentsFilter(toEntity, fromEntity);
            if (componentData.Count == 0)
            {
                return "";
            }
            var serializedData = SlicedSerialize(fromEntity);
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

        public static string BuildFullSerializedEntity(ECSEntity Entity)
        {
            var serializedData = FullSerialize(Entity, false);
            if(serializedData[1] == "")
            {
                return "";
            }
            return "{\"entity\":" + serializedData[0] + ",\"SerializationContainer\":{" + serializedData[1] + "}}";
        }

        public static ECSEntity Deserialize(string serializedData)
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
        public static void UpdateDeserialize(string serializedData)
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
                        Logger.Log(bufEntity.entity.instanceId.ToString() + " new entity");
                        entity = bufEntity.entity;
                        bufEntity.ReworkDictionary();
                        storage = new EntityComponentStorage(entity);
                        storage.SerializationContainer = bufEntity.SerializationContainer;
                        storage.RestoreComponentsAfterSerialization(entity);
                        entity.entityComponents = storage;
                        entity.AddComponentSilent(new EntityManagersComponent());
                        entity.fastEntityComponentsId = new ConcurrentDictionary<long, int>(entity.entityComponents.Components.ToDictionary(k => k.instanceId, t => 0));
                        ManagerScope.instance.entityManager.OnAddNewEntity(entity);
                        return;
                    }
                    bufEntity.ReworkDictionary();

                    if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    {
                        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ServerComponentGroup.Id });
                    }
                    else if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                    {
                        entity.entityComponents.FilterRemovedComponents(bufEntity.entity.fastEntityComponentsId.Keys.ToList(), new List<long>() { ClientComponentGroup.Id });
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
    }
    public class UnserializedEntity : CachingSerializable
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
}
