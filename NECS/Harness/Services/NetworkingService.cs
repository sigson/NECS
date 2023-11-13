using NECS.Core.Logging;
using NECS.ECS.DefaultObjects.Events.ECSEvents;
using NECS.ECS.ECSCore;
using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using NECS.Network.NetworkModels.TCP;
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
        public ConcurrentDictionary<long, SocketAdapter> SocketAdapters = new ConcurrentDictionary<long, SocketAdapter>();
        #region NetworkRealization
        private TCPGameClient tcpClient;
        private TCPGameServer tcpServer;
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
                        tcpClient = new TCPGameClient(HostAddress, Port);
                    }
                    else
                    {
                        tcpServer = new TCPGameServer(HostAddress, Port);
                    }
                    break;
            }
        }

        public void OnConnected(SocketAdapter socketAdapter)
        {
            SocketAdapters[socketAdapter.Id] = socketAdapter;
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                if(Defines.LowLevelNetworkEventsLogging)
                {
                    Logger.LogNetwork($"Connection start from {socketAdapter.Address}:{socketAdapter.Port}");
                }
                NetworkMaliciousEventCounteractionService.instance.maliciousScoringStorage[socketAdapter.Id] = new ScoreObject() { SocketId = socketAdapter.Id };
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if (Defines.LowLevelNetworkEventsLogging)
                {
                    Logger.LogNetwork($"Connected to server on {socketAdapter.Address}:{socketAdapter.Port}");
                }
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
                if (Defines.LowLevelNetworkEventsLogging)
                {
                    Logger.LogNetwork($"Client {socketAdapter.Address}:{socketAdapter.Port} disconnected from server");
                }
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

                TaskEx.RunAsync(() =>
                {
                    bool stop_check = false;
                    while (!stop_check)
                    {
                        Task.Delay(1000).Wait();
                        if (Defines.LowLevelNetworkEventsLogging)
                        {
                            Logger.LogNetwork($"Disconnected from server {socketAdapter.Address}:{socketAdapter.Port} try to connect");
                        }
                        // Try to connect again
                        socketAdapter.Connect();
                    }
                });
                
            }
        }

        public void OnReceived(byte[] buffer, long offset, long size, SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                //NetSerializer.Serializer.Default.Deserialize()
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
