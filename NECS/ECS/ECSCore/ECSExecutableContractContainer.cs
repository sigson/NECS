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
        public Type SystemType = null;
        /// <summary>
        /// key long - entityid
        /// </summary>
        public Dictionary<long, List<Func<ECSEntity, bool>>> ContractConditions { get; set; } = null;
        /// <summary>
        /// key long - entityownerid
        ///long - componentTypeId
        ///bool - presence state
        /// </summary>
        public Dictionary<long, Dictionary<long, bool>> EntityComponentPresenceSign { get; set; } = null;

        public List<long> NeededEntities {
            get{
                if(ContractConditions != null && EntityComponentPresenceSign != null)
                {
                    var allentities = ContractConditions.Keys.ToList();
                    allentities.AddRange(this.EntityComponentPresenceSign.Keys);
                    return allentities;
                }
                return null;
            }
        }

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
        public long MaxTries { get; set; } = long.MaxValue;
        public long NowTried { get; set; } = 0;
        public bool TimeDependActive { get; set; } = true;
        public bool AsyncExecution { get; set; } = true;
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

        public ECSExecutableContractContainer()
        {
            if(this.GetType() == typeof(ECSExecutableContractContainer))
            {
                this.MaxTries = 1;
            }
            else
            {
                this.RemoveAfterExecution = false;
            }
        }

        /// <summary>
        /// Methor running before ECS system initialliation
        /// </summary>
        /// <param name="SystemManager"></param>
        public virtual void Initialize()
        {

        }
        /// <summary>
        /// contractEntities not null if it time depend contract
        /// </summary>
        /// <param name="ExecuteContract"></param>
        /// <param name="contractEntities"></param>
        /// <returns></returns>
        public bool TryExecuteContract(bool ExecuteContract = true, List<long> contractEntities = null )
        {
            lock (ContractLocker)
            {
                NowTried++;
                if (!ContractExecuted)
                {
                    if(contractEntities == null)
                    {
                        var allentities = ContractConditions.Keys.ToList();
                        allentities.AddRange(this.EntityComponentPresenceSign.Keys);
                        if(GetContractLockers(allentities, this.ContractConditions, this.EntityComponentPresenceSign, false, out var lockers, out var executionEntities) && lockers != null)
                        {
                            if(ExecuteContract)
                            {
                                this.InWork = true;
                                if(AsyncExecution)
                                {
                                    TaskEx.RunAsync(() => {
                                        ContractExecutable(this, executionEntities.ToArray());
                                        this.LastEndExecutionTimestamp = DateTime.Now.Ticks;
                                        this.InWork = false;
                                    });
                                }
                                else
                                {
                                    ContractExecutable(this, executionEntities.ToArray());
                                    this.LastEndExecutionTimestamp = DateTime.Now.Ticks;
                                    this.InWork = false;
                                }
                            }
                            lockers.ForEach(x => x.Dispose());
                            if(ExecuteContract)
                                ContractExecuted = true;
                            return true;
                        }
                    }
                    else
                    {
                        var filledContractConditions = new Dictionary<long, List<Func<ECSEntity, bool>>>();
                        var filledEntityComponentPresenceSign = new Dictionary<long, Dictionary<long, bool>>();
                        foreach (var contractCond in this.ContractConditions)
                        {
                            foreach (var contractEntity in contractEntities)
                            {
                                filledContractConditions[contractEntity] = contractCond.Value;
                            }
                        }
                        foreach (var presenceSign in this.EntityComponentPresenceSign)
                        {
                            foreach (var contractEntity in contractEntities)
                            {
                                filledEntityComponentPresenceSign[contractEntity] = presenceSign.Value;
                            }
                        }
                        if(GetContractLockers(contractEntities, filledContractConditions, filledEntityComponentPresenceSign, true, out var lockers, out var executionEntities) && lockers != null)
                        {
                            if(ExecuteContract)
                                ContractExecutable(this, executionEntities.ToArray());
                            lockers.ForEach(x => x.Dispose());
                            if(ExecuteContract)
                                ContractExecuted = true;
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool GetContractLockers(List<long> contractEntities, IDictionary<long, List<Func<ECSEntity, bool>>> LocalContractConditions, IDictionary<long, Dictionary<long, bool>> LocalEntityComponentPresenceSign, bool partialEntityTargetListLockingAllowed, out List<RWLock.LockToken> lockTokens, out List<ECSEntity> executionEntities)
        {
            Dictionary<long, List<RWLock.LockToken>> Lockers = new Dictionary<long, List<RWLock.LockToken>>();
            lockTokens = null;
            executionEntities = null;
            var localExecutionEntities = new List<ECSEntity>();
            bool globalViolationSeizure = false;
            foreach (var entityid in contractEntities)
            {
                ManagerScope.instance.entityManager.EntityStorage.ExecuteReadLockedContinuously(entityid, (entid, contentity) =>
                {
                    bool violationSeizure = false;
                    Lockers.Add(entid, new List<RWLock.LockToken>());
                    if(LocalEntityComponentPresenceSign.TryGetValue(entid, out var neededComponents))
                    {
                        foreach (var component in neededComponents)
                        {
                            if(component.Value)
                                if(contentity.entityComponents.GetReadLockedComponent(component.Key.IdToECSType(), out var componentInstance, out var token))
                                {
                                    Lockers[entid].Add(token);
                                    continue;
                                }
                            else
                                if(contentity.entityComponents.HoldComponentAddition(component.Key.IdToECSType(), out token))
                                {
                                    if (!contentity.entityComponents.HasComponent(component.Key.IdToECSType()))
                                    {
                                        Lockers[entid].Add(token);
                                        continue;
                                    }
                                    else
                                    {
                                        token.Dispose();
                                    }
                                }
                            violationSeizure = true;
                            globalViolationSeizure = true;
                        }
                    }

                    if(LocalContractConditions.TryGetValue(entid, out var conditions))
                    {
                        foreach (var condition in conditions)
                        {
                            if(!condition(contentity))
                            {
                                violationSeizure = true;
                                globalViolationSeizure = true;
                            }
                        }
                    }

                    if(violationSeizure)
                    {
                        Lockers[entid].ForEach(x => x.Dispose());
                        Lockers.Remove(entid);
                    }
                    else
                    {
                        localExecutionEntities.Add(contentity);
                    }
                }, out var entitytoken);
                if(entitytoken != null && Lockers.ContainsKey(entityid))
                    Lockers[entityid].Add(entitytoken);

            }
            if(globalViolationSeizure && !partialEntityTargetListLockingAllowed)
            {
                Lockers.ForEach(x => x.Value.ForEach(y => y.Dispose()));
                return !globalViolationSeizure;
            }
            else
            {
                lockTokens = Lockers.Values.SelectMany(x => x).ToList();
                executionEntities = localExecutionEntities;
                return true;
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
