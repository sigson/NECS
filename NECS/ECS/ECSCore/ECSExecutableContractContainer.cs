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
    /// <summary>
    /// Уровни логгирования для контрактов
    /// </summary>
    public enum ContractLoggingLevel
    {
        /// <summary>
        /// Без логгирования
        /// </summary>
        None = 0,
        /// <summary>
        /// Только ошибки
        /// </summary>
        ErrorsOnly = 1,
        /// <summary>
        /// Полная информация
        /// </summary>
        Verbose = 2
    }

    public class ECSExecutableContractContainer
    {
        public string ContractId { get; set; }
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

        protected ContractLoggingLevel _loggingLevel = ContractLoggingLevel.Verbose;
        /// <summary>
        /// Уровень логгирования для контракта
        /// </summary>
        public ContractLoggingLevel LoggingLevel
        {
            get => _loggingLevel;
            set
            {
                lock (ContractLocker)
                {
                    _loggingLevel = value;
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

        protected bool _bypassFinalization = false;
        public bool BypassFinalization
        {
            get => _bypassFinalization;
            set
            {
                lock (ContractLocker)
                {
                    _bypassFinalization = value;
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
                BypassFinalization = false;
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
                                if(!BypassFinalization)
                                {
                                    ContractExecuted = true;
                                }
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
                {
                    if (LoggingLevel == ContractLoggingLevel.Verbose)
                    {
                        NLogger.Log($"Contract {this.GetType().Name} (ID: {this.ContractId}): Entity {entityId} not found in EntityStorage");
                    }
                    continue;
                }

                bool violationSeizure = false;
                var entityTokens = new List<RWLock.LockToken>();
                bool yescomponent = false;
                
                // Check component requirements
                if (localEntityComponentPresenceSign.TryGetValue(entityId, out var neededComponents))
                {
                    var expectedPresent = new List<Type>();
                    var expectedAbsent = new List<Type>();
                    var actualComponents = new List<Type>();
                    var missingExpected = new List<Type>();
                    var unexpectedPresent = new List<Type>();

                    // Собираем информацию о всех компонентах сущности
                    if (LoggingLevel == ContractLoggingLevel.Verbose)
                    {
                        actualComponents = entity.entityComponents.ComponentClasses.ToList();
                    }

                    foreach (var component in neededComponents)
                    {
                        var componentType = component.Key.IdToECSType();
                        bool hasComponent = entity.entityComponents.HasComponent(componentType);

                        if (component.Value != hasComponent)
                        {
                            //expectedPresent.Add(componentType);

                            //missingExpected.Add(componentType);
                            violationSeizure = true;
                            globalViolationSeizure = true;

                            if (component.Value)
                            {
                                missingExpected.Add(componentType);
                            }
                            else
                            {
                                unexpectedPresent.Add(componentType);
                            }
                        }
                        else
                        {
                            if (component.Value)
                            {
                                expectedPresent.Add(componentType);
                            }
                            else
                            {
                                expectedAbsent.Add(componentType);
                            }
                        }
                        
                        if (!violationSeizure)
                        {
                            yescomponent = true;
                        }
                    }

                    if (violationSeizure && LoggingLevel == ContractLoggingLevel.Verbose)
                    {
                        var logMessage = new StringBuilder();
                        logMessage.AppendLine($"Contract {this.GetType().Name} (ID: {this.ContractId}): Component requirements violation for Entity {entityId}:");
                        
                        if (missingExpected.Count > 0)
                        {
                            logMessage.AppendLine($"  Missing expected components: {string.Join(", ", missingExpected.Select(t => t.Name))}");
                        }
                        
                        if (unexpectedPresent.Count > 0)
                        {
                            logMessage.AppendLine($"  Unexpected present components: {string.Join(", ", unexpectedPresent.Select(t => t.Name))}");
                        }
                        
                        logMessage.AppendLine($"  Expected present: {string.Join(", ", expectedPresent.Select(t => t.Name))}");
                        logMessage.AppendLine($"  Expected absent: {string.Join(", ", expectedAbsent.Select(t => t.Name))}");

                        var rawRules = new StringBuilder();
                        this.EntityComponentPresenceSign.ForEach(x => {
                            rawRules.Append($"EntityId: {x.Key} = ");
                            x.Value.ForEach(y =>
                            {
                                rawRules.Append($"{y.Key.IdToECSType().Name} = {y.Value}; ");
                            });
                            rawRules.AppendLine();
                        });

                        logMessage.AppendLine($"  Raw contract presence rules: {rawRules.ToString()}\n==!!==!!==!!==!!==!!==!!==!!==");

                        logMessage.AppendLine($"  All entity components: {string.Join(", ", actualComponents.Select(t => t.Name))}");
                        
                        NLogger.Log(logMessage.ToString());
                    }
                }

                // Check conditions
                if (!violationSeizure && localContractConditions.TryGetValue(entityId, out var conditions))
                {
                    for (int i = 0; i < conditions.Count; i++)
                    {
                        if (!conditions[i](entity))
                        {
                            violationSeizure = true;
                            globalViolationSeizure = true;
                            
                            if (LoggingLevel == ContractLoggingLevel.Verbose)
                            {
                                NLogger.Log($"Contract {this.GetType().Name} (ID: {this.ContractId}): Condition #{i} failed for Entity {entityId}");
                            }
                        }
                    }
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
                var entityWorld = ECSService.instance.GetEntityWorld(entityid);
                if (entityWorld == null || entityWorld.entityManager == null)
                {
                    if (LoggingLevel == ContractLoggingLevel.Verbose)
                    {
                        NLogger.Log($"Contract {this.GetType().Name} (ID: {this.ContractId}): Entity {entityid} - world or entity manager not found");
                    }
                    continue;
                }

                entityWorld.entityManager.EntityStorage.ExecuteReadLockedContinuously(entityid, (entid, contentity) =>
                {
                    bool violationSeizure = false;
                    Lockers.Add(entid, new List<RWLock.LockToken>());
                    
                    if (LocalEntityComponentPresenceSign.TryGetValue(entid, out var neededComponents))
                    {
                        var expectedPresent = new List<Type>();
                        var expectedAbsent = new List<Type>();
                        var actualComponents = new List<Type>();
                        var missingExpected = new List<Type>();
                        var unexpectedPresent = new List<Type>();

                        // Собираем информацию о всех компонентах сущности для логгирования
                        if (LoggingLevel == ContractLoggingLevel.Verbose)
                        {
                            actualComponents = contentity.entityComponents.ComponentClasses.ToList();
                        }

                        foreach (var component in neededComponents)
                        {
                            var componentType = component.Key.IdToECSType();

                            if (component.Value)
                            {
                                
                                if (contentity.entityComponents.GetReadLockedComponent(componentType, out var componentInstance, out var token))
                                {
                                    expectedPresent.Add(componentType);
                                    Lockers[entid].Add(token);
                                    continue;
                                }
                                else
                                {
                                    missingExpected.Add(componentType);
                                    violationSeizure = true;
                                    globalViolationSeizure = true;
                                }
                            }
                            else
                            {
                                if (contentity.entityComponents.HoldComponentAddition(componentType, out var token))
                                {
                                    if (!contentity.entityComponents.HasComponent(componentType))
                                    {
                                        expectedAbsent.Add(componentType);
                                        Lockers[entid].Add(token);
                                        continue;
                                    }
                                    else
                                    {
                                        token.Dispose();
                                        unexpectedPresent.Add(componentType);
                                        violationSeizure = true;
                                        globalViolationSeizure = true;
                                    }
                                }
                            }
                            violationSeizure = true;
                            globalViolationSeizure = true;
                        }

                        if (violationSeizure && LoggingLevel == ContractLoggingLevel.Verbose)
                        {
                            var logMessage = new StringBuilder();
                            logMessage.AppendLine($"Contract {this.GetType().Name} (ID: {this.ContractId}): Component requirements violation for Entity {entid}:");
                            
                            if (missingExpected.Count > 0)
                            {
                                logMessage.AppendLine($"  Missing expected components: {string.Join(", ", missingExpected.Select(t => t.Name))}");
                            }
                            
                            if (unexpectedPresent.Count > 0)
                            {
                                logMessage.AppendLine($"  Unexpected present components: {string.Join(", ", unexpectedPresent.Select(t => t.Name))}");
                            }
                            
                            logMessage.AppendLine($"  Expected present: {string.Join(", ", expectedPresent.Select(t => t.Name))}");
                            logMessage.AppendLine($"  Expected absent: {string.Join(", ", expectedAbsent.Select(t => t.Name))}");

                            var rawRules = new StringBuilder();
                            this.EntityComponentPresenceSign.ForEach(x => {
                                rawRules.Append($"EntityId: {x.Key} = ");
                                x.Value.ForEach(y =>
                                {
                                    rawRules.Append($"{y.Key.IdToECSType().Name} = {y.Value}; ");
                                });
                                rawRules.AppendLine();
                            });

                            logMessage.AppendLine($"  Raw contract presence rules: {rawRules.ToString()}\n==!!==!!==!!==!!==!!==!!==!!==");

                            logMessage.AppendLine($"  All entity components: {string.Join(", ", actualComponents.Select(t => t.Name))}");
                            
                            NLogger.Log(logMessage.ToString());
                        }
                    }

                    if (LocalContractConditions.TryGetValue(entid, out var conditions))
                    {
                        for (int i = 0; i < conditions.Count; i++)
                        {
                            if (!conditions[i](contentity))
                            {
                                violationSeizure = true;
                                globalViolationSeizure = true;
                                
                                if (LoggingLevel == ContractLoggingLevel.Verbose)
                                {
                                    NLogger.Log($"Contract {this.GetType().Name} (ID: {this.ContractId}): Condition #{i} failed for Entity {entid}");
                                }
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
                if (LoggingLevel == ContractLoggingLevel.Verbose)
                {
                    NLogger.Log($"Contract {this.GetType().Name} (ID: {this.ContractId}): No entities passed contract requirements");
                }
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
