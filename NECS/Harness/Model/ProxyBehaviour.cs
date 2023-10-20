using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.GameEngineAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class ProxyBehaviour : EngineApiObjectBehaviour
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
                ecsEvent.instanceId = Guid.NewGuid().GuidToLong() + DateTime.Now.Ticks;
                ManagerScope.instance.eventManager.OnEventAdd(ecsEvent);
            });
        }

        private object invocationLocker = new object();
        private int invocationCounter = 0;
        public void ExecuteInstruction(Action<object> action, object Object, string ErrorLog = "", Action exceptionCallback = null)
        {
            if (Thread.CurrentThread != unityThread)
            {
                var actionkey = Guid.NewGuid().GuidToLong();
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
                                Logger.Error("Error execution: \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            else
                            {
                                Logger.Error(ErrorLog + ": \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    Logger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
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
            if (Thread.CurrentThread != unityThread)
            {
                var actionkey = Guid.NewGuid().GuidToLong();
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
                                Logger.Error("Error execution: \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            else
                            {
                                Logger.Error(ErrorLog + ": \n" + ex.Message + "\n" + action.Method.ToString() + "\n" + ex.StackTrace);
                            }
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    Logger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
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
            if (Thread.CurrentThread != unityThread)
            {
                long resuldUID = 0;
                var actionkey = Guid.NewGuid().GuidToLong();
                {
                    resuldUID = Guid.NewGuid().GuidToLong();
                    Action<long, Func<T>> resultActionWrapper = (id, func) =>
                    {
                        T result = default(T);
                        try
                        {
                            result = func();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ErrorLog + ": " + ex.Message + "\n" + ex.StackTrace);
                            if (exceptionCallback != null)
                            {
                                try
                                {
                                    exceptionCallback();
                                }
                                catch (Exception exw)
                                {
                                    Logger.Error(ErrorLog + ": " + exw.Message + "\n" + exw.StackTrace);
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

        protected virtual void LateUpdate()
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
        }
    }
}
