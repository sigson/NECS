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
        IEngineApiObjectBehaviour gameObject { get; set; } 
        bool enabled { get; set; }
        IEngineApiObjectBehaviour AddComponent(Type componentType);
        T AddComponent<T>() where T : IEngineApiObjectBehaviour;

        IEngineApiObjectBehaviour GetComponent(string type);
        T GetComponent<T>();
        IEngineApiObjectBehaviour GetComponent(Type type);
        IEngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive);
        T GetComponentInChildren<T>();
        IEngineApiObjectBehaviour GetComponentInChildren(Type type);
        IEngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive);
        IEngineApiObjectBehaviour GetComponentInParent(Type type);
        T GetComponentInParent<T>();
        void GetComponents<T>(List<T> results);
        IEngineApiObjectBehaviour[] GetComponents(Type type);
        T[] GetComponents<T>();
        void GetComponents(Type type, List<IEngineApiObjectBehaviour> results);
        void GetComponentsInChildren<T>(List<T> results);
        T[] GetComponentsInChildren<T>();
        void GetComponentsInChildren<T>(bool includeInactive, List<T> results);
        T[] GetComponentsInChildren<T>(bool includeInactive);
        IEngineApiObjectBehaviour[] GetComponentsInChildren(Type type);
        T[] GetComponentsInParent<T>();
        void GetComponentsInParent<T>(bool includeInactive, List<T> results);
        T[] GetComponentsInParent<T>(bool includeInactive);
        IEngineApiObjectBehaviour[] GetComponentsInParent(Type type);
        void SetActive(bool value);
        bool TryGetComponent(Type type, out IEngineApiObjectBehaviour component);
        bool TryGetComponent<T>(out T component);
        void Destroy(Object obj);
        void DestroyImmediate(Object obj);
    }
}