
using NECS.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class ECSEntityManager
    {

        public LockedDictionary<long, ECSEntity> EntityStorage = new LockedDictionary<long, ECSEntity>();
        public LockedDictionary<string, ECSEntity> PreinitializedEntities = new LockedDictionary<string, ECSEntity>();//for selectablemap, shopdb, ect.

        public void OnAddNewEntity(ECSEntity Entity)
        {
            Entity.manager = this;
            if (!EntityStorage.TryAdd(Entity.instanceId, Entity))
                NLogger.Error("error add entity to storage");
            TaskEx.RunAsync(() =>
            {
                Entity.entityComponents.RegisterAllComponents();
                ManagerScope.instance.systemManager.OnEntityCreated(Entity);
            });
        }

        public void OnRemoveEntity(ECSEntity Entity)
        {
            EntityStorage.Remove(Entity.instanceId, out Entity);
            Entity.OnDelete();
            TaskEx.RunAsync(() =>
            {
                ManagerScope.instance.systemManager.OnEntityDestroyed(Entity);
            });
        }


        public void OnAddComponent(ECSEntity Entity, ECSComponent Component)
        {
            TaskEx.RunAsync(() =>
            {
                ManagerScope.instance.systemManager.OnEntityComponentAddedReaction(Entity, Component);
            });
        }

        public void OnRemoveComponent(ECSEntity Entity, ECSComponent Component)
        {
            TaskEx.RunAsync(() =>
            {
                ManagerScope.instance.systemManager.OnEntityComponentRemovedReaction(Entity, Component);
            });
        }
    }
}
