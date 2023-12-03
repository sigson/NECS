using NECS.GameEngineAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;

namespace NECS
{
    public interface IEngineApiObjectBehaviour
    {
#if UNITY_5_3_OR_NEWER
        UnityEngine.GameObject gameObject { get; set; }
#elif NET
        EngineApiObjectBehaviour gameObject { get; set; }
#endif
        bool enabled { get; set; }
        EngineApiCompoent AddComponent(Type componentType);
        T AddComponent<T>() where T : class;

        EngineApiObjectBehaviour GetComponent(string type);
        T GetComponent<T>() where T : class;
        T GetOrAddComponent<T>() where T : class;
        EngineApiCompoent GetComponent(Type type);
        EngineApiCompoent GetComponentInChildren(Type type, bool includeInactive);
        T GetComponentInChildren<T>()where T : class;
        EngineApiCompoent GetComponentInChildren(Type type);
        EngineApiCompoent GetComponentInParent(Type type, bool includeInactive);
        EngineApiCompoent GetComponentInParent(Type type);
        T GetComponentInParent<T>() where T : class;
        void GetComponents<T>(List<T> results)where T : class;
        EngineApiCompoent[] GetComponents(Type type);
        T[] GetComponents<T>() where T : class;
        void GetComponents(Type type, List<EngineApiCompoent> results);
        void GetComponentsInChildren<T>(List<T> results)where T : class;
        T[] GetComponentsInChildren<T>()where T : class;
        void GetComponentsInChildren<T>(bool includeInactive, List<T> results)where T : class;
        T[] GetComponentsInChildren<T>(bool includeInactive)where T : class;
        EngineApiCompoent[] GetComponentsInChildren(Type type);
        T[] GetComponentsInParent<T>()where T : class;
        void GetComponentsInParent<T>(bool includeInactive, List<T> results)where T : class;
        T[] GetComponentsInParent<T>(bool includeInactive)where T : class;
        EngineApiCompoent[] GetComponentsInParent(Type type);
        void SetActive(bool value);
        bool TryGetComponent(Type type, out EngineApiCompoent component);
        bool TryGetComponent<T>(out T component)where T : class;
        void Destroy(Object obj);
        void DestroyImmediate(Object obj);
        bool activeInHierarchy { get; set; }

#if UNITY_5_3_OR_NEWER
        UnityEngine.Coroutine StartCoroutine(string methodName);
        UnityEngine.Coroutine StartCoroutine(IEnumerator routine);

        UnityEngine.Coroutine StartCoroutine_Auto(IEnumerator routine);

        public void StopAllCoroutines();
        public void StopCoroutine(IEnumerator routine);
        public void StopCoroutine(UnityEngine.Coroutine routine);
        public void StopCoroutine(string methodName);
#endif
    }
}