
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
using NECS.Extensions.ThreadingSync;

namespace NECS.ECS.ECSCore
{
    public class ECSEntityManager
    {
        private ECSWorld world;
        public LockedDictionary<long, ECSEntity> EntityStorage = new LockedDictionary<long, ECSEntity>();
        public LockedDictionary<string, ECSEntity> PreinitializedEntities = new LockedDictionary<string, ECSEntity>();//for selectablemap, shopdb, ect.

        public ECSEntityManager(ECSWorld world)
        {
            this.world = world;
        }

        public void OnAddNewEntity(ECSEntity Entity, bool silent = false)
        {
            Entity.manager = this;
            Entity.ECSWorldOwner = world;
            if (!EntityStorage.TryAdd(Entity.instanceId, Entity))
                NLogger.Error("error add entity to storage");
            OnAddNewEntityReaction(Entity, silent);
        }

        public void OnAddNewEntityReaction(ECSEntity Entity, bool silent = false)
        {
            // Entity.manager = this;
            // if (!EntityStorage.TryAdd(Entity.instanceId, Entity))
            //     NLogger.Error("error add entity to storage");
            if(!silent)
            {
                TaskEx.RunAsync(() =>
                {
                    Entity.entityComponents.RegisterAllComponents();
                    this.world.contractsManager.OnEntityCreated(Entity);
                });
            }
        }

        public void OnRemoveEntity(ECSEntity Entity)
        {
            EntityStorage.ExecuteOnRemoveLocked(Entity.instanceId, out Entity, (longv, entt) => {});
            Entity.OnDelete();
            TaskEx.RunAsync(() =>
            {
                this.world.contractsManager.OnEntityDestroyed(Entity);
            });
        }


        public void OnAddComponent(ECSEntity Entity, ECSComponent Component)
        {
            if (Entity == null || Entity.manager == null)
                return;
            TaskEx.RunAsync(() =>
            {
                this.world.contractsManager.OnEntityComponentAddedReaction(Entity, Component);
            });
        }

        public void OnRemoveComponent(ECSEntity Entity, ECSComponent Component)
        {
            if (Entity == null || Entity.manager == null)
                return;
            TaskEx.RunAsync(() =>
            {
                this.world.contractsManager.OnEntityComponentRemovedReaction(Entity, Component);
            });
        }
    }
}
