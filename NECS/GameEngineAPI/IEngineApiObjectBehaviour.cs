using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;

namespace NECS
{
    public interface IEngineApiObjectBehaviour
    {
        public IEngineApiObjectBehaviour gameObject { get; set; } 
        public bool enabled { get; set; }
        public IEngineApiObjectBehaviour AddComponent(Type componentType);
        public T AddComponent<T>() where T : IEngineApiObjectBehaviour;

        public IEngineApiObjectBehaviour GetComponent(string type);
        public T GetComponent<T>();
        public IEngineApiObjectBehaviour GetComponent(Type type);
        public IEngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive);
        public T GetComponentInChildren<T>();
        public IEngineApiObjectBehaviour GetComponentInChildren(Type type);
        public IEngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive);
        public IEngineApiObjectBehaviour GetComponentInParent(Type type);
        public T GetComponentInParent<T>();
        public void GetComponents<T>(List<T> results);
        public IEngineApiObjectBehaviour[] GetComponents(Type type);
        public T[] GetComponents<T>();
        public void GetComponents(Type type, List<IEngineApiObjectBehaviour> results);
        public void GetComponentsInChildren<T>(List<T> results);
        public T[] GetComponentsInChildren<T>();
        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results);
        public T[] GetComponentsInChildren<T>(bool includeInactive);
        public IEngineApiObjectBehaviour[] GetComponentsInChildren(Type type);
        public T[] GetComponentsInParent<T>();
        public void GetComponentsInParent<T>(bool includeInactive, List<T> results);
        public T[] GetComponentsInParent<T>(bool includeInactive);
        public IEngineApiObjectBehaviour[] GetComponentsInParent(Type type);
        public void SetActive(bool value);
        public bool TryGetComponent(Type type, out IEngineApiObjectBehaviour component);
        public bool TryGetComponent<T>(out T component);
        public void Destroy(Object obj);
        public void DestroyImmediate(Object obj);
    }
}