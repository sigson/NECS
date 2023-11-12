using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using NetCoreServer;
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
        public string HostAddress;
        public int Port;
        public int BufferSize;
        public string Protocol;

        #region NetworkRealization
        private TcpClient tcpClient;
        private TcpServer tcpServer;
        #endregion


        public override void InitializeProcess()
        {
            HostAddress = ConstantService.instance.GetByConfigPath("socket").GetObject<string>("Networking/HostAddress");
            Port = ConstantService.instance.GetByConfigPath("socket").GetObject<int>("Networking/Port");
            BufferSize = ConstantService.instance.GetByConfigPath("socket").GetObject<int>("Networking/BufferSize");
            Protocol = ConstantService.instance.GetByConfigPath("socket").GetObject<string>("Networking/Protocol");

            switch(Protocol.ToLower())
            {
                case "tcp":
                    if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    {
                        tcpClient = new TcpClient(HostAddress, Port);
                    }
                    else
                    {
                        tcpServer = new TcpServer(HostAddress, Port);
                    }
                    break;
            }
        }

        public void OnConnected(SocketAdapter socketAdapter)
        {

        }

        public void OnDisconnected(SocketAdapter socketAdapter)
        {

        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
