using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.Extensions;

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
#if GODOT
        : Godot.Node, IEngineApiObjectBehaviour, IEngineApiCallableMethods
    {
        public EngineApiObjectBehaviour gameObject { get => this; set => throw new NotImplementedException(); }
        [Godot.Export]
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
        public EngineApiObjectBehaviour ComponentsOwner = null;
        private bool isEnabled = true;
        public bool activeInHierarchy { get => enabled; set => enabled = value; }
        public GodotPhysicAgent PhysicAgent;
        private bool isInit = false;
        private bool enterToInit = false;
        private Godot.Node componentsStorage = null;

        //public EngineApiObjectBehaviour OwnerEAOB = null;

        public EngineApiObjectBehaviour InitEAOB(string name = null)
        {
            //bool enterToInit = false;
            if(enterToInit)
            return this;
            lock (this)
            {
                if (!isInit && !enterToInit)
                {
                    enterToInit = true;
                }
            }
            if (enterToInit)
            {
                if (name != null)
                this.Name = name;
                componentsStorage = new Godot.Node();
                componentsStorage.Name = "ComponentsStorage";
                lock (GodotRootStorage.TreeLocker)
                {
                    this.AddChild(componentsStorage);
                }
                this.CallDeferred("DeferredInit", name);
            }
            return this;
        }

        private void DeferredInit(string name)
        {
            isInit = true;
            if(enabled)
            {
                this.Start();
                this.isEnabled = false;
                this.enabled = true;
            }
        }

        protected SynchronizedList<EngineApiObjectBehaviour> childComponents = new SynchronizedList<EngineApiObjectBehaviour>();

        #region GodotBase

        public override void _EnterTree()//non usable because running before full init of childs
        {
            InitEAOB();
            if (enabled)
                base._EnterTree();
        }


        public override void _Ready() // awake
        {
            InitEAOB();
            if (enabled)
            {
                base._Ready();
                this.Awake();
            }

        }


        public override void _ExitTree()
        {
            if (enabled && isInit)
            {
                base._ExitTree();
                this.OnDisable();
            }
        }

#if GODOT4_0_OR_GREATER
        public override void _Process(double delta)
        {
            // Called every frame.
            if (enabled && isInit)
            {
                base._Process(delta);
                this.Update();
                this.Update(delta);
                this.Update(Convert.ToDouble(delta));
                this.LateUpdate();
                this.LateUpdate(delta);
                this.LateUpdate(Convert.ToDouble(delta));
            }

        }
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        public override void _Process(float delta)
        {
            // Called every frame.
            if (enabled && isInit)
            {
                base._Process(delta);
                this.Update();
                this.Update(delta);
                this.Update(Convert.ToDouble(delta));
                this.LateUpdate();
                this.LateUpdate(delta);
                this.LateUpdate(Convert.ToDouble(delta));
            }

        }
#endif
#if GODOT4_0_OR_GREATER
        public override void _PhysicsProcess(double delta)
        {
            if (enabled && isInit)
            {
                base._PhysicsProcess(delta);
                this.FixedUpdate();
                this.FixedUpdate(delta);
                this.FixedUpdate(Convert.ToDouble(delta));
            }
        }
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        public override void _PhysicsProcess(float delta)
        {
            if (enabled && isInit)
            {
                base._PhysicsProcess(delta);
                this.FixedUpdate();
                this.FixedUpdate(delta);
                this.FixedUpdate(Convert.ToDouble(delta));
            }
        }
#endif

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

        }

        public virtual void Start()
        {

        }

        public virtual void FixedUpdate(float delta)
        {
            
        }

        public virtual void Update(float delta)
        {
            
        }

        public virtual void LateUpdate(float delta)
        {
            
        }

        public virtual void Update(double delta)
        {

        }

        public virtual void Update()
        {

        }

        public virtual void FixedUpdate()
        {

        }

        public virtual void FixedUpdate(double delta)
        {

        }

        public virtual void LateUpdate()
        {

        }

        public virtual void LateUpdate(double delta)
        {

        }

        public virtual void OnApplicationFocus(bool focus)
        {

        }

        public virtual void OnApplicationPause(bool pause)
        {

        }

        public virtual void OnApplicationQuit()
        {

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

        }

        public virtual void OnDisable()
        {

        }

        public virtual void OnEnable()
        {

        }

        public virtual void OnRenderObject()
        {

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

        }
        #endregion

        #region component functions



        public EngineApiObjectBehaviour AddComponent(Type componentType)
        {
            var newComponent = (EngineApiObjectBehaviour)Activator.CreateInstance(componentType);
            childComponents.Add(newComponent);
            newComponent.Name = componentType.Name;
            newComponent.ComponentsOwner = this;
            lock (GodotRootStorage.TreeLocker)
            {
                this.componentsStorage?.AddChild(newComponent);
            }
            return newComponent;
        }

        public T AddComponent<T>() where T : class
        {
            return this.AddComponent(typeof(T)) as T;
        }

        public EngineApiObjectBehaviour GetComponent(string type)
        {
            return null;
        }

        public T GetComponent<T>() where T : class
        {
            return this.GetComponent(typeof(T)) as T;
        }

        public EngineApiObjectBehaviour GetComponent(Type type)
        {
            EngineApiObjectBehaviour findedComponent = null;
            childComponents.ForEach(x => { if (findedComponent == null && x.GetType() == type) findedComponent = x; });
            return findedComponent;
        }

        public EngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive)
        {
            var result = new List<EngineApiObjectBehaviour>();
            childComponents.ForEach(x =>
            {
                if (result.Count > 0)
                {
                    return;
                }
                var childresult = x.GetComponent(type);
                if (childresult != null)
                {
                    result.Add(childresult);
                }
                else
                {
                    childresult = x.GetComponentInChildren(type, includeInactive);
                    if (childresult != null)
                    {
                        result.Add(childresult);
                    }
                }
            });
            if (result.Count > 0)
            {
                return result[0];
            }
            return null;
        }

        public T GetComponentInChildren<T>() where T : class
        {
            return this.GetComponentInChildren(typeof(T)) as T;
        }

        public EngineApiObjectBehaviour GetComponentInChildren(Type type)
        {
            return this.GetComponentInChildren(type, true);
        }

        public EngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive)
        {
            EngineApiObjectBehaviour component = null;
            Godot.Node cached = this;
            while (component == null)
            {
                lock (GodotRootStorage.TreeLocker)
                {
                    var parent = cached.GetParent();
                    if (parent != null)
                    {
                        if (parent is EngineApiObjectBehaviour)
                        {
                            var parentEAOB = parent as EngineApiObjectBehaviour;
                            component = parentEAOB.GetComponent(type);
                        }
                        else
                        {
                            cached = parent;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return component;
        }

        public EngineApiObjectBehaviour GetComponentInParent(Type type)
        {
            return GetComponentInParent(type, true);
        }

        public T GetComponentInParent<T>() where T : class
        {
            return GetComponentInParent(typeof(T)) as T;
        }

        public void GetComponents<T>(List<T> results) where T : class
        {
            (GetComponents(typeof(T)) as T[]).ForEach(x => results.Add(x));
        }

        public EngineApiObjectBehaviour[] GetComponents(Type type)
        {
            List<EngineApiObjectBehaviour> findedComponent = new List<EngineApiObjectBehaviour>();
            childComponents.ForEach(x => { if (x.GetType() == type) findedComponent.Add(x); });
            return findedComponent.ToArray();
        }

        public T[] GetComponents<T>() where T : class
        {
            return this.GetComponents(typeof(T)) as T[];
        }

        public void GetComponents(Type type, List<EngineApiObjectBehaviour> results)
        {
            (GetComponents(type)).ForEach(x => results.Add(x));
        }

        public void GetComponentsInChildren<T>(List<T> results) where T : class
        {
            (GetComponentsInChildren(typeof(T)) as T[]).ForEach(x => results.Add(x));
        }

        public T[] GetComponentsInChildren<T>() where T : class
        {
            return this.GetComponentsInChildren(typeof(T)) as T[];
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results) where T : class
        {
            (GetComponentsInChildren(typeof(T)) as T[]).ForEach(x => results.Add(x));
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            return this.GetComponentsInChildren(typeof(T)) as T[];
        }

        public EngineApiObjectBehaviour[] GetComponentsInChildren(Type type)
        {
            var result = new List<EngineApiObjectBehaviour>();
            childComponents.ForEach(x =>
            {
                x.GetComponents(type).ForEach(y => result.Add(y));
                x.GetComponentsInChildren(type).ForEach(y => result.Add(y));
            });
            return null;
        }

        public List<EngineApiObjectBehaviour> GetComponentsInParent(Type type, bool includeInactive)
        {
            var components = new List<EngineApiObjectBehaviour>();
            Godot.Node cached = this;
            while (true)
            {
                lock (GodotRootStorage.TreeLocker)
                {
                    var parent = cached.GetParent();
                    if (parent != null)
                    {
                        if (parent is EngineApiObjectBehaviour)
                        {
                            var parentEAOB = parent as EngineApiObjectBehaviour;
                            var component = parentEAOB.GetComponent(type);
                            if(component != null)
                            {
                                components.Add(component);
                            }
                        }
                        else
                        {
                            cached = parent;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return components;
        }

        public T[] GetComponentsInParent<T>() where T : class
        {
            return GetComponentsInParent(typeof(T), true).ToArray() as T[];
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results) where T : class
        {
            (GetComponentsInParent(typeof(T))).ForEach(x => results.Add(x as T));
        }

        public T[] GetComponentsInParent<T>(bool includeInactive) where T : class
        {
            return GetComponentsInParent(typeof(T), includeInactive).ToArray() as T[];
        }

        public EngineApiObjectBehaviour[] GetComponentsInParent(Type type)
        {
            return GetComponentsInParent(type).ToArray();
        }

        public T GetOrAddComponent<T>() where T : class
        {
            var comp = this.GetComponent<T>();
            if(comp == null)
            {
                comp = this.AddComponent<T>();
            }
            return comp;
        }

        public bool TryGetComponent(Type type, out EngineApiObjectBehaviour component)
        {
            var comp = this.GetComponent(type);
            if(comp != null)
            {
                component = comp;
                return true;
            }
            component = null;
            return false;
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            var comp = this.GetComponent<T>();
            if(comp != null)
            {
                component = comp;
                return true;
            }
            component = null;
            return false;
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
