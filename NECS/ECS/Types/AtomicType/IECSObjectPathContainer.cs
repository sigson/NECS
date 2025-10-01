using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(105)]
    public class IECSObjectPathContainer : BaseCustomType
    {
        static new public long Id { get; set; } = 105;
        public List<string> pathToECSObject = new List<string>();

        public long serializableInstanceId = -1;
        public long CacheInstanceId {
            get{
                if(Interlocked.Read(ref this.serializableInstanceId) == -1)
                {
                    if(ECSObject != null)
                    {
                        Interlocked.Exchange(ref this.serializableInstanceId, ECSObject.instanceId);
                    }
                }
                return Interlocked.Read(ref this.serializableInstanceId);
            }
        }

        public IECSObjectPathContainer() {}

        public IECSObjectPathContainer(bool enableClientBehaviour = false, bool updateCache = false)
        {
            AlwaysUpdateCache = updateCache;
            if (enableClientBehaviour)
            {
                if (GlobalProgramState.instance != null && GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                {
                    AlwaysUpdateCache = true;
                }
            }
        }

        public bool AlwaysUpdateCache = false;

        [System.NonSerialized]
        private IECSObject cacheECSObject = null;

        [Newtonsoft.Json.JsonIgnore]
        public IECSObject ECSObject
        {
            get
            {
                if (AlwaysUpdateCache)
                {
                    cacheECSObject = null;
                }
                if (cacheECSObject == null && pathToECSObject.Count > 0)
                    {
                        IECSObject currentObject = null;
                        foreach (var pathelement in pathToECSObject)
                        {
                            var pathParts = pathelement.Split(';');
                            var instanceId = long.Parse(pathParts[0]);
                            var objectType = pathParts[1];

                            if (objectType == "ent")
                            {
                                currentObject = ECSService.instance.GetWorldAndEntity(instanceId).entity;
                            }
                            else if (objectType == "cmp")
                            {
                                if (currentObject == null)
                                {
                                    NLogger.Error($"Something went wrong during deserialization of IECSObject '{instanceId}: {this.GetType().Name}': currentObject == null && objectType == 'cmp'");
                                }
                                else if (currentObject is ECSEntity owentity)
                                {
                                    currentObject = owentity.GetComponent(instanceId);
                                }
                                else if (currentObject is ComponentsDBComponent dbowner)
                                {
                                    currentObject = dbowner.GetComponent(instanceId).Item1;
                                }
                                else
                                {
                                    NLogger.Error($"Something went wrong during deserialization of IECSObject '{instanceId}: {this.GetType().Name}': objectType == 'cmp' && objectType != ComponentsDBComponent");
                                }
                            }
                        }
                        cacheECSObject = currentObject;
                        if (Interlocked.Read(ref this.serializableInstanceId) == -1)
                        {
                            if (cacheECSObject != null)
                            {
                                Interlocked.Exchange(ref this.serializableInstanceId, cacheECSObject.instanceId);
                            }
                        }
                    }
                return cacheECSObject;
            }
            set
            {
                if(value == null)
                {
                    NLogger.Error($"Try to set null value to IECSObjectPathContainer");
                    return;
                }
                var child = value;
                if (child is ECSEntity)
                {
                    pathToECSObject.Add($"{child.instanceId};ent");
                }
                if (child is ECSComponent)
                {
                    var compChild = (ECSComponent)child;
                    if (compChild.ownerDB != null)
                    {
                        pathToECSObject.Add($"{child.instanceId};cmp");
                        pathToECSObject.Add($"{compChild.ownerDB.GetId()};cmp");
                        pathToECSObject.Add($"{compChild.ownerDB.ownerEntity.instanceId};ent");
                    }
                    else
                    {
                        pathToECSObject.Add($"{child.GetId()};cmp");
                        pathToECSObject.Add($"{compChild.ownerEntity.instanceId};ent");
                    }
                    pathToECSObject.Reverse();
                }
                cacheECSObject = value;
                if (Interlocked.Read(ref this.serializableInstanceId) == -1)
                {
                    if (cacheECSObject != null)
                    {
                        Interlocked.Exchange(ref this.serializableInstanceId, cacheECSObject.instanceId);
                    }
                }
            }
        }
    }
}
