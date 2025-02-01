using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(105)]
    public class IECSObjectPathContainer : BaseCustomType
    {
        public List<string> pathToECSObject = new List<string>();

        private IECSObject cacheECSObject = null;

        [Newtonsoft.Json.JsonIgnore]
        public IECSObject ECSObject
        {
            get
            {
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
                            currentObject = ManagerScope.instance.entityManager.EntityStorage[instanceId];
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
                }
                return cacheECSObject;
            }
            set
            {
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
                        pathToECSObject.Add($"{compChild.ownerDB.instanceId};cmp");
                        pathToECSObject.Add($"{compChild.ownerDB.ownerEntity.instanceId};ent");
                    }
                    else
                    {
                        pathToECSObject.Add($"{child.instanceId};cmp");
                        pathToECSObject.Add($"{compChild.ownerEntity.instanceId};ent");
                    }
                    pathToECSObject.Reverse();
                }
                cacheECSObject = value;
            }
        }
    }
}
