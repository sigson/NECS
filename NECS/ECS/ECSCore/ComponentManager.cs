
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

namespace NECS.ECS.ECSCore
{
    public class ECSComponentManager
    {
        public static Dictionary<long, ECSComponent> AllComponents = new Dictionary<long, ECSComponent>();
        public static HashSet<long> DirectiveUpdateComponents = new HashSet<long>();

        public static Dictionary<long, List<Action<ECSEntity, ECSComponent>>> OnChangeCallbacksDB = new Dictionary<long, List<Action<ECSEntity, ECSComponent>>>();

        public static ECSComponentGroup GlobalProgramComponentGroup;
        static public void IdStaticCache()
        {
            var AllDirtyComponents = ECSAssemblyExtensions.GetAllSubclassOf(typeof(ECSComponent)).Where(x=>!x.IsAbstract).Select(x => (ECSComponent)Activator.CreateInstance(x)).ToList(); 
            foreach(var comp in AllDirtyComponents)
            {
                if (AllComponents.ContainsKey(comp.GetId()))
                    Logger.Error(comp.GetTypeFast().Name + " id is presened");
                AllComponents[comp.GetId()] = comp;
            }
            ECSEntity entity = new ECSEntity();
            foreach (var comp in AllComponents.Values)
            {
                try
                {
                    var field = comp.GetType().GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var customAttrib = comp.GetType().GetCustomAttribute<TypeUidAttribute>();
                    if (customAttrib != null)
                        field.SetValue(null, customAttrib.Id);
                    entity.AddComponentSilent((ECSComponent)comp.Clone());
                }
                catch(Exception ex)
                {
                    Console.WriteLine(comp.GetType().Name);
                    entity.AddComponentSilent((ECSComponent)comp.Clone());
                }
                if (comp.DirectiveUpdate)
                    DirectiveUpdateComponents.Add(comp.GetId());
            }
            //var checkData = EntitySerialization.FullSerialize(entity); // fill json serialization cache
            BurstSerializationManager.InitSerialize(entity.entityComponents.Components.Cast<IECSObject>().ToList()); // fill json serialization cache
            entity.entityComponents.OnEntityDelete();
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                GlobalProgramComponentGroup = new ClientComponentGroup();
            else if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                GlobalProgramComponentGroup = new ServerComponentGroup();
        }
    }
}
