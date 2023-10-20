﻿using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IECSObjectManager<T> : IManager where T : IECSObject
    {
        private new static Type _managerTypeValue = null;

        protected Dictionary<long, IManagable> ManagableStorage = new Dictionary<long, IManagable>();

        public virtual void AddManagable(IManagable managable)
        {
            lock (ManagableStorage)
            {
                if (!ManagableStorage.ContainsKey(managable.instanceId))
                {
                    ManagableStorage.Add(managable.instanceId, managable);
                }
            }
        }

        public virtual void RemoveManagable(IManagable managable)
        {
            lock (ManagableStorage)
            {
                if (ManagableStorage.ContainsKey(managable.instanceId))
                {
                    ManagableStorage.Remove(managable.instanceId);
                }
            }
        }

        public virtual void ClearManagable()
        {
            ManagableStorage.ForEach(x => Destroy(x.Value));
        }

        protected T entityValue;
        public T ManagerECSObject
        {
            get
            {
                return getECSObject();
            }
            set
            {
                setECSObject(value);
            }
        }

        public long ManagerECSObjectId
        {
            get
            {
                if (ManagerECSObject != null)
                    return entityValue.instanceId;
                else
                    return 0;
            }
        }

        protected virtual T getECSObject() => entityValue;
        protected virtual IECSObjectManager<T> setECSObject(T _entity)
        {
            entityValue = _entity;
            return this;
        }
    }
}
