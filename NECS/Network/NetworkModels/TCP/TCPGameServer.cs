﻿using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Network.NetworkModels.TCP
{
    public class TCPGameServer : TcpServer
    {
        public TCPGameServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new TCPGameSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }
}