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
public abstract
#if GODOT4_0_OR_GREATER
    partial
#endif
    class IManager : ProxyBehaviour
    {
        #if GODOT
        [Godot.Export]
        #endif
        public long instanceId = Guid.NewGuid().GuidToLongR();
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
        #if GODOT
        [Godot.Export]
        #endif
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
        #if GODOT
        [Godot.Export]
        #endif
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

        public
#if NET
            override
#endif
            void Awake()
        {
            if (!isPrefabScript)
            {
                OnAwakeManager();
            }
        }

        public
#if NET
            override
#endif
             void OnEnable()
        {
            if (!isPrefabScript)
            {
                OnActivateManager();
            }
        }

        public
#if NET || GODOT
            override
#endif
             void Start()
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

        public
#if NET
            override
#endif
             void Reset()
        {
            if (!isPrefabScript)
                ResetManager();
        }

        protected virtual void ResetManager()
        {

        }

        public
#if NET
            override
#endif
             void FixedUpdate()
        {
            if (!isPrefabScript)
            {
                this.FixedUpdateManager();
            }

        }
        protected virtual void FixedUpdateManager()
        {

        }

        public
#if NET
            override
#endif
             void Update()
        {
            if (!isPrefabScript)
            {
                this.UpdateManager();
            }

        }
        protected virtual void UpdateManager()
        {

        }



        public
#if NET
            override
#endif
             void OnCollisionEnter(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
              collision)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionEnterManager(collision);
            }
        }

        protected virtual void OnCollisionEnterManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
              collision)
        {

        }

        public
#if NET
            override
#endif
             void OnCollisionExit(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
              collisionInfo)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionExitManager(collisionInfo);
            }
        }

        protected virtual void OnCollisionExitManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
              collisionInfo)
        {

        }

        public
#if NET
            override
#endif
             void OnCollisionStay(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
              collisionInfo)
        {
            if (!isPrefabScript)
            {
                this.OnCollisionStayManager(collisionInfo);
            }
        }

        protected virtual void OnCollisionStayManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collision
#else
            EngineApiCollision3D
#endif
             collisionInfo)
        {

        }

        public
#if NET
            override
#endif
             void OnTriggerEnter(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
             other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerEnterManager(other);
            }
        }

        protected virtual void OnTriggerEnterManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
             other)
        {

        }

        public
#if NET
            override
#endif
             void OnTriggerExit(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
             other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerExitManager(other);
            }
        }

        protected virtual void OnTriggerExitManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
             other)
        {

        }

        public
#if NET
            override
#endif
             void OnTriggerStay(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
             other)
        {
            if (!isPrefabScript)
            {
                this.OnTriggerStayManager(other);
            }
        }

        protected virtual void OnTriggerStayManager(
#if UNITY_5_3_OR_NEWER
            UnityEngine.Collider
#else
            EngineApiCollider3D
#endif
            other)
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

        public
#if NET
            override
#endif
             void OnDisable()
        {
            if (!isPrefabScript)
                OnDeactivateManager();
        }

        public
#if NET
            override
#endif
             void OnDestroy()
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
                NLogger.Error("Error destroy " + this.GetType() + "  " + ex.Message + " ____ " + ex.StackTrace);
            }
        }
    }
}
