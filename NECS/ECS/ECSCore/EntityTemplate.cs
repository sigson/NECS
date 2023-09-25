using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    [TypeUid(210198972242373120)]
    public abstract class EntityTemplate
    {
        public override string ToString()
        {
            return $"[{this.GetType().Name}, \"{ConfigPath}\"]";
        }

        public abstract ECSEntity SetupEntity(ECSEntity newEntity);

        public long GetId()
        {
            if (Id == 0)
                try
                {
                    if (TemplateAccessorType == null)
                    {
                        TemplateAccessorType = GetType();
                    }
                    if (ReflectionId == 0)
                        ReflectionId = TemplateAccessorType.GetCustomAttribute<TypeUidAttribute>().Id;
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
        public List<string> ConfigPath { get; set; } = new List<string>();
        public List<string> ConfigLibName { get; set; }
        public Type TemplateAccessorType;
        protected long ReflectionId = 0;
        public static long Id { get; set; }
    }
}
