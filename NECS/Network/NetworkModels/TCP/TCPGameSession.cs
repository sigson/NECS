using BitNet;
using NECS.Core.Logging;
using NECS.Harness.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameSession : IPeer
    {
        SocketAdapter socketAdapter;
        public long Id = 0;

        public Socket Socket => token.socket;
        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsSocketDisposed { get; private set; }

        public CUserToken token;

        public bool SocketClosed;
        public int errorCount;//after no error - clear
        public TimerCompat heartBeat;
        long pingId = 0;
        long lastResponsesPingId = 0;

        public Socket socket { get; protected set; }
        public int userPackets = 0;
        public int emitPackets = 0;
        public TCPGameServer Server { get; protected set; }
        byte[] buffer;
        Dictionary<long, object> Events = new Dictionary<long, object>();

        private void Setup()
        {
            socketAdapter = new SocketAdapter(this);
            Id = Guid.NewGuid().GuidToLong();
        }

        public TCPGameSession()
        {
            SocketClosed = true;
            Setup();
        }

        public TCPGameSession(CUserToken token, TCPGameServer server)
        {
            NLogger.Log("Client accepted");
            this.Server = server;
            this.token = token;
            this.token.set_peer(this);
            this.token.disable_auto_heartbeat();
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
            Close();
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
            //Close();
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

        void OnReceive(byte[] newBuffer)
        {
            //byte[] newBuffer = null;

            if (newBuffer.Length == 0)
            {
                Close();
                return;
            }

            var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(newBuffer);

            if(result.Item2)
            {
                NetworkingService.instance.OnReceived(result.Item1, 0, 0, this.socketAdapter);
            }

            //Array.Copy(buffer, newBuffer, newBuffer.Length);
            userPackets++;
            SocketClosed = false;
            
        }

        private void SendImpl(byte[] packet)
        {
            emitPackets++;
            var byteBuffer = new List<byte>(packet);
            int position = 0;
            if (SocketClosed)
                return;
            //Logger.Log("send data " + packet.GetType().ToString() + " " + byteBuffer.Count.ToString());
            while ((((float)byteBuffer.Count) / ((float)this.Server.BufferSize)) - position - 1 > 0)
            {
                //lock(sendLocker)
                {
                    try
                    {
                        CPacket cPacket = CPacket.create((short)PROTOCOL.Server);
                        cPacket.push(byteBuffer.GetRange(position * this.Server.BufferSize, this.Server.BufferSize).ToArray());
                        this.token.send(cPacket);
                        errorCount = 0;
                    }
                    catch (Exception ex)
                    {
                        NLogger.Log("SocketEmitError: " + ex.StackTrace);
                        errorCount++;
                    }
                }

                position++;
            }
            //lock (sendLocker)
            {
                try
                {
                    CPacket cPacket = CPacket.create((short)PROTOCOL.Server);
                    cPacket.push(byteBuffer.GetRange(position * this.Server.BufferSize, byteBuffer.Count - position * this.Server.BufferSize).ToArray());
                    this.token.send(cPacket);
                    errorCount = 0;
                }
                catch (Exception ex)
                {
                    NLogger.Log("SocketEmitError: " + ex.StackTrace);
                    errorCount++;
                }
            }
            if (errorCount > 100)
                SocketClosed = true;
        }

        public void Close()
        {
            this.token.close();
            DisconnectProcess();
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
    }
}
