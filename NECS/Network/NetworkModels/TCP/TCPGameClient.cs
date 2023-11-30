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

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameClient : IPeer
    {
        SocketAdapter socketAdapter;
        public long Id = 0;
        public Socket Socket => token.socket;
        public bool IsConnected { get => token != null ? token.is_connected() : false; }
        public bool IsConnecting { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsSocketDisposed { get; private set; }

        public CUserToken token;
        public CNetworkService service;
        public CConnector connector;

        public string Address;
        public int Port;
        public int BufferSize;

        private void Setup()
        {
            socketAdapter = new SocketAdapter(this);
            Id = Guid.NewGuid().GuidToLong();
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
            NetworkingService.instance.OnConnected(this.socketAdapter);
        }

        public void on_message(CPacket msg)
        {
            msg.pop_protocol_id();
            OnReceive(msg.pop_bytepack());
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

            //Array.Copy(buffer, newBuffer, newBuffer.Length);
            var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(newBuffer);

            if (result.Item2)
            {
                NetworkingService.instance.OnReceived(result.Item1, 0, 0, this.socketAdapter);
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
            if (NetworkingService.instance.SocketAdapters.ContainsKey(this.socketAdapter.Id))
                NetworkingService.instance.OnDisconnected(this.socketAdapter);
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
            });
        }

        public void DisconnectAsync()
        {
            TaskEx.RunAsync(() =>
            {
                this.disconnect();
            });
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
            });
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
    }
}
