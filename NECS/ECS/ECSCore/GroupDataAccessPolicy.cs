using System.Reflection;
using NECS.Core.Logging;

namespace NECS.ECS.ECSCore
{
    [Serializable]
    public class GroupDataAccessPolicy
    {
        public static long Id;
        public long instanceId = Guid.NewGuid().GuidToLong();
        public List<long> AvailableComponents = new List<long>();
        public List<long> RestrictedComponents = new List<long>();
        public Type GroupDataAccessPolicyType;
        protected long ReflectionId = 0;
        public Dictionary<long, byte[]> BinAvailableComponents = new Dictionary<long, byte[]>();
        public Dictionary<long, byte[]> BinRestrictedComponents = new Dictionary<long, byte[]>();
        public string JsonAvailableComponents = "";
        public string JsonRestrictedComponents = "";
        public bool IncludeRemovedAvailable = false;
        public bool IncludeRemovedRestricted = false;

        public static (string, Dictionary<long, byte[]>) ComponentsFilter(ECSEntity baseEntity, ECSEntity otherEntity)
        {
            string filteredComponents = "";
            Dictionary<long, byte[]> binFilteredComponents = new Dictionary<long, byte[]>();
            bool includeRemovedAvailable = false;
            bool includeRemovedRestricted = false;
            if(!otherEntity.emptySerialized)
            {
                for (int i = 0; i < baseEntity.dataAccessPolicies.Count; i++)
                {
                    var baseDataAP = baseEntity.dataAccessPolicies[i];
                    for (int i2 = 0; i2 < otherEntity.dataAccessPolicies.Count; i2++)
                    {
                        var otherDataAP = otherEntity.dataAccessPolicies[i2];
                        if (baseDataAP.instanceId == otherDataAP.instanceId)
                        {
                            filteredComponents += otherDataAP.JsonAvailableComponents;
                            binFilteredComponents = binFilteredComponents.Concat(otherDataAP.BinAvailableComponents.Where(x => !binFilteredComponents.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value);
                            if (otherDataAP.IncludeRemovedAvailable)
                                includeRemovedAvailable = true;
                        }
                        else if (baseDataAP.GetId() == otherDataAP.GetId())
                        {
                            filteredComponents += otherDataAP.JsonRestrictedComponents;
                            binFilteredComponents = binFilteredComponents.Concat(otherDataAP.BinRestrictedComponents.Where(x => !binFilteredComponents.ContainsKey(x.Key))).ToDictionary(x => x.Key, x => x.Value);
                            if (otherDataAP.IncludeRemovedRestricted)
                                includeRemovedRestricted = true;
                        }
                    }
                }
            }
            
            if ((binFilteredComponents.Count() == 0) && (includeRemovedAvailable || includeRemovedRestricted))
                return ("#INCLUDEREMOVED#", binFilteredComponents);
            else
                return (filteredComponents, binFilteredComponents);
        }

        public static List<long> RawComponentsFilter(ECSEntity baseEntity, ECSEntity otherEntity)
        {
            List<long> filteredComponents = new List<long>();
            for (int i = 0; i < baseEntity.dataAccessPolicies.Count; i++)
            {
                var baseDataAP = baseEntity.dataAccessPolicies[i];
                for (int i2 = 0; i2 < otherEntity.dataAccessPolicies.Count; i2++)
                {
                    var otherDataAP = otherEntity.dataAccessPolicies[i2];
                    try
                    {
                        if (baseDataAP.instanceId == otherDataAP.instanceId)
                        {
                            filteredComponents = filteredComponents.Concat(otherDataAP.AvailableComponents).ToList();
                        }
                        else if (baseDataAP.GetId() == otherDataAP.GetId())
                        {
                            filteredComponents = filteredComponents.Concat(otherDataAP.RestrictedComponents).ToList();
                        }
                    }
                    catch
                    {
                        Logger.Error("error GDAP filtering");
                    }
                    
                }
            }
            return filteredComponents;
        }

        public long GetId()
        {
            if (Id == 0)
                try
                {
                    if (GroupDataAccessPolicyType == null)
                    {
                        GroupDataAccessPolicyType = GetType();
                    }
                    if (ReflectionId == 0)
                        ReflectionId = GroupDataAccessPolicyType.GetCustomAttribute<TypeUidAttribute>().Id;
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
        public object Clone()
        {
            var cloned =  MemberwiseClone() as GroupDataAccessPolicy;
            return cloned;
        }
    }

}
