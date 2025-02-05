
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
    public class ECSSystemManager
    {
        public IDictionary<long, SynchronizedList<ECSExecutableContractContainer>> AwaitingContractDatabase = new ConcurrentDictionary<long, SynchronizedList<ECSExecutableContractContainer>>();
        public IDictionary<ECSExecutableContractContainer, SynchronizedList<long>> ContractExecutionArgsDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, SynchronizedList<long>>(); // list of entity id in conditions and action


        public ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> SystemsInterestedEntityDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>>();//List of interested entity Instance ID
        public int SystemsInterestedEntityDatabaseCount;
        //fill all id before running ecs
        public ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> InterestedIDECSComponentsDatabase = new ConcurrentDictionary<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>>();
        public int InterestedIDECSComponentsDatabaseCount;

        public static bool LockSystems = false;

        public void InitializeSystems()
        {
            var AllSystems = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSExecutableContractContainer)).Select(x => (ECSExecutableContractContainer)Activator.CreateInstance(x)).ToList();
            AllSystems = AllSystems.Except(ReturnExceptedSystems()).ToList<ECSExecutableContractContainer>();
            FullUpdateSystemListOfInterestECSComponents(AllSystems);
            foreach(ECSExecutableContractContainer system in AllSystems)
            {
                system.Initialize();
                if(system.TimeDependExecution)
                    SystemsInterestedEntityDatabase.TryAdd(system, new ConcurrentDictionaryEx<long, int>());
                
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
            foreach(var SystemPair in SystemsInterestedEntityDatabase)
            {
                if (Interlocked.Equals(SystemPair.Key.TimeDependExecution, true) && Interlocked.Equals(SystemPair.Key.InWork, false) && SystemPair.Key.LastEndExecutionTimestamp + DateTimeExtensions.MillisecondToTicks
                    (SystemPair.Key.DelayRunMilliseconds) < DateTime.Now.Ticks)
                {
                    SystemPair.Key.InWork = true;
                    if(Syncronizable)
                    {
                        SystemPair.Key.Run(SystemsInterestedEntityDatabase[SystemPair.Key].Keys.ToArray());
                    }
                    else
                    {
                        TaskEx.RunAsync(() =>
                        {
                            SystemPair.Key.Run(SystemsInterestedEntityDatabase[SystemPair.Key].Keys.ToArray());
                        });
                    }
                }   
            }
        }

        

        private void RemoveContract(ECSExecutableContractContainer contract)
        {
            if(ContractExecutionArgsDatabase.TryGetValue(contract, out var entitiesargs))
            {
                foreach(var entity in entitiesargs)
                {
                    if(AwaitingContractDatabase.TryGetValue(entity, out var contracts))
                    {
                        contracts.Remove(contract);
                    }
                }
            }
            ContractExecutionArgsDatabase.Remove(contract);
        }

        private void TryExecuteContracts(List<ECSExecutableContractContainer> contracts)
        {
            foreach (var contract in contracts)
            {
                List<ECSEntity> contractArgs = new List<ECSEntity>();
                if(ContractExecutionArgsDatabase.TryGetValue(contract, out var contractArgsId))
                {
                    foreach(var arg in contractArgsId)
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
            ContractExecutionArgsDatabase.Add(contract, new SynchronizedList<long>(contract.ContractConditions.Keys));
            foreach(var entityid in contract.ContractConditions.Keys)
            {
                if(ManagerScope.instance.entityManager.EntityStorage.ContainsKey(entityid))
                {
                    SynchronizedList<ECSExecutableContractContainer> listContracts = null;
                    if(!AwaitingContractDatabase.TryGetValue(entityid, out listContracts))
                    {
                        listContracts = new SynchronizedList<ECSExecutableContractContainer>();
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
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> pair in this.InterestedIDECSComponentsDatabase)
            {
                int nulled = 0;
                if (pair.Value.TryGetValue(component.GetId(), out nulled))
                {
                    ConcurrentDictionaryEx<long, int> bufDict;
                    if (SystemsInterestedEntityDatabase.TryGetValue(pair.Key, out bufDict))
                        if (SystemsInterestedEntityDatabase[pair.Key].TryAdd(entity.instanceId, nulled))
                            Interlocked.Increment(ref SystemsInterestedEntityDatabase[pair.Key].FastCount);
                }
            }
        }

        public void OnEntityComponentChangedReaction(ECSEntity entity, ECSComponent component)
        {
            
        }
        public void OnEntityComponentRemovedReaction(ECSEntity entity, ECSComponent component)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> pair in this.InterestedIDECSComponentsDatabase)
            {
                if (pair.Value.TryGetValue(component.GetId(), out _))
                {
                    ConcurrentDictionaryEx<long, int> bufDict;
                    if (SystemsInterestedEntityDatabase.TryGetValue(pair.Key, out bufDict))
                    {
                        if (bufDict.Keys.Contains(entity.instanceId))
                        {
                            if (SystemsInterestedEntityDatabase[pair.Key].Remove(entity.instanceId, out _))
                                Interlocked.Decrement(ref SystemsInterestedEntityDatabase[pair.Key].FastCount);
                        }
                    }
                }
            }
        }

        public void OnEntityDestroyed(ECSEntity entity)
        {
            bool cleared = false;
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> pair in this.SystemsInterestedEntityDatabase)
            {
                int nulled = 0;
                ConcurrentDictionaryEx<long, int> bufDict;
                if(SystemsInterestedEntityDatabase.TryGetValue(pair.Key, out bufDict))
                    if(pair.Value.TryRemove(entity.instanceId, out nulled))
                    {
                        Interlocked.Decrement(ref SystemsInterestedEntityDatabase[pair.Key].FastCount);
                        cleared = true;
                    }
                    
            }
            if(!cleared)
            {
                NLogger.LogError("core system error");
            }
        }

        public void OnEntityCreated(ECSEntity entity)
        {
            foreach (KeyValuePair<ECSExecutableContractContainer, ConcurrentDictionaryEx<long, int>> pair in this.InterestedIDECSComponentsDatabase)
            {
                int nulled = 0;
                ConcurrentDictionaryEx<long, int> bufDict;
                if(SystemsInterestedEntityDatabase.TryGetValue(pair.Key, out bufDict))
                {
                    if (Collections.FirstIntersect(pair.Value, entity.entityComponents.IdToTypeComponent.Keys))
                    {
                        if (SystemsInterestedEntityDatabase[pair.Key].TryAdd(entity.instanceId, nulled))
                            Interlocked.Increment(ref SystemsInterestedEntityDatabase[pair.Key].FastCount);
                    }
                }
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
            FullUpdateSystemListOfInterestECSComponents(new List<ECSExecutableContractContainer> { system });
            SystemsInterestedEntityDatabase.TryAdd(system, new ConcurrentDictionaryEx<long, int>());
        }

        private void FullUpdateSystemListOfInterestECSComponents(List<ECSExecutableContractContainer> allSystems)
        {
            foreach(ECSExecutableContractContainer system in allSystems)
            {
                if(InterestedIDECSComponentsDatabase.TryAdd(system, new ConcurrentDictionaryEx<long, int>(system.GetInterestedComponentsList())))
                {
                    Interlocked.Increment(ref InterestedIDECSComponentsDatabase[system].FastCount);
                }
            }
        }

        public void UpdateSystemListOfInterestECSComponents(ECSExecutableContractContainer system, List<long> updatedIds)
        {

        }
    }
}
