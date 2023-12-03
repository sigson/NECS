using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public partial class EngineApiObjectBehaviour
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
#if NET
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

        public virtual EngineApiCompoent AddComponent(Type componentType)
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

        public virtual EngineApiCompoent GetComponent(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiCompoent GetComponentInChildren(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual T GetComponentInChildren<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiCompoent GetComponentInChildren(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiCompoent GetComponentInParent(Type type, bool includeInactive)
        {
            throw new NotImplementedException();
        }

        public virtual EngineApiCompoent GetComponentInParent(Type type)
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

        public virtual EngineApiCompoent[] GetComponents(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual T[] GetComponents<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public virtual void GetComponents(Type type, List<EngineApiCompoent> results)
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

        public virtual EngineApiCompoent[] GetComponentsInChildren(Type type)
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

        public virtual EngineApiCompoent[] GetComponentsInParent(Type type)
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

        public virtual bool TryGetComponent(Type type, out EngineApiCompoent component)
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
    public class StaticEngineApiObjectBehaviour : EngineApiCompoent
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
