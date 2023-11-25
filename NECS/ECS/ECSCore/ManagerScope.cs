
using NECS.Core.Logging;
using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    public class ManagerScope : IService
    {
        public ECSSystemManager systemManager;
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
            systemManager = new ECSSystemManager();
            systemManager.InitializeSystems();
            eventManager.InitializeEventManager();
            TaskEx.RunAsync(() =>
            {
                systemManager.RunSystems();
                Task.Delay(5).Wait();
            });
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
