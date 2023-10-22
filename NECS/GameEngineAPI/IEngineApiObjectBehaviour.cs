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
        T GetComponent<T>() where T : IEngineApiObjectBehaviour;
        IEngineApiObjectBehaviour GetComponent(Type type);
        IEngineApiObjectBehaviour GetComponentInChildren(Type type, bool includeInactive);
        T GetComponentInChildren<T>()where T : IEngineApiObjectBehaviour;
        IEngineApiObjectBehaviour GetComponentInChildren(Type type);
        IEngineApiObjectBehaviour GetComponentInParent(Type type, bool includeInactive);
        IEngineApiObjectBehaviour GetComponentInParent(Type type);
        T GetComponentInParent<T>() where T : IEngineApiObjectBehaviour;
        void GetComponents<T>(List<T> results)where T : IEngineApiObjectBehaviour;
        IEngineApiObjectBehaviour[] GetComponents(Type type);
        T[] GetComponents<T>() where T : IEngineApiObjectBehaviour;
        void GetComponents(Type type, List<IEngineApiObjectBehaviour> results);
        void GetComponentsInChildren<T>(List<T> results)where T : IEngineApiObjectBehaviour;
        T[] GetComponentsInChildren<T>()where T : IEngineApiObjectBehaviour;
        void GetComponentsInChildren<T>(bool includeInactive, List<T> results)where T : IEngineApiObjectBehaviour;
        T[] GetComponentsInChildren<T>(bool includeInactive)where T : IEngineApiObjectBehaviour;
        IEngineApiObjectBehaviour[] GetComponentsInChildren(Type type);
        T[] GetComponentsInParent<T>()where T : IEngineApiObjectBehaviour;
        void GetComponentsInParent<T>(bool includeInactive, List<T> results)where T : IEngineApiObjectBehaviour;
        T[] GetComponentsInParent<T>(bool includeInactive)where T : IEngineApiObjectBehaviour;
        IEngineApiObjectBehaviour[] GetComponentsInParent(Type type);
        void SetActive(bool value);
        bool TryGetComponent(Type type, out IEngineApiObjectBehaviour component);
        bool TryGetComponent<T>(out T component)where T : IEngineApiObjectBehaviour;
        void Destroy(Object obj);
        void DestroyImmediate(Object obj);
    }
}