using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NECS.Harness.Services;
using BitNet;
using NECS.Core.Logging;
using NECS.Extensions;

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameServer
    {
        public int Port { get; private set; }
        public Socket Socket { get; protected set; }
        public int BufferSize { get; private set; }
        public string Address { get; private set; }

        CNetworkService serverService;

        public TCPGameServer(int bufferSize = 2048)
        {
            this.BufferSize = bufferSize;
        }

        public TCPGameServer(string address, int port, int bufferSize = 1024)
        {
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException("Parameter 'port' must be between 1 and 65,535");
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("Parameter 'bufferSize' must be above 0");

            serverService = new CNetworkService(false);
            serverService.session_created_callback += OnConnected;
            serverService.initialize(10000, bufferSize);

            this.Port = port;
            this.BufferSize = bufferSize;
            this.Address = address;
        }

        public void Listen()
        {
            serverService.listen("0.0.0.0", this.Port, 100);
            //serverService.listen("127.0.0.1", this.port, 100);
        }

        void OnConnected(CUserToken token)
        {
            TCPGameSession user;
            try { user = new TCPGameSession(token, this); }
            catch
            {
                Logger.Log("error add user");
                return;
            }
        }

        public void Broadcast(byte[] packet)
        {
            foreach (var user in NetworkingService.instance.SocketAdapters)
                user.Value.SendAsync(packet);
        }
    }
}
