using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public partial class EngineApiObjectBehaviour : StaticEngineApiObjectBehaviour, IEngineApiObjectBehaviour, IEngineApiCallableMethods //: ENGINEGAMEOBJECT, IEngineApiObjectBehaviour, IEngineApiCallableMethods
    {
        public EngineApiObjectBehaviour()
        {
        }

        public EngineApiObjectBehaviour(string name)
        {
        }

        public IEngineApiObjectBehaviour gameObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual IEngineApiObjectBehaviour AddComponent(Type componentType)
        {
            throw new NotImplementedException();
        }

        public virtual T AddComponent<T>() where T : IEngineApiObjectBehaviour
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

        public virtual IEngineApiObjectBehaviour GetComponent(string type)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponent<T>()
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour GetComponent(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponentInChildren<T>()
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour GetComponentInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour GetComponentInParent(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponentInParent<T>()
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponents<T>(List<T> results)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour[] GetComponents(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponents<T>()
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponents(Type type, List<IEngineApiObjectBehaviour> results)
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInChildren<T>(List<T> results)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInChildren<T>()
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour[] GetComponentsInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInParent<T>()
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponentsInParent<T>(bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual IEngineApiObjectBehaviour[] GetComponentsInParent(Type type)
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

        public virtual bool TryGetComponent(Type type, out IEngineApiObjectBehaviour component)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetComponent<T>(out T component)
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

    public class StaticEngineApiObjectBehaviour
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
