using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.Extensions;

namespace NECS.ECS.ECSCore
{
    public static class ECSSharedDictionaryCache
    {
        public static Dictionary<long, Dictionary<string, object>> fieldsCache = new Dictionary<long, Dictionary<string, object>>();
    }
    public class ECSSharedField<T> : IDisposable
    {


        private static Dictionary<long, Dictionary<string, object>> fieldsCache => ECSSharedDictionaryCache.fieldsCache;

        public T Value { get => GetValue(); set => SetValue(value); }

        private readonly long entityId;
        private readonly string fieldName;

        public ECSSharedField(long id, string name, T value)
        {
            entityId = id;
            fieldName = name;

            // Проверяем, есть ли словарь для данного ID
            if (!fieldsCache.ContainsKey(id))
            {
                fieldsCache[id] = new Dictionary<string, object>();
            }

            // Проверяем, есть ли уже значение в кеше
            if (fieldsCache[id].ContainsKey(name))
            {
                // Используем существующее значение из кеша
                Value = (T)fieldsCache[id][name];
            }
            else
            {
                // Сохраняем новое значение в кеш
                Value = value;
                fieldsCache[id][name] = value;
            }
        }

        private T GetValue()
        {
            if (fieldsCache.ContainsKey(entityId) && fieldsCache[entityId].ContainsKey(fieldName))
            {
                var value = fieldsCache[entityId][fieldName];
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default(T);
        }

        private void SetValue(T value)
        {
            if (!fieldsCache.ContainsKey(entityId))
            {
                fieldsCache[entityId] = new Dictionary<string, object>();
            }
            fieldsCache[entityId][fieldName] = value;
        }

        /// <summary>
        /// Обновляет значение в кеше
        /// </summary>
        public void UpdateValue(T newValue)
        {
            Value = newValue;
            if (fieldsCache.ContainsKey(entityId) && fieldsCache[entityId].ContainsKey(fieldName))
            {
                fieldsCache[entityId][fieldName] = newValue;
            }
        }

        public static T GetOrAdd(long id, string name, Func<T> valueFactory)
        {
            // Проверяем наличие значения в кеше
            if (fieldsCache.ContainsKey(id) && fieldsCache[id].ContainsKey(name))
            {
                var cachedValue = fieldsCache[id][name];
                if (cachedValue is T typedValue)
                {
                    return typedValue;
                }
            }

            // Если значения нет, создаем новое
            if (!fieldsCache.ContainsKey(id))
            {
                fieldsCache[id] = new Dictionary<string, object>();
            }

            T newValue = valueFactory();
            fieldsCache[id][name] = newValue;
            return newValue;
        }

        /// <summary>
        /// Получает значение из кеша или добавляет значение по умолчанию
        /// </summary>
        public static T GetOrAdd(long id, string name, T defaultValue = default(T))
        {
            return GetOrAdd(id, name, () => defaultValue);
        }

        /// <summary>
        /// Получает значение из кеша по ID и имени
        /// </summary>
        public static T GetCachedValue(long id, string name)
        {
            if (fieldsCache.ContainsKey(id) && fieldsCache[id].ContainsKey(name))
            {
                return (T)fieldsCache[id][name];
            }
            return default(T);
        }

        /// <summary>
        /// Проверяет наличие значения в кеше
        /// </summary>
        public static bool HasCachedValue(long id, string name)
        {
            return fieldsCache.ContainsKey(id) && fieldsCache[id].ContainsKey(name);
        }

        /// <summary>
        /// Устанавливает значение в кеш напрямую
        /// </summary>
        public static object SetCachedValue(long id, string name, object value)
        {
            if (!fieldsCache.ContainsKey(id))
            {
                fieldsCache[id] = new Dictionary<string, object>();
            }
            fieldsCache[id][name] = value;
            return value;
        }

        /// <summary>
        /// Удаляет конкретное значение из кеша
        /// </summary>
        public static bool RemoveCachedValue(long id, string name)
        {
            if (fieldsCache.ContainsKey(id) && fieldsCache[id].ContainsKey(name))
            {
                return fieldsCache[id].Remove(name);
            }
            return false;
        }

        /// <summary>
        /// Удаляет все значения для конкретного ID
        /// </summary>
        public static bool RemoveAllCachedValuesForId(long id)
        {
            return fieldsCache.Remove(id);
        }

        /// <summary>
        /// Очищает весь кеш
        /// </summary>
        public static void ClearCache()
        {
            fieldsCache.Clear();
        }

        /// <summary>
        /// Получает количество закешированных ID
        /// </summary>
        public static int GetCachedIdsCount()
        {
            return fieldsCache.Count;
        }

        /// <summary>
        /// Получает количество закешированных полей для конкретного ID
        /// </summary>
        public static int GetCachedFieldsCount(long id)
        {
            if (fieldsCache.ContainsKey(id))
            {
                return fieldsCache[id].Count;
            }
            return 0;
        }

        /// <summary>
        /// Получает все имена полей для конкретного ID
        /// </summary>
        public static IEnumerable<string> GetCachedFieldNames(long id)
        {
            if (fieldsCache.ContainsKey(id))
            {
                return fieldsCache[id].Keys;
            }
            return new List<string>();
        }

        /// <summary>
        /// Получает все ID из кеша
        /// </summary>
        public static IEnumerable<long> GetAllCachedIds()
        {
            return fieldsCache.Keys;
        }

        public void Dispose()
        {
            //fieldsCache[entityId].Remove(fieldName);
        }
    }
}