using BitNet;
using NECS.Harness.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NECS.Extensions.ThreadingSync;
using NECS.ECS.ECSCore;
using NECS.Extensions;

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameClient : IPeer, ISocketRealization
    {
        //ISocketRealization socketAdapter;
        public long Id = 0;
        public bool IsConnected { get => token != null ? token.is_connected() : false; }
        public bool IsConnecting { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsSocketDisposed { get; private set; }
        long ISocketRealization.Id { get => this.Id; set => this.Id = value; }

        string ISocketRealization.Address => this.Address;

        int ISocketRealization.Port => this.Port;

        public bool ProxyMode { get; set; } = false;

        public CUserToken token;
        public CNetworkService service;
        public CConnector connector;

        public string Address;
        public int Port;
        public int BufferSize;

        public event Action<ISocketRealization, byte[]> DataReceived;
        public event Action<ISocketRealization, Exception> ErrorOccurred;
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;

        public TimeSequencedEventBus<Action> oneThreadEventBus = new TimeSequencedEventBus<Action>(new TimeSpan(0, 1, 0));
        public TimerCompat eventsUpdateTimer;

        private void Setup()
        {
            //socketAdapter = new ISocketRealization(this);
            Id = Guid.NewGuid().GuidToLong();
            lock (this)
            {
                if (Defines.OneThreadMode && eventsUpdateTimer == null)
                {
                    oneThreadEventBus.Subscribe("Update", (x) =>
                    {
                        x.Invoke();
                        return TimeSequencedEventBus<Action>.ProcessingResult.Processed;
                    });
                    eventsUpdateTimer = new TimerCompat(5, (obj, arg) =>
                    {
                        oneThreadEventBus.Update();
                    }, true).Start();
                }
            }
        }

        public TCPGameClient(string host, int port, int bufferSize = 1024)
        {
            service = new CNetworkService(true);
            connector = new CConnector(service);
            connector.connected_callback += OnConnect;

            this.Address = host;
            this.Port = port;
            this.BufferSize = port;
        }

        public void Connect()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(this.Address), this.Port);
            connector.connect(endpoint);
            Setup();
        }

        void OnConnect(CUserToken server_token)
        {
            this.token = server_token;
            this.token.set_peer(this);
            server_token.on_connected();
            Setup();
            Action action = () => {

                Connected?.Invoke(this);
                if(!ProxyMode)
                {
                    NetworkingService.instance.OnConnected(this);
                }
            };
            if (Defines.OneThreadMode)
            {
                oneThreadEventBus.Publish(action);
            }
            else
            {
                action.Invoke();
            }
        }

        public void on_message(CPacket msg)
        {
            msg.pop_protocol_id();
            Action action = () => OnReceive(msg.pop_bytepack());
            if (Defines.OneThreadMode)
            {
                oneThreadEventBus.Publish(action);
            }
            else
            {
                action.Invoke();
            }
        }

        public void on_removed()
        {
            DisconnectProcess();
        }

        public void send(CPacket msg)
        {
            msg.record_size();
            this.token.send(new ArraySegment<byte>(msg.buffer, 0, msg.position));
        }

        public void disconnect()
        {
            this.token.ban();
            DisconnectProcess();
        }


        void OnReceive(byte[] newBuffer)
        {
            if (newBuffer.Length == 0)
            {
                this.disconnect();
                return;
            }

            DataReceived?.Invoke(this, newBuffer);

            if(!ProxyMode)
            {
                //Array.Copy(buffer, newBuffer, newBuffer.Length);
                var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(newBuffer);

                if (result.Item2)
                {
                    NetworkingService.instance.OnReceived(this, result.Item1);
                }
            }
        }

        public void SendImpl(byte[] sendBuffer)
        {
            var byteBuffer = new List<byte>(sendBuffer);
            int position = 0;
            CPacket cPacket = null;
            while ((((float)byteBuffer.Count) / ((float)this.BufferSize)) - position - 1 > 0)
            {
                cPacket = CPacket.create((short)PROTOCOL.Server);
                cPacket.push(byteBuffer.GetRange(position * this.BufferSize, this.BufferSize).ToArray());
                this.token.send(cPacket);
                position++;
            }
            cPacket = CPacket.create((short)PROTOCOL.Server);
            cPacket.push(byteBuffer.GetRange(position * this.BufferSize, byteBuffer.Count - position * this.BufferSize).ToArray());
            this.token.send(cPacket);
        }

        public void DisconnectProcess()
        {
            if (IsConnected)
            {
                token.close();
            }
            Action action = () =>
            {
                Disconnected?.Invoke(this);
                if(!ProxyMode)
                {
                    if (NetworkingService.instance.SocketAdapters.ContainsKey(this.Id))
                        NetworkingService.instance.OnDisconnected(this);
                }
            };
            if (Defines.OneThreadMode)
            {
                oneThreadEventBus.Publish(action);
            }
            else
            {
                action.Invoke();
            }
        }

        public bool Reconnect()
        {
            throw new NotImplementedException();
        }

        public void ConnectAsync()
        {
            TaskEx.RunAsync(() =>
            {
                this.Connect();
            }, true);
        }

        public void DisconnectAsync()
        {
            TaskEx.RunAsync(() =>
            {
                this.disconnect();
            }, true);
        }

        public bool ReconnectAsync()
        {
            throw new NotImplementedException();
        }

        public void SendAsync(byte[] buffer)
        {
            TaskEx.RunAsync(() =>
            {
                SendImpl(buffer);
            }, true);
        }

        public void Send(byte[] buffer)
        {
            SendImpl(buffer);
        }

        public void Close()
        {
            this.token.close();
            DisconnectProcess();
        }

        public void Disconnect()
        {
            this.disconnect();
        }

        void ISocketRealization.Reconnect()
        {
            this.ReconnectAsync();
        }

        public void Send(ECSEvent ecsEvent)
        {
            this.Send(ecsEvent.GetNetworkPacket());
        }
    }
}
