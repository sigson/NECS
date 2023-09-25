
using NECS.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    public class ECSEntityManager
    {

        public ConcurrentDictionary<long, ECSEntity> EntityStorage = new ConcurrentDictionary<long, ECSEntity>();
        public ConcurrentDictionary<string, ECSEntity> PreinitializedEntities = new ConcurrentDictionary<string, ECSEntity>();//for selectablemap, shopdb, ect.

        public void OnAddNewEntity(ECSEntity Entity)
        {
            Entity.manager = this;
            if (!EntityStorage.TryAdd(Entity.instanceId, Entity))
                Logger.Error("error add entity to storage");
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => {
                    Entity.entityComponents.RegisterAllComponents();
                    ManagerScope.instance.systemManager.OnEntityCreated(Entity);
                });
            };
            asyncUpd();
        }

        public void OnRemoveEntity(ECSEntity Entity)
        {
            EntityStorage.Remove(Entity.instanceId, out Entity);
            Entity.OnDelete();
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => {
                    ManagerScope.instance.systemManager.OnEntityDestroyed(Entity);
                });
            };
            asyncUpd();
        }


        public void OnAddComponent(ECSEntity Entity, ECSComponent Component)
        {
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => {
                    ManagerScope.instance.systemManager.OnEntityComponentAddedReaction(Entity, Component);
                });
            };
            asyncUpd();
        }

        public void OnRemoveComponent(ECSEntity Entity, ECSComponent Component)
        {
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => ManagerScope.instance.systemManager.OnEntityComponentRemovedReaction(Entity, Component));
            };
            asyncUpd();
        }
    }
}
