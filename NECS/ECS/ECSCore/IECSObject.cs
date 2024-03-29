﻿using NECS.Core.Logging;
using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(1)]
    public class IECSObject
    {
        static public long Id { get; set; } = 0;
        static public long GId<T>() => EntitySerialization.TypeIdStorage[typeof(T)];
        public long instanceId = Guid.NewGuid().GuidToLongR();
        [System.NonSerialized]
        public List<IManager> connectPoints = new List<IManager>();
        [System.NonSerialized]
        public Type ObjectType;
        [System.NonSerialized]
        protected long ReflectionId = 0;
        [System.NonSerialized]
        public object SerialLocker = new object();

        /// <summary>
        /// signalise IECSObject for starting process serialization
        /// </summary>
        public void EnterToSerialization()
        {
            //lock(SerialLocker)
            {
                EnterToSerializationImpl();
            }
        }

        /// <summary>
        /// override this method for store property values to serializable fields
        /// </summary>
        protected virtual void EnterToSerializationImpl()
        {

        }

        public void AfterSerialization()
        {
            //lock(SerialLocker)
            {
                AfterSerializationImpl();
            }
        }

        protected virtual void AfterSerializationImpl()
        {

        }

        public void AfterDeserialization()
        {
            lock (SerialLocker)
            {
                AfterDeserializationImpl();
            }
        }
        protected virtual void AfterDeserializationImpl()
        {

        }

        public long GetId()
        {
            if (Id == 0)
                try
                {
                    if (ObjectType == null)
                    {
                        ObjectType = GetType();
                    }
                    if (ReflectionId == 0)
                        ReflectionId = ObjectType.GetCustomAttribute<TypeUidAttribute>().Id;
                    return ReflectionId;
                }
                catch
                {
                    NLogger.Error(this.GetType().ToString() + "Could not find Id field");
                    return 0;
                }
            else
                return Id;
        }
    }
}
