using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Extensions;
using NECS.GameEngineAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IManager : ProxyBehaviour
    {
        public long instanceId = Guid.NewGuid().GuidToLong();
        #region typeId
        private static Type _managerTypeValue = null;
        public static Type ManagerType
        {
            get
            {
                if (_managerTypeValue == null)
                {
                    var AllDirtyManagers = ECSAssemblyExtensions.GetAllSubclassOf(typeof(IManager)).Where(x => !x.IsAbstract).Select(x => (IManager)Activator.CreateInstance(x)).ToList();
                    foreach (var manager in AllDirtyManagers)
                    {
                        var field = manager.GetType().GetField("_managerTypeValue", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                        field.SetValue(manager.GetType(), manager.GetType());
                    }
                }
                return _managerTypeValue;
            }
        }
        #endregion
        private IECSObject _ConnectPoint;
        public IECSObject ConnectPoint
        {
            get
            {
                return _ConnectPoint;
            }
            set
            {
                if (this is IEntityManager && value is ECSEntity)
                {
                    (this as IEntityManager).ManagerEntity = value as ECSEntity;
                }
                value.connectPoints.Add(this);
                _ConnectPoint = value;
            }
        }

        private bool NoSetupChild = false;
        public bool isNoSetupChild
        {
            get
            {
                return NoSetupChild;
            }
            set
            {
                NoSetupChild = value;
            }
        }

        private bool PrefabScript = false;
        public bool isPrefabScript
        {
            get
            {
                return PrefabScript;
            }
            set
            {
                PrefabScript = value;
            }
        }

        #region mockFunctions

        public override void Awake()
        {
            if (!isPrefabScript)
            {
                OnAwakeManager();
            }
        }

        public override void OnEnable()
        {
            if (!isPrefabScript)
            {
                OnActivateManager();
            }
        }

        public override void Start()
        {
            if (!isPrefabScript)
            {
                if (!NoSetupChild)
                {
                    if (this is IEntityManager)
                        ManagerSpace.InstantiatedProcess(this.gameObject, (IEntityManager)this);
                    if (SetupAction != null)
                        SetupAction(this);
                }
                OnStartManager();
            }
        }

        public override void Reset()
        {
            if (!isPrefabScript)
                ResetManager();
        }

        protected virtual void ResetManager()
        {

        }

        public override void FixedUpdate()
        {
            if (!isPrefabScript)
            {
                this.FixedUpdateManager();
            }

        }
        protected virtual void FixedUpdateManager()
        {

        }

        public override void Update()
        {
            if (!isPrefabScript)
            {
                this.UpdateManager();
            }

        }
        protected virtual void UpdateManager()
        {

        }



        public override void OnCollisionEnter(EngineApiCollision3D collision)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionEnterManager(collision);
            }
        }

        protected virtual void OnCollisionEnterManager(EngineApiCollision3D collision)
        {

        }

        public override void OnCollisionExit(EngineApiCollision3D collisionInfo)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionExitManager(collisionInfo);
            }
        }

        protected virtual void OnCollisionExitManager(EngineApiCollision3D collisionInfo)
        {

        }

        public override void OnCollisionStay(EngineApiCollision3D collisionInfo)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionStayManager(collisionInfo);
            }
        }

        protected virtual void OnCollisionStayManager(EngineApiCollision3D collisionInfo)
        {

        }

        public override void OnTriggerEnter(EngineApiCollider3D other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerEnterManager(other);
            }
        }

        protected virtual void OnTriggerEnterManager(EngineApiCollider3D other)
        {

        }

        public override void OnTriggerExit(EngineApiCollider3D other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerExitManager(other);
            }
        }

        protected virtual void OnTriggerExitManager(EngineApiCollider3D other)
        {

        }

        public override void OnTriggerStay(EngineApiCollider3D other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerStayManager(other);
            }
        }

        protected virtual void OnTriggerStayManager(EngineApiCollider3D other)
        {

        }

        protected virtual void OnApplicationPause()
        {
            if (!isPrefabScript)
            {
                this.OnApplicationPauseManager();
            }
        }

        protected virtual void OnApplicationPauseManager()
        {

        }

        public override void OnDisable()
        {
            if (!isPrefabScript)
                OnDeactivateManager();
        }

        public override void OnDestroy()
        {
            if (!isPrefabScript)
            {
                OnRemoveManager();
                if (this is IEntityManager)
                    ((IEntityManager)this).ClearManagable();
                if (RemoveAction != null)
                    RemoveAction(this);
            }
        }

        #endregion

        public abstract void AddManager();
        protected abstract void OnStartManager();
        protected abstract void OnAwakeManager();
        protected abstract void OnRemoveManager();
        protected abstract void OnActivateManager();
        protected abstract void OnDeactivateManager();

        public Action<IManager> SetupAction;
        public Action<IManager> RemoveAction;
        public virtual void ActivateManager()
        {
            this.enabled = true;
        }
        public virtual void DeactivateManager()
        {
            this.enabled = false;
        }
        public virtual void RemoveManager()
        {
            try
            {
                this.ExecuteInstruction(() => Destroy(this));
            }
            catch (Exception ex)
            {
                Logger.Error("Error destroy " + this.GetType() + "  " + ex.Message + " ____ " + ex.StackTrace);
            }
        }
    }
}
