
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NECS.Core.Logging;
using NECS.ECS.Components.ECSComponentsGroup;
using NECS.Extensions;
using NECS.Harness.Services;
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
    public class ECSComponentManager
    {
        public static Dictionary<long, List<Action<ECSEntity, ECSComponent>>> OnChangeCallbacksDB = new Dictionary<long, List<Action<ECSEntity, ECSComponent>>>();

        public static ECSComponentGroup GlobalProgramComponentGroup;

        private ECSWorld world;
        public ECSComponentManager(ECSWorld world)
        {
            this.world = world;
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                GlobalProgramComponentGroup = new ClientComponentGroup();
            else if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                GlobalProgramComponentGroup = new ServerComponentGroup();
        }
    }
}
