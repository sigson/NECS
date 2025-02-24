
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NECS.Core.Logging;
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
    public class ECSContractsManager
    {
        public IDictionary<long, ConcurrentHashSet<ECSExecutableContractContainer>> AwaitingContractDatabase = new ConcurrentDictionary<long, ConcurrentHashSet<ECSExecutableContractContainer>>();
        //public IDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, bool>> ContractExecutionArgsDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, bool>>(); // list of entity id in conditions and action


        public IDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> TimeDependContractEntityDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, int>>();//List of interested entity Instance ID
        //fill all id before running ecs

        public static bool LockSystems = false;

        public void InitializeSystems()
        {
            var AllSystems = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSExecutableContractContainer)).Select(x => (ECSExecutableContractContainer)Activator.CreateInstance(x)).ToList();
            AllSystems = AllSystems.Except(ReturnExceptedSystems()).ToList<ECSExecutableContractContainer>();
            foreach(ECSExecutableContractContainer system in AllSystems)
            {
                system.Initialize();
                if (system.TimeDependExecution)
                {
                    if (system.ContractConditions != null && system.EntityComponentPresenceSign != null)
                        TimeDependContractEntityDatabase.TryAdd(system, new ConcurrentDictionary<long, int>());
                    else
                        NLogger.Error($"System {system.GetType().Name} has not initialized conditions.");
                }

                foreach (var CallbackData in system.ComponentsOnChangeCallbacks)
                {
                    List<Action<ECSEntity, ECSComponent>> callBack;
                    if (ECSComponentManager.OnChangeCallbacksDB.TryGetValue(CallbackData.Key, out callBack))
                    {
                        ECSComponentManager.OnChangeCallbacksDB[CallbackData.Key] = callBack.Concat(CallbackData.Value).ToList();
                    }
                    else
                    {
                        ECSComponentManager.OnChangeCallbacksDB[CallbackData.Key] = CallbackData.Value;
                    }
                }
                
            }
        }

        public void RunTimeDependContracts()
        {
            if (LockSystems)
                return;
            foreach(var SystemPair in TimeDependContractEntityDatabase)
            {
                if (SystemPair.Key.TimeDependActive && !SystemPair.Key.InWork && SystemPair.Key.LastEndExecutionTimestamp + DateTimeExtensions.MillisecondToTicks
                    (SystemPair.Key.DelayRunMilliseconds) < DateTime.Now.Ticks)
                {
                    TryExecuteContracts(new List<ECSExecutableContractContainer> { SystemPair.Key }, TimeDependContractEntityDatabase[SystemPair.Key].Keys.ToList());
                }
            }
        }

        private void TryExecuteContracts(IEnumerable<ECSExecutableContractContainer> contracts, List<long> argEntities = null)
        {
            foreach (var contract in contracts)
            {
                if(contract.TimeDependExecution && argEntities != null)
                {
                    if(contract.TryExecuteContract(true, argEntities))
                    {
                        if(contract.RemoveAfterExecution)
                            RemoveContract(contract);
                    }
                }
                else
                {
                    if(contract.TryExecuteContract())
                    {
                        if(contract.RemoveAfterExecution)
                            RemoveContract(contract);
                    }
                    if(contract.NowTried >= contract.MaxTries)
                    {
                        RemoveContract(contract);
                    }
                }
            }
        }

        private void RemoveContract(ECSExecutableContractContainer contract)
        {
            foreach (var entity in contract.NeededEntities)
            {
                if (contract.RemoveAfterExecution && AwaitingContractDatabase.TryGetValue(entity, out var contracts))
                {
                    contracts.Remove(contract);
                }
                if (contract.RemoveAfterExecution && TimeDependContractEntityDatabase.TryGetValue(contract, out var entities))
                {
                    TimeDependContractEntityDatabase.Remove(contract);
                }
            }
        }

        public void RegisterContract(ECSExecutableContractContainer contract, bool autoExecute = true)
        {
            if(contract.EntityComponentPresenceSign == null || contract.ContractConditions == null || (contract.ContractConditions.Count == 0 && contract.EntityComponentPresenceSign.Count == 0))
            {
                NLogger.Log("Contract aborted. No conditions");
                return;
            }

            foreach(var entityid in contract.NeededEntities)
            {
                if(ManagerScope.instance.entityManager.EntityStorage.ContainsKey(entityid))
                {
                    ConcurrentHashSet<ECSExecutableContractContainer> listContracts = null;
                    if(!AwaitingContractDatabase.TryGetValue(entityid, out listContracts))
                    {
                        listContracts = new ConcurrentHashSet<ECSExecutableContractContainer>();
                        AwaitingContractDatabase[entityid] = listContracts;
                    }
                    listContracts.Add(contract);
                }
            }
            if(autoExecute)
                TryExecuteContracts(new List<ECSExecutableContractContainer>{ contract });
        }

        public void OnEntityComponentAddedReaction(ECSEntity entity, ECSComponent component)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                if (pair.Key.TryExecuteContract(false, new List<long> { entity.instanceId }))
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.TryAdd(entity.instanceId, 0);
                }
                else
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.Remove(entity.instanceId, out _);
                }
            }
            if(this.AwaitingContractDatabase.TryGetValue(entity.instanceId, out var contracts))
            {
                TryExecuteContracts(contracts);
            }
        }

        public void OnEntityComponentChangedReaction(ECSEntity entity, ECSComponent component)
        {
            
        }
        public void OnEntityComponentRemovedReaction(ECSEntity entity, ECSComponent component)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                if (pair.Key.TryExecuteContract(false, new List<long> { entity.instanceId }))
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.TryAdd(entity.instanceId, 0);
                }
                else
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.Remove(entity.instanceId, out _);
                }
            }
            if(this.AwaitingContractDatabase.TryGetValue(entity.instanceId, out var contracts))
            {
                TryExecuteContracts(contracts);
            }
        }

        public void OnEntityDestroyed(ECSEntity entity)
        {
            bool cleared = false;
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                int nulled = 0;
                ConcurrentDictionary<long, int> bufDict;
                if(TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                    if(pair.Value.TryRemove(entity.instanceId, out nulled))
                    {
                        cleared = true;
                    }
            }
            if(this.AwaitingContractDatabase.TryGetValue(entity.instanceId, out var contracts))
            {
                contracts.ForEach(x => RemoveContract(x));
            }
            if(!cleared)
            {
                NLogger.LogError("core system error");
            }
        }

        public void OnEntityCreated(ECSEntity entity)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                if (pair.Key.TryExecuteContract(false, new List<long> { entity.instanceId }))
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.TryAdd(entity.instanceId, 0);
                }
                else
                {
                    ConcurrentDictionary<long, int> bufDict;
                    if (TimeDependContractEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        bufDict.Remove(entity.instanceId, out _);
                }
            }
            if(this.AwaitingContractDatabase.TryGetValue(entity.instanceId, out var contracts))
            {
                TryExecuteContracts(contracts);
            }
        }

        public List<ECSExecutableContractContainer> ReturnExceptedSystems()
        {
            List<ECSExecutableContractContainer> list = new List<ECSExecutableContractContainer> {

            };
            return list;
        }

        public void AppendSystemInRuntime(ECSExecutableContractContainer system)
        {
            TimeDependContractEntityDatabase.TryAdd(system, new ConcurrentDictionary<long, int>());
        }

        public void UpdateSystemListOfInterestECSComponents(ECSExecutableContractContainer system, List<long> updatedIds)
        {

        }
    }
}
