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

        ///<summary>The guid of the connected client</summary>

        /// <summary>The server that the client is connected to</summary>
        private WSServer _server;

        /// <summary>If the server has sent a ping to the client and is waiting for a pong</summary>
        private bool _bIsWaitingForPong;
        private int userPackets;

        public event Action<ISocketRealization, byte[]> DataReceived;
        public event Action<ISocketRealization, Exception> ErrorOccurred;
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;

        public long Id { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public bool IsConnected { get; set; }

        public bool IsConnecting { get; set; }

        public bool IsDisposed { get; set; }

        public bool IsSocketDisposed { get; set; }
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
                GetSocket().EndReceive(AsyncResult);

                // Read the incomming message 
                byte[] messageBuffer = new byte[8];
                int bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) Array.Resize<byte>(ref messageBuffer, bytesReceived);

                // Get the opcode of the frame
                EOpcodeType opcode = Helpers.GetFrameOpcode(messageBuffer);

                // If the connection was closed
                if (opcode == EOpcodeType.ClosedConnection)
                {
                    GetServer().ClientDisconnect(this);
                    return;
                }

                // Pass the message to the server event to handle the logic
                this.ReceiveMessage(this, Helpers.GetByteFromFrame(messageBuffer));

                // Start to receive messages again
                GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);

            }
            catch (Exception Exception)
            {
                GetSocket().Close();
                GetSocket().Dispose();
                GetServer().ClientDisconnect(this);
            }
        }

        public void Connect()
        {

        }

        public void Disconnect()
        {

            GetServer().ClientDisconnect(this);
        }

        public void Close()
        {
            GetSocket().Close();
            GetSocket().Dispose();
        }

        public void Reconnect()
        {

        }

        public void ConnectAsync()
        {

        }

        public void DisconnectAsync()
        {

        }

        public bool ReconnectAsync()
        {
            throw new NotImplementedException();
        }

        public void Send(byte[] buffer)
        {
            this.SendMessage(this, buffer);
        }

        public void Send(ECSEvent ecsEvent)
        {
            this.SendMessage(this, ecsEvent.GetNetworkPacket());
        }

        public void SendAsync(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(WSServerClient Client, string Data)
        {
            // Create a websocket frame around the data to send
            byte[] frameMessage = Helpers.GetFrameFromString(Data);

            // Send the framed message to the in client
            Client.GetSocket().Send(frameMessage);

        }

        public void SendMessage(WSServerClient Client, byte[] Data)
        {
            // Create a websocket frame around the data to send
            byte[] frameMessage = Helpers.GetFrameFromByte(Data);

            // Send the framed message to the in client
            Client.GetSocket().Send(frameMessage);

            // Call the on send message callback event 
        }

        public void ReceiveMessage(WSServerClient Client, byte[] Message)
        {
            DataReceived?.Invoke(this, Message);
            if (Message.Length == 0)
            {
                Close();
                return;
            }

            var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(Message);

            if(result.Item2)
            {
                NetworkingService.instance.OnReceived(result.Item1, 0, 0, this);
            }

            userPackets++;
            SocketClosed = false;
        }

        #endregion

    }
}
