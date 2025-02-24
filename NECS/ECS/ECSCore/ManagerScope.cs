
using NECS.Core.Logging;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NECS.ECS.ECSCore
{
public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class ManagerScope : IService
    {
        public ECSContractsManager systemManager;
        public ECSEntityManager entityManager;
        public ECSComponentManager componentManager;
        public ECSEventManager eventManager;

        private static ManagerScope cacheInstance;
        public static ManagerScope instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<ManagerScope>();
                return cacheInstance;
            }
        }

        public void InitManagerScope()
        {
            entityManager = new ECSEntityManager();
            componentManager = new ECSComponentManager();
            EntitySerialization.InitSerialize();
            ECSComponentManager.IdStaticCache();
            eventManager = new ECSEventManager();
            eventManager.IdStaticCache();
            systemManager = new ECSContractsManager();
            systemManager.InitializeSystems();
            eventManager.InitializeEventManager();
            var timer = new TimerCompat(5, (obj, arg) => systemManager.RunTimeDependContracts(), true);
            timer.Start();
        }

        public override void InitializeProcess()
        {
            InitManagerScope();
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
