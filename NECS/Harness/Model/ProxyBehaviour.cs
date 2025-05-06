using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions.ThreadingSync;
using NECS.GameEngineAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract
#if GODOT4_0_OR_GREATER
    partial
#endif
    class ProxyBehaviour : EngineApiObjectBehaviour
    {
        private ConcurrentDictionary<long, Action> ExecuteInstructionEvent = new ConcurrentDictionary<long, Action>();
        //private ConcurrentDictionary<long, ProxyJob> Jobs = new ConcurrentDictionary<long, ProxyJob>();
        //private List<long> ExecutedList = new List<long>();
        private ConcurrentDictionary<long, object> FunctionsResult = new ConcurrentDictionary<long, object>();
        private System.Threading.Thread unityThread = System.Threading.Thread.CurrentThread;
        public void SendEvent(ECSEvent ecsEvent)
        {
            TaskEx.RunAsync(() =>
            {
                ecsEvent.instanceId = Guid.NewGuid().GuidToLongR() + DateTime.Now.Ticks;
                ManagerScope.instance.eventManager.OnEventAdd(ecsEvent);
            });
        }

        private object invocationLocker = new object();
        private int invocationCounter = 0;

        private bool proxyInited = false;
        private void InitProxyBehaviour()
        {
            if(!proxyInited)
            {
                this.OnLateUpdate += () =>
                {
                    if (ExecuteInstructionEvent != null && invocationCounter > 0)
                    {
                        //ExecutedList.Clear();
                        foreach (var invoke in ExecuteInstructionEvent)
                        {
                            invoke.Value();
                            Interlocked.Decrement(ref invocationCounter);
                            ExecuteInstructionEvent.TryRemove(invoke.Key, out _);
                        }
                    }
                };
                proxyInited = true;
            }
        }

        public void ExecuteInstruction(Action<object> action, object Object, string ErrorLog = "", Action exceptionCallback = null)
        {
            InitProxyBehaviour();
            if (Thread.CurrentThread != unityThread)
            {
                var actionkey = Guid.NewGuid().GuidToLongR();
                {
                    ExecuteInstructionEvent.TryAdd(actionkey, () =>
                    {
                        try
                        {
                            action(Object);
                        }
                        catch (Exception ex)
                        {
                            if (ErrorLog == "")
                            {
                                NLogger.Error("Error execution: \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            else
                            {
                                NLogger.Error(ErrorLog + ": \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    NLogger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
                                }
                            }
                        }
                        //ExecutedList.Add(actionkey);
                    });
                    Interlocked.Increment(ref invocationCounter);
                }
            }
            else
            {
                action(Object);
            }
        }

        public void ExecuteInstruction(Action action, string ErrorLog = "", Action exceptionCallback = null)
        {
            InitProxyBehaviour();
            if (Thread.CurrentThread != unityThread)
            {
                var actionkey = Guid.NewGuid().GuidToLongR();
                {
                    ExecuteInstructionEvent.TryAdd(actionkey, () =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            if (ErrorLog == "")
                            {
                                NLogger.Error("Error execution: \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            else
                            {
                                NLogger.Error(ErrorLog + ": \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    NLogger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
                                }
                            }
                        }
                        //ExecutedList.Add(actionkey);
                    });
                    Interlocked.Increment(ref invocationCounter);
                }
            }
            else
            {
                action();
            }
        }

        public T ExecuteFunction<T>(Func<T> function, string ErrorLog = "", Action exceptionCallback = null) where T : class
        {
            InitProxyBehaviour();
            if (Thread.CurrentThread != unityThread)
            {
                long resuldUID = 0;
                var actionkey = Guid.NewGuid().GuidToLongR();
                {
                    resuldUID = Guid.NewGuid().GuidToLongR();
                    Action<long, Func<T>> resultActionWrapper = (id, func) =>
                    {
                        T result = default(T);
                        try
                        {
                            result = func();
                        }
                        catch (Exception ex)
                        {
                            NLogger.Error(ErrorLog + ": " + ex.Message + "\n" + ex.StackTrace);
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    NLogger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
                                }
                            }
                        }
                        //ExecutedList.Add(actionkey);
                        FunctionsResult.TryAdd(id, result);
                    };
                    ExecuteInstructionEvent.TryAdd(actionkey, () => resultActionWrapper(resuldUID, function));
                    Interlocked.Increment(ref invocationCounter);
                }
                int leaveCounter = 0;
                while (!FunctionsResult.ContainsKey(resuldUID) && leaveCounter < 5000)
                {
                    Task.Delay(5).Wait();
                    leaveCounter++;
                }
                var ret = FunctionsResult[resuldUID] as T;
                //lock (FunctionsResult)
                FunctionsResult.TryRemove(resuldUID, out _);
                return ret;
            }
            else
                return function();
        }
    }
}
