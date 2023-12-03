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

        public static T InitalizeSingleton<T>(EngineApiObjectBehaviour behaviour = null, bool packetInitialize = false) where T : SGT
        {
            return (T)InitalizeSingleton(typeof(T), behaviour, packetInitialize);
        }

        public static SGT InitalizeSingleton(Type singletonType, EngineApiObjectBehaviour behaviour = null, bool packetInitialize = false)
        {
            SGT instance = null;
            lock (instances)
            {
                try
                {
                    instances.TryGetValue(singletonType, out instance);
                    if (instance == null)
                    {
                        if (behaviour != null)
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
                        }
                        else
                        {
                            instance = (SGT)Activator.CreateInstance(singletonType);
                        }
                        instances.Add(singletonType, instance);
                    }
                }
                catch (Exception ex)
                {
                    NLogger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
                }
            }
            if(!packetInitialize)
                instance.InitializeProcess();
            return instance;
        }

        public static T Get<T>(EngineApiObjectBehaviour behaviour = null) where T : SGT
        {
            return (T)getInstance<T>(behaviour);
        }

        public static T tryGetInstance<T>(EngineApiObjectBehaviour behaviour = null) where T : SGT
        {
            try
            {
                return getInstance<T>(behaviour);
            }
            catch (Exception ex)
            {
                NLogger.Log(ex.Message + "\n" + ex.StackTrace);
            }
            return null;
        }

        public static T getInstance<T>(EngineApiObjectBehaviour behaviour = null) where T : SGT
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
        public abstract void PostInitializeProcess();
        public abstract void OnDestroyReaction();

        public
#if NET
            override
#endif
              void OnDestroy()
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
                    NLogger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
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
#if UNITY_5_3_OR_NEWER
                            UnityEngine.GameObject.Destroy(sgt);
#else
                            sgt.Destroy(sgt);
#endif
                        }
                        catch { }
                        instances.Remove(type);
                    }
                }
                catch (Exception ex)
                {
                    NLogger.Error("Escape from lock: " + ex.Message + " _________ " + ex.StackTrace);
                }
            }
        }
    }
}
