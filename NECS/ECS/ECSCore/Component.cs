using NECS.Core.Logging;
using NECS.Extensions;

namespace NECS.ECS.ECSCore
{
    [Serializable]
    [TypeUid(3)]
    public class ECSComponent : IECSObject, ICloneable
    {
        static new public long Id { get; set; } = 0;

        [NonSerialized]
        public ECSEntity ownerEntity;
        [NonSerialized]
        public ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        [NonSerialized]
        public List<string> ConfigPath = new List<string>();

        public Dictionary<long, ECSComponentGroup> ComponentGroups = new Dictionary<long, ECSComponentGroup>();//todo: concurrent replace to normal
        [NonSerialized]
        static public List<Action> StaticOnChangeHandlers = new List<Action>();
        [NonSerialized]
        public List<Action<ECSEntity, ECSComponent>> OnChangeHandlers = new List<Action<ECSEntity, ECSComponent>>();
        [NonSerialized]
        public bool Unregistered = true;
        [NonSerialized]
        public ComponentManagers componentManagers;

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
                Logger.Log(ObjectType);
                Logger.Log("Type not has callbacks");
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

        public void MarkAsChanged(bool silent = false)
        {
            if (ownerEntity != null)
            {
                ownerEntity.entityComponents.MarkComponentChanged(this, silent);
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

        /// overridable functional for damage transformer, after adding component of damage effect - in this method we send transformer action to damage transformers agregator
        public virtual void OnAdded(ECSEntity entity)
        {
            this.MarkAsChanged();
        }

        public virtual void OnChange(ECSEntity entity)
        {

        }

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
