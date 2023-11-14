using NECS.Extensions;
using NECS.GameEngineAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IService : SGT
    {
        public abstract void PostInitializeProcess();

        #region static
        private static EngineApiObjectBehaviour ServiceStorage;
        private static List<IService> AllServiceList;

        public static void RegisterAllServices()
        {
            //FindObjectsOfType<IService>();
            AllServiceList = ECSAssemblyExtensions.GetAllSubclassOf(typeof(IService)).Where(x => !x.IsAbstract).Select(x => IService.InitalizeSingleton(x, ServiceStorage, true)).Cast<IService>().ToList();
        }

        public static void InitializeAllServices()
        {
            AllServiceList.ForEach(x => x.InitializeProcess());
            AllServiceList.ForEach(x => x.PostInitializeProcess());
        }
        #endregion
    }
}
