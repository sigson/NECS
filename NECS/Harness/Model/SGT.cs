using NECS.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class SGT
    {
        private static IDictionary<Type, SGT> instances = new ConcurrentDictionary<Type, SGT>();

        public static Action<object> ExtendedInstancing = null; //for game engine additional instancing

        public static T InitalizeSingleton<T>(object behaviour = null) where T : SGT
        {
            return (T)InitalizeSingleton(typeof(T), behaviour);
        }

        public static SGT InitalizeSingleton(Type singletonType, object behaviour = null)
        {
            SGT instance = null;
            lock (instances)
            {
                try
                {
                    instances.TryGetValue(singletonType, out instance);
                    if (instance == null)
                    {
                        if(ExtendedInstancing != null)
                        {
                            ExtendedInstancing(behaviour);
                        }
                        instances.Add(singletonType, instance);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
                }
            }
            instance.InitializeProcess();
            return instance;
        }

        public static T Get<T>(object behaviour = null) where T : SGT
        {
            return (T)getInstance<T>(behaviour);
        }

        public static T tryGetInstance<T>(object behaviour = null) where T : SGT
        {
            try
            {
                return getInstance<T>(behaviour);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message + "\n" + ex.StackTrace);
            }
            return null;
        }

        public static T getInstance<T>(object behaviour = null) where T : SGT
        {
            SGT instance = null;
            instances.TryGetValue(typeof(T), out instance);
            if (instance == null)
            {
                throw new Exception($"Singleton {typeof(T)} not initialized");
                //return null;
            }
            return (T)instance;
        }

        public abstract void InitializeProcess();
        public abstract void OnDestroyReaction();

        protected virtual void OnDestroy()
        {
            lock (instances)
            {
                try
                {
                    SGT instance = null;
                    instances.TryGetValue(this.GetType(), out instance);
                    if (instance != null)
                    {
                        instances.Remove(this.GetType());
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
                }
            }
            OnDestroyReaction();
        }

        public static void DestroySGT<T>() where T : SGT
        {
            DestroySGT(typeof(T));
        }

        public static void DestroySGT(Type type)
        {
            try
            {
                if (instances.TryGetValue(type, out var sgt))
                {
                    instances.Remove(type);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
            }
        }
    }
}
