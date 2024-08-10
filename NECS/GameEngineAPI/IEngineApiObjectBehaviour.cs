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
        EngineApiObjectBehaviour AddComponent(Type componentType);
        T AddComponent<T>() where T : class;

        EngineApiObjectBehaviour GetComponent(string type);
        T GetComponent<T>() where T : class;
        T GetOrAddComponent<T>() where T : class;
        EngineApiObjectBehaviour GetComponent(Type type);
        EngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive);
        T GetComponentInChildren<T>()where T : class;
        EngineApiObjectBehaviour GetComponentInChildren(Type type);
        EngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive);
        EngineApiObjectBehaviour GetComponentInParent(Type type);
        T GetComponentInParent<T>() where T : class;
        void GetComponents<T>(List<T> results)where T : class;
        EngineApiObjectBehaviour[] GetComponents(Type type);
        T[] GetComponents<T>() where T : class;
        void GetComponents(Type type, List<EngineApiObjectBehaviour> results);
        void GetComponentsInChildren<T>(List<T> results)where T : class;
        T[] GetComponentsInChildren<T>()where T : class;
        void GetComponentsInChildren<T>(bool includeInactive, List<T> results)where T : class;
        T[] GetComponentsInChildren<T>(bool includeInactive)where T : class;
        EngineApiObjectBehaviour[] GetComponentsInChildren(Type type);
        T[] GetComponentsInParent<T>()where T : class;
        void GetComponentsInParent<T>(bool includeInactive, List<T> results)where T : class;
        T[] GetComponentsInParent<T>(bool includeInactive)where T : class;
        EngineApiObjectBehaviour[] GetComponentsInParent(Type type);
        void SetActive(bool value);
        bool TryGetComponent(Type type, out EngineApiObjectBehaviour component);
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