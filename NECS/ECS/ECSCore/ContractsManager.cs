
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
        public IDictionary<long, ConcurrentDictionary<ECSExecutableContractContainer, bool>> AwaitingContractDatabase = new ConcurrentDictionary<long, ConcurrentDictionary<ECSExecutableContractContainer, bool>>();
        public IDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, bool>> ContractExecutionArgsDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionary<long, bool>>(); // list of entity id in conditions and action


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
                if(system.TimeDependExecution)
                    TimeDependContractEntityDatabase.TryAdd(system, new ConcurrentDictionary<long, int>());
                
                foreach(var CallbackData in system.ComponentsOnChangeCallbacks)
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

        public void RunTimeDependContracts(bool Syncronizable)
        {
            if (LockSystems)
                return;
            foreach(var SystemPair in TimeDependContractEntityDatabase)
            {
                if (Interlocked.Equals(SystemPair.Key.TimeDependExecution, true) && Interlocked.Equals(SystemPair.Key.InWork, false) && SystemPair.Key.LastEndExecutionTimestamp + DateTimeExtensions.MillisecondToTicks
                    (SystemPair.Key.DelayRunMilliseconds) < DateTime.Now.Ticks)
                {
                    SystemPair.Key.InWork = true;
                    if(Syncronizable)
                    {
                        SystemPair.Key.Run(TimeDependContractEntityDatabase[SystemPair.Key].Keys.ToArray());
                    }
                    else
                    {
                        TaskEx.RunAsync(() =>
                        {
                            SystemPair.Key.Run(TimeDependContractEntityDatabase[SystemPair.Key].Keys.ToArray());
                        });
                    }
                }   
            }
        }

        

        private void RemoveContract(ECSExecutableContractContainer contract)
        {
            if(ContractExecutionArgsDatabase.TryGetValue(contract, out var entitiesargs))
            {
                foreach(var entity in entitiesargs.Keys)
                {
                    if(contract.RemoveAfterExecution && AwaitingContractDatabase.TryGetValue(entity, out var contracts))
                    {
                        contracts.Remove(contract, out _);
                    }
                    if(contract.RemoveAfterExecution && TimeDependContractEntityDatabase.TryGetValue(contract, out var entities))
                    {
                        TimeDependContractEntityDatabase.Remove(contract);
                    }
                }
            }
            ContractExecutionArgsDatabase.Remove(contract);
        }

        private void TryExecuteContracts(IEnumerable<ECSExecutableContractContainer> contracts)
        {
            foreach (var contract in contracts)
            {
                List<ECSEntity> contractArgs = new List<ECSEntity>();
                if(ContractExecutionArgsDatabase.TryGetValue(contract, out var contractArgsId))
                {
                    foreach(var arg in contractArgsId.Keys)
                    {
                        if(ManagerScope.instance.entityManager.EntityStorage.TryGetValue(arg, out var entity))
                        {
                            contractArgs.Add(entity);
                        }
                    }
                    if(contractArgs.Count != contractArgsId.Count)
                    {
                        NLogger.Log("Contract aborted. Entity not found");
                        RemoveContract(contract);
                    }
                }
                if(contractArgs.Count != 0)
                {
                    if(contract.TryExecuteContract(contractArgs))
                    {
                        RemoveContract(contract);
                    }
                }
                else
                {
                    NLogger.Log("Contract aborted. No has entities");
                    RemoveContract(contract);
                }
            }
        }

        public void RegisterContract(ECSExecutableContractContainer contract, bool autoExecute = true)
        {
            var argsId = new ConcurrentDictionary<long, bool>();
            contract.ContractConditions.Keys.ForEach(x => argsId.TryAdd(x, false));
            ContractExecutionArgsDatabase.Add(contract, argsId);
            foreach(var entityid in contract.ContractConditions.Keys)
            {
                if(ManagerScope.instance.entityManager.EntityStorage.ContainsKey(entityid))
                {
                    ConcurrentDictionary<ECSExecutableContractContainer, bool> listContracts = null;
                    if(!AwaitingContractDatabase.TryGetValue(entityid, out listContracts))
                    {
                        listContracts = new ConcurrentDictionary<ECSExecutableContractContainer, bool>();
                        AwaitingContractDatabase[entityid] = listContracts;
                    }
                    listContracts.TryAdd(contract, false);
                }
            }
            if(autoExecute)
                TryExecuteContracts(new List<ECSExecutableContractContainer>{ contract });
        }

        public void OnEntityComponentAddedReaction(ECSEntity entity, ECSComponent component)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                if (pair.Key.TryExecuteContract(new List<ECSEntity> { entity }, false))
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
                TryExecuteContracts(contracts.Keys);
            }
        }

        public void OnEntityComponentChangedReaction(ECSEntity entity, ECSComponent component)
        {
            
        }
        public void OnEntityComponentRemovedReaction(ECSEntity entity, ECSComponent component)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionary<long, int>> pair in this.TimeDependContractEntityDatabase)
            {
                if (pair.Key.TryExecuteContract(new List<ECSEntity> { entity }, false))
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
                TryExecuteContracts(contracts.Keys);
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
                contracts.Keys.ForEach(x => RemoveContract(x));
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
                if (pair.Key.TryExecuteContract(new List<ECSEntity> { entity }, false))
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
                TryExecuteContracts(contracts.Keys);
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
