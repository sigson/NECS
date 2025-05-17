
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
        public static Dictionary<long, ECSComponent> AllComponents = new Dictionary<long, ECSComponent>();

        public static Dictionary<long, List<Action<ECSEntity, ECSComponent>>> OnChangeCallbacksDB = new Dictionary<long, List<Action<ECSEntity, ECSComponent>>>();

        public static ECSComponentGroup GlobalProgramComponentGroup;

        private ECSWorld world;
        public ECSComponentManager(ECSWorld world)
        {
            this.world = world;
        }

        static public void IdStaticCache()
        {
            //var tempWorld = ECSService.instance.GetWorld();
            var AllDirtyComponents = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSComponent)).Where(x=>!x.IsAbstract).Select(x => (ECSComponent)Activator.CreateInstance(x)).ToList(); 
            foreach(var comp in AllDirtyComponents)
            {
                if (AllComponents.ContainsKey(comp.GetId()))
                    NLogger.Error(comp.GetTypeFast().Name + " id is presened");
                AllComponents[comp.GetId()] = comp;
            }
            //ECSEntity entity = new ECSEntity();
            //tempWorld.entityManager.OnAddNewEntity(entity);
            foreach (var comp in AllComponents.Values)
            {
                try
                {
                    var field = comp.GetType().GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var customAttrib = comp.GetType().GetCustomAttribute<TypeUidAttribute>();
                    if (customAttrib != null && field != null)
                        field.SetValue(null, customAttrib.Id);
                    else
                        NLogger.LogError($"WARNING! Type{comp.GetType().ToString()} no have static id field or ID attribute");
                    //entity.AddComponentSilent((ECSComponent)comp.Clone());
                }
                catch (Exception ex)
                {
                    NLogger.Error(comp.GetType().Name + " no have static id field or ID attribute");
                    //entity.AddComponentSilent((ECSComponent)comp.Clone());
                }
            }
            //var checkData = EntitySerialization.FullSerialize(entity); // fill json serialization cache
            //EntitySerialization.InitSerialize(); // fill json serialization cache
            //entity.entityComponents.OnEntityDelete();
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                GlobalProgramComponentGroup = new ClientComponentGroup();
            else if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                GlobalProgramComponentGroup = new ServerComponentGroup();
        }
    }
}
