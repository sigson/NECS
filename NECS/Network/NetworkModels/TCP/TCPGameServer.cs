using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NECS.Harness.Services;

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameServer : TcpServer
    {
        public TCPGameServer(string address, int port) : base(address, port)
        {
            this.OptionReceiveBufferSize = NetworkingService.instance.BufferSize;
            this.OptionSendBufferSize = NetworkingService.instance.BufferSize;
        }

        protected override TcpSession CreateSession() { return new TCPGameSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }
}
