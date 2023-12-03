using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Network.NetworkModels.TCP;
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
                    if (TCPGameClientSocket != null)
                    {
                        return TCPGameClientSocket.Id;
                    }
                    if (TCPGameSessionSocket != null)
                    {
                        return TCPGameSessionSocket.Id;
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.Address;
                }
                if (TCPGameSessionSocket != null)
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.Port;
                }
                if (TCPGameSessionSocket != null)
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.Socket;
                }
                if (TCPGameSessionSocket != null)
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.IsConnected;
                }
                if (TCPGameSessionSocket != null)
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.IsConnecting;
                }
                if (TCPGameSessionSocket != null)
                {
                    NLogger.LogError("Try IsConnecting from session socket");
                }
                return false;
            }
        }

        public bool IsDisposed
        {
            get
            {
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.IsDisposed;
                }
                if (TCPGameSessionSocket != null)
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
                if (TCPGameClientSocket != null)
                {
                    return TCPGameClientSocket.IsSocketDisposed;
                }
                if (TCPGameSessionSocket != null)
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
        public void Connect()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.Connect();
            }
            if (TCPGameSessionSocket != null)
            {
                NLogger.LogError("Try Connect from session socket");
            }
        }
        /// <summary>
        /// multistate program socket method
        /// </summary>
        /// <returns></returns>
        public void Disconnect()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.disconnect();
            }
            if (TCPGameSessionSocket != null)
            {
                TCPGameSessionSocket.disconnect();
            }
        }

        public void Close()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.Close();
            }
            if (TCPGameSessionSocket != null)
            {
                TCPGameSessionSocket.Close();
            }
        }

        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public void Reconnect()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.Reconnect();
            }
            if (TCPGameSessionSocket != null)
            {
                NLogger.LogError("Try Reconnect from session socket");
            }
        }
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public void ConnectAsync()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.ConnectAsync();
            }
            if (TCPGameSessionSocket != null)
            {
                NLogger.LogError("Try ConnectAsync from session socket");
            }
        }
        /// <summary>
        /// multistate program socket method
        /// </summary>
        /// <returns></returns>
        public void DisconnectAsync()
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.DisconnectAsync();
            }
            if (TCPGameSessionSocket != null)
            {
                TCPGameSessionSocket.disconnect();
            }
        }
        /// <summary>
        /// client-only method
        /// </summary>
        /// <returns></returns>
        public bool ReconnectAsync()
        {
            if (TCPGameClientSocket != null)
            {
                return TCPGameClientSocket.ReconnectAsync();
            }
            if (TCPGameSessionSocket != null)
            {
                NLogger.LogError("Try ReconnectAsync from session socket");
            }
            return false;
        }
        #endregion

        #region send
        public virtual void Send(byte[] buffer)
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.Send(buffer);
            }
            if (TCPGameSessionSocket != null)
            {
                TCPGameSessionSocket.Send(buffer);
            }
        }

        public virtual void Send(ECSEvent ecsEvent)
        {
            this.Send(ecsEvent.GetNetworkPacket());
        }

        public virtual void SendAsync(byte[] buffer)
        {
            if (TCPGameClientSocket != null)
            {
                TCPGameClientSocket.SendAsync(buffer);
            }
            if (TCPGameSessionSocket != null)
            {
                TCPGameSessionSocket.SendAsync(buffer);
            }
        }
        #endregion

    }
}
