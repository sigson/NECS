using System.Collections.Concurrent;
using NECS.ECS.ECSCore;

namespace NECS.ECS.ECSCore
{
    public class GlobalMutex
    {
        /// <summary>
        /// <entity instance id,<id = Entity.instanceid + IECSObject.GetId , lock object>>
        /// </summary>
        private static ConcurrentDictionary<long, ConcurrentDictionary<long, object>> MutexStorage = new ConcurrentDictionary<long, ConcurrentDictionary<long, object>>();

        private static ConcurrentDictionary<object, object> objectLock = new ConcurrentDictionary<object, object>();

        public static object GetSimpleMutex(object lockableObject)
        {
            object mutex = null;
            if (!objectLock.TryGetValue(lockableObject, out mutex))
            {
                mutex = new object();
                objectLock.TryAdd(lockableObject, mutex);
            }
            return mutex;
        }

        public static object GetMutex(ECSEntity lockEntity, IECSObject lockObjectPair)
        {
            object mutex = null;
            var id = lockEntity.instanceId + lockObjectPair.GetId();
            lock (MutexStorage)
            {
                ConcurrentDictionary<long, object> entityMutex = null;
                if (!MutexStorage.TryGetValue(lockEntity.instanceId, out entityMutex))
                {
                    entityMutex = new ConcurrentDictionary<long, object>();
                    MutexStorage.TryAdd(lockEntity.instanceId, entityMutex);
                }
                if (!entityMutex.TryGetValue(id, out mutex))
                {
                    mutex = new object();
                    entityMutex.TryAdd(id, mutex);
                }
            }
            return mutex;
        }
        /// <summary>
        /// dont use, really bad idea
        /// </summary>
        /// <param name="removedEntity"></param>
        public static void ClearEntity(ECSEntity removedEntity)
        {
            lock(MutexStorage)
            {
                MutexStorage.TryRemove(removedEntity.instanceId, out _);
            }
        }
    }
}