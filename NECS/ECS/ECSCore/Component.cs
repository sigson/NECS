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
        static new public long Id { get; set; } = 0;

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
        public ComponentManagers componentManagers = new ComponentManagers();

        public ECSComponent()
        {
            componentManagers.ownerComponent = this;
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
            if (ownerEntity != null)
            {
                ownerEntity.entityComponents.DirectiveChange(this.GetType());
            }
        }

        public void MarkAsChanged(bool serializationSilent = false, bool eventSilent = false)
        {
            if (ownerEntity != null)
            {
                ownerEntity.entityComponents.MarkComponentChanged(this, serializationSilent, eventSilent);
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
        public virtual void OnAdded(ECSEntity entity)
        {
            this.MarkAsChanged();
        }

        /// <summary>
        /// ATTENTION! Use lock(this.SerialLocker) if you want to edit fields value for prevent serialization error!
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnChange(ECSEntity entity)
        {

        }
        /// <summary>
        /// ATTENTION! Use lock(this.SerialLocker) if you want to edit fields value for prevent serialization error!
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnRemoving(ECSEntity entity)
        {

        }

        public void OnRemove()
        {
            ConfigPath.Clear();
            ComponentGroups.ClearI(this.SerialLocker);
            OnChangeHandlers.Clear();
        }
        public void RunOnChangeCallbacks(ECSEntity parentEntity)
        {
            List<Action<ECSEntity, ECSComponent>> callbackActions;
            ECSComponentManager.OnChangeCallbacksDB.TryGetValue(this.GetId(), out callbackActions);
            this.OnChange(parentEntity);
            if (callbackActions!=null)
            {
                foreach (var act in callbackActions)
                {
                    act(parentEntity, this);
                }
            }            
        }
        public object Clone() => MemberwiseClone();
    }
}
