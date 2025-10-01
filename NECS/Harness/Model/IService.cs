using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions;
using NECS.Extensions.ThreadingSync;
using NECS.GameEngineAPI;

namespace NECS.Harness.Model
{
    public abstract
#if GODOT4_0_OR_GREATER
    partial
#endif
    class IService : SGT
    {
        #region SyncManagers
        
        // Информация о callback-е
        public class ServiceCallback
        {
            public string ServiceId { get; set; }
            public string AuthorServiceId { get; set; } // Сервис, который создал этот callback
            public int TargetStep { get; set; }
            public int AuthorBlockingStep { get; set; } // Шаг, с которого начинать блокировать автора
            public Func<Dictionary<string, ServiceStepInfo>, bool> Condition { get; set; }
            public Action Callback { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsRunning { get; set; }
            
            public ServiceCallback(string serviceId, string authorServiceId, int targetStep, int authorBlockingStep, Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback)
            {
                ServiceId = serviceId;
                AuthorServiceId = authorServiceId;
                TargetStep = targetStep;
                AuthorBlockingStep = authorBlockingStep;
                Condition = condition;
                Callback = callback;
                IsCompleted = false;
                IsRunning = false;
            }
        }

        // Информация о состоянии шага сервиса
        public class ServiceStepInfo
        {
            public string ServiceId { get; set; }
            public int CurrentStep { get; set; }
            public int TotalSteps { get; set; }
            public bool IsStepCompleted { get; set; }
            public bool IsServiceFailed { get; set; }
            public bool IsStepRunning { get; set; }
            public bool IsFrozen { get; set; } // Новое поле для отслеживания заморозки
            public DateTime StepStartTime { get; set; }
            public DateTime? StepEndTime { get; set; }
            public DateTime? FrozenTime { get; set; } // Время заморозки
            
            public ServiceStepInfo(string serviceId, int totalSteps)
            {
                ServiceId = serviceId;
                CurrentStep = 0;
                TotalSteps = totalSteps;
                IsStepCompleted = false;
                IsServiceFailed = false;
                IsStepRunning = false;
                IsFrozen = false;
                StepStartTime = DateTime.Now;
            }
            
            public ServiceStepInfo Clone()
            {
                return new ServiceStepInfo(ServiceId, TotalSteps)
                {
                    CurrentStep = this.CurrentStep,
                    IsStepCompleted = this.IsStepCompleted,
                    IsServiceFailed = this.IsServiceFailed,
                    IsStepRunning = this.IsStepRunning,
                    IsFrozen = this.IsFrozen,
                    StepStartTime = this.StepStartTime,
                    StepEndTime = this.StepEndTime,
                    FrozenTime = this.FrozenTime
                };
            }
        }

        // Event Loop события
        public abstract class EventLoopEvent
        {
            public DateTime Timestamp { get; private set; }

            public Action CallbackOnApply = () => {};
            
            protected EventLoopEvent()
            {
                Timestamp = DateTime.Now;
            }
        }

        public class RegisterServiceEvent : EventLoopEvent
        {
            public string ServiceId { get; set; }
            public Action<int>[] Steps { get; set; }
            
            public RegisterServiceEvent(string serviceId, Action<int>[] steps)
            {
                ServiceId = serviceId;
                Steps = steps;
            }
        }

        public class RegisterCallbackEvent : EventLoopEvent
        {
            public string TargetServiceId { get; set; }
            public string AuthorServiceId { get; set; }
            public int TargetStep { get; set; }
            public int AuthorBlockingStep { get; set; }
            public Func<Dictionary<string, ServiceStepInfo>, bool> Condition { get; set; }
            public Action Callback { get; set; }
            
            public RegisterCallbackEvent(string targetServiceId, string authorServiceId, int targetStep, int authorBlockingStep,
                Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback)
            {
                TargetServiceId = targetServiceId;
                AuthorServiceId = authorServiceId;
                TargetStep = targetStep;
                AuthorBlockingStep = authorBlockingStep;
                Condition = condition;
                Callback = callback;
            }
        }

        public class CompleteStepEvent : EventLoopEvent
        {
            public string ServiceId { get; set; }
            
            public CompleteStepEvent(string serviceId)
            {
                ServiceId = serviceId;
            }
        }

        public class FailServiceEvent : EventLoopEvent
        {
            public string ServiceId { get; set; }
            public string Reason { get; set; }
            
            public FailServiceEvent(string serviceId, string reason)
            {
                ServiceId = serviceId;
                Reason = reason;
            }
        }

        public class CallbackCompletedEvent : EventLoopEvent
        {
            public ServiceCallback Callback { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            
            public CallbackCompletedEvent(ServiceCallback callback, bool success, string errorMessage = null)
            {
                Callback = callback;
                Success = success;
                ErrorMessage = errorMessage;
            }
        }

        // Новые события для заморозки/разморозки
        public class FreezeServiceEvent : EventLoopEvent
        {
            public string ServiceId { get; set; }
            
            public FreezeServiceEvent(string serviceId)
            {
                ServiceId = serviceId;
            }
        }

        public class UnfreezeServiceEvent : EventLoopEvent
        {
            public string ServiceId { get; set; }
            
            public UnfreezeServiceEvent(string serviceId)
            {
                ServiceId = serviceId;
            }
        }

        // Система синхронизации с Event Loop
        public class ServiceSynchronizationManager
        {
            // Event Loop - потокобезопасная очередь событий
            private readonly ConcurrentQueue<EventLoopEvent> _eventQueue = new ConcurrentQueue<EventLoopEvent>();
            
            // Основное состояние (обрабатывается только в мониторинговом потоке)
            private readonly Dictionary<string, ServiceStepInfo> _serviceStates = new Dictionary<string, ServiceStepInfo>();
            
            // ИСПРАВЛЕНИЕ: Callback'и теперь индексируются по комбинации serviceId + step
            private readonly Dictionary<string, Dictionary<int, List<ServiceCallback>>> _serviceStepCallbacks = new Dictionary<string, Dictionary<int, List<ServiceCallback>>>();
            private readonly Dictionary<string, Action<int>[]> _serviceSteps = new Dictionary<string, Action<int>[]>();
            
            // Отслеживание callback'ов по сервисам-авторам
            private readonly Dictionary<string, List<ServiceCallback>> _authorCallbacks = new Dictionary<string, List<ServiceCallback>>();
            
            private bool _isMonitoringRunning = false;
            private bool _stopMonitoring = false;
            // УДАЛЯЕМ: _currentMonitoringStep больше не нужен
            
            // События (потокобезопасные)
            public event Action<string, int, string> OnServiceStepChanged;
            public event Action<string, string> OnServiceFailed;
            public event Action<string> OnServiceCompleted;
            public event Action OnAllServicesCompleted;
            public event Action<string> OnServiceFrozen; // Новое событие
            public event Action<string> OnServiceUnfrozen; // Новое событие
            
            // Асинхронная регистрация сервиса через Event Loop
            public void RegisterService(string serviceId, Action<int>[] steps)
            {
                _eventQueue.Enqueue(new RegisterServiceEvent(serviceId, steps));
            }
            
            // Асинхронная регистрация callback-а через Event Loop
            public void RegisterCallback(string targetServiceId, string authorServiceId, int targetStep, int authorBlockingStep,
                Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback)
            {
                _eventQueue.Enqueue(new RegisterCallbackEvent(targetServiceId, authorServiceId, targetStep, authorBlockingStep, condition, callback));
            }

            // Асинхронное завершение шага через Event Loop
            public void CompleteCurrentStep(string serviceId)
            {
                _eventQueue.Enqueue(new CompleteStepEvent(serviceId));
            }
            
            // Асинхронная отметка об ошибке сервиса через Event Loop
            public void FailService(string serviceId, string reason)
            {
                _eventQueue.Enqueue(new FailServiceEvent(serviceId, reason));
            }

            // Новые методы для заморозки/разморозки
            public void FreezeServiceInitialization(string serviceId, Action Callback)
            {
                _eventQueue.Enqueue(new FreezeServiceEvent(serviceId) { CallbackOnApply = Callback });
            }

            public void UnfreezeServiceInitialization(string serviceId, Action Callback)
            {
                _eventQueue.Enqueue(new UnfreezeServiceEvent(serviceId) {CallbackOnApply = Callback});
            }
            
            // Асинхронное уведомление о завершении callback-а через Event Loop
            internal void NotifyCallbackCompleted(ServiceCallback callback, bool success, string errorMessage = null)
            {
                _eventQueue.Enqueue(new CallbackCompletedEvent(callback, success, errorMessage));
            }

            // Запуск Event Loop мониторинга
            public void StartAllServices(int awaitServicesCount = 0)
            {
                bool shouldStart = false;
                
                // Атомарная проверка и установка флага
                if (!_isMonitoringRunning)
                {
                    _isMonitoringRunning = true;
                    _stopMonitoring = false;
                    shouldStart = true;
                }
                
                if (shouldStart)
                {
                    TaskEx.RunAsync(() => EventLoopMonitoring(awaitServicesCount));
                }
            }
            
            // Остановка мониторинга
            public void StopMonitoring()
            {
                _stopMonitoring = true;
            }
            
            // Основной Event Loop мониторинг (выполняется в одном потоке)
            private void EventLoopMonitoring(int awaitServicesCount)
            {
                TimerCompat timer = null;

                Action eventAfterLoopFunc = () =>
                {
                    // Обрабатываем оставшиеся события
                    ProcessEventQueue();
                    
                    _isMonitoringRunning = false;
                    
                    if (AreAllServicesCompleted())
                    {
                        OnAllServicesCompleted?.Invoke();
                    }
                };

                Action eventLoopFunc = () =>
                {
                    // Обрабатываем все события из очереди
                    ProcessEventQueue();

                    // Выполняем логику мониторинга
                    ProcessMonitoringStep();

                    // Небольшая задержка

                    if (!(!_stopMonitoring && (!AreAllServicesCompleted() || _serviceStates.Count < awaitServicesCount)))
                    {
                        if (timer != null)
                        {
                            timer?.Stop();
                            eventAfterLoopFunc();
                        }
                    }
                };



                if (Defines.OneThreadMode)
                {
                    timer = new TimerCompat(10, (obj, arg) => eventLoopFunc(), true);
                    timer.Start();

                }
                else
                {
                    while (!_stopMonitoring && (!AreAllServicesCompleted() || _serviceStates.Count < awaitServicesCount))
                    {
                        eventLoopFunc();
                        Thread.Sleep(10);
                    }

                    eventAfterLoopFunc();
                }
            }
            
            // Обработка всех событий из очереди (синхронно в мониторинговом потоке)
            private void ProcessEventQueue()
            {
                while (_eventQueue.TryDequeue(out var eventItem))
                {
                    ProcessEvent(eventItem);
                }
            }

            // Обработка одного события (синхронно, без блокировок)
            private void ProcessEvent(EventLoopEvent eventItem)
            {
                switch (eventItem)
                {
                    case RegisterServiceEvent registerEvent:
                        ProcessRegisterService(registerEvent);
                        break;

                    case RegisterCallbackEvent callbackEvent:
                        ProcessRegisterCallback(callbackEvent);
                        break;

                    case CompleteStepEvent completeEvent:
                        ProcessCompleteStep(completeEvent);
                        break;

                    case FailServiceEvent failEvent:
                        ProcessFailService(failEvent);
                        break;

                    case CallbackCompletedEvent callbackCompletedEvent:
                        ProcessCallbackCompleted(callbackCompletedEvent);
                        break;

                    case FreezeServiceEvent freezeEvent:
                        ProcessFreezeService(freezeEvent);
                        break;

                    case UnfreezeServiceEvent unfreezeEvent:
                        ProcessUnfreezeService(unfreezeEvent);
                        break;
                }
                eventItem.CallbackOnApply();
            }
            
            // Обработка регистрации сервиса (синхронно)
            private void ProcessRegisterService(RegisterServiceEvent eventItem)
            {
                _serviceSteps[eventItem.ServiceId] = eventItem.Steps;
                _serviceStates[eventItem.ServiceId] = new ServiceStepInfo(eventItem.ServiceId, eventItem.Steps.Length);
                
                // ИСПРАВЛЕНИЕ: Инициализируем структуру callback'ов для каждого сервиса
                if (!_serviceStepCallbacks.ContainsKey(eventItem.ServiceId))
                {
                    _serviceStepCallbacks[eventItem.ServiceId] = new Dictionary<int, List<ServiceCallback>>();
                }
                
                // Инициализируем список callback'ов для этого сервиса-автора
                if (!_authorCallbacks.ContainsKey(eventItem.ServiceId))
                {
                    _authorCallbacks[eventItem.ServiceId] = new List<ServiceCallback>();
                }
                
                IService.getInstance<IService>(eventItem.ServiceId).SetupCallbacks(IService.AllServiceList.ToList());
                
                OnServiceStepChanged?.Invoke(eventItem.ServiceId, 0, "Service registered");
            }
            
            // ИСПРАВЛЕНИЕ: Обработка регистрации callback-а (синхронно)
            public void ProcessRegisterCallback(RegisterCallbackEvent eventItem)
            {
                // Инициализируем структуру для целевого сервиса, если её нет
                if (!_serviceStepCallbacks.ContainsKey(eventItem.TargetServiceId))
                {
                    _serviceStepCallbacks[eventItem.TargetServiceId] = new Dictionary<int, List<ServiceCallback>>();
                }
                
                // Инициализируем список для конкретного шага целевого сервиса
                if (!_serviceStepCallbacks[eventItem.TargetServiceId].ContainsKey(eventItem.TargetStep))
                {
                    _serviceStepCallbacks[eventItem.TargetServiceId][eventItem.TargetStep] = new List<ServiceCallback>();
                }
                
                var callback = new ServiceCallback(
                    eventItem.TargetServiceId, 
                    eventItem.AuthorServiceId,
                    eventItem.TargetStep,
                    eventItem.AuthorBlockingStep,
                    eventItem.Condition, 
                    eventItem.Callback);
                
                _serviceStepCallbacks[eventItem.TargetServiceId][eventItem.TargetStep].Add(callback);
                
                // Добавляем callback в список callback'ов автора
                if (!_authorCallbacks.ContainsKey(eventItem.AuthorServiceId))
                {
                    _authorCallbacks[eventItem.AuthorServiceId] = new List<ServiceCallback>();
                }
                _authorCallbacks[eventItem.AuthorServiceId].Add(callback);
            }

            // Обработка завершения шага (синхронно)
            private void ProcessCompleteStep(CompleteStepEvent eventItem)
            {
                if (!_serviceStates.ContainsKey(eventItem.ServiceId))
                    return;

                var serviceState = _serviceStates[eventItem.ServiceId];
                if (serviceState.IsServiceFailed || serviceState.IsFrozen) // Проверяем заморозку
                    return;

                int currentStep = serviceState.CurrentStep;

                OnServiceStepChanged?.Invoke(eventItem.ServiceId, currentStep, $"Step {currentStep} completed");

                // Проверяем, завершен ли сервис
                if (currentStep >= serviceState.TotalSteps - 1)
                {
                    OnServiceCompleted?.Invoke(eventItem.ServiceId);
                }
                
                serviceState.IsStepCompleted = true;
                serviceState.IsStepRunning = false;
                serviceState.StepEndTime = DateTime.Now;
            }
            
            // Обработка ошибки сервиса (синхронно)
            private void ProcessFailService(FailServiceEvent eventItem)
            {
                if (_serviceStates.ContainsKey(eventItem.ServiceId))
                {
                    _serviceStates[eventItem.ServiceId].IsServiceFailed = true;
                    _serviceStates[eventItem.ServiceId].IsStepRunning = false;
                    OnServiceFailed?.Invoke(eventItem.ServiceId, eventItem.Reason);
                }
            }

            // Обработка заморозки сервиса (синхронно)
            private void ProcessFreezeService(FreezeServiceEvent eventItem)
            {
                if (_serviceStates.ContainsKey(eventItem.ServiceId))
                {
                    var serviceState = _serviceStates[eventItem.ServiceId];
                    if (!serviceState.IsFrozen)
                    {
                        serviceState.IsFrozen = true;
                        serviceState.FrozenTime = DateTime.Now;
                        OnServiceFrozen?.Invoke(eventItem.ServiceId);
                    }
                }
            }

            // Обработка разморозки сервиса (синхронно)
            private void ProcessUnfreezeService(UnfreezeServiceEvent eventItem)
            {
                if (_serviceStates.ContainsKey(eventItem.ServiceId))
                {
                    var serviceState = _serviceStates[eventItem.ServiceId];
                    if (serviceState.IsFrozen)
                    {
                        serviceState.IsFrozen = false;
                        serviceState.FrozenTime = null;
                        OnServiceUnfrozen?.Invoke(eventItem.ServiceId);

                        int currentStep = serviceState.CurrentStep;

                        OnServiceStepChanged?.Invoke(eventItem.ServiceId, currentStep, $"Step {currentStep} completed");

                        // Проверяем, завершен ли сервис
                        if (currentStep >= serviceState.TotalSteps - 1)
                        {
                            OnServiceCompleted?.Invoke(eventItem.ServiceId);
                        }
                        
                        serviceState.IsStepCompleted = true;
                        serviceState.IsStepRunning = false;
                        serviceState.StepEndTime = DateTime.Now;
                    }
                }
            }
            
            // Обработка завершения callback-а (синхронно)
            private void ProcessCallbackCompleted(CallbackCompletedEvent eventItem)
            {
                eventItem.Callback.IsRunning = false;
                
                if (eventItem.Success)
                {
                    eventItem.Callback.IsCompleted = true;
                }
                else
                {
                    // Ошибка в callback-е приводит к ошибке сервиса
                    ProcessFailService(new FailServiceEvent(eventItem.Callback.ServiceId, eventItem.ErrorMessage));
                }
            }
            
            // Логика мониторинга (синхронно, без блокировок)
            private void ProcessMonitoringStep()
            {
                
                
                // Шаг 2: Запускаем готовые сервисы
                StartReadyServices();
            }

            // ИСПРАВЛЕНИЕ: Обработка callback-ов для всех сервисов (синхронно)
            private void ProcessCallbacksForService(string serviceId, Dictionary<string, ServiceStepInfo> currentStates, Dictionary<int, List<ServiceCallback>> serviceCallbacks, int hiddenStep)
            {
                //string serviceId = serviceCallbacks.Key;

                // Проверяем, не заморожен ли сервис
                if (currentStates.ContainsKey(serviceId) && currentStates[serviceId].IsFrozen)
                {
                    return; // Пропускаем callback'и для замороженных сервисов
                }

                foreach (var stepCallbacks in serviceCallbacks)
                {
                    int step = stepCallbacks.Key;
                    var callbacks = stepCallbacks.Value;

                    foreach (var callback in callbacks)
                    {
                        if (!callback.IsCompleted && !callback.IsRunning)
                        {
                            try
                            {
                                if (callback.Condition(currentStates) && hiddenStep >= callback.TargetStep)
                                {
                                    callback.IsRunning = true;
                                    TaskEx.RunAsync(() =>
                                    {
                                        try
                                        {
                                            callback.Callback();
                                            NotifyCallbackCompleted(callback, true);
                                        }
                                        catch (Exception ex)
                                        {
                                            NotifyCallbackCompleted(callback, false,
                                                $"Error in callback execution for step {callback.TargetStep}: {ex.Message}");
                                        }
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                ProcessFailService(new FailServiceEvent(callback.ServiceId,
                                    $"Error in callback condition for step {step}: {ex.Message}"));
                            }
                        }
                    }
                }
            }

            // Запуск готовых сервисов (синхронно)
            private void StartReadyServices()
            {
                var servicesToStart = new List<string>();
                
                foreach (var kvp in _serviceStates)
                {
                    var serviceId = kvp.Key;
                    var serviceState = kvp.Value;
                    
                    if (serviceState.IsServiceFailed || serviceState.IsStepRunning || serviceState.IsFrozen) // Проверяем заморозку
                        continue;
                        
                    int nextStep = serviceState.IsStepCompleted ? serviceState.CurrentStep + 1 : serviceState.CurrentStep;
                    
                    if (nextStep >= serviceState.TotalSteps)
                        continue;
                    
                    if (IsServiceReadyForStep(serviceId, nextStep))
                    {
                        servicesToStart.Add(serviceId);
                    }
                }
                
                // Запускаем сервисы последовательно
                foreach (var serviceId in servicesToStart)
                {
                    StartServiceStepSequentially(serviceId);
                }
            }
            
            // ИСПРАВЛЕНИЕ: Проверка готовности сервиса (синхронно)
            private bool IsServiceReadyForStep(string serviceId, int step)
            {
                ProcessCallbacksForService(serviceId, _serviceStates, _serviceStepCallbacks[serviceId], step);

                // ИСПРАВЛЕНИЕ: Проверяем callback'и для конкретного сервиса и шага
                if (_serviceStepCallbacks.ContainsKey(serviceId) &&
                    _serviceStepCallbacks[serviceId].ContainsKey(step))
                {
                    var serviceCallbacks = _serviceStepCallbacks[serviceId][step];
                    if (serviceCallbacks.Any(cb => !cb.IsCompleted))
                    {
                        return false;
                    }
                }
                
                // Проверяем, что все callback'и, созданные этим сервисом, завершены
                // НО ТОЛЬКО если текущий шаг >= AuthorBlockingStep для каждого callback'а
                if (_authorCallbacks.ContainsKey(serviceId))
                {
                    var authoredCallbacks = _authorCallbacks[serviceId];
                    
                    // Фильтруем callback'и, которые должны блокировать на данном шаге
                    var blockingCallbacks = authoredCallbacks.Where(cb => step >= cb.AuthorBlockingStep).ToList();
                    
                    if (blockingCallbacks.Any(cb => !cb.IsCompleted && !cb.IsRunning))
                    {
                        // Есть незавершенные callback'и, которые должны блокировать на этом шаге
                        return false;
                    }
                    
                    if (blockingCallbacks.Any(cb => cb.IsRunning))
                    {
                        // Есть выполняющиеся callback'и, которые должны блокировать на этом шаге
                        return false;
                    }
                }
                
                return true;
            }
            
            // Последовательный запуск шага сервиса (синхронно)
            private void StartServiceStepSequentially(string serviceId)
            {
                if (!_serviceStates.ContainsKey(serviceId) || !_serviceSteps.ContainsKey(serviceId))
                    return;
                    
                var serviceState = _serviceStates[serviceId];
                var serviceSteps = _serviceSteps[serviceId];
                
                if (serviceState.IsServiceFailed || serviceState.IsStepRunning || serviceState.IsFrozen) // Проверяем заморозку
                    return;
                
                int stepToExecute = serviceState.IsStepCompleted ? serviceState.CurrentStep + 1 : serviceState.CurrentStep;
                
                if (stepToExecute >= serviceSteps.Length)
                    return;
                
                // Обновляем состояние сервиса
                serviceState.CurrentStep = stepToExecute;
                serviceState.IsStepCompleted = false;
                serviceState.IsStepRunning = true;
                serviceState.StepStartTime = DateTime.Now;
                serviceState.StepEndTime = null;
                
                OnServiceStepChanged?.Invoke(serviceId, stepToExecute, $"Starting step {stepToExecute}");

                // Выполняем шаг последовательно
                TaskEx.RunAsync(() =>
                {
                    try
                    {
                        serviceSteps[stepToExecute](stepToExecute);

                        // Автоматически завершаем шаг
                        //ProcessCompleteStep();
                        this.CompleteCurrentStep(serviceId);
                    }
                    catch (Exception ex)
                    {
                        // ProcessFailService(new FailServiceEvent(serviceId, $"Error in step {stepToExecute}: {ex.Message}"));
                        this.FailService(serviceId, $"Error in step {stepToExecute}: {ex.Message}");
                    }
                });
                
            }
            
            // Получение снимка состояний (синхронно)
            private Dictionary<string, ServiceStepInfo> GetCurrentStatesSnapshot()
            {
                var snapshot = new Dictionary<string, ServiceStepInfo>();
                foreach (var kvp in _serviceStates)
                {
                    snapshot[kvp.Key] = kvp.Value.Clone();
                }
                return snapshot;
            }
            
            // Публичные методы для получения состояния (потокобезопасные через снимки)
            public ServiceStepInfo GetServiceState(string serviceId)
            {
                // Создаем событие для получения состояния и ждем результат
                // Для простоты возвращаем null, если сервис не найден
                // В production можно реализовать через отдельное событие запроса состояния
                return null;
            }
            
            public Dictionary<string, ServiceStepInfo> GetAllServiceStates()
            {
                // Аналогично, для получения полного состояния нужно отдельное событие
                return new Dictionary<string, ServiceStepInfo>();
            }
            
            // Проверка завершения всех сервисов (синхронно)
            private bool AreAllServicesCompleted()
            {
                return _serviceStates.Values.All(state => 
                    (state.CurrentStep >= state.TotalSteps - 1 && state.IsStepCompleted) || state.IsServiceFailed);
            }
            
            // Информация о мониторинге
            public bool IsMonitoringRunning()
            {
                return _isMonitoringRunning;
            }
            
            // УДАЛЯЕМ: GetCurrentMonitoringStep больше не актуален
            
            // Получение размера очереди событий
            public int GetEventQueueSize()
            {
                return _eventQueue.Count;
            }
            
            // Получение информации о callback'ах автора (для отладки)
            public List<ServiceCallback> GetAuthorCallbacks(string authorServiceId)
            {
                return _authorCallbacks.ContainsKey(authorServiceId) 
                    ? new List<ServiceCallback>(_authorCallbacks[authorServiceId]) 
                    : new List<ServiceCallback>();
            }

            // Проверка заморозки сервиса
            public bool IsServiceFrozen(string serviceId)
            {
                return _serviceStates.ContainsKey(serviceId) && _serviceStates[serviceId].IsFrozen;
            }
        }
        
        private static readonly ServiceSynchronizationManager _syncManager = new ServiceSynchronizationManager();
        public static ServiceSynchronizationManager SyncManager => _syncManager;

        private static ConcurrentHashSet<IService> AllServiceList = new ConcurrentHashSet<IService>();
        private static EngineApiObjectBehaviour ServiceStorage;

        public static void StartAllServices()
        {
            _syncManager.StartAllServices();
        }

        public static void InitializeService(IService service)
        {
            InitializeAllServices(new List<IService> { service });
        }

        // Новые статические методы для заморозки/разморозки
        public static void FreezeServiceInitialization(string serviceId)
        {
            _syncManager.FreezeServiceInitialization(serviceId, () => {});
        }

        public static void UnfreezeServiceInitialization(string serviceId)
        {
            _syncManager.UnfreezeServiceInitialization(serviceId, () => {});
        }

        public static void RegisterAllServices(List<Type> excludeServices = null)
        {
#if UNITY_5_3_OR_NEWER
            ServiceStorage = new UnityEngine.GameObject("ServiceStorage").AddComponent<EngineApiObjectBehaviour>();
#endif
#if GODOT
            ServiceStorage = new EngineApiObjectBehaviour().InitEAOB("ServiceStorage");
            lock(GodotRootStorage.TreeLocker)
            {
                GodotRootStorage.globalRoot.AddChild(ServiceStorage);
            }
#endif
            if (ServiceStorage != null)
                ServiceStorage.AddComponent<ProxyMockComponent>();

            if (excludeServices == null)
            {
                excludeServices = new List<Type>();
            }

            AllServiceList = new ConcurrentHashSet<IService>(ECSAssemblyExtensions.GetAllSubclassOf(typeof(IService))
                .Where(x => !x.IsAbstract && !excludeServices.Contains(x))
                .Select(x => IService.InitalizeSingleton(x, ServiceStorage, true))
                .Cast<IService>()
                .ToList());
                
            SyncManager.OnServiceStepChanged += (serviceid, step, message) =>
            {
                NLogger.LogService($"Service {serviceid} step {step}: {message}");
            };
            SyncManager.OnServiceFailed += (serviceid, reason) =>
            {
                NLogger.LogService($"Service {serviceid} failed: {reason}");
            };
            SyncManager.OnServiceCompleted += (serviceid) =>
            {
                NLogger.LogService($"Service {serviceid} completed");
            };
            SyncManager.OnAllServicesCompleted += () =>
            {
                NLogger.LogService($"All services is initialized");
            };
            SyncManager.OnServiceFrozen += (serviceid) =>
            {
                NLogger.LogService($"Service {serviceid} frozen");
            };
            SyncManager.OnServiceUnfrozen += (serviceid) =>
            {
                NLogger.LogService($"Service {serviceid} unfrozen");
            };
        }

        public static void InitializeAllServices(List<IService> selectedServices = null)
        {
            var serviceList = selectedServices == null ? AllServiceList : new ConcurrentHashSet<IService>(selectedServices);
            serviceList.ForEach(x => x.BeginInitializationProcess());
            _syncManager.StartAllServices(serviceList.Count);
        }

        #endregion

        // Абстрактные методы для реализации в наследниках
        protected abstract Action<int>[] GetInitializationSteps();
        protected abstract void SetupCallbacks(List<IService> allServices);

        // Инициализация сервиса (асинхронная регистрация через Event Loop)
        public override void BeginInitializationProcess()
        {
            var steps = GetInitializationSteps();
            var serviceId = GetSGTId();

            _syncManager.RegisterService(serviceId, steps);
        }
        
        // Регистрация callback-а (асинхронная через Event Loop)
        protected void RegisterCallback(string targetServiceId, int targetStep, 
            Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback, int authorBlockingStep = 1)
        {
            _syncManager.RegisterCallback(targetServiceId, GetSGTId(), targetStep, authorBlockingStep, condition, callback);
        }
        
        // Регистрация callback-а на текущий сервис (асинхронная через Event Loop)
        protected void RegisterCallback(int targetStep, 
            Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback, int authorBlockingStep = 1)
        {
            _syncManager.RegisterCallback(GetSGTId(), GetSGTId(), targetStep, authorBlockingStep, condition, callback);
        }

        protected void RegisterCallbackUnsafe(string targetServiceId, int targetStep,
            Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback, int authorBlockingStep = 1)
        {
            _syncManager.ProcessRegisterCallback(new RegisterCallbackEvent(targetServiceId, GetSGTId(), targetStep, authorBlockingStep, condition, callback));
        }

        // Регистрация callback-а на текущий сервис (асинхронная через Event Loop)
        protected void RegisterCallbackUnsafe(int targetStep,
            Func<Dictionary<string, ServiceStepInfo>, bool> condition, Action callback, int authorBlockingStep = 1)
        {
            _syncManager.ProcessRegisterCallback(new RegisterCallbackEvent(GetSGTId(), GetSGTId(), targetStep, authorBlockingStep, condition, callback));
        }
        
        // Завершение текущего шага (асинхронное через Event Loop)
        protected void CompleteCurrentStep()
        {
            _syncManager.CompleteCurrentStep(GetSGTId());
        }

        // Методы для заморозки/разморозки текущего сервиса
        protected void FreezeCurrentService(Action Callback = null)
        {
            _syncManager.FreezeServiceInitialization(GetSGTId(), Callback == null ? () => { } : Callback);
        }

        protected void UnfreezeCurrentService(Action Callback = null)
        {
            _syncManager.UnfreezeServiceInitialization(GetSGTId(), Callback == null ? () => { } : Callback);
        }

        // Проверка состояния заморозки текущего сервиса
        protected bool IsCurrentServiceFrozen()
        {
            return _syncManager.IsServiceFrozen(GetSGTId());
        }
    }
}