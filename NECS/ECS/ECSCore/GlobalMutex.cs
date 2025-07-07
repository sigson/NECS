using System.Collections.Concurrent;
using NECS.ECS.ECSCore;
using NECS.Extensions;

namespace NECS.ECS.ECSCore
{
    public class GlobalMutex
    {
        /// <summary>
        /// <entity instance id,<id = Entity.instanceid + IECSObject.GetId , lock object>>
        /// </summary>
        private static DictionaryWrapper<long, DictionaryWrapper<long, object>> MutexStorage = new DictionaryWrapper<long, DictionaryWrapper<long, object>>();

        private static DictionaryWrapper<object, object> objectLock = new DictionaryWrapper<object, object>();

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
                DictionaryWrapper<long, object> entityMutex = null;
                if (!MutexStorage.TryGetValue(lockEntity.instanceId, out entityMutex))
                {
                    entityMutex = new DictionaryWrapper<long, object>();
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
                MutexStorage.Remove(removedEntity.instanceId, out _);
            }
        }
    }
}