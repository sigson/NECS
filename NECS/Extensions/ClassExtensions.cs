using NECS.Core.Logging;
using NECS.ECS.Types.AtomicType;
using NECS.Extensions;
using NECS.Extensions.ThreadingSync;
using NECS.GameEngineAPI;
using NECS.Harness.Model;
using NECS.Harness.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace NECS
{
    public static class JsonUtil
    {
        public static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        public static T GetObjectByPath<T>(this JObject storage, string path, bool fixPath = true)
        {
            return storage.GetJTokenByPath(path, fixPath).ToObject<T>();
        }

        public static JToken GetJTokenByPath(this JObject storage, string path, bool fixPath = true)
        {
            if (fixPath)
                path = path.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);

            var pathSplit = path.Split(GlobalProgramState.instance.PathSeparator[0]);
            var nowStorage = storage[pathSplit[0]];
            for (int i = 1; i < pathSplit.Length; i++)
            {
                if (!Lambda.TryExecute(() => nowStorage = nowStorage[pathSplit[i]]))
                    if (!Lambda.TryExecute(() => nowStorage = nowStorage[int.Parse(pathSplit[i])]))
                        throw new Exception("Wrong JObject iterator");
            }
            return nowStorage;
        }
    }

    public static class ClassEx
    {
		public static float NextFloat(this Random random, float startFloat, float endFloat)
		{
			if (startFloat >= endFloat)
			{
				throw new ArgumentException("startFloat must be less than endFloat");
			}

			// Генерация случайного числа в диапазоне [0, 1)
			float randomValue = (float)random.NextDouble();

			// Масштабирование и смещение для получения числа в диапазоне [startFloat, endFloat)
			return startFloat + randomValue * (endFloat - startFloat);
		}
		
        public static string RandomString(this Random random, int countSymbols)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, countSymbols)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static float FastFloat(this string str)
        {
            return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static long GuidToLongR(this Guid guid)
        {
            return DateTime.UtcNow.Ticks + BitConverter.ToInt64(guid.ToByteArray(), 8);
        }
        public static long GuidToLong(this Guid guid)
        {
            return BitConverter.ToInt64(guid.ToByteArray(), 8);
        }

        public static Type IdToECSType(this long id)
        {
            if (NECS.ECS.ECSCore.EntitySerialization.TypeStorage.TryGetValue(id, out var result))
            {
                return result;
            }
            return default;
        }

        public static long TypeId(this Type id)
        {
            try
            {
                return id.GetCustomAttribute<ECS.ECSCore.TypeUidAttribute>().Id;
            }
            catch
            {
                NLogger.Error(id.ToString() + " no have static id field or ID attribute");    
            }
            return default;
        }

        public static long IdToECSType(this Type id)
        {
            if (NECS.ECS.ECSCore.EntitySerialization.TypeIdStorage.TryGetValue(id, out var result))
            {
                return result;
            }
            return default;
        }

        public static Type NameToECSType(this string componentName)
        {
            if (NECS.ECS.ECSCore.EntitySerialization.TypeStringStorage.TryGetValue(componentName, out var result))
            {
                return result;
            }
            return default;
        }

        public static long NameToECSId(this string componentName)
        {
            return componentName.NameToECSType().IdToECSType();
        }
    }

#if UNITY_5_3_OR_NEWER

    public static class ManagerSpace
    {
        #region Instantiate
        public static UnityEngine.Object InstantiatedProcess(UnityEngine.Object instantiated, IEntityManager entityManagerOwner = null)
        {
            if (entityManagerOwner != null)
            {
                if (instantiated is UnityEngine.GameObject)
                {
                    (instantiated as UnityEngine.GameObject).GetComponentsInChildren<IManagable>().ForEach(x => (x as IManagable).ownerManagerSpace = entityManagerOwner);
                }
            }
            return instantiated;
        }

        public static T Instantiate<T>(T original, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, parent);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }
        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, position, rotation);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Transform parent, bool worldPositionStays, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, parent, worldPositionStays);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, position, rotation, parent);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Transform parent, bool instantiateInWorldSpace, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, parent, instantiateInWorldSpace);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, position, rotation);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, position, rotation, parent);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, parent);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }
        #endregion
        #region Component

        private static UnityEngine.Component ComponentProcess(UnityEngine.Component component, IEntityManager entityManagerOwner)
        {
            if (component is IManagable && entityManagerOwner != null)
                (component as IManagable).ownerManagerSpace = entityManagerOwner;
            return component;
        }

        public static UnityEngine.Component AddComponent<T>(this UnityEngine.GameObject gameObject, IEntityManager entityManagerOwner) where T : UnityEngine.Component
        {
            return (T)ComponentProcess(gameObject.AddComponent<T>(), entityManagerOwner);
        }
        public static UnityEngine.Component AddComponent(this UnityEngine.GameObject gameObject, Type typeComponent, IEntityManager entityManagerOwner)
        {
            return ComponentProcess(gameObject.AddComponent(typeComponent), entityManagerOwner);
        }
        #endregion
    }
    
#else
    public static class ManagerSpace
    {
        #region Instantiate
        public static EngineApiObjectBehaviour InstantiatedProcess(EngineApiObjectBehaviour instantiated, IEntityManager entityManagerOwner = null)
        {
            if (entityManagerOwner != null)
            {
                if (instantiated is EngineApiObjectBehaviour)
                {
                    (instantiated as EngineApiObjectBehaviour).GetComponentsInChildren<IManagable>().ForEach(x => (x as IManagable).ownerManagerSpace = entityManagerOwner);
                }
            }
            return instantiated;
        }
        #endregion
    }
#endif
    

    
}
