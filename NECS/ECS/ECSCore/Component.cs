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
using System.Threading;
using NECS.Harness.Services;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(3)]
    /// <summary>
    /// ATTENTION! Use lock(this.SerialLocker) when you edit component fields if you want to edit fields value for prevent serialization error!
    /// </summary>
    /// <param name="entity"></param>
    public class ECSComponent : IECSObject, ICloneable
    {
        static new public long Id { get; set; } = 3;

        [System.NonSerialized]
        public ECSEntity ownerEntity;
        [System.NonSerialized]
        public ComponentsDBComponent ownerDB;
        [System.NonSerialized]
        public ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        [System.NonSerialized]
        public List<string> ConfigPath = new List<string>();

        public Dictionary<long, ECSComponentGroup> ComponentGroups = new Dictionary<long, ECSComponentGroup>();//todo: concurrent replace to normal
        [System.NonSerialized]
        static public List<Action> StaticOnChangeHandlers = new List<Action>();
        [System.NonSerialized]
        public List<Action<ECSEntity, ECSComponent>> OnChangeHandlers = new List<Action<ECSEntity, ECSComponent>>();
        [System.NonSerialized]
        public bool Unregistered = true;
        [System.NonSerialized]
        public bool AlreadyRemovedReaction = false;
        public ComponentManagersStorage componentManagers
        {
            get
            {
                if (this.ownerEntity != null)
                {
                    if (!this.ownerEntity.entityComponents.ComponentsManagers.TryGetValue(this.instanceId, out var manager))
                    {
                        manager = new ComponentManagersStorage();
                        manager.ownerComponent.ECSObject = this;
                        manager.ownerComponent.AlwaysUpdateCache = true;
                        this.ownerEntity.entityComponents.ComponentsManagers[this.instanceId] = manager;
                    }
                    return manager;
                }
                return null;
            }
        }

        public enum StateReactionType
        {
            Added,
            Changed,
            Removed
        }

        [System.NonSerialized]
        private PriorityEventQueue<StateReactionType, Action> _stateReactionQueue = null;
        [JsonIgnore]
        public PriorityEventQueue<StateReactionType, Action> StateReactionQueue
        {
            get
            {
                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    return ECSSharedField<PriorityEventQueue<StateReactionType, Action>>.GetOrAdd(instanceId, "StateReactionQueue", new PriorityEventQueue<StateReactionType, Action>(new List<StateReactionType>() { StateReactionType.Added, StateReactionType.Changed, StateReactionType.Removed }, 1, x => x + 2, this.GetTypeFast()));
                else
                {
                    if (_stateReactionQueue == null)
                    {
                        _stateReactionQueue = new PriorityEventQueue<StateReactionType, Action>(new List<StateReactionType>() { StateReactionType.Added, StateReactionType.Changed, StateReactionType.Removed }, 1, x => x + 2, this.GetTypeFast());
                    }
                    return _stateReactionQueue;
                }

            }
            set
            {
                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    ECSSharedField<PriorityEventQueue<StateReactionType, Action>>.SetCachedValue(instanceId, "StateReactionQueue", value);
                else
                    _stateReactionQueue = value;
            }
        }


        public ECSComponent()
        {
            //componentManagers.ownerComponent = this;
            //StateReactionQueue = new PriorityEventQueue<StateReactionType, Action>(new List<StateReactionType>() { StateReactionType.Added, StateReactionType.Changed, StateReactionType.Removed }, 1, x => x + 2);
        }

        public List<Action<ECSEntity, ECSComponent>> GetOnChangeComponentCallback()
        {
            if (ObjectType == null)
            {
                ObjectType = GetType();
            }
            try
            {
                if(OnChangeHandlers == null)
                {
                    OnChangeHandlers = ECSComponentManager.OnChangeCallbacksDB[this.GetId()];
                }
                
                return OnChangeHandlers;
            }
            catch
            {
                NLogger.Log(ObjectType);
                NLogger.Log("Type not has callbacks");
                return null;
            }
            
        }

        public void DirectiveSetChanged()
        {
            if (ownerEntity != null && ownerDB == null)
            {
                ownerEntity.entityComponents.DirectiveChange(this.GetType());
            }
        }

        public void MarkAsChanged(bool serializationSilent = false, bool eventSilent = false)
        {
            if (ownerEntity != null && ownerDB == null)
            {
                ownerEntity.entityComponents.MarkComponentChanged(this, serializationSilent, eventSilent);
            }
            if(ownerDB != null && (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server || GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Offline))
            {
                ownerDB.ChangeComponent(this);
                ownerDB.MarkAsChanged();
            }
        }

        public ECSComponent SetGlobalComponentGroup()
        {
            this.ComponentGroups.SetI(ECSComponentManager.GlobalProgramComponentGroup.GetId(), ECSComponentManager.GlobalProgramComponentGroup, this.SerialLocker);

            return this; 
        }

        public ECSComponent AddComponentGroup(ECSComponentGroup componentGroup)
        {
            this.ComponentGroups.SetI(componentGroup.GetId(), componentGroup, this.SerialLocker);
            return this;
        }

        public Type GetTypeFast()
        {
            if (ObjectType == null)
            {
                ObjectType = GetType();
            }
            return ObjectType;
        }

        // overridable functional for damage transformer, after adding component of damage effect - in this method we send transformer action to damage transformers agregator
        /// <summary>
        /// ATTENTION! Use lock(this.SerialLocker) if you want to edit fields value for prevent serialization error!
        /// </summary>
        /// <param name="entity"></param>
        public void AddedReaction(ECSEntity entity)
        {
            StateReactionQueue.AddEvent(StateReactionType.Added, () =>
            {
                lock (this.StateReactionQueue)
                {
                    this.OnAdded(entity);
                }
            });
        }

        protected virtual void OnAdded(ECSEntity entity)
        {
            this.MarkAsChanged();
        }

        /// <summary>
        /// ATTENTION! Use lock(this.SerialLocker) if you want to edit fields value for prevent serialization error!
        /// </summary>
        /// <param name="entity"></param>
        public void ChangeReaction(ECSEntity entity)
        {
            StateReactionQueue.AddEvent(StateReactionType.Changed, () =>
            {
                lock (this.StateReactionQueue)
                {
                    List<Action<ECSEntity, ECSComponent>> callbackActions;
                    ECSComponentManager.OnChangeCallbacksDB.TryGetValue(this.GetId(), out callbackActions);
                    this.OnChanged(entity);
                    if (callbackActions != null)
                    {
                        foreach (var act in callbackActions)
                        {
                            act(entity, this);
                        }
                    }
                }
            });
        }

        protected virtual void OnChanged(ECSEntity entity)
        {
            
        }
        /// <summary>
        /// ATTENTION! Use lock(this.SerialLocker) if you want to edit fields value for prevent serialization error!
        /// </summary>
        /// <param name="entity"></param>
        public void RemovingReaction(ECSEntity entity)
        {
            if (AlreadyRemovedReaction)//unsafe, but i don't care
            {
                return;
            }
            AlreadyRemovedReaction = true;
            StateReactionQueue.AddEvent(StateReactionType.Removed, () =>
            {
                lock (this.StateReactionQueue)
                {
                    if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client || GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Offline)
                    {
                        try
                        {
                            this.connectPoints.ForEach(x => x.RemoveManager());
                            this.connectPoints.Clear();
                        }
                        catch (Exception ex)
                        {
                            NLogger.Error(ex + "\n" + this.GetType() + "\n" + ex.StackTrace);
                        }
                    }
                    
                    this.OnRemoved(entity);
                    ECSSharedField<object>.RemoveAllCachedValuesForId(this.instanceId);
                    this.IECSDispose();
                }
            });
        }

        protected virtual void OnRemoved(ECSEntity entity)
        {
            
        }

        public override void ChainedIECSDispose()
        {
            base.ChainedIECSDispose();
            if(this.ownerEntity != null)
            {
                if(this.ownerDB != null)
                {
                    this.ownerDB.RemoveComponent(this.instanceId);
                }
                else
                {
                    this.ownerEntity.RemoveComponent(this.GetTypeFast());
                }
            }
        }

        public void OnRemove()
        {
            ConfigPath.Clear();
            ComponentGroups.ClearI(this.SerialLocker);
            OnChangeHandlers.Clear();
            ECSSharedField<object>.RemoveAllCachedValuesForId(this.instanceId);
        }
        public void RunOnChangeCallbacks(ECSEntity parentEntity)
        {
            
        }

        public object Clone() => MemberwiseClone();
    }
}
