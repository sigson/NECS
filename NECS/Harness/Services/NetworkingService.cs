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
using NECS.Extensions.ThreadingSync;
using NECS.Extensions;
using WebSocketRealization;

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class NetworkingService : IService
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
        public DictionaryWrapper<long, ISocketRealization> SocketAdapters = new DictionaryWrapper<long, ISocketRealization>();
        #region client
        [System.NonSerialized] public bool AuthError;
        [System.NonSerialized] public string AuthErrorReason;
        public string Username;
        public long PlayerEntityId = 0;
        public bool LoggedInGame;
        public bool Connected = false;
        public bool ServerAvailable = false;
        public delegate void SocketHandler(ISocketRealization socketAdapter);
        public event SocketHandler OnConnectExternal = (socket) => { };
        public event SocketHandler OnDisconnectExternal = (socket) => { };
        private ISocketRealization cachedClientSocket;
        public ISocketRealization ClientSocket
        {
            get
            {
                if (cachedClientSocket == null)
                {
                    var socket = SocketAdapters.First().Value;
                    if (socket != null)
                        cachedClientSocket = socket;
                }
                return cachedClientSocket;
            }
        }
        #endregion
        #region NetworkRealization
        private ISocketRealization tcpClient;
        private IServerRealization tcpServer;
        #endregion


        public override void InitializeProcess()
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Offline)
            {
                return;
            }
            HostAddress = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("Networking/HostAddress");
            Port = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("Networking/Port");
            BufferSize = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("Networking/BufferSize");
            Protocol = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<string>("Networking/Protocol");

            Action initact = () =>
            {
                switch (Protocol.ToLower())
                {
                    case "tcp":
                        if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                        {
                            TaskEx.RunAsync(() =>
                            {
                                tcpClient = new TCPGameClient(HostAddress, Port, BufferSize);
                                tcpClient.Connect();
                            }, true);
                        }
                        else
                        {
                            TaskEx.RunAsync(() =>
                            {
                                tcpServer = new TCPGameServer(HostAddress, Port, BufferSize);
                                tcpServer.Listen();
                            }, true);
                        }
                        break;
                    case "websocket":
                        if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
                        {
#if GODOT4_0_OR_GREATER || GODOT
                            this.ExecuteInstruction(() =>
                            {
                                tcpClient = new NECS.Network.WebSocket.WSClientGodot();
                                this.AddChild(tcpClient as NECS.Network.WebSocket.WSClientGodot);
                                (tcpClient as NECS.Network.WebSocket.WSClientGodot).InitializeClient(HostAddress, Port, BufferSize);
                                tcpClient.Connected += this.OnConnected;
                                tcpClient.Disconnected += this.OnDisconnected;
                                //tcpClient.ErrorOccurred += this.OnErrorOccurred;
                                tcpClient.DataReceived += this.OnReceived;
                                (tcpClient as NECS.Network.WebSocket.WSClientGodot).EnablePacketQueuing = true;
                                tcpClient.Connect();
                            });
#else
                        TaskEx.RunAsync(() =>
                        {
                            tcpClient = new WSClient(HostAddress, Port, BufferSize);
                            tcpClient.Connect();
                        });
#endif
                        }
                        else
                        {
                            TaskEx.RunAsync(() =>
                            {
                                tcpServer = new WSServer(HostAddress, Port, BufferSize);
                                tcpServer.Listen();
                            });
                        }
                        break;
                }
            };

            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                this.FreezeCurrentService(initact);
            }
            else
            {
                initact();
            }
            
        }

        public void OnConnected(ISocketRealization socketAdapter)
        {
            stopDisconnectConnecting = false;
            SocketAdapters[socketAdapter.Id] = socketAdapter;
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                if (Defines.LowLevelNetworkEventsLogging)
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
                this.UnfreezeCurrentService();
                OnConnectExternal.Invoke(socketAdapter);
            }
            Connected = true;
        }

        private bool stopDisconnectConnecting = false;
        public void OnDisconnected(ISocketRealization socketAdapter)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                SocketAdapters.Remove(socketAdapter.Id, out _);
                NetworkMaliciousEventCounteractionService.instance.maliciousScoringStorage.Remove(socketAdapter.Id, out _);
                ECSService.instance.eventManager.OnEventAdd(new ClientDisconnectedEvent()
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
                stopDisconnectConnecting = false;
                var timer = new TimerEx();
                timer.Elapsed += (sender, e) =>
                {
                    if (stopDisconnectConnecting)
                    {
                        timer.Stop();
                        return;
                    }
                    if (Defines.LowLevelNetworkEventsLogging)
                    {
                        NLogger.LogNetwork($"Disconnected from server {socketAdapter.Address}:{socketAdapter.Port} try to connect");
                    }
                    socketAdapter.Connect();
                };
                timer.Interval = 1000;
                timer.Start();
                OnDisconnectExternal.Invoke(socketAdapter);
            }
        }

        public void OnReceived(ISocketRealization socketAdapter, byte[] buffer)
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
                //var shallowCopy = DeepCopy.CopyObject(unserializedObject);
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
            //deserializedEvent.cachedGameDataEvent = NetworkPacketBuilderService.instance.SliceAndRepackForSendNetworkPacket(buffer);
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
                ECSService.instance.eventManager.OnEventAdd(deserializedEvent, socketAdapter);
            });


            //using (var memoryStream = new MemoryStream())
            //{
            //    memoryStream.Write(buffer, 0, buffer.Length);
            //    memoryStream.Position = 0;
            //    var unserializedObject = NetSerializer.Serializer.Default.Deserialize(memoryStream);    
            //}
        }

        public void OnError(System.Net.Sockets.SocketError error, ISocketRealization socketAdapter)
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

        public void Connect(ISocketRealization socketAdapter)
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

        public void Send(ISocketRealization socketAdapter, byte[] packet)
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                if (socketAdapter != null)
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

        public void Disconnect(ISocketRealization socketAdapter)
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
        
        protected override Action<int>[] GetInitializationSteps()
        {
            return new Action<int>[]
            {
                (step) => {  },
                (step) => { InitializeProcess(); },
            };
        }

        protected override void SetupCallbacks(List<IService> allServices)
        {
            this.RegisterCallbackUnsafe(ECSService.instance.GetSGTId(), 1, (d) => { return true; }, () =>
            {
                //await for ecs initalization
            }, 0);
        }
    }
}
