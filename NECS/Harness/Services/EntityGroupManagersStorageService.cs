using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions;
using NECS.GameEngineAPI;
using NECS.Harness.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Services
{
    public class EntityGroupManagersStorageService : IService
    {
        public static EntityGroupManagersStorageService instance => IService.Get<EntityGroupManagersStorageService>();

        private ConcurrentDictionary<long, ConcurrentDictionary<Type, IManager>> groupManagersStorage = new ConcurrentDictionary<long, ConcurrentDictionary<Type, IManager>>(); //long is id of connectpoint id(component id, entity id, random id)

        private ConcurrentDictionary<Type, ConcurrentDictionary<long, ConcurrentHashSet<IManager>>> cachedManagersEntities = new ConcurrentDictionary<Type, ConcurrentDictionary<long, ConcurrentHashSet<IManager>>>(); //type manager, long id entity, hashset managers
        private IEngineApiObjectBehaviour ManagersStorageObject = null;
        private object lockObject = new object();

        public void AddEntityCache(IManager manager, long entityId)// where T : IGroupManager<T1> where T1 : IECSObject
        {
            var managerType = manager.GetType();
            //lock(cachedManagersEntities)
            {
                ConcurrentDictionary<long, ConcurrentHashSet<IManager>> typeManagerStorage = null;
                if (!cachedManagersEntities.TryGetValue(managerType, out typeManagerStorage))
                {
                    typeManagerStorage = new ConcurrentDictionary<long, ConcurrentHashSet<IManager>>();
                    cachedManagersEntities[managerType] = typeManagerStorage;
                }
                ConcurrentHashSet<IManager> entityManagersSrotage = null;
                if (!typeManagerStorage.TryGetValue(entityId, out entityManagersSrotage))
                {
                    entityManagersSrotage = new ConcurrentHashSet<IManager>();
                    entityManagersSrotage.Add(manager);
                    typeManagerStorage[entityId] = entityManagersSrotage;
                }
            }
        }

        public void RemoveEntityCache(IManager manager, long entityId)// where T : IGroupManager<T1> where T1 : IECSObject
        {
            var managerType = manager.GetType();
            //lock (cachedManagersEntities)
            {
                if (cachedManagersEntities.TryGetValue(managerType, out var typeManagerStorage))
                {
                    if (typeManagerStorage.TryGetValue(entityId, out var entityManagersSrotage))
                    {
                        var _manager = manager;// as IGroupManager<IECSObject>;
                        if (entityManagersSrotage.Contains(_manager))
                        {
                            entityManagersSrotage.Remove(_manager);
                        }
                        if (entityManagersSrotage.Count == 0)
                        {
                            typeManagerStorage.TryRemove(entityId, out _);
                        }
                    }
                    if (typeManagerStorage.Count == 0)
                    {
                        cachedManagersEntities.TryRemove(managerType, out _);
                    }
                }
            }
        }
        public T AddGroupManager<T, T1>(IECSObject id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            //return (T)AddGroupManager<T1>(id, typeof(T));

            IManager manager = null;
            var managerType = typeof(T);
            //lock (lockObject)
            {
                ConcurrentDictionary<Type, IManager> idGroup = null;
                if (!groupManagersStorage.TryGetValue(id.instanceId, out idGroup))
                {
                    idGroup = new ConcurrentDictionary<Type, IManager>();
                    groupManagersStorage.TryAdd(id.instanceId, idGroup);
                }
                if (idGroup.TryGetValue(managerType, out manager))
                {
                    Logger.Error("error adding group manager - manager already added");
                    return (T)manager;
                }
                else
                {
                    if (ManagersStorageObject != null)
                    {
                        var cleanManager = this.ExecuteFunction<IEngineApiObjectBehaviour>(() => {
                            var newManager = ManagersStorageObject.AddComponent(managerType);
                            (newManager as IManager).isNoSetupChild = true;
                            (newManager as IManager).ConnectPoint = id;
                            return newManager;
                        });
                        manager = (T)cleanManager;
                        //manager.ConnectPoint = id;
                        idGroup.TryAdd(managerType, manager);
                    }
                }
            }
            return (T)manager;
        }

        public T GetGroupManager<T, T1>(IECSObject id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            return GetGroupManager<T, T1>(id.instanceId);
        }

        public T AddOrGetGroupManager<T, T1>(IECSObject id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            var groupManager = GetGroupManager<T, T1>(id);
            if (groupManager == null)
                groupManager = AddGroupManager<T, T1>(id);
            return (T)groupManager;
        }

        public T GetGroupManager<T, T1>(long id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            ConcurrentDictionary<Type, IManager> idGroup = null;
            var managerType = typeof(T);
            //lock (lockObject)
            {
                if (groupManagersStorage.TryGetValue(id, out idGroup))
                {
                    IManager manager = null;
                    if (idGroup.TryGetValue(managerType, out manager))
                    {
                        return (T)manager;
                    }
                }
            }
            return default(T);
        }

        public T GetGroupManagerCacheClientFirst<T, T1>(long entityId) where T : IGroupManager<T1> where T1 : IECSObject
        {
            var gManagers = GetGroupManagerCacheClient<T, T1>(entityId);
            if (gManagers != null)
                return gManagers.First();
            return default(T);
        }

        public IEnumerable<T> GetGroupManagerCacheClient<T, T1>(long entityId) where T : IGroupManager<T1> where T1 : IECSObject
        {
            var gManagerType = typeof(T);
            //lock (cachedManagersEntities)
            {
                if (cachedManagersEntities.TryGetValue(gManagerType, out var entityManagersStorage))
                {
                    if (entityManagersStorage.TryGetValue(entityId, out var entityManagers))
                    {
                        return entityManagers.Cast<T>();
                    }
                }
            }
            return null;
        }

        public T RemoveGroupManager<T, T1>(IECSObject id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            return RemoveGroupManager<T, T1>(id.instanceId);
        }

        public T RemoveGroupManager<T, T1>(long id) where T : IGroupManager<T1> where T1 : IECSObject
        {
            T manager = default(T);
            var managerType = typeof(T);
            //lock (lockObject)
            {
                ConcurrentDictionary<Type, IManager> idGroup = null;
                if (!groupManagersStorage.TryGetValue(id, out idGroup))
                {
                    Logger.Error("error removing group manager - id is not presented");
                    return default(T);
                }

                if (!idGroup.TryGetValue(managerType, out var _manager))
                {
                    Logger.Error("error removing group manager - type element is not presented");
                    return default(T);
                }
                else
                {
                    manager = (T)_manager;
                    manager.ForEach(x => manager.Remove(x));
                    idGroup.TryRemove(managerType, out _);
                    if (ManagersStorageObject != null)
                    {
                        this.ExecuteInstruction(() => Destroy(manager));
                    }
                }
            }
            return manager;
        }

        public override void InitializeProcess()
        {
            ManagersStorageObject = new EngineApiObjectBehaviour("EntityGroupManagersStorage");
            DontDestroyOnLoad(ManagersStorageObject);
        }

        public override void OnDestroyReaction()
        {

        }

        public override void PostInitializeProcess()
        {

        }
    }

}
