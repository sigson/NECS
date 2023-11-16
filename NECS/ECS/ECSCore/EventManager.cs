using System.Reflection;
using NECS.Core.Logging;
using NECS.Extensions;
using NECS.Harness.Services;

namespace NECS.ECS.ECSCore
{
    public class ECSEventManager
    {
        public List<long> AcceptedOutsideEvents = new List<long>()
        {

        };//allowed events going from internet

        public ConcurrentDictionaryEx<long, ConcurrentDictionaryEx<ECSSystem, List<Func<ECSEvent, object>>>> SystemHandlers = new ConcurrentDictionaryEx<long, ConcurrentDictionaryEx<ECSSystem, List<Func<ECSEvent, object>>>>();
        public ConcurrentDictionaryEx<long, ECSEvent> EventBus = new ConcurrentDictionaryEx<long, ECSEvent>();
        public Dictionary<long, Type> EventSerializationCache = new Dictionary<long, Type>();

        public ObjectPool<EventWatcher> watcherPool;

        public ECSEventManager()
        {
            watcherPool = new ObjectPool<EventWatcher>(() => new EventWatcher(this, 0, 0));
        }

        public void IdStaticCache()
        {
            var AllEvents = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSEvent)).Select(x => (ECSEvent)Activator.CreateInstance(x));
            foreach (var Event in AllEvents)
            {
                EventSerializationCache.Add(Event.GetId(), Event.GetType());
                try
                {
                    var field = Event.GetType().GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var customAttrib = Event.GetType().GetCustomAttribute<TypeUidAttribute>();
                    if (customAttrib != null)
                        field.SetValue(null, customAttrib.Id);
                }
                catch
                {
                    Console.WriteLine(Event.GetType().Name);
                }
            }
        }

        public void InitializeEventManager()
        {
            var AllEvents = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSEvent)).Select(x => (ECSEvent)Activator.CreateInstance(x));
            
            foreach (ECSSystem system in ManagerScope.instance.systemManager.InterestedIDECSComponentsDatabase.Keys.ToList())
            {
                var SystemInterest = system.GetInterestedEventsList();
                foreach (var Event in AllEvents)
                {
                    if(SystemInterest.Keys.Contains(Event.GetId()))
                    {
                        ConcurrentDictionaryEx<ECSSystem, List<Func<ECSEvent, object>>> NewDictionary;
                        if(SystemHandlers.TryGetValue(Event.GetId(), out NewDictionary))
                        {
                            List<Func<ECSEvent, object>> outfunc;
                            if(system.SystemEventHandler.TryGetValue(Event.GetId(), out outfunc))
                                NewDictionary.TryAdd(system, outfunc);
                        }
                        else
                        {
                            NewDictionary = new ConcurrentDictionaryEx<ECSSystem, List<Func<ECSEvent, object>>>();
                            List<Func<ECSEvent, object>> outfunc;
                            if (system.SystemEventHandler.TryGetValue(Event.GetId(), out outfunc))
                                NewDictionary.TryAdd(system, outfunc);
                            SystemHandlers.TryAdd(Event.GetId(), NewDictionary);
                        }
                    }
                }
            }
        }

        public void OnEventAdd(ECSEvent ecsEvent)
        {
            if(ecsEvent.SocketSourceId != 0)
            {
                if(NetworkMaliciousEventCounteractionService.instance.maliciousScoringStorage.TryGetValue(ecsEvent.SocketSourceId, out var scoreObject))
                {
                    scoreObject.Score += ecsEvent.GetType().GetCustomAttribute<NetworkScore>().Score + ecsEvent.NetworkScoreBooster();
                }
            }
            if(!ecsEvent.CheckPacket())
            {
                Logger.LogError("Was received error packed " + ecsEvent.GetType().ToString());
                return;
            }

            ecsEvent.Execute();

            if (SystemHandlers.TryGetValue(ecsEvent.GetId(), out var eventHandlers))
            {
                ecsEvent.eventWatcher = watcherPool.Get().EventWatcherUpdate(eventHandlers.Count, ecsEvent.instanceId);

                if (eventHandlers.Count > 0)
                    if (!EventBus.TryAdd(ecsEvent.instanceId, ecsEvent))
                        Logger.LogError("error add event to bus");
                
                foreach (var system in eventHandlers)
                {
                    foreach (var func in system.Value)
                    {
                        TaskEx.RunAsync(() =>
                        {
                            try
                            {
                                func.DynamicInvoke(ecsEvent);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex);
#if DEBUG
                                throw;
#endif
                            }
                            finally
                            {
                                ecsEvent.eventWatcher.Watchers--;
                            }
                        });
                    }
                }
            }
        }

        public void OnEventProcessed(long ecsEventId)
        {
            ECSEvent removed;
            if(!EventBus.TryRemove(ecsEventId, out removed))
            {
                Logger.LogError("core event error");
            }
            watcherPool.Return(removed.eventWatcher);
        }
        public void UpdateSystemHandlers(long eventId, List<Func<ECSEvent, object>> handler)
        {

        }

        public void RemoveSystemHandlers(long eventId, List<Func<ECSEvent, object>> handler)
        {

        }
    }

    public class EventWatcher
    {
        public ECSEventManager eventManager;
        private volatile int watchers;
        public long EventId;
        public int Watchers
        {
            get
            {
                return watchers;
            }
            set
            {
                Interlocked.Exchange(ref watchers, value);
                //watchers = value;
                if (watchers == 0)
                {
                    eventManager.OnEventProcessed(EventId);
                }
            }
        }
        public EventWatcher(ECSEventManager eCSEventManager, int allWatchers, long eventId)
        {
            eventManager = eCSEventManager;
            watchers = allWatchers;
            EventId = eventId;
        }

        public EventWatcher EventWatcherUpdate(ECSEventManager eCSEventManager, int allWatchers, long eventId)
        {
            eventManager = eCSEventManager;
            watchers = allWatchers;
            EventId = eventId;
            return this;
        }

        public EventWatcher EventWatcherUpdate(int allWatchers, long eventId)
        {
            watchers = allWatchers;
            EventId = eventId;
            return this;
        }
    }
}
