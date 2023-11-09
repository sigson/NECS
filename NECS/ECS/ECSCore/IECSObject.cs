using NECS.Core.Logging;
using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    [Serializable]
    [TypeUid(1)]
    public class IECSObject
    {
        static public long Id { get; set; } = 0;
        public long instanceId = Guid.NewGuid().GuidToLong();
        [NonSerialized]
        public List<IManager> connectPoints = new List<IManager>();
        [NonSerialized]
        public Type ObjectType;
        [NonSerialized]
        protected long ReflectionId = 0;
        [NonSerialized]
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
                    Logger.Error(this.GetType().ToString() + "Could not find Id field");
                    return 0;
                }
            else
                return Id;
        }
    }
}
