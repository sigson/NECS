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
        protected bool CustomSetupInitialized;
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
            PostInitializeProcess();
            if(!CustomSetupInitialized)
            {
                ServicePostInitialized = true;
                initializedCallback?.Invoke();
            }
            else
            {
                postInitializedCallbackCache = initializedCallback;
            }
        }

        public void ServiceInitialize(Action initializedCallback)
        {
            InitializeProcess();
            if (!CustomSetupInitialized)
            {
                ServiceInitialized = true;
                initializedCallback?.Invoke();
            }
            else
            {
                initializedCallbackCache = initializedCallback;
            }
        }


        #region static
        private static EngineApiObjectBehaviour ServiceStorage;
        private static List<IService> AllServiceList;
        private static int servicesInitialized = 0;
        private static int servicesPostInitialized = 0;
        public static bool ServicesInitialized
        {
            get
            {
                if (AllServiceList.Count == 0)
                    return false;
                if (servicesInitialized == AllServiceList.Count) return true; else return false;
            }
        }

        public static bool ServicesPostInitialized
        {
            get
            {
                if (AllServiceList.Count == 0)
                    return false;
                if (servicesPostInitialized == AllServiceList.Count) return true; else return false;
            }
        }

        public static void RegisterAllServices()
        {
            //FindObjectsOfType<IService>();
            AllServiceList = ECSAssemblyExtensions.GetAllSubclassOf(typeof(IService)).Where(x => !x.IsAbstract).Select(x => IService.InitalizeSingleton(x, ServiceStorage, true)).Cast<IService>().ToList();
        }

        public static void InitializeAllServices()
        {
            AllServiceList.ForEach(x => x.ServiceInitialize(() => IService.servicesInitialized++));
            AllServiceList.ForEach(x => x.ServicePostInitialize(() => IService.servicesPostInitialized++));
        }
        #endregion
    }
}
