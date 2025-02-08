using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NECS.Extensions;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics.Contracts;

namespace NECS.ECS.ECSCore
{
    public class ECSExecutableContractContainer
    {
        public long Id { get; set; }
        [System.NonSerialized]
        public Type SystemType;
        public IDictionary<long, Func<ECSEntity, bool>> ContractConditions { get; set; } = new ConcurrentDictionary<long, Func<ECSEntity, bool>>();
        /// <summary>
        /// key long - entityownerid
        ///long - componentTypeId
        ///bool - presence state
        /// </summary>
        public IDictionary<long, Dictionary<long, bool>> ComponentPresenceSign { get; set; } = new Dictionary<long, Dictionary<long, bool>>();

        public Action<ECSExecutableContractContainer, ECSEntity[]> ContractExecutable = (ECSExecutableContractContainer contract, ECSEntity[] entities) => {
            foreach (var entity in entities)
            {
                contract.ContractExecutableSingle(contract, entity);
            }
        };

        public Action<ECSExecutableContractContainer, ECSEntity> ContractExecutableSingle = (contract, entity) => {
            
        };

        public bool TimeDependExecution { get; set; } = false;

        /// <summary>
        /// Set FALSE if contract is time depend
        /// </summary>
        public bool RemoveAfterExecution { get; set; } = true;
        public bool TimeDependActive { get; set; } = true;
        public bool InWork { get; set; }
        public long LastEndExecutionTimestamp { get; set; }
        public long DelayRunMilliseconds { get; set; }

        private bool ContractExecuted { get; set; } = false;
        private object ContractLocker { get; set; } = new object();

        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// SystemEventHandler.Add(GameEvent.Id, new List<Func<ECSEvent, object>>() {
        ///         (Event) => {
        ///             return (Event as GameEvent);
        ///         }
        ///     });
        /// </summary>
        public IDictionary<long, List<Func<ECSEvent, object>>> SystemEventHandler = new ConcurrentDictionary<long, List<Func<ECSEvent, object>>>();//id of event and func
        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// ComponentsOnChangeCallbacks.Add(GameComponent.Id, new List<Action<ECSEntity, ECSComponent>>() {
        ///         (entity, component) => {
        ///             (component as GameComponent);
        ///         }
        ///     });
        /// </summary>
        public IDictionary<long, List<Action<ECSEntity, ECSComponent>>> ComponentsOnChangeCallbacks = new ConcurrentDictionary<long, List<Action<ECSEntity, ECSComponent>>>();//id of component and func

        /// <summary>
        /// Methor running before ECS system initialliation
        /// </summary>
        /// <param name="SystemManager"></param>
        public virtual void Initialize()
        {

        }

        public bool TryExecuteContract(List<ECSEntity> contractEntities, bool ExecuteContract = true)
        {
            lock (ContractLocker)
            {
                if (!ContractExecuted)
                {
                    var LockedPoints = new List<NECS.RWLock.ReadLockToken>();
                    foreach (var entity in contractEntities)
                    {
                        foreach (var presenceCondition in ComponentPresenceSign)
                        {

                            if (presenceCondition.Value(entity))
                            {
                                LockedPoints.Add(entity.entityComponents.StabilizationLocker.WriteLock());
                            }
                        }
                    }
                    foreach (var entity in contractEntities)
                    {
                        foreach (var condition in ContractConditions)
                        {
                            if (condition.Value(entity))
                            {
                                LockedPoints.Add(entity.entityComponents.StabilizationLocker.WriteLock());
                            }
                        }
                    }
                    if (LockedPoints.Count == contractEntities.Count)
                    {
                        if(ExecuteContract)
                        {
                            ContractExecutable(contractEntities.ToArray());
                        }
                        LockedPoints.ForEach(locker => locker.Dispose());
                        return true;
                    }
                    else
                    {
                        LockedPoints.ForEach(locker => locker.Dispose());
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Method running every ECS tick
        /// </summary>
        /// <param name="entities"></param>
        public virtual void Run(long[] entities)
        {

        }

        /// <summary>
        /// Return system events components dictionary <StaticIDEvent, randomint>
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<long, int> GetInterestedEventsList()
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
    }
}
