using NECS.Core.Logging;
using NECS.GameEngineAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class SGT : ProxyBehaviour
    {
        private static Dictionary<Type, SGT> instances = new Dictionary<Type, SGT>();

        public static T InitalizeSingleton<T>(IEngineApiObjectBehaviour behaviour = null) where T : SGT
        {
            return (T)InitalizeSingleton(typeof(T), behaviour);
        }

        public static SGT InitalizeSingleton(Type singletonType, IEngineApiObjectBehaviour behaviour = null)
        {
            SGT instance = null;
            lock (instances)
            {
                try
                {
                    instances.TryGetValue(singletonType, out instance);
                    if (instance == null)
                    {
                        instance = (SGT)behaviour.GetComponent(singletonType);
                        if (instance == null)
                        {
                            if (behaviour != null)
                                instance = (SGT)behaviour.gameObject.AddComponent(singletonType);
                            else
                            {
                                instance = (SGT)new EngineApiObjectBehaviour().AddComponent(singletonType);
                                DontDestroyOnLoad(instance.gameObject);
                            }
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

        public static T Get<T>(IEngineApiObjectBehaviour behaviour = null) where T : SGT
        {
            return (T)getInstance<T>(behaviour);
        }

        public static T tryGetInstance<T>(IEngineApiObjectBehaviour behaviour = null) where T : SGT
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

        public static T getInstance<T>(IEngineApiObjectBehaviour behaviour = null) where T : SGT
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

        public override void OnDestroy()
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
            lock (instances)
            {
                try
                {
                    if (instances.TryGetValue(type, out var sgt))
                    {
                        try
                        {
                            sgt.Destroy(sgt);
                        }
                        catch { }
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
}
