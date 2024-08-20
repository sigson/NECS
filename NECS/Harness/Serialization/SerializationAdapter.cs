using NECS.ECS.ECSCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static NECS.ECS.ECSCore.EntitySerialization;

namespace NECS.Harness.Serialization
{
    public static class SerializationAdapter
    {
        private static JsonSerializer storeJsonSerializer = null;
        private static JsonSerializer cacheJsonSerializer
        {
            get
            {
                if(storeJsonSerializer == null)
                {
                    storeJsonSerializer = JsonSerializer.CreateDefault();
                }
                return storeJsonSerializer;
            }
        }
        public static byte[] SerializeAdapterEntity(SerializedEntity entity)
        {
            if (Defines.AOTMode)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, entity);
                    return memoryStream.ToArray();
                }
            }
        }

        public static SerializedEntity DeserializeAdapterEntity(byte[] entity)
        {
            if(Defines.AOTMode)
            {
                return JsonConvert.DeserializeObject<SerializedEntity>(Encoding.UTF8.GetString(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(entity, 0, entity.Length);
                    memoryStream.Position = 0;
                    return (SerializedEntity)DeepCopy.CopyObject(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }
        }

        public static byte[] SerializeAdapterEvent(SerializedEvent entity)
        {
            if (Defines.AOTMode)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, entity);
                    return memoryStream.ToArray();
                }
            }
        }

        public static SerializedEvent DeserializeAdapterEvent(byte[] entity)
        {
            if (Defines.AOTMode)
            {
                return JsonConvert.DeserializeObject<SerializedEvent>(Encoding.UTF8.GetString(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(entity, 0, entity.Length);
                    memoryStream.Position = 0;
                    return (SerializedEvent)DeepCopy.CopyObject(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }
        }


        public static byte[] SerializeECSComponent(ECSComponent component)
        {
            if (Defines.AOTMode)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(component));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, component);
                    return memoryStream.ToArray();
                }
            }
        }

        public static ECSComponent DeserializeECSComponent(byte[] component, long typeId)
        {
            if (Defines.AOTMode)
            {
                return (ECSComponent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(component), EntitySerialization.TypeStorage[typeId]);
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(component, 0, component.Length);
                    memoryStream.Position = 0;
                    return (ECSComponent)DeepCopy.CopyObject(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }
        }

        public static byte[] SerializeECSEntity(ECSEntity entity)
        {
            if (Defines.AOTMode)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, entity);
                    return memoryStream.ToArray();
                }
            }
        }

        public static ECSEntity DeserializeECSEntity(byte[] entity)
        {
            if (Defines.AOTMode)
            {
                return JsonConvert.DeserializeObject<ECSEntity>(Encoding.UTF8.GetString(entity));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(entity, 0, entity.Length);
                    memoryStream.Position = 0;
                    return (ECSEntity)DeepCopy.CopyObject(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }
        }

        public static byte[] SerializeECSEvent(ECSEvent ecsevent)
        {
            if (Defines.AOTMode)
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ecsevent));
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    NetSerializer.Serializer.Default.Serialize(memoryStream, ecsevent);
                    return memoryStream.ToArray();
                }
            }
        }

        public static ECSEvent DeserializeECSEvent(byte[] ecsevent, long typeId)
        {
            if (Defines.AOTMode)
            {
                return (ECSEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(ecsevent), EntitySerialization.TypeStorage[typeId]);
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(ecsevent, 0, ecsevent.Length);
                    memoryStream.Position = 0;
                    return (ECSEvent)DeepCopy.CopyObject(NetSerializer.Serializer.Default.Deserialize(memoryStream));
                }
            }
        }
    }
}
