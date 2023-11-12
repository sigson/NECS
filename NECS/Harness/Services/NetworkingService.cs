using NECS.ECS.DefaultObjects.Events.ECSEvents;
using NECS.ECS.ECSCore;
using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using NetCoreServer;
using System;
using System.Collections.Concurrent;
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
        public int MaxNetworkMaliciousScore = 1000;
        public int MaliciousIPTimeoutInSeconds = 300;
        public ConcurrentDictionary<long, SocketAdapter> SocketAdapters = new ConcurrentDictionary<long, SocketAdapter>();

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
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public void OnDisconnected(SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                ManagerScope.instance.eventManager.OnEventAdd(new ClientDisconnectedEvent()
                {
                    SocketSourceId = socketAdapter.Id
                });
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                TaskEx.RunAsync(() =>
                {
                    bool stop_check = false;
                    while (!stop_check)
                    {
                        Task.Delay(1000).Wait();

                        // Try to connect again
                        //Connect();
                    }
                });
                
            }
        }

        public void OnReceived(byte[] buffer, long offset, long size, SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public void OnError(System.Net.Sockets.SocketError error, SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public void OnServerError(System.Net.Sockets.SocketError error)
        {

        }

        public void Connect(SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public void Send(SocketAdapter socketAdapter, byte[] packet)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public void Disconnect(SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
