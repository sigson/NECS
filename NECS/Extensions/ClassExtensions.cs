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

        public static ConfigObj GetConfig(this string str)
        {
            return ConstantService.instance.GetByConfigPath(str);
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

    public class PredicateExecutor
    {
        // Статический кеш всех инстансов
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PredicateExecutor> InstanceCache = 
            new System.Collections.Concurrent.ConcurrentDictionary<string, PredicateExecutor>();
        
        // Свойства класса
        public string PredicateId { get; private set; }
        private List<Func<bool>> predicates;
        public Action payloadAction;
        private int maxAttempts;
        private int timeoutBetweenAttempts;
        private Action<Exception, string> errorHandler;
        private StackTrace stackTrace;
        private int currentAttempt = 0;
        private TimerCompat timer;
        private bool isDisposed = false;
        
        /// <summary>
        /// Конструктор PredicateExecutor
        /// </summary>
        /// <param name="predicateId">Уникальный идентификатор предиката</param>
        /// <param name="predicates">Список предикатов для выполнения</param>
        /// <param name="payloadAction">Действие, выполняемое после успешного прохождения предикатов</param>
        /// <param name="maxAttempts">Максимальное количество попыток</param>
        /// <param name="timeoutBetweenAttempts">Таймаут между попытками в миллисекундах</param>
        /// <param name="errorHandler">Опциональный обработчик ошибок</param>
        public PredicateExecutor(
            string predicateId,
            List<Func<bool>> predicates,
            Action payloadAction,
            int timeoutBetweenAttempts = 1000,
            int maxAttempts = 3,
            Action<Exception, string> errorHandler = null, bool replaceExist = false)
        {
            // Валидация параметров
            if (string.IsNullOrWhiteSpace(predicateId))
                throw new ArgumentNullException(nameof(predicateId));
            if (predicates == null || predicates.Count == 0)
                throw new ArgumentNullException(nameof(predicates));
            if (payloadAction == null)
                throw new ArgumentNullException(nameof(payloadAction));
            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be greater than 0");
            if (timeoutBetweenAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutBetweenAttempts), "Must be non-negative");
            
            this.PredicateId = predicateId;
            this.predicates = new List<Func<bool>>(predicates);
            this.payloadAction = payloadAction;
            this.maxAttempts = maxAttempts;
            this.timeoutBetweenAttempts = timeoutBetweenAttempts;
            
            // Установка обработчика ошибок (стандартный или пользовательский)
            this.errorHandler = errorHandler ?? DefaultErrorHandler;

            // Сохраняем stack trace для диагностики
            this.stackTrace = new StackTrace(true);

            // Добавляем в кеш
            if (replaceExist)
            {
                if (InstanceCache.TryGetValue(predicateId, out var existPredicate))
                {
                    existPredicate.Stop();
                }
            }
            if (!InstanceCache.TryAdd(predicateId, this))
            {
                NLogger.Error($"Failed to add PredicateExecutor with ID '{predicateId}' to cache - ID already exists");
                throw new InvalidOperationException($"PredicateExecutor with ID '{predicateId}' already exists");
            }
            
            NLogger.Log($"Created PredicateExecutor with ID: {predicateId}, MaxAttempts: {maxAttempts}, Timeout: {timeoutBetweenAttempts}ms");
        }

        /// <summary>
        /// Запускает выполнение предикатов
        /// </summary>
        public PredicateExecutor Start()
        {
            if (isDisposed)
            {
                NLogger.Error($"Cannot start disposed PredicateExecutor with ID: {PredicateId}");
                return this;
            }

            NLogger.Log($"Starting PredicateExecutor with ID: {PredicateId}");
            TryExecutePredicates();
            return this;
        }
        
        /// <summary>
        /// Попытка выполнения предикатов
        /// </summary>
        private void TryExecutePredicates()
        {
            if (isDisposed) return;
            
            currentAttempt++;
            NLogger.Log($"PredicateExecutor '{PredicateId}': Attempt {currentAttempt}/{maxAttempts}");
            
            try
            {
                // Проверяем все предикаты
                bool allPredicatesPassed = true;
                for (int i = 0; i < predicates.Count; i++)
                {
                    var predicate = predicates[i];
                    bool result = false;
                    
                    try
                    {
                        result = predicate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        NLogger.Error($"PredicateExecutor '{PredicateId}': Predicate {i + 1} threw exception: {ex.Message}");
                        allPredicatesPassed = false;
                        break;
                    }
                    
                    if (!result)
                    {
                        NLogger.Log($"PredicateExecutor '{PredicateId}': Predicate {i + 1} returned false");
                        allPredicatesPassed = false;
                        break;
                    }
                    
                    NLogger.Log($"PredicateExecutor '{PredicateId}': Predicate {i + 1} passed");
                }
                
                if (allPredicatesPassed)
                {
                    // Все предикаты прошли успешно - выполняем полезную нагрузку
                    ExecutePayload();
                }
                else
                {
                    // Не все предикаты прошли - планируем повторную попытку
                    ScheduleRetry();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error during predicate execution");
            }
        }
        
        /// <summary>
        /// Выполняет полезную нагрузку
        /// </summary>
        private void ExecutePayload()
        {
            try
            {
                NLogger.Log($"PredicateExecutor '{PredicateId}': All predicates passed, executing payload");
                payloadAction.Invoke();
                NLogger.Log($"PredicateExecutor '{PredicateId}': Payload executed successfully");
                
                // Успешное выполнение - удаляем из кеша
                RemoveFromCache();
            }
            catch (Exception ex)
            {
                HandleError(ex, $"Error during payload execution \n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n{new StackTrace(ex, true)}\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");
            }
        }
        
        /// <summary>
        /// Планирует повторную попытку
        /// </summary>
        private void ScheduleRetry()
        {
            if (currentAttempt >= maxAttempts)
            {
                var error = new Exception($"Max attempts ({maxAttempts}) reached for PredicateExecutor '{PredicateId}'");
                HandleError(error, "Max attempts exceeded");
                return;
            }
            
            if (timeoutBetweenAttempts > 0)
            {
                NLogger.Log($"PredicateExecutor '{PredicateId}': Scheduling retry in {timeoutBetweenAttempts}ms");
                
                // Создаем и запускаем таймер для повторной попытки
                timer = new TimerCompat();
                timer.TimerCompatInit(timeoutBetweenAttempts, (obj, arg) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                    TryExecutePredicates();
                }, loop: false);
                timer.Start();
            }
            else
            {
                // Немедленная повторная попытка
                TryExecutePredicates();
            }
        }
        
        /// <summary>
        /// Обработка ошибок
        /// </summary>
        private void HandleError(Exception ex, string context)
        {
            NLogger.Error($"PredicateExecutor '{PredicateId}' - {context}: {ex.Message}");
            NLogger.Error($"Stack trace from creation:\n{stackTrace}");
            
            // Вызываем обработчик ошибок
            errorHandler?.Invoke(ex, context);
            
            // Удаляем из кеша при ошибке
            RemoveFromCache();
        }
        
        /// <summary>
        /// Стандартный обработчик ошибок
        /// </summary>
        private void DefaultErrorHandler(Exception ex, string context)
        {
            NLogger.Error($"[DEFAULT ERROR HANDLER] PredicateExecutor '{PredicateId}': {context}");
            NLogger.Error($"Exception details: {ex}");
        }
        
        /// <summary>
        /// Удаляет экземпляр из кеша
        /// </summary>
        private void RemoveFromCache()
        {
            if (InstanceCache.TryRemove(PredicateId, out _))
            {
                NLogger.Log($"PredicateExecutor '{PredicateId}' removed from cache");
            }
            Dispose();
        }

        /// <summary>
        /// Обновляет предикаты
        /// </summary>
        public PredicateExecutor UpdatePredicates(List<Func<bool>> newPredicates)
        {
            if (newPredicates == null || newPredicates.Count == 0)
                throw new ArgumentNullException(nameof(newPredicates));

            this.predicates = new List<Func<bool>>(newPredicates);
            NLogger.Log($"PredicateExecutor '{PredicateId}': Updated predicates (count: {newPredicates.Count})");
            return this;
        }

        /// <summary>
        /// Добавляет предикат
        /// </summary>
        public PredicateExecutor AddPredicate(Func<bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            predicates.Add(predicate);
            NLogger.Log($"PredicateExecutor '{PredicateId}': Added predicate (total: {predicates.Count})");
            return this;
        }

        /// <summary>
        /// Останавливает выполнение и удаляет из кеша
        /// </summary>
        public PredicateExecutor Stop()
        {
            NLogger.Log($"Stopping PredicateExecutor '{PredicateId}'");
            RemoveFromCache();
            return this;
        }
        
        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            
            isDisposed = true;
            
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
            
            predicates?.Clear();
            payloadAction = null;
            errorHandler = null;
            
            NLogger.Log($"PredicateExecutor '{PredicateId}' disposed");
        }
        
        // Статические методы для работы с кешем
        
        /// <summary>
        /// Получает экземпляр из кеша по ID
        /// </summary>
        public static PredicateExecutor GetFromCache(string predicateId)
        {
            InstanceCache.TryGetValue(predicateId, out var instance);
            return instance;
        }
        
        /// <summary>
        /// Получает все активные экземпляры
        /// </summary>
        public static IEnumerable<PredicateExecutor> GetAllActive()
        {
            return InstanceCache.Values.ToList();
        }
        
        /// <summary>
        /// Очищает весь кеш
        /// </summary>
        public static void ClearCache()
        {
            var instances = InstanceCache.Values.ToList();
            foreach (var instance in instances)
            {
                instance.Dispose();
            }
            InstanceCache.Clear();
            NLogger.Log("PredicateExecutor cache cleared");
        }
        
        /// <summary>
        /// Получает количество активных экземпляров
        /// </summary>
        public static int GetActiveCount()
        {
            return InstanceCache.Count;
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
