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
using System.Diagnostics;
using NECS.Extensions.ThreadingSync;
using NECS.Harness.Services;

namespace NECS.ECS.ECSCore
{
    public class ECSExecutableContractContainer
    {
        public long Id { get; set; }
        [System.NonSerialized]
        protected Type _systemType = null;
        public Type SystemType
        {
            get => _systemType;
            set
            {
                lock (ContractLocker)
                {
                    _systemType = value;
                }
            }
        }

        protected StackTrace genTrace = null;
        public StackTrace GenerationStackTrace
        {
            get => genTrace;
            set
            {
                lock (ContractLocker)
                {
                    genTrace = value;
                }
            }
        }

        protected Dictionary<long, List<Func<ECSEntity, bool>>> _contractConditions = null;
        /// <summary>
        /// key long - entityid
        /// </summary>
        public Dictionary<long, List<Func<ECSEntity, bool>>> ContractConditions
        {
            get => _contractConditions;
            set
            {
                lock (ContractLocker)
                {
                    _contractConditions = value;
                }
            }
        }

        protected Dictionary<long, Dictionary<long, bool>> _entityComponentPresenceSign = null;
        /// <summary>
        /// key long - entityownerid
        ///long - componentTypeId
        ///bool - presence state
        /// </summary>
        public Dictionary<long, Dictionary<long, bool>> EntityComponentPresenceSign
        {
            get => _entityComponentPresenceSign;
            set
            {
                lock (ContractLocker)
                {
                    _entityComponentPresenceSign = value;
                }
            }
        }

        protected Action<ECSExecutableContractContainer, ECSEntity[]> _contractExecutable =
            (ECSExecutableContractContainer contract, ECSEntity[] entities) =>
            {
                foreach (var entity in entities)
                {
                    contract.ContractExecutableSingle(contract, entity);
                }
            };
        public Action<ECSExecutableContractContainer, ECSEntity[]> ContractExecutable
        {
            get => _contractExecutable;
            set
            {
                lock (ContractLocker)
                {
                    _contractExecutable = value;
                }
            }
        }

        protected Action<ECSExecutableContractContainer, ECSEntity> _contractExecutableSingle =
            (contract, entity) => { };
        public Action<ECSExecutableContractContainer, ECSEntity> ContractExecutableSingle
        {
            get => _contractExecutableSingle;
            set
            {
                lock (ContractLocker)
                {
                    _contractExecutableSingle = value;
                }
            }
        }

        protected Action<ECSExecutableContractContainer, long[]> _errorExecution =
            (ECSExecutableContractContainer contract, long[] entities) => { };
        public Action<ECSExecutableContractContainer, long[]> ErrorExecution
        {
            get => _errorExecution;
            set
            {
                lock (ContractLocker)
                {
                    _errorExecution = value;
                }
            }
        }


        protected Func<ECSWorld, bool> _worldFilter =
            (world) => { return true; };
        public Func<ECSWorld, bool> WorldFilter
        {
            get => _worldFilter;
            set
            {
                lock (ContractLocker)
                {
                    _worldFilter = value;
                }
            }
        }


        protected bool _timeDependExecution = false;
        public bool TimeDependExecution
        {
            get => _timeDependExecution;
            set
            {
                lock (ContractLocker)
                {
                    _timeDependExecution = value;
                }
            }
        }

        protected bool _noPresenceSignAllowed = true;
        /// <summary>
        /// Set FALSE if contract is time depend
        /// </summary>
        public bool NoPresenceSignAllowed
        {
            get => _noPresenceSignAllowed;
            set
            {
                lock (ContractLocker)
                {
                    _noPresenceSignAllowed = value;
                }
            }
        }

        protected bool _removeAfterExecution = true;
        /// <summary>
        /// Set FALSE if contract is time depend
        /// </summary>
        public bool RemoveAfterExecution
        {
            get => _removeAfterExecution;
            set
            {
                lock (ContractLocker)
                {
                    _removeAfterExecution = value;
                }
            }
        }

        protected long _maxTries = long.MaxValue;
        public long MaxTries
        {
            get => _maxTries;
            set
            {
                lock (ContractLocker)
                {
                    _maxTries = value;
                }
            }
        }

        protected long _nowTried = 0;
        public long NowTried
        {
            get => _nowTried;
            set
            {
                lock (ContractLocker)
                {
                    _nowTried = value;
                }
            }
        }

        protected bool _timeDependActive = true;
        public bool TimeDependActive
        {
            get => _timeDependActive;
            set
            {
                lock (ContractLocker)
                {
                    _timeDependActive = value;
                }
            }
        }

        protected bool _partialEntityFiltering = false;
        public bool PartialEntityFiltering
        {
            get => _partialEntityFiltering;
            set
            {
                lock (ContractLocker)
                {
                    _partialEntityFiltering = value;
                }
            }
        }

        protected bool _asyncExecution = true;
        public bool AsyncExecution
        {
            get => _asyncExecution;
            set
            {
                lock (ContractLocker)
                {
                    _asyncExecution = value;
                }
            }
        }

        protected bool _inWork = false;
        public bool InWork
        {
            get => _inWork;
            set
            {
                lock (ContractLocker)
                {
                    _inWork = value;
                }
            }
        }

        protected bool _inProgress = false;
        public bool InProgress
        {
            get => _inProgress;
            set
            {
                lock (ContractLocker)
                {
                    _inProgress = value;
                }
            }
        }

        protected long _lastEndExecutionTimestamp = 0;
        public long LastEndExecutionTimestamp
        {
            get => _lastEndExecutionTimestamp;
            set
            {
                lock (ContractLocker)
                {
                    _lastEndExecutionTimestamp = value;
                }
            }
        }

        protected long _delayRunMilliseconds = 0;
        public long DelayRunMilliseconds
        {
            get => _delayRunMilliseconds;
            set
            {
                lock (ContractLocker)
                {
                    _delayRunMilliseconds = value;
                }
            }
        }

        protected bool _contractExecuted = false;
        public bool ContractExecuted
        {
            get => _contractExecuted;
            set
            {
                lock (ContractLocker)
                {
                    _contractExecuted = value;
                }
            }
        }

        protected bool _manualExitFromWorkingState = false;
        public bool ManualExitFromWorkingState
        {
            get => _manualExitFromWorkingState;
            set
            {
                lock (ContractLocker)
                {
                    _manualExitFromWorkingState = value;
                }
            }
        }

        public object ContractLocker { get; set; } = new object();

        public List<long> NeededEntities
        {
            get
            {
                if (ContractConditions != null && EntityComponentPresenceSign != null)
                {
                    var allentities = ContractConditions.Keys.ToList();
                    allentities.AddRange(this.EntityComponentPresenceSign.Keys);
                    return new HashSet<long>(allentities).ToList();
                }
                return null;
            }
        }

        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// SystemEventHandler.Add(GameEvent.Id, new List<Func<ECSEvent, object>>() {
        ///         (Event) => {
        ///             return (Event as GameEvent);
        ///         }
        ///     });
        /// </summary>
        public IDictionary<long, List<Func<ECSEvent, object>>> SystemEventHandler = new DictionaryWrapper<long, List<Func<ECSEvent, object>>>();//id of event and func
        /// <summary>
        /// Need to setup in initalize method. Setting up look like is:
        /// ComponentsOnChangeCallbacks.Add(GameComponent.Id, new List<Action<ECSEntity, ECSComponent>>() {
        ///         (entity, component) => {
        ///             (component as GameComponent);
        ///         }
        ///     });
        /// </summary>
        public IDictionary<long, List<Action<ECSEntity, ECSComponent>>> ComponentsOnChangeCallbacks = new DictionaryWrapper<long, List<Action<ECSEntity, ECSComponent>>>();//id of component and func

        public ECSExecutableContractContainer()
        {
            GenerationStackTrace = new System.Diagnostics.StackTrace();
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
                    OnTryExecute();
                    if(contractEntities == null)
                    {
                        var allentities = ContractConditions.Keys.ToList();
                        allentities.AddRange(this.EntityComponentPresenceSign.Keys);

                        bool contractResult = false;
                        List<RWLock.LockToken> lockers = null;
                        List<ECSEntity> executionEntities = null;
                        if (Defines.OneThreadMode)
                        {
                            contractResult = GetContractLockersOneThread(allentities, this.ContractConditions, this.EntityComponentPresenceSign, false, out lockers, out executionEntities);
                        }
                        else
                        {
                            contractResult = GetContractLockers(allentities, this.ContractConditions, this.EntityComponentPresenceSign, false, out lockers, out executionEntities);
                        }
                        if (contractResult && lockers != null)
                        {
                            var errorState = false;
                            if (ExecuteContract)
                            {
                                this.InWork = true;
                                try
                                {
                                    ContractExecutable(this, executionEntities.ToArray());
                                }
                                catch (Exception ex)
                                {
                                    NLogger.LogError(ex);
                                    ErrorExecution(this, executionEntities.Select(x => x.instanceId).ToArray());
                                    errorState = true;
                                }
                                lockers.ForEach(x => x.Dispose());
                                if (!ManualExitFromWorkingState)
                                {
                                    this.LastEndExecutionTimestamp = DateTime.Now.Ticks;
                                    this.InWork = false;
                                }
                            }
                            else
                            {
                                lockers.ForEach(x => x.Dispose());
                            }
                            if (ExecuteContract && !errorState)
                            {
                                ContractExecuted = true;
                                return true;
                            }
                        }
                        else
                        {
                            if (ExecuteContract)
                                ErrorExecution(this, allentities.ToArray());
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
                        
                        bool contractResult = false;
                        List<RWLock.LockToken> lockers = null;
                        List<ECSEntity> executionEntities = null;
                        if (Defines.OneThreadMode)
                        {
                            contractResult = GetContractLockersOneThread(contractEntities, filledContractConditions, filledEntityComponentPresenceSign, true, out lockers, out executionEntities);
                        }
                        else
                        {
                            contractResult = GetContractLockers(contractEntities, filledContractConditions, filledEntityComponentPresenceSign, true, out lockers, out executionEntities);
                        }

                        if (contractResult && lockers != null)
                        {
                            var errorState = false;
                            if (ExecuteContract)
                            {
                                this.InWork = true;
                                try
                                {
                                    ContractExecutable(this, executionEntities.ToArray());
                                }
                                catch (Exception ex)
                                {
                                    NLogger.LogError(ex);
                                    ErrorExecution(this, executionEntities.Select(x => x.instanceId).ToArray());
                                    errorState = true;
                                }
                                lockers.ForEach(x => x.Dispose());
                                if (!ManualExitFromWorkingState)
                                {
                                    this.LastEndExecutionTimestamp = DateTime.Now.Ticks;
                                    this.InWork = false;
                                }
                            }
                            else
                            {
                                lockers.ForEach(x => x.Dispose());
                            }
                            // if(ExecuteContract)
                            //     ContractExecuted = true;
                            if (!errorState)
                                return true;
                        }
                        else
                        {
                            if (ExecuteContract)
                                ErrorExecution(this, contractEntities.ToArray());
                        }
                    }
                    return false;
                }
                else
                {
                    NLogger.Log("You tried to execute contract that was already executed\n" + this.GenerationStackTrace.ToString() + "\n================================");
                    return false;
                }
            }
        }

        private bool GetContractLockersOneThread(
    List<long> contractEntities,
    IDictionary<long, List<Func<ECSEntity, bool>>> localContractConditions,
    IDictionary<long, Dictionary<long, bool>> localEntityComponentPresenceSign,
    bool partialEntityTargetListLockingAllowed,
    out List<RWLock.LockToken> lockTokens,
    out List<ECSEntity> executionEntities)
        {
            lockTokens = new List<RWLock.LockToken>();
            executionEntities = new List<ECSEntity>();
            bool globalViolationSeizure = false;

            foreach (var entityId in contractEntities)
            {
                var entityManager = ECSService.instance.GetEntityWorld(entityId).entityManager;
                if (!entityManager.EntityStorage.TryGetValue(entityId, out var entity))
                    continue;

                bool violationSeizure = false;
                var entityTokens = new List<RWLock.LockToken>();
                bool yescomponent = false;
                // Check component requirements
                if (localEntityComponentPresenceSign.TryGetValue(entityId, out var neededComponents))
                {
                    foreach (var component in neededComponents)
                    {
                        bool hasComponent = entity.entityComponents.HasComponent(component.Key.IdToECSType());
                        if (component.Value != hasComponent)
                        {
                            violationSeizure = true;
                            globalViolationSeizure = true;
                            break;
                        }
                        yescomponent = true;
                        // if (component.Value && entity.entityComponents.GetReadLockedComponent(
                        //     component.Key.IdToECSType(), out _, out var token))
                        // {
                        //     entityTokens.Add(token);
                        // }
                    }
                }

                // Check conditions
                if (!violationSeizure && localContractConditions.TryGetValue(entityId, out var conditions))
                {
                    violationSeizure = conditions.Any(condition => !condition(entity));
                    globalViolationSeizure |= violationSeizure;
                }

                // Handle entity based on violation status
                if (!violationSeizure || (partialEntityTargetListLockingAllowed &&
                    _partialEntityFiltering && (yescomponent || (NoPresenceSignAllowed && !yescomponent))))
                {
                    executionEntities.Add(entity);
                    lockTokens.AddRange(entityTokens);
                }
                else
                {
                    entityTokens.ForEach(token => token.Dispose());
                }
            }

            if (globalViolationSeizure && !partialEntityTargetListLockingAllowed)
            {
                lockTokens.ForEach(token => token.Dispose());
                lockTokens.Clear();
                executionEntities.Clear();
                return false;
            }

            return executionEntities.Count > 0;
        }

        private bool GetContractLockers(List<long> contractEntities, IDictionary<long, List<Func<ECSEntity, bool>>> LocalContractConditions, IDictionary<long, Dictionary<long, bool>> LocalEntityComponentPresenceSign, bool partialEntityTargetListLockingAllowed, out List<RWLock.LockToken> lockTokens, out List<ECSEntity> executionEntities)
        {
            Dictionary<long, List<RWLock.LockToken>> Lockers = new Dictionary<long, List<RWLock.LockToken>>();
            lockTokens = null;
            executionEntities = null;
            var localExecutionEntities = new List<ECSEntity>();
            bool globalViolationSeizure = false;
            foreach (var entityid in new HashSet<long>(contractEntities))
            {
                ECSService.instance.GetEntityWorld(entityid).entityManager.EntityStorage.ExecuteReadLockedContinuously(entityid, (entid, contentity) =>
                {
                    bool violationSeizure = false;
                    Lockers.Add(entid, new List<RWLock.LockToken>());
                    if (LocalEntityComponentPresenceSign.TryGetValue(entid, out var neededComponents))
                    {
                        foreach (var component in neededComponents)
                        {
                            if (component.Value)
                            {
                                if (contentity.entityComponents.GetReadLockedComponent(component.Key.IdToECSType(), out var componentInstance, out var token))
                                {
                                    Lockers[entid].Add(token);
                                    continue;
                                }
                            }
                            else
                            {
                                if (contentity.entityComponents.HoldComponentAddition(component.Key.IdToECSType(), out var token))
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
                            }
                            violationSeizure = true;
                            globalViolationSeizure = true;
                        }
                    }

                    if (LocalContractConditions.TryGetValue(entid, out var conditions))
                    {
                        foreach (var condition in conditions)
                        {
                            if (!condition(contentity))
                            {
                                violationSeizure = true;
                                globalViolationSeizure = true;
                            }
                        }
                    }

                    if (violationSeizure)
                    {
                        if (partialEntityTargetListLockingAllowed && this._partialEntityFiltering && (Lockers[entid].Count > 1 || (NoPresenceSignAllowed && neededComponents.Count > 0)))
                        {
                            localExecutionEntities.Add(contentity);
                        }
                        else
                        {
                            Lockers[entid].ForEach(x => x.Dispose());
                            Lockers.Remove(entid);
                        }
                    }
                    else
                    {
                        localExecutionEntities.Add(contentity);
                    }
                }, out var entitytoken);
                if (entitytoken != null && Lockers.ContainsKey(entityid))
                    Lockers[entityid].Add(entitytoken);

            }
            if (globalViolationSeizure && !partialEntityTargetListLockingAllowed)
            {
                Lockers.ForEach(x => x.Value.ForEach(y => y.Dispose()));
                return !globalViolationSeizure;
            }
            if (localExecutionEntities.Count == 0)
            {
                return false;
            }
            lockTokens = Lockers.Values.SelectMany(x => x).ToList();
            executionEntities = localExecutionEntities;
            return true;
        }

        /// <summary>
        /// Method running every ECS tick
        /// </summary>
        /// <param name="entities"></param>
        public virtual void Run(long[] entities)
        {

        }

        /// <summary>
        /// overridable function for debug purposes in tryexecution process
        /// </summary>
        public virtual void OnTryExecute()
        {
            
        }

        /// <summary>
        /// Return system events components dictionary <StaticIDEvent, randomint>
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<long, int> GetInterestedEventsList()
        {
            var result = new DictionaryWrapper<long, int>();
            foreach (var eventid in SystemEventHandler)
            {
                result.TryAdd(eventid.Key, 0);
            }
            return result;
        }
    }
}
