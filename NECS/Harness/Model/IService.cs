using NECS.Extensions;
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
        public static void InitializeAllServices()
        {
            //FindObjectsOfType<IService>();
            var services = ECSAssemblyExtensions.GetAllSubclassOf(typeof(IService)).Where(x => !x.IsAbstract).Select(x => IService.InitalizeSingleton(x)).Cast<IService>().ToList();
            services.ForEach(x => x.PostInitializeProcess());
        }
        #endregion
    }
}
