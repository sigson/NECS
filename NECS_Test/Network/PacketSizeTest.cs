using NECS;
using NECS.Harness.Services;
using NECS.Network.NetworkModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NECS_Test.Network
{
    
    [TestClass]
    public class PacketSizeTest
    {
        [TestMethod]
        public void PacketSizeMethodTest()
        {
            NetworkPacketBuilderService.InitalizeSingleton<NetworkPacketBuilderService>();
            NetworkingService.InitalizeSingleton<NetworkingService>();
            
        }

        
    }
}
