using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Network.NetworkModels
{
    public class NetworkPacketBuilderService : IService
    {
        public Dictionary<long, List<byte[]>> SlicedReceivedStorage = new Dictionary<long, List<byte[]>>();
        public override void InitializeProcess()
        {
            throw new NotImplementedException();
        }

        public override void OnDestroyReaction()
        {
            throw new NotImplementedException();
        }

        public override void PostInitializeProcess()
        {
            throw new NotImplementedException();
        }
    }
}
