// Copyright © 2017 - MazyModz. Created by Dennis Andersson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NECS.Network.NetworkModels;
using NECS.ECS.ECSCore;
using NECS;
using NECS.Harness.Services;
using NECS.Extensions.ThreadingSync;

namespace WebSocketRealization
{
    ///<summary>
    /// Object for all connectecd clients
    /// </summary>
    public partial class WSServerClient : ISocketRealization
    {
        #region Fields

        ///<summary>The socket of the connected client</summary>
        private Socket _socket;

        /// <summary>The server that the client is connected to</summary>
        private WSServer _server;

        /// <summary>If the server has sent a ping to the client and is waiting for a pong</summary>
        private bool _bIsWaitingForPong;
        private int userPackets;

        private volatile bool _isConnected;
        private volatile bool _isConnecting;
        private volatile bool _isDisposed;
        private volatile bool _isSocketDisposed;

        public event Action<ISocketRealization, byte[]> DataReceived;
        public event Action<ISocketRealization, Exception> ErrorOccurred;
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;

        public long Id { get; set; }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public bool IsConnected => _isConnected && _socket != null && _socket.Connected;
        public bool IsConnecting => _isConnecting;
        public bool IsDisposed => _isDisposed;
        public bool IsSocketDisposed => _isSocketDisposed;
        public bool SocketClosed { get; private set; }

        #endregion

        #region Class Events

        /// <summary>Create a new object for a connected client</summary>
        /// <param name="Server">The server object instance that the client is connected to</param>
        /// <param name="Socket">The socket of the connected client</param>
        public WSServerClient(WSServer Server, Socket Socket)
        {
            this._server = Server;
            this._socket = Socket;
            this.Id = Guid.NewGuid().GuidToLong();

            // Extract address and port from the connected socket
            if (Socket.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                this.Address = remoteEndPoint.Address.ToString();
                this.Port = remoteEndPoint.Port;
            }

            _isConnected = true;
            _isConnecting = false;
            _isDisposed = false;
            _isSocketDisposed = false;
            SocketClosed = false;

            // Start to detect incomming messages 
            GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);
        }

        #endregion

        #region Field Getters

        ///<summary>Gets the socket of the connected client</summary>
        ///<returns>The socket of the client</return>
        public Socket GetSocket()
        {
            return _socket;
        }

        /// <summary>The socket that this client is connected to</summary>
        /// <returns>Listen socket</returns>
        public WSServer GetServer()
        {
            return _server;
        }

        /// <summary>Gets if the server is waiting for a pong response</summary>
        /// <returns>If the server is waiting for a pong response</returns>
        public bool GetIsWaitingForPong()
        {
            return _bIsWaitingForPong;
        }

        #endregion

        #region Field Setters

        /// <summary>Sets if the server is waiting for a pong response</summary>
        /// <param name="bIsWaitingForPong">If the server is waiting for a pong response</param>
        public void SetIsWaitingForPong(bool bIsWaitingForPong)
        {
            _bIsWaitingForPong = bIsWaitingForPong;
        }

        #endregion

        #region Methods

        /// <summary>Called when a message was received from the client</summary>
        private void messageCallback(IAsyncResult AsyncResult)
        {
            try
            {
                if (_isDisposed || _socket == null || !_socket.Connected)
                    return;

                GetSocket().EndReceive(AsyncResult);

                // Read the incomming message 
                byte[] messageBuffer = new byte[this._server.BufferSize];
                int bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) 
                    Array.Resize<byte>(ref messageBuffer, bytesReceived);

                // Get the opcode of the frame
                //EOpcodeType opcode = Helpers.GetFrameOpcode(messageBuffer);
                EOpcodeType opcode = EOpcodeType.Binary;

                // If the connection was closed
                if (opcode == EOpcodeType.ClosedConnection)
                {
                    InternalDisconnect();
                    return;
                }

                // Pass the message to the server event to handle the logic
                //this.ReceiveMessage(this, Helpers.GetByteFromFrame(messageBuffer));
                this.ReceiveMessage(this, Helpers.GetByteFromFrame(messageBuffer));

                // Start to receive messages again
                if (!_isDisposed && _socket != null && _socket.Connected)
                {
                    GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);

                // pass error
                //InternalDisconnect();
            }
        }

        private void InternalDisconnect()
        {
            if (_isDisposed)
                return;

            _isConnected = false;
            _isSocketDisposed = true;
            SocketClosed = true;

            try
            {
                _socket?.Close();
                _socket?.Dispose();
            }
            catch { }

            GetServer().ClientDisconnect(this);
        }

        public void Connect()
        {
            // Server clients are already connected when created
            throw new InvalidOperationException("Server client is already connected");
        }

        public void Disconnect()
        {
            if (_isDisposed)
                return;

            _isConnected = false;
            _isSocketDisposed = true;
            InternalDisconnect();
        }

        public void Close()
        {
            Disconnect();
        }

        public void Reconnect()
        {
            throw new InvalidOperationException("Server clients cannot reconnect");
        }

        public void ConnectAsync()
        {
            throw new InvalidOperationException("Server clients are already connected");
        }

        public void DisconnectAsync()
        {
            TaskEx.Run(() =>
            {
                try
                {
                    Disconnect();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            });
        }

        public bool ReconnectAsync()
        {
            throw new InvalidOperationException("Server clients cannot reconnect");
        }

        public void Send(byte[] buffer)
        {
            if (_isDisposed || !_isConnected)
                throw new InvalidOperationException("Cannot send data: client is not connected");

            this.SendMessage(this, buffer);
        }

        public void Send(ECSEvent ecsEvent)
        {
            if (_isDisposed || !_isConnected)
                throw new InvalidOperationException("Cannot send data: client is not connected");

            this.SendMessage(this, ecsEvent.GetNetworkPacket());
        }

        public void SendAsync(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (_isDisposed || !_isConnected)
            {
                ErrorOccurred?.Invoke(this, new InvalidOperationException("Cannot send data: client is not connected"));
                return;
            }

            TaskEx.Run(() =>
            {
                try
                {
                    Send(buffer);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            });
        }

        public void SendMessage(WSServerClient Client, string Data)
        {
            if (_isDisposed || !_isConnected || _socket == null || !_socket.Connected)
                throw new InvalidOperationException("Cannot send message: client is not connected");

            // Create a websocket frame around the data to send
            //byte[] frameMessage = Helpers.GetFrameFromString(Data);
            byte[] frameMessage = Encoding.Default.GetBytes(Data);//Helpers.GetFrameFromString(Data);

            // Send the framed message to the client
            Client.GetSocket().Send(Helpers.GetFrameFromByte(frameMessage));
        }

        public void SendMessage(WSServerClient Client, byte[] Data)
        {
            if (_isDisposed || !_isConnected || _socket == null || !_socket.Connected)
                throw new InvalidOperationException("Cannot send message: client is not connected");

            // Create a websocket frame around the data to send
            //byte[] frameMessage = Helpers.GetFrameFromByte(Data);
            byte[] frameMessage = Helpers.GetFrameFromByte(Data);

            // Send the framed message to the client
            Client.GetSocket().Send(frameMessage);
        }

        public void ReceiveMessage(WSServerClient Client, byte[] Message)
        {
            //DataReceived?.Invoke(this, Message);
            
            if (Message.Length == 0)
            {
                Close();
                return;
            }

            var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(Message);

            if(result.Item2)
            {
                NetworkingService.instance.OnReceived(this, result.Item1);
            }

            userPackets++;
            SocketClosed = false;
        }

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Disconnect();
        }

        #endregion
    }
}