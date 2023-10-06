using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NECS.Extensions;

namespace NECS.ECS.ECSCore
{
    public abstract class ECSSystem
    {
        public long Id { get; set; }
        [NonSerialized]
        public Type SystemType;

        public bool Enabled { get; set; }
        public bool InWork { get; set; }
        public long LastEndExecutionTimestamp { get; set; }
        public long DelayRunMilliseconds { get; set; }
        /// <summary>
        /// Ignore all SystemEventHandler events. Need setup in Initalize method
        /// </summary>
        public bool EventIgnoring { get; set; }

        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// SystemEventHandler.Add(GameEvent.Id, new List<Func<ECSEvent, object>>() {
        ///         (Event) => {
        ///             return (Event as GameEvent);
        ///         }
        ///     });
        /// </summary>
        public ConcurrentDictionary<long, List<Func<ECSEvent, object>>> SystemEventHandler = new ConcurrentDictionary<long, List<Func<ECSEvent, object>>>();//id of event and func
        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// ComponentsOnChangeCallbacks.Add(GameComponent.Id, new List<Action<ECSEntity, ECSComponent>>() {
        ///         (entity, component) => {
        ///             (component as GameComponent);
        ///         }
        ///     });
        /// </summary>
        public ConcurrentDictionary<long, List<Action<ECSEntity, ECSComponent>>> ComponentsOnChangeCallbacks = new ConcurrentDictionary<long, List<Action<ECSEntity, ECSComponent>>>();//id of component and func

        /// <summary>
        /// Methor running before ECS system initialliation
        /// </summary>
        /// <param name="SystemManager"></param>
        public abstract void Initialize(ECSSystemManager SystemManager);
        /// <summary>
        /// Method running every ECS tick
        /// </summary>
        /// <param name="entities"></param>
        public abstract void Run(long[] entities);

        //public abstract void Operation(ECSEntity entity, ECSComponent Component);//obsolete shit

        public virtual bool HasInterest(ECSEntity entity)//obsolete check is system interested
        {
            var thisSystemInterest = this.GetInterestedComponentsList();
            foreach (var interestComponent in thisSystemInterest)
            {
                if (entity.HasComponent(interestComponent.Key))
                    return true;
            }
            return false;
        }

        //public abstract bool UpdateInterestedList(List<long> ComponentsId);//obsolete and not needed

        /// <summary>
        /// Return system interested components dictionary <StaticIDComponent, randomint>
        /// </summary>
        /// <returns></returns>
        public abstract ConcurrentDictionary<long, int> GetInterestedComponentsList();
        /// <summary>
        /// Return system events components dictionary <StaticIDEvent, randomint>
        /// </summary>
        /// <returns></returns>
        public virtual ConcurrentDictionary<long, int> GetInterestedEventsList()
        {
            var result = new ConcurrentDictionary<long, int>();
            foreach (var eventid in SystemEventHandler)
            {
                result.TryAdd(eventid.Key, 0);
            }
            return result;
        }

        protected void RegisterEventHandler(long eventId)
        {
            ManagerScope.instance.eventManager.UpdateSystemHandlers(eventId, this.SystemEventHandler[eventId]);
        }

        protected virtual void UpdateEventWatcher(ECSEvent eCSEvent)
        {
            eCSEvent.eventWatcher.Watchers--;
        }

        public Type GetTypeFast()
        {
            if (SystemType == null)
            {
                SystemType = GetType();
            }
            return SystemType;
        }
    }
}
