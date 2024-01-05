using NECS.Core.Logging;
using NECS.Extensions;
using NECS.GameEngineAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IService : SGT
    {
        public bool ServiceInitialized;
        public bool ServicePostInitialized;
        protected bool CustomSetupInitialized = false;
        protected Action initializedCallbackCache;
        protected Action postInitializedCallbackCache;
        public void ServicePostInitializeProcessAsync(Action initializedCallback)
        {
            TaskEx.RunAsync(() =>
            {
                ServicePostInitialize(initializedCallback);
            });
        }
        
        public void ServiceInitializeAsync(Action initializedCallback)
        {
            TaskEx.RunAsync(() =>
            {
                ServiceInitialize(initializedCallback);
            });
        }

        public void ServicePostInitialize(Action initializedCallback)
        {
            CustomSetupInitialized = false;
            postInitializedCallbackCache = initializedCallback;
            PostInitializeProcess();
            if(!CustomSetupInitialized)
            {
                ServicePostInitialized = true;
                initializedCallback?.Invoke();
            }
        }

        public void ServiceInitialize(Action initializedCallback)
        {
            CustomSetupInitialized = false;
            initializedCallbackCache = initializedCallback;
            InitializeProcess();
            if (!CustomSetupInitialized)
            {
                ServiceInitialized = true;
                initializedCallback?.Invoke();
            }
        }


        #region static
        public static Action? OnInitializedAllServices;
        private static EngineApiObjectBehaviour ServiceStorage;
        private static ConcurrentHashSet<IService> AllServiceList = new ConcurrentHashSet<IService>();
        private static ConcurrentHashSet<IService> servicesInitialized = new ConcurrentHashSet<IService>();
        private static ConcurrentHashSet<IService> servicesPostInitialized = new ConcurrentHashSet<IService>();
        public static bool ServicesInitialized
        {
            get
            {
                if (AllServiceList.Count == 0)
                    return false;
                if (servicesInitialized.Count == AllServiceList.Count) return true; else return false;
            }
        }

        public static bool ServicesPostInitialized
        {
            get
            {
                if (AllServiceList.Count == 0)
                    return false;
                if (servicesPostInitialized.Count == AllServiceList.Count) return true; else return false;
            }
        }

        public static void RegisterAllServices()
        {
            #if UNITY_5_3_OR_NEWER
            ServiceStorage = new UnityEngine.GameObject("ServiceStorage").AddComponent<EngineApiObjectBehaviour>();
            #endif
            AllServiceList = new ConcurrentHashSet<IService>(ECSAssemblyExtensions.GetAllSubclassOf(typeof(IService)).Where(x => !x.IsAbstract).Select(x => IService.InitalizeSingleton(x, ServiceStorage, true)).Cast<IService>().ToList());
        }

        public static void InitializeService(IService service)
        {
            InitializeAllServices(new List<IService> { service });
        }

        public static void InitializeAllServices(List<IService> selectedServices = null)
        {
            var serviceList = selectedServices == null ? AllServiceList : new ConcurrentHashSet<IService>(selectedServices);
            serviceList.Where(x => !servicesInitialized.Contains(x)).ForEach(x => x.ServiceInitialize(() => {
                IService.servicesInitialized.Add(x);
                if (Defines.ServiceSetupLogging)
                    NLogger.LogService($"[{servicesInitialized.Count}/{AllServiceList.Count}]Service {x.GetType().Name} preinitialized.");
                if(ServicesInitialized)
                    NLogger.LogSuccess($"All services pre-initialized!");
            }));
            IService.servicesInitialized.Where(x => !servicesPostInitialized.Contains(x)).ForEach(x => x.ServicePostInitialize(() => {
                IService.servicesPostInitialized.Add(x);
                if (Defines.ServiceSetupLogging)
                    NLogger.LogService($"[{servicesPostInitialized.Count}/{AllServiceList.Count}]Service {x.GetType().Name} fully initialized.");
                if (ServicesPostInitialized)
                {
                    NLogger.LogSuccess($"All services fully initialized!");
                    OnInitializedAllServices?.Invoke();
                }
            }));
            TaskEx.RunAsync(() =>
            {
                int countTries = 0;
                
                while (IService.servicesPostInitialized.Count != AllServiceList.Count)
                {
                    var serviceToPostInitialize = servicesInitialized.Where(x => !servicesPostInitialized.Contains(x)).ToList();
                    serviceToPostInitialize.ForEach(x => x.ServicePostInitialize(() => {
                        IService.servicesPostInitialized.Add(x);
                        if(Defines.ServiceSetupLogging)
                            NLogger.LogService($"[{servicesPostInitialized.Count}/{AllServiceList.Count}]Service {x.GetType().Name} fully initialized.");
                        if (ServicesPostInitialized)
                        {
                            NLogger.LogSuccess($"All services fully initialized!");
                            OnInitializedAllServices?.Invoke();
                        }
                    }));
                    countTries++;
                    if (countTries > 100)
                    {
                        string notInitializedServices = "\n";
                        AllServiceList.Where(x => !servicesPostInitialized.Contains(x)).ToList().ToList().ForEach(x => notInitializedServices += x.GetType().ToString() + "\n");
                        NLogger.LogError("No all services will initialized " + notInitializedServices);
                        break;
                    }
                    Task.Delay(600).Wait();
                }
            });
        }
#endregion
    }
}
