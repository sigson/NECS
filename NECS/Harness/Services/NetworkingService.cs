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
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using NECS.Harness.Serialization;

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
        [System.NonSerialized] public bool AuthError;
        [System.NonSerialized] public string AuthErrorReason;
        public string Username;
        public long PlayerEntityId = 0;
        public bool LoggedInGame;
        public bool Connected = false;
        public bool ServerAvailable = false;
        public delegate void SocketHandler(SocketAdapter socketAdapter);
        public event SocketHandler? OnConnectExternal = (socket) => { };
        public event SocketHandler? OnDisconnectExternal = (socket) => { };
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
                        TaskEx.RunAsync(() =>
                        {
                            tcpClient = new TCPGameClient(HostAddress, Port, BufferSize);
                            tcpClient.Connect();
                        });
                    }
                    else
                    {
                        TaskEx.RunAsync(() =>
                        {
                            tcpServer = new TCPGameServer(HostAddress, Port, BufferSize);
                            tcpServer.Listen();
                        });
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
                    NLogger.LogNetwork($"Connection start from {socketAdapter.Address}:{socketAdapter.Port}");
                }
                NetworkMaliciousEventCounteractionService.instance.maliciousScoringStorage[socketAdapter.Id] = new ScoreObject() { SocketId = socketAdapter.Id };
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if (Defines.LowLevelNetworkEventsLogging)
                {
                    NLogger.LogNetwork($"Connected to server on {socketAdapter.Address}:{socketAdapter.Port}");
                }
                ServiceInitialized = true;
                initializedCallbackCache();
                OnConnectExternal.Invoke(socketAdapter);
            }
            Connected = true;
        }

        public void OnDisconnected(SocketAdapter socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                SocketAdapters.Remove(socketAdapter.Id, out _);
                NetworkMaliciousEventCounteractionService.instance.maliciousScoringStorage.Remove(socketAdapter.Id, out _);
                ManagerScope.instance.eventManager.OnEventAdd(new ClientDisconnectedEvent()
                {
                    SocketSource = socketAdapter
                });
                if (Defines.LowLevelNetworkEventsLogging)
                {
                    NLogger.LogNetwork($"Client {socketAdapter.Address}:{socketAdapter.Port} disconnected from server");
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
                            NLogger.LogNetwork($"Disconnected from server {socketAdapter.Address}:{socketAdapter.Port} try to connect");
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

            ECSEvent deserializedEvent = null;
            try
            {
                //var shallowCopy = ReflectionCopy.MakeReverseShallowCopy(unserializedObject);
                //deserializedEvent = (ECSEvent)shallowCopy;
                var unserializedObject = SerializationAdapter.DeserializeAdapterEvent(buffer);
                try
                {
                    deserializedEvent = unserializedObject.Deserialize();
                }
                catch (Exception ex)
                {
                    NLogger.Error($"Failed to deserialize {unserializedObject.GetType().Name} with error {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                NLogger.Error($"Failed to deserialize buffer data with error {ex.Message}");
            }
            deserializedEvent.cachedGameDataEvent = NetworkPacketBuilderService.instance.SliceAndRepackForSendNetworkPacket(buffer);
            if (Defines.ECSNetworkTypeLogging)
            {
                NLogger.Log($"Received {deserializedEvent.GetType().Name}");
            }
            try
            {
                if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                {
                    deserializedEvent.EntityOwnerId = AuthService.instance.SocketToEntity[socketAdapter].instanceId;
                }
            }
            catch
            {
                if (deserializedEvent.GetType().GetCustomAttribute<LowLevelNetworkEvent>() == null)
                {
                    NLogger.Error($"Entity Owner Id not found for {deserializedEvent.GetType().Name}");
                }
            }

            TaskEx.RunAsync(() =>
            {
                ManagerScope.instance.eventManager.OnEventAdd(deserializedEvent, socketAdapter);
            });


            //using (var memoryStream = new MemoryStream())
            //{
            //    memoryStream.Write(buffer, 0, buffer.Length);
            //    memoryStream.Position = 0;
            //    var unserializedObject = NetSerializer.Serializer.Default.Deserialize(memoryStream);    
            //}
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
