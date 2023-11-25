using NECS.Core.Logging;
using NECS.ECS.DefaultObjects.Events.ECSEvents;
using NECS.ECS.ECSCore;
using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using NECS.Network.NetworkModels.TCP;
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
        private static NetworkingService cacheInstance;
        public static NetworkingService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<NetworkingService>();
                return cacheInstance;
            }
        }
        public string HostAddress = "127.0.0.1";
        public int Port = 6667;
        public int BufferSize = 1024;
        public string Protocol = "tcp";
        public ConcurrentDictionary<long, SocketAdapter> SocketAdapters = new ConcurrentDictionary<long, SocketAdapter>();
        #region client
        public delegate void SocketHandler(SocketAdapter socketAdapter);
        public event SocketHandler? OnConnectExternal;
        public event SocketHandler? OnDisconnectExternal;
        public static long ClientEntityId = 0;
        private SocketAdapter cachedClientSocket;
        public SocketAdapter ClientSocket
        {
            get
            {
                if(cachedClientSocket == null)
                {
                    var socket = SocketAdapters.First().Value;
                    if(socket != null)
                        cachedClientSocket = socket;
                }
                return cachedClientSocket;
            }
        }
        #endregion
        #region NetworkRealization
        private TCPGameClient tcpClient;
        private TCPGameServer tcpServer;
        #endregion


        public override void InitializeProcess()
        {
            HostAddress = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("Networking/HostAddress");
            Port = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("Networking/Port");
            BufferSize = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("Networking/BufferSize");
            Protocol = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("Networking/Protocol");

            switch (Protocol.ToLower())
            {
                case "tcp":
                    if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                    {
                        tcpClient = new TCPGameClient(HostAddress, Port, BufferSize);
                        tcpClient.Connect();
                    }
                    else
                    {
                        tcpServer = new TCPGameServer(HostAddress, Port, BufferSize);
                        tcpServer.Listen();
                    }
                    break;
            }
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                CustomSetupInitialized = true;
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
                ServiceInitialized = true;
                initializedCallbackCache();
                OnConnectExternal.Invoke(socketAdapter);
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
                OnDisconnectExternal.Invoke(socketAdapter);
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
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(buffer, 0, buffer.Length);
                memoryStream.Position = 0;
                ManagerScope.instance.eventManager.OnEventAdd((ECSEvent)ReflectionCopy.MakeReverseShallowCopy(NetSerializer.Serializer.Default.Deserialize(memoryStream)), socketAdapter.Id);
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
                if (socketAdapter != null)
                {
                    socketAdapter.Connect();
                }
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if (socketAdapter == null)
                {
                    socketAdapter = SocketAdapters.First().Value;
                }
                socketAdapter.Connect();
            }
        }

        public void Send(SocketAdapter socketAdapter, byte[] packet)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                if(socketAdapter != null)
                {
                    socketAdapter.Send(packet);
                }
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if (socketAdapter == null)
                {
                    socketAdapter = SocketAdapters.First().Value;
                }
                socketAdapter.Send(packet);
            }
        }

        public void Disconnect(SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                if (socketAdapter != null)
                {
                    socketAdapter.Disconnect();
                }
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if (socketAdapter == null)
                {
                    socketAdapter = SocketAdapters.First().Value;
                }
                socketAdapter.Disconnect();
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
