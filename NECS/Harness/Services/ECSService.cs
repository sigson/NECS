
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
using NECS.ECS.ECSCore;

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class ECSService : IService
    {
        private static ECSService cacheInstance;
        public static ECSService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<ECSService>();
                return cacheInstance;
            }
        }

        public DictionaryWrapper<long, ECSEntity> EntityCache = new DictionaryWrapper<long, ECSEntity>();

        private DictionaryWrapper<long, long> EntityWorldOwnerCache = new DictionaryWrapper<long, long>();

        public ECSEventManager eventManager;

        public (ECSWorld world, ECSEntity entity) GetWorldAndEntity(long entityId)
        {
            if (EntityWorldOwnerCache.TryGetValue(entityId, out var worldId))
            {
                if (WorldDB.TryGetValue(worldId, out var world))
                {
                    if (world.entityManager.EntityStorage.TryGetValue(entityId, out var entity))
                    {
                        return (world, entity);
                    }
                    else
                    {
                        EntityWorldOwnerCache.Remove(entityId, out _);
                    }
                }
                else
                {
                    EntityWorldOwnerCache.Remove(entityId, out _);
                }
            }
            foreach (var world in WorldDB.Values)
            {
                if (world.entityManager.EntityStorage.TryGetValue(entityId, out var entity))
                {
                    EntityWorldOwnerCache.TryAdd(entityId, world.instanceId);
                    return (world, entity);
                }
            }
            if (EntityCache.TryGetValue(entityId, out var cachedentity))
            {
                return (cachedentity.ECSWorldOwner, cachedentity);
            }
            else
            {
                return (null, null);
            }
        }

        public ECSWorld GetEntityWorld(long entityId)
        {
            if (EntityWorldOwnerCache.TryGetValue(entityId, out var worldId))
            {
                if (WorldDB.TryGetValue(worldId, out var world))
                {
                    if (world.entityManager.EntityStorage.ContainsKey(entityId))
                    {
                        return world;
                    }
                    EntityWorldOwnerCache.Remove(entityId, out _);
                }
                else
                {
                    EntityWorldOwnerCache.Remove(entityId, out _);
                }
            }
            
            foreach (var world in WorldDB.Values)
            {
                if (world.entityManager.EntityStorage.ContainsKey(entityId))
                {
                    EntityWorldOwnerCache.TryAdd(entityId, world.instanceId);
                    return world;
                }
            }
            
            return null;
        }

        public ECSWorld GetWorld(long worldId)
        {
            if (!WorldDB.TryGetValue(worldId, out var world))
            {
                lock(WorldDB)//what are you looking? go away creep
                {
                    if (!WorldDB.TryGetValue(worldId, out var gworld))
                    {
                        gworld = new ECSWorld();
                        gworld.instanceId = worldId;
                        WorldDB.TryAdd(worldId, gworld);
                        gworld.InitWorldScope(null);
                        world = gworld;
                    }
                    return gworld;
                }
            }
            return world;
        }

        public ECSWorld GetFirstWorld()
        {
            return WorldDB.Values.FirstOrDefault();
        }

        public ECSWorld GetWorld()
        {
            var world = new ECSWorld();
            WorldDB.TryAdd(world.instanceId, world);
            world.InitWorldScope(null);
            return world;
        }

        private static DictionaryWrapper<long, ECSWorld> WorldDB = new DictionaryWrapper<long, ECSWorld>();

        public IEnumerable<ECSWorld> GetAllWorlds(bool onlyNonInitialized = false) => WorldDB.Values.Where(x => onlyNonInitialized ? !x.Initialized : x.Initialized);
        
        public override void InitializeProcess()
        {
            EntitySerialization.InitSerialize();
            eventManager = new ECSEventManager();
            eventManager.IdStaticCache();
            eventManager.InitializeEventManager();
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }

        protected override Action<int>[] GetInitializationSteps()
        {
            return new Action<int>[]
            {
                (step) => {
                    InitializeProcess();
                },
                (step) => {
                    PostInitializeProcess();
                }
            };
        }

        protected override void SetupCallbacks(List<IService> allServices)
        {
            
        }
    }
}
