
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

        private ConcurrentDictionary<long, long> EntityWorldOwnerCache = new ConcurrentDictionary<long, long>();

        public (ECSWorld world, ECSEntity entity) GetWorldAndEntity(long entityId)
        {
            if(EntityWorldOwnerCache.TryGetValue(entityId, out var worldId))
            {
                if(WorldDB.TryGetValue(worldId, out var world))
                {
                    if(world.entityManager.EntityStorage.TryGetValue(entityId, out var entity))
                    {
                        return (world, entity);
                    }
                    else
                    {
                        EntityWorldOwnerCache.TryRemove(entityId, out _);
                    }
                }
                else
                {
                    EntityWorldOwnerCache.TryRemove(entityId, out _);
                }
            }
            foreach(var world in WorldDB.Values)
            {
                if(world.entityManager.EntityStorage.TryGetValue(entityId, out var entity))
                {
                    EntityWorldOwnerCache.TryAdd(entityId, world.instanceId);
                    return (world, entity);
                }
            }
            return (null, null);
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
                    EntityWorldOwnerCache.TryRemove(entityId, out _);
                }
                else
                {
                    EntityWorldOwnerCache.TryRemove(entityId, out _);
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
                        world = new ECSWorld();
                        world.InitWorldScope(null);
                        WorldDB.TryAdd(worldId, world);
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
            world.InitWorldScope(null);
            WorldDB.TryAdd(Guid.NewGuid().GuidToLong(), world);
            return world;
        }

        private static ConcurrentDictionary<long, ECSWorld> WorldDB = new ConcurrentDictionary<long, ECSWorld>();
        
        public override void InitializeProcess()
        {
            EntitySerialization.InitSerialize();
            ECSComponentManager.IdStaticCache();
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
