using NECS.Network.NetworkModels.TCP;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Network.NetworkModels
{
    public class SocketAdapter
    {
        TCPGameClient TCPGameClientSocket;
        TCPGameSession TCPGameSessionSocket;

        public long Id
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.Id.GuidToLong();
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.Id.GuidToLong();
                }
                return 0;
            }
        }

        public string Address
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.Address;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.Server.Address;
                }
                return "";
            }
        }

        public int Port
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.Port;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.Server.Port;
                }
                return -1;
            }
        }
        public Socket Socket
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.Socket;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.Socket;
                }
                return null;
            }
        }



        public SocketAdapter(TCPGameClient client)
        {
            TCPGameClientSocket = client;
            client.IsConnected;
            client.IsConnecting;
            client.IsDisposed;
            client.IsSocketDisposed;
        }

        public SocketAdapter(TCPGameSession session)
        {
            TCPGameSessionSocket = session;
        }

        #region send
        public virtual long Send(byte[] buffer) => Send(buffer.AsSpan());

        public virtual long Send(byte[] buffer, long offset, long size) => Send(buffer.AsSpan((int)offset, (int)size));

        public virtual long Send(ReadOnlySpan<byte> buffer)
        {
            if(TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.Send(buffer);
            }
            if (TCPGameSessionSocket == null)
            {
                return TCPGameSessionSocket.Send(buffer);
            }
            return -1;
        }

        public virtual long Send(string text) => Send(Encoding.UTF8.GetBytes(text));

        public virtual long Send(ReadOnlySpan<char> text) => Send(Encoding.UTF8.GetBytes(text.ToArray()));

        public virtual bool SendAsync(byte[] buffer) => SendAsync(buffer.AsSpan());

        public virtual bool SendAsync(byte[] buffer, long offset, long size) => SendAsync(buffer.AsSpan((int)offset, (int)size));

        public virtual bool SendAsync(ReadOnlySpan<byte> buffer)
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.SendAsync(buffer);
            }
            if (TCPGameSessionSocket == null)
            {
                return TCPGameSessionSocket.SendAsync(buffer);
            }
            return false;
        }

        public virtual bool SendAsync(string text) => SendAsync(Encoding.UTF8.GetBytes(text));

        public virtual bool SendAsync(ReadOnlySpan<char> text) => SendAsync(Encoding.UTF8.GetBytes(text.ToArray()));
        #endregion

    }
}
