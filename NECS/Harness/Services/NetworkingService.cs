using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Services
{
    public class NetworkingService : IService
    {
        public static NetworkingService instance => SGT.Get<NetworkingService>();

        public override void InitializeProcess()
        {
            ConstantService.instance.GetByConfigPath("socket");
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
