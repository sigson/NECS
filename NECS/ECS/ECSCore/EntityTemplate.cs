﻿using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NECS.Harness.Model;

namespace NECS.ECS.ECSCore
{
    [System.Serializable]
    [TypeUid(5)]
    public abstract class EntityTemplate
    {
        public UserDataRowBase userDataRow;
        public EntityTemplate() { }

        public EntityTemplate(UserDataRowBase userDataRowBase)
        {
            userDataRow = userDataRowBase;
        }
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
                    NLogger.Error(this.GetType().ToString() + "Could not find Id field");
                    return 0;
                }
            else
                return Id;
        }

        public virtual void TemplateInitialize()
        {

        }

        public List<string> ConfigPath { get; set; } = new List<string>();
        public List<string> ConfigLibName { get; set; }
        public Type TemplateAccessorType;
        protected long ReflectionId = 0;
        public static long Id { get; set; }
    }
}
