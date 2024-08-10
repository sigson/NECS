using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class EngineApiObjectBehaviour
#if UNITY_5_3_OR_NEWER
: UnityEngine.MonoBehaviour
    {
        public UnityEngine.Component AddComponent(Type componentType)
        {
            return this.gameObject.AddComponent(componentType);
        }

        public T AddComponent<T>() where T : UnityEngine.Component
        {
            return this.gameObject.AddComponent<T>();
        }
    }
#endif
#if GODOT4_0_OR_GREATER
        : Godot.Node, IEngineApiObjectBehaviour, IEngineApiCallableMethods
    {
        public EngineApiObjectBehaviour gameObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool enabled
        {
            get => isEnabled;
            set
            {
                childComponents.ForEach(x => x.enabled = value);
                if (this.isEnabled && this.isEnabled != value)
                {
                    this.OnDisable();
                }
                if (!this.isEnabled && this.isEnabled != value)
                {
                    this.OnEnable();
                }
                this.isEnabled = value;
            }
        }
        private bool isEnabled = true;
        public bool activeInHierarchy { get => enabled; set => enabled = value; }
        public GodotPhysicAgent PhysicAgent;
        
        protected List<EngineApiObjectBehaviour> childComponents;

        #region GodotBase

        public override void _EnterTree()//non usable because running before full init of childs
        {
            if (enabled)
                base._EnterTree();
        }


        public override void _Ready() // awake
        {
            if (enabled)
            {
                base._Ready();
                this.Start();
            }

        }


        public override void _ExitTree()
        {
            if (enabled)
            {
                base._ExitTree();
                this.OnDisable();
            }
        }

        public override void _Process(double delta)
        {
            // Called every frame.
            if (enabled)
            {
                base._Process(delta);
                this.Update();
                this.Update(delta);
                this.LateUpdate();
                this.LateUpdate(delta);
            }

        }

        public override void _PhysicsProcess(double delta)
        {
            if (enabled)
            {
                base._PhysicsProcess(delta);
                this.FixedUpdate();
                this.FixedUpdate(delta);
            }
        }

        // Called once for every event.
        public override void _UnhandledInput(Godot.InputEvent @event)
        {
            base._UnhandledInput(@event);
        }

        // Called once for every event before _UnhandledInput(), allowing you to
        // consume some events.
        public override void _Input(Godot.InputEvent @event)
        {
            base._Input(@event);
        }

        #endregion

        #region overridable functions

        public virtual void Awake()
        {
            throw new NotImplementedException();
        }

        public virtual void Start()
        {
            throw new NotImplementedException();
        }

        public virtual void Update(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual void Update()
        {
            throw new NotImplementedException();
        }

        public virtual void FixedUpdate()
        {
            throw new NotImplementedException();
        }

        public virtual void FixedUpdate(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual void LateUpdate()
        {
            throw new NotImplementedException();
        }

        public virtual void LateUpdate(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationFocus(bool focus)
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationPause(bool pause)
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationQuit()
        {
            throw new NotImplementedException();
        }


        public virtual void OnCollisionEnter(EngineApiCollision3D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionEnter = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnCollisionEnter2D(EngineApiCollision2D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionEnter2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnCollisionExit(EngineApiCollision3D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionExit = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnCollisionExit2D(EngineApiCollision2D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionExit2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnCollisionStay(EngineApiCollision3D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionStay = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnCollisionStay2D(EngineApiCollision2D collision)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnCollisionStay2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnDestroy()
        {
            throw new NotImplementedException();
        }

        public virtual void OnDisable()
        {
            throw new NotImplementedException();
        }

        public virtual void OnEnable()
        {
            throw new NotImplementedException();
        }

        public virtual void OnRenderObject()
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerEnter(EngineApiCollider3D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerEnter = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnTriggerEnter2D(EngineApiCollider2D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerEnter2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnTriggerExit(EngineApiCollider3D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerExit = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnTriggerExit2D(EngineApiCollider2D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerExit2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnTriggerStay(EngineApiCollider3D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerStay = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void OnTriggerStay2D(EngineApiCollider2D other)
        {
            if (enabled && PhysicAgent != null)
            {
                PhysicAgent.OnTriggerStay2D = (Godot.Node x) =>
                {

                };
            }
        }

        public virtual void Reset()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region component functions



        public EngineApiObjectBehaviour AddComponent(Type componentType)
        {
            throw new NotImplementedException();
        }

        public T AddComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponent(string type)
        {
            throw new NotImplementedException();
        }

        public T GetComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponent(Type type)
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public T GetComponentInChildren<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponentInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour GetComponentInParent(Type type)
        {
            throw new NotImplementedException();
        }

        public T GetComponentInParent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void GetComponents<T>(List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour[] GetComponents(Type type)
        {
            throw new NotImplementedException();
        }

        public T[] GetComponents<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void GetComponents(Type type, List<EngineApiObjectBehaviour> results)
        {
            throw new NotImplementedException();
        }

        public void GetComponentsInChildren<T>(List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public T[] GetComponentsInChildren<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour[] GetComponentsInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public T[] GetComponentsInParent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public T[] GetComponentsInParent<T>(bool includeInactive) where T : class
        {
            throw new NotImplementedException();
        }

        public EngineApiObjectBehaviour[] GetComponentsInParent(Type type)
        {
            throw new NotImplementedException();
        }

        public T GetOrAddComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public bool TryGetComponent(Type type, out EngineApiObjectBehaviour component)
        {
            throw new NotImplementedException();
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            throw new NotImplementedException();
        }

        #endregion

        public void SetActive(bool value)
        {
            this.enabled = value;
        }

        public void DestroyImmediate(object obj = null)
        {
            Destroy(obj);
        }

        public void Destroy(object obj = null)
        {
            childComponents.ForEach(x => x.OnDestroy());
            OnDestroy();
            QueueFree();
        }

        public static void DontDestroyOnLoad(EngineApiObjectBehaviour obj)
        {
            
        }
    }

#elif NET
        : StaticEngineApiObjectBehaviour, IEngineApiObjectBehaviour, IEngineApiCallableMethods //: ENGINEGAMEOBJECT, IEngineApiObjectBehaviour, IEngineApiCallableMethods
    {
        public EngineApiObjectBehaviour()
        {
        }

        public EngineApiObjectBehaviour(string name)
        {
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.GameObject gameObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
#elif NET
        public EngineApiObjectBehaviour gameObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
#endif
        public bool enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool activeInHierarchy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual EngineApiObjectBehaviour AddComponent(Type componentType)
        {
            throw new NotImplementedException();
        }

        public virtual T AddComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void Awake()
        {
            throw new NotImplementedException();
        }

        public virtual void Destroy(object obj)
        {
            
        }

        public virtual void DestroyImmediate(object obj)
        {
            throw new NotImplementedException();
        }

        public virtual void FixedUpdate()
        {
            throw new NotImplementedException();
        }

        public virtual void FixedUpdate(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponent(string type)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponent(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponentInChildren<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponentInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour GetComponentInParent(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponentInParent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponents<T>(List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour[] GetComponents(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponents<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponents(Type type, List<EngineApiObjectBehaviour> results)
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInChildren<T>(List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInChildren<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInChildren<T>(bool includeInactive, List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour[] GetComponentsInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInParent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInParent<T>(bool includeInactive, List<T> results) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInParent<T>(bool includeInactive) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiObjectBehaviour[] GetComponentsInParent(Type type)
        {
            throw new NotImplementedException();
        }

        public T GetOrAddComponent<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void LateUpdate(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual void LateUpdate()
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationFocus(bool focus)
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationPause(bool pause)
        {
            throw new NotImplementedException();
        }

        public virtual void OnApplicationQuit()
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionEnter(EngineApiCollision3D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionEnter2D(EngineApiCollision2D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionExit(EngineApiCollision3D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionExit2D(EngineApiCollision2D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionStay(EngineApiCollision3D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnCollisionStay2D(EngineApiCollision2D collision)
        {
            throw new NotImplementedException();
        }

        public virtual void OnDestroy()
        {
            throw new NotImplementedException();
        }

        public virtual void OnDisable()
        {
            throw new NotImplementedException();
        }

        public virtual void OnEnable()
        {
            throw new NotImplementedException();
        }

        public virtual void OnRenderObject()
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerEnter(EngineApiCollider3D other)
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerEnter2D(EngineApiCollider2D other)
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerExit(EngineApiCollider3D other)
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerExit2D(EngineApiCollider2D other)
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerStay(EngineApiCollider3D other)
        {
            throw new NotImplementedException();
        }

        public virtual void OnTriggerStay2D(EngineApiCollider2D other)
        {
            throw new NotImplementedException();
        }

        public virtual void Reset()
        {
            throw new NotImplementedException();
        }

        public virtual void SetActive(bool value)
        {
            throw new NotImplementedException();
        }

        public virtual void Start()
        {
            throw new NotImplementedException();
        }

#if UNITY_5_3_OR_NEWER

        public UnityEngine.Coroutine StartCoroutine(string methodName)
        {
            throw new NotImplementedException();
        }

        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            throw new NotImplementedException();
        }

        public UnityEngine.Coroutine StartCoroutine_Auto(IEnumerator routine)
        {
            throw new NotImplementedException();
        }

        public void StopAllCoroutines()
        {
            throw new NotImplementedException();
        }

        public void StopCoroutine(IEnumerator routine)
        {
            throw new NotImplementedException();
        }

        public void StopCoroutine(UnityEngine.Coroutine routine)
        {
            throw new NotImplementedException();
        }

        public void StopCoroutine(string methodName)
        {
            throw new NotImplementedException();
        }
#endif

        public virtual bool TryGetComponent(Type type, out EngineApiObjectBehaviour component)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetComponent<T>(out T component) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void Update(double delta)
        {
            throw new NotImplementedException();
        }

        public virtual void Update()
        {
            throw new NotImplementedException();
        }

        
    }
#endif
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class StaticEngineApiObjectBehaviour : EngineApiCompoent
    {
        public static void Destroy(object obj)
        {
            //Destroy(obj.GetType());
        }

        public static void DontDestroyOnLoad(object obj)
        {

        }
    }
}
