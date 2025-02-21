﻿
using NECS.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(2)]//base type of entity
    public class ECSEntity : IECSObject, ICloneable
    {
        static new public long Id { get; set; } = 2;
        public string AliasName = "";
        [System.NonSerialized]
        public List<Type> TemplateAccessor = new List<Type>();

        [System.NonSerialized]
        public ECSEntityManager manager;
        [System.NonSerialized]
        public ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        [System.NonSerialized]
        public object contextSwitchLocker = new object();
        [ServerOnlyData]
        [System.NonSerialized]
        public ConcurrentDictionary<long, ECSEntityGroup> entityGroups;
        [System.NonSerialized]
        public EntityComponentStorage entityComponents;
        
        public Dictionary<long, int> fastEntityComponentsId;//todo: concurrent replace to normal
        [System.NonSerialized]
        public SynchronizedList<GroupDataAccessPolicy> dataAccessPolicies;
        [System.NonSerialized]
        public string Name;
        [System.NonSerialized]
        public string serializedEntity;
        [System.NonSerialized]
        public byte[] binSerializedEntity;
        [System.NonSerialized]
        public bool emptySerialized = true;
        [Newtonsoft.Json.JsonIgnore]
        public List<string> ConfigPath { get; }
        [System.NonSerialized]
        public bool Alive = false;

        public ECSEntity() {
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new ConcurrentDictionary<long, ECSEntityGroup>();
        }

        public ECSEntity(long instanceid)
        {
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new ConcurrentDictionary<long, ECSEntityGroup>();
            this.instanceId = instanceid;
        }

        public ECSEntity(EntityTemplate userTemplate, ECSComponent[] eCSComponents)
        {
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new ConcurrentDictionary<long, ECSEntityGroup>();
            foreach (var component in eCSComponents)
            {
                this.AddComponentSilent(component);
            }
            userTemplate.SetupEntity(this);
            this.TemplateAccessor.Add(userTemplate.GetType());
        }
        public void AddComponent<T>() where T : ECSComponent, new()
        {
            this.AddComponent(typeof(T));
        }

        public void AddComponent(ECSComponent component)
        {
            this.AddComponentImpl(component, true);
        }

        public bool TryAddComponent(ECSComponent component)
        {
            try
            {
                if (!this.HasComponent(component.GetId()))
                {
                    this.AddComponentImpl(component, true);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public void AddComponents(IEnumerable<ECSComponent> components)
        {
            foreach(var component in components)
            {
                this.AddComponentImpl(component, false);
            }
            entityComponents.RegisterAllComponents(false);
        }

        public void AddComponentsSilent(IEnumerable<ECSComponent> components)
        {
            foreach (var component in components)
            {
                this.AddComponentSilent(component);
            }
        }

        public void AddComponent(Type componentType)
        {
            ECSComponent component = this.CreateNewComponentInstance(componentType);
            this.AddComponent(component);
        }

        private void AddComponentImpl(ECSComponent component, bool sendEvent)
        {
            Type componentClass = component.GetTypeFast();
            if (!this.entityComponents.HasComponent(componentClass))//|| !this.IsSkipExceptionOnAddRemove(componentClass)
            {
                this.entityComponents.AddComponentImmediately(component.GetTypeFast(), component, false, !sendEvent);
                
                if (sendEvent)
                {
                    this.manager.OnAddComponent(this, component);
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public void AddOrChangeComponent(ECSComponent component)
        {
            this.AddOrChangeComponentImpl(component, true);
        }

        public void AddOrChangeComponentWithOwnerRestoring(ECSComponent component)
        {
            this.AddOrChangeComponentImpl(component, true, true);
        }

        public void AddOrChangeComponentSilentWithOwnerRestoring(ECSComponent component)
        {
            this.AddOrChangeComponentImpl(component, false, true);
        }

        public void AddOrChangeComponentSilent(ECSComponent component)
        {
            this.AddOrChangeComponentImpl(component, false);
        }
        private void AddOrChangeComponentImpl(ECSComponent component, bool sendEvent, bool restoringOwner = false)
        {
            Type componentClass = component.GetTypeFast();
            if (!this.entityComponents.HasComponent(componentClass))
            {
                this.entityComponents.AddComponentImmediately(component.GetTypeFast(), component, false, !sendEvent);
                
                if (sendEvent)
                {
                    this.manager.OnAddComponent(this, component);
                }
            }
            else
            {
                if (restoringOwner)
				{
					if (component is DBComponent)
                        this.GetComponent<DBComponent>(component.GetId()).serializedDB = (component as DBComponent).serializedDB;
                    else
                        this.entityComponents.ChangeComponent(component, false, this);
				}
                else
                    this.entityComponents.ChangeComponent(component);
            }
        }

        public T AddComponentAndGetInstance<T>() where T : ECSComponent, new()
        {
            ECSComponent component = this.CreateNewComponentInstance(typeof(T));
            this.AddComponent(component);
            return (T) component;
        }

        public void AddComponentIfAbsent<T>() where T : ECSComponent, new()
        {
            if (!this.HasComponent<T>())
            {
                this.AddComponent(typeof(T));
            }
        }

        public void ExclusiveLockedOperation(Action operation) => this.entityComponents.ExclusiveLockedOperation(operation);

        public void ReadLockedOperation(Action operation) => this.entityComponents.ReadLockedOperation(operation);

        public void AddComponentSilent(ECSComponent component)
        {
            this.AddComponentImpl(component, false);
        }

        private int calcHashCode() =>
            this.GetHashCode();

        public void ChangeComponent(ECSComponent component)
        {
            bool flag = this.HasComponent(component.GetTypeFast()) && this.GetComponent(component.GetTypeFast()).Equals(component);
            this.entityComponents.ChangeComponent(component);
        }
        public void ChangeComponentSilent(ECSComponent component)//for fast components, who not must autoupdate, because we broadcast his event from user to other users, like moving or shooting
        {
            bool flag = this.HasComponent(component.GetTypeFast()) && this.GetComponent(component.GetTypeFast()).Equals(component);
            this.entityComponents.ChangeComponent(component, true);
        }

        public void HasComponentLockedAction<T>(Action action) where T : ECSComponent
        {
            HasComponentLockedAction(typeof(T), action);
        }

        public void HasComponentLockedAction(Type type, Action action)
        {
            using (entityComponents.StabilizationLocker.ReadLock())
            {
                if( this.HasComponent(type))
                {
                    action();
                }
            }
        }

        public int CompareTo(ECSEntity other) =>
            (int)(this.instanceId - other.instanceId);

        public ECSComponent CreateNewComponentInstance(Type componentType)
        {
            return Activator.CreateInstance(componentType) as ECSComponent;
        }

        protected bool Equals(ECSEntity other) =>
            this.GetId() == other.GetId();

        public override bool Equals(object obj)
        {
            return (!ReferenceEquals(null, obj) ? (!ReferenceEquals(this, obj) ? (ReferenceEquals(obj.GetType(), base.GetType()) ? this.Equals((ECSEntity)obj) : false) : true) : false);
        }

        public T GetComponent<T>() where T : ECSComponent =>
            (T)this.GetComponent(typeof(T));

		public ECSComponent[] GetComponents(params long[] componentTypeId)
        {
            List<ECSComponent> returnComponents = new List<ECSComponent>();
            foreach(var compId in componentTypeId)
            {
                try { returnComponents.Add(this.entityComponents.GetComponent(compId)); } catch { }
            }
            return returnComponents.Where(x => x != null).ToArray();
        }

        public T TryGetComponent<T>() where T : ECSComponent
        {
            try { return (T)this.GetComponent(typeof(T)); } catch { return null; }
        }
            

        public ECSComponent GetComponent(Type componentType) =>
            this.entityComponents.GetComponent(componentType);

        public ECSComponent GetComponent(long componentTypeId) =>
            this.entityComponents.GetComponent(componentTypeId);

        public T GetComponent<T>(long componentTypeId) where T : ECSComponent =>
            (T)this.entityComponents.GetComponent(componentTypeId);

        public ECSComponent GetComponentUnsafe(Type componentType) =>
            this.entityComponents.GetComponentUnsafe(componentType);

        public ECSComponent GetComponentUnsafe(long componentTypeId) =>
            this.entityComponents.GetComponentUnsafe(componentTypeId);

        public bool HasComponent<T>() where T : ECSComponent =>
            this.HasComponent(typeof(T));

        public bool HasComponent(Type type) =>
            this.entityComponents.HasComponent(type);

        public bool HasComponent(long componentClassId) =>
            this.entityComponents.HasComponent(componentClassId);

        public void Init()
        {
            this.Alive = true;
        }

        public bool IsSameGroup<T>(ECSEntity otherEntity) where T : ECSComponentGroup =>
            (this.HasComponent<T>() && otherEntity.HasComponent<T>()) && this.GetComponent<T>().GetId().Equals(otherEntity.GetComponent<T>().GetId());

        public void OnDelete()
        {
            this.Alive = false;
            this.dataAccessPolicies.Clear();
            this.entityComponents.OnEntityDelete();
            this.entityGroups.Clear();
            this.fastEntityComponentsId.ClearI(this.SerialLocker);
        }

        public void RemoveComponent<T>() where T : ECSComponent
        {
            this.RemoveComponent(typeof(T));
        }

        public void RemoveComponent(Type componentType)
        {
            this.entityComponents.RemoveComponentImmediately(componentType);
        }

        public void RemoveComponentsWithGroup(ECSComponentGroup componentGroup)
        {
            this.entityComponents.RemoveComponentsWithGroup(componentGroup.GetId());
        }

        public void RemoveComponentsWithGroup(long componentGroup)
        {
            this.entityComponents.RemoveComponentsWithGroup(componentGroup);
        }

        public void RemoveComponent(long componentTypeId)
        {
            this.entityComponents.RemoveComponentImmediately(componentTypeId);
        }

        public void TryRemoveComponent(long componentTypeId)
        {
            if(this.HasComponent(componentTypeId))
            {
                try
                {
                    this.entityComponents.RemoveComponentImmediately(componentTypeId);
                }
                catch { };
            }
                
        }

        public void RemoveComponentIfPresent<T>() where T : ECSComponent
        {
            if (this.HasComponent<T>())
            {
                this.RemoveComponent(typeof(T));
            }
        }

        public override string ToString() =>
            $"{this.GetId()}({this.Name})";

        public object Clone() => MemberwiseClone();
    }
}
