using NECS.Core.Logging;
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
        #region propertys

        TCPGameClient TCPGameClientSocket;
        TCPGameSession TCPGameSessionSocket;

        private long customId = -1;
        public long Id
        {
            get
            {
                if(customId == -1)
                {
                    if (TCPGameClientSocket == null)
                    {
                        return TCPGameClientSocket.Id.GuidToLong();
                    }
                    if (TCPGameSessionSocket == null)
                    {
                        return TCPGameSessionSocket.Id.GuidToLong();
                    }
                }
                return customId;
            }
            set
            {
                customId = value;
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

        public bool IsConnected
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.IsConnected;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.IsConnected;
                }
                return false;
            }
        }

        /// <summary>
        /// client-only property
        /// </summary>
        /// <returns></returns>
        public bool IsConnecting
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.IsConnecting;
                }
                if (TCPGameSessionSocket == null)
                {
                    Logger.LogError("Try IsConnecting from session socket");
                }
                return false;
            }
        }

        public bool IsDisposed
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.IsDisposed;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.IsDisposed;
                }
                return false;
            }
        }

        public bool IsSocketDisposed
        {
            get
            {
                if (TCPGameClientSocket == null)
                {
                    return TCPGameClientSocket.IsSocketDisposed;
                }
                if (TCPGameSessionSocket == null)
                {
                    return TCPGameSessionSocket.IsSocketDisposed;
                }
                return false;
            }
        }

        #endregion

        public SocketAdapter(TCPGameClient client)
        {
            TCPGameClientSocket = client;
        }

        public SocketAdapter(TCPGameSession session)
        {
            TCPGameSessionSocket = session;
        }

        #region connect
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.Connect();
            }
            if (TCPGameSessionSocket == null)
            {
                Logger.LogError("Try Connect from session socket");
            }
            return false;
        }
        /// <summary>
        /// multistate program socket method
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.Disconnect();
            }
            if (TCPGameSessionSocket == null)
            {
                return TCPGameSessionSocket.Disconnect();
            }
            return false;
        }
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public bool Reconnect()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.Reconnect();
            }
            if (TCPGameSessionSocket == null)
            {
                Logger.LogError("Try Reconnect from session socket");
            }
            return false;
        }
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public bool ConnectAsync()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.ConnectAsync();
            }
            if (TCPGameSessionSocket == null)
            {
                Logger.LogError("Try ConnectAsync from session socket");
            }
            return false;
        }
        /// <summary>
        /// multistate program socket method
        /// </summary>
        /// <returns></returns>
        public bool DisconnectAsync()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.DisconnectAsync();
            }
            if (TCPGameSessionSocket == null)
            {
                return TCPGameSessionSocket.Disconnect();
            }
            return false;
        }
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public bool ReconnectAsync()
        {
            if (TCPGameClientSocket == null)
            {
                return TCPGameClientSocket.ReconnectAsync();
            }
            if (TCPGameSessionSocket == null)
            {
                Logger.LogError("Try ReconnectAsync from session socket");
            }
            return false;
        }
        #endregion

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
