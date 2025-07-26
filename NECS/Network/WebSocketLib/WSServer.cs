// Copyright © 2017 - MazyModz. Created by Dennis Andersson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NECS.Network.NetworkModels;
using NECS.Harness.Services;

namespace WebSocketRealization
{
    ///<summary>
    /// Object for all listen servers
    ///</summary>
    public partial class WSServer : IServerRealization
    {
        #region Fields

        /// <summary>The listen socket (server socket)</summary>
        private Socket _socket;

        /// <summary>The listen ip end point of the server</summary>
        private IPEndPoint _endPoint;

        /// <summary>The connected clients to the server </summary>
        private List<WSServerClient> _clients = new List<WSServerClient>();

        public int Port => throw new NotImplementedException();

        public int BufferSize => throw new NotImplementedException();

        public string Address => throw new NotImplementedException();

        #endregion

        #region Class Events

        /// <summary>Create and start a new listen socket server</summary>
        /// <param name="EndPoint">The listen endpoint of the server</param>
        public WSServer(string address, int port, int bufferSize = 1024)
        {
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException("Parameter 'port' must be between 1 and 65,535");
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("Parameter 'bufferSize' must be above 0");

            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(address), port);

            // Set the endpoint if the input is valid
            if (EndPoint == null) return;
            this._endPoint = EndPoint;

            // Create a new listen socket
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Start the server
            
        }

        #endregion

        #region Field Getters

        /// <summary>Gets the listen socket</summary>
        /// <returns>The listen socket</returns>
        public Socket GetSocket()
        {
            return _socket;
        }

        /// <summary>Get the listen socket endpoint</summary>
        /// <returns>The listen socket endpoint</returns>
        public IPEndPoint GetEndPoint()
        {
            return _endPoint;
        }

        /// <summary>Gets a connected client at the given index</summary>
        /// <param name="Index">The connected client array index</param>
        /// <returns>The connected client at the index, returns null if the index is out of bounds</returns>
        public WSServerClient GetConnectedClient(int Index)
        {
            if (Index < 0 || Index >= _clients.Count) return null;
            return _clients[Index];
        }

        /// <summary>Gets a connected client with the given guid</summary>
        /// <param name="Guid">The Guid of the client to get</param>
        /// <returns>The client with the given id, return null if no client with the guid could be found</returns>
        public WSServerClient GetConnectedClient(long Guid)
        {
            foreach (WSServerClient client in _clients)
            {
                if (client.Id == Guid) return client;
            }
            return null;
        }

        /// <summary>Gets a connected client with the given socket</summary>
        /// <param name="Socket">The socket of the client </param>
        /// <returns>The connected client with the given socket, returns null if no client with the socket was found</returns>
        public WSServerClient GetConnectedClient(Socket Socket)
        {
            foreach (WSServerClient client in _clients)
            {
                if (client.GetSocket() == Socket) return client;
            }
            return null;
        }

        /// <summary>Get the number of clients that are connected to the server</summary>
        /// <returns>The number of connected clients</returns>
        public int GetConnectedClientCount()
        {
            return _clients.Count;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the listen server when a server object is created
        /// </summary>
        private void start()
        {
            // Bind the socket and start listending
            GetSocket().Bind(GetEndPoint());
            GetSocket().Listen(0);

            // Start to accept clients and accept incomming connections 
            GetSocket().BeginAccept(connectionCallback, null);
        }

        /// <summary>
        /// Stops the listen server 
        /// </summary>
        public void Stop()
        {
            GetSocket().Close();
            GetSocket().Dispose();
        }

        /// <summary>Called when the socket is trying to accept an incomming connection</summary>
        /// <param name="AsyncResult">The async operation state</param>
        private void connectionCallback(IAsyncResult AsyncResult)
        {
            try
            {
                // Gets the client thats trying to connect to the server
                Socket clientSocket = GetSocket().EndAccept(AsyncResult);

                // Read the handshake updgrade request
                byte[] handshakeBuffer = new byte[1024];
                int handshakeReceived = clientSocket.Receive(handshakeBuffer);

                // Get the hanshake request key and get the hanshake response
                string requestKey = Helpers.GetHandshakeRequestKey(Encoding.Default.GetString(handshakeBuffer));
                string hanshakeResponse = Helpers.GetHandshakeResponse(Helpers.HashKey(requestKey));

                // Send the handshake updgrade response to the connecting client 
                clientSocket.Send(Encoding.Default.GetBytes(hanshakeResponse));

                // Create a new client object and add 
                // it to the list of connected clients
                WSServerClient client = new WSServerClient(this, clientSocket);
                _clients.Add(client);

                // Call the event when a client has connected to the listen server 
                NetworkingService.instance.OnConnected(client);
                Connected(client);
                

                // Start to accept incomming connections again 
                GetSocket().BeginAccept(connectionCallback, null);

            }
            catch (Exception Exception)
            {
                Console.WriteLine("An error has occured while trying to accept a connecting client.\n\n{0}", Exception.Message);
            }
        }

        /// <summary>Called when a message was recived, calls the OnMessageReceived event</summary>
        /// <param name="Client">The client that sent the message</param>
        /// <param name="Message">The message that the client sent</param>

        /// <summary>Called when a client disconnectes, calls event OnClientDisconnected</summary>
        /// <param name="Client">The client that disconnected</param>
        public void ClientDisconnect(WSServerClient Client)
        {
            // Remove the client from the connected clients list
            _clients.Remove(Client);
            NetworkingService.instance.OnDisconnected(Client);
            Disconnected(Client);

            // Call the OnClientDisconnected event
            // if (OnClientDisconnected == null) throw new Exception("Server error: OnClientDisconnected is not bound!");
            // OnClientDisconnected(this, new OnClientDisconnectedHandler(Client));
        }

        public void Listen()
        {
            start();
        }

        public void StopListen()
        {
            Stop();
        }

        public void Broadcast(byte[] packet)
        {
            foreach (var user in NetworkingService.instance.SocketAdapters)
                user.Value.SendAsync(packet);
        }

        #endregion

        #region Server Events

        /// <summary>Send a message to a connected client</summary>
        /// <param name="Client">The client to send the data to</param>
        /// <param name="Data">The data to send the client</param>


        /// <summary>Called when a client disconnected</summary>
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;

        #endregion
    }
}
