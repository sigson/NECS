
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
using NECS.Harness.Services;

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
        [ServerOnlyData]
        [System.NonSerialized]
        public DictionaryWrapper<long, ECSEntityGroup> entityGroups;
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

        public ECSEntity()
        {
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new DictionaryWrapper<long, ECSEntityGroup>();
            ECSService.instance.EntityCache.TryAdd(this.instanceId,this);
        }

        public ECSEntity(long instanceid)
        {
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new DictionaryWrapper<long, ECSEntityGroup>();
            this.instanceId = instanceid;
            ECSService.instance.EntityCache.TryAdd(this.instanceId,this);
        }

        public ECSEntity(ECSWorld world, EntityTemplate userTemplate, ECSComponent[] eCSComponents)
        {
            this.ECSWorldOwner = world;
            entityComponents = new EntityComponentStorage(this);
            fastEntityComponentsId = new Dictionary<long, int>();
            dataAccessPolicies = new SynchronizedList<GroupDataAccessPolicy>();
            entityGroups = new DictionaryWrapper<long, ECSEntityGroup>();
            ECSService.instance.EntityCache.TryAdd(this.instanceId,this);
            foreach (var component in eCSComponents)
            {
                this.AddComponentSilent(component);
            }
            userTemplate.SetupEntity(this);
            this.TemplateAccessor.Add(userTemplate.GetType());
        }


        #region Locked functionas

        public void ExecuteReadLockedComponent(Type type, Action <Type, ECSComponent> action)
        {
            this.entityComponents.ExecuteReadLockedComponent(type, action);
        }

        public void ExecuteReadLockedComponent<T>(Action<Type, ECSComponent> action) where T : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T), action);
        }

        #region Generic_Extension

        public void ExecuteWriteLockedComponent<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action) where T1 : ECSComponent where T2 : ECSComponent where T3 : ECSComponent where T4 : ECSComponent where T5 : ECSComponent where T6 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t, c1) =>
            {
                ExecuteWriteLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteWriteLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteWriteLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            ExecuteWriteLockedComponent(typeof(T5), (t5, c5) =>
                            {
                                ExecuteWriteLockedComponent(typeof(T5), (t6, c6) =>
                                {
                                    action((T1)c1, (T2)c2, (T3)c3, (T4)c4, (T5)c5, (T6)c6);
                                });
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteWriteLockedComponent<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
            where T4 : ECSComponent
            where T5 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteWriteLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteWriteLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteWriteLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            ExecuteWriteLockedComponent(typeof(T5), (t5, c5) =>
                            {
                                action((T1)c1, (T2)c2, (T3)c3, (T4)c4, (T5)c5);
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteWriteLockedComponent<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
            where T4 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteWriteLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteWriteLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteWriteLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            action((T1)c1, (T2)c2, (T3)c3, (T4)c4);
                        });
                    });
                });
            });
        }

        public void ExecuteWriteLockedComponent<T1, T2, T3>(Action<T1, T2, T3> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteWriteLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteWriteLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        action((T1)c1, (T2)c2, (T3)c3);
                    });
                });
            });
        }
        
        public void ExecuteWriteLockedComponent<T1, T2>(Action<T1, T2> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteWriteLockedComponent(typeof(T2), (t2, c2) =>
                {
                    action((T1)c1, (T2)c2);
                });
            });
        }

        public void ExecuteWriteLockedComponent<T1>(Action<T1> action)
            where T1 : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T1), (t1, c1) =>
            {
                action((T1)c1);
            });
        }

        public void ExecuteReadLockedComponent<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action) where T1 : ECSComponent where T2 : ECSComponent where T3 : ECSComponent where T4 : ECSComponent where T5 : ECSComponent where T6 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteReadLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteReadLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteReadLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            ExecuteReadLockedComponent(typeof(T5), (t5, c5) =>
                            {
                                ExecuteReadLockedComponent(typeof(T5), (t6, c6) =>
                                {
                                    action((T1)c1, (T2)c2, (T3)c3, (T4)c4, (T5)c5, (T6)c6);
                                });
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteReadLockedComponent<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
            where T4 : ECSComponent
            where T5 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteReadLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteReadLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteReadLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            ExecuteReadLockedComponent(typeof(T5), (t5, c5) =>
                            {
                                action((T1)c1, (T2)c2, (T3)c3, (T4)c4, (T5)c5);
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteReadLockedComponent<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
            where T4 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteReadLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteReadLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        ExecuteReadLockedComponent(typeof(T4), (t4, c4) =>
                        {
                            action((T1)c1, (T2)c2, (T3)c3, (T4)c4);
                        });
                    });
                });
            });
        }

        public void ExecuteReadLockedComponent<T1, T2, T3>(Action<T1, T2, T3> action)
            where T1 : ECSComponent
            where T2 : ECSComponent
            where T3 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteReadLockedComponent(typeof(T2), (t2, c2) =>
                {
                    ExecuteReadLockedComponent(typeof(T3), (t3, c3) =>
                    {
                        action((T1)c1, (T2)c2, (T3)c3);
                    });
                });
            });
        }
        
        public void ExecuteReadLockedComponent<T1, T2>(Action<T1, T2> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                ExecuteReadLockedComponent(typeof(T2), (t2, c2) =>
                {
                    action((T1)c1, (T2)c2);
                });
            });
        }

        public void ExecuteReadLockedComponent<T1>(Action<T1> action)
            where T1 : ECSComponent
        {
            ExecuteReadLockedComponent(typeof(T1), (t1, c1) =>
            {
                action((T1)c1);
            });
        }

        public void ExecuteHoldComponent<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent 
            where T3 : ECSComponent 
            where T4 : ECSComponent 
            where T5 : ECSComponent 
            where T6 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                this.entityComponents.ExecuteOnNotHasComponent(typeof(T2), () =>
                {
                    this.entityComponents.ExecuteOnNotHasComponent(typeof(T3), () =>
                    {
                        this.entityComponents.ExecuteOnNotHasComponent(typeof(T4), () =>
                        {
                            this.entityComponents.ExecuteOnNotHasComponent(typeof(T5), () =>
                            {
                                this.entityComponents.ExecuteOnNotHasComponent(typeof(T6), () =>
                                {
                                    action(default(T1), default(T2), default(T3), default(T4), default(T5), default(T6));
                                });
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteHoldComponent<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent 
            where T3 : ECSComponent 
            where T4 : ECSComponent 
            where T5 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                this.entityComponents.ExecuteOnNotHasComponent(typeof(T2), () =>
                {
                    this.entityComponents.ExecuteOnNotHasComponent(typeof(T3), () =>
                    {
                        this.entityComponents.ExecuteOnNotHasComponent(typeof(T4), () =>
                        {
                            this.entityComponents.ExecuteOnNotHasComponent(typeof(T5), () =>
                            {
                                action(default(T1), default(T2), default(T3), default(T4), default(T5));
                            });
                        });
                    });
                });
            });
        }

        public void ExecuteHoldComponent<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent 
            where T3 : ECSComponent 
            where T4 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                this.entityComponents.ExecuteOnNotHasComponent(typeof(T2), () =>
                {
                    this.entityComponents.ExecuteOnNotHasComponent(typeof(T3), () =>
                    {
                        this.entityComponents.ExecuteOnNotHasComponent(typeof(T4), () =>
                        {
                            action(default(T1), default(T2), default(T3), default(T4));
                        });
                    });
                });
            });
        }

        public void ExecuteHoldComponent<T1, T2, T3>(Action<T1, T2, T3> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent 
            where T3 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                this.entityComponents.ExecuteOnNotHasComponent(typeof(T2), () =>
                {
                    this.entityComponents.ExecuteOnNotHasComponent(typeof(T3), () =>
                    {
                        action(default(T1), default(T2), default(T3));
                    });
                });
            });
        }

        public void ExecuteHoldComponent<T1, T2>(Action<T1, T2> action) 
            where T1 : ECSComponent 
            where T2 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                this.entityComponents.ExecuteOnNotHasComponent(typeof(T2), () =>
                {
                    action(default(T1), default(T2));
                });
            });
        }

        public void ExecuteHoldComponent<T1>(Action<T1> action)
            where T1 : ECSComponent
        {
            this.entityComponents.ExecuteOnNotHasComponent(typeof(T1), () =>
            {
                action(default(T1));
            });
        }
        
        #endregion

        public void ExecuteWriteLockedComponent(Type type, Action <Type, ECSComponent> action)
        {
            this.entityComponents.ExecuteWriteLockedComponent(type, action);
        }

        public void ExecuteWriteLockedComponent<T>(Action<Type, ECSComponent> action) where T : ECSComponent
        {
            ExecuteWriteLockedComponent(typeof(T), action);
        }
        #endregion


        #region BasedRealizarion

        private void AddComponentImpl(ECSComponent component, bool sendEvent)
        {
            this.entityComponents.AddComponentImmediately(component.GetTypeFast(), component, false, !sendEvent);
        }

        private void AddOrChangeComponentImpl(ECSComponent component, bool sendEvent, bool restoringOwner = false)
        {
            this.entityComponents.AddOrChangeComponentImmediately(component.GetTypeFast(), component,restoringOwner, !sendEvent);
        }

        public ECSComponent[] GetComponents(params long[] componentTypeId)
        {
            List<ECSComponent> returnComponents = new List<ECSComponent>();
            foreach(var compId in componentTypeId)
            {
                try { returnComponents.Add(this.entityComponents.GetComponent(compId)); } catch { }
            }
            return returnComponents.Where(x => x != null).ToArray();
        }

        #endregion
        #region Adapters

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
            return this.entityComponents.AddComponentImmediately(component.GetTypeFast(), component);
        }

        public void AddComponents(IEnumerable<ECSComponent> components)
        {
            foreach(var component in components)
            {
                this.AddComponentImpl(component, false);
            }
            //entityComponents.RegisterAllComponents(false);
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
        

        public T AddComponentAndGetInstance<T>() where T : ECSComponent, new()
        {
            ECSComponent component = this.CreateNewComponentInstance(typeof(T));
            this.AddComponent(component);
            return (T) component;
        }

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
        public bool ChangeComponentSilent(ECSComponent component)
        {
            return this.entityComponents.ChangeComponent(component, true);
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

        public bool IsSameGroup<T>(ECSEntity otherEntity) where T : ECSComponentGroup =>
            (this.HasComponent<T>() && otherEntity.HasComponent<T>()) && this.GetComponent<T>().GetId().Equals(otherEntity.GetComponent<T>().GetId());

        public void OnDelete()
        {
            this.Alive = false;
            this.dataAccessPolicies.Clear();
            this.entityComponents.OnEntityDelete();
            this.entityGroups.Clear();
            this.entityComponents.ComponentsManagers.ForEach(x => x.Value.Clear());
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

        public bool TryRemoveComponent(long componentTypeId)
        {
            return this.entityComponents.RemoveComponentImmediately(componentTypeId) == null? false : true;
        }

        public void RemoveComponentIfPresent<T>() where T : ECSComponent
        {
            if (this.HasComponent<T>())
            {
                this.RemoveComponent(typeof(T));
            }
        }

        #endregion


        

        public override string ToString() =>
            $"{this.GetId()}({this.Name})";

        public object Clone() => MemberwiseClone();
    }
}
