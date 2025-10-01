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

        private readonly int _port;
        private readonly int _bufferSize;
        private readonly string _address;

        public int Port => _port;
        public int BufferSize => _bufferSize;
        public string Address => _address;

        #endregion

        #region Class Events

        /// <summary>Create and start a new listen socket server</summary>
        /// <param name="address">The listen address of the server</param>
        /// <param name="port">The port to listen on</param>
        /// <param name="bufferSize">The buffer size for operations</param>
        public WSServer(string address, int port, int bufferSize = 1024)
        {
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException("Parameter 'port' must be between 1 and 65,535");
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("Parameter 'bufferSize' must be above 0");

            _address = address;
            _port = port;
            _bufferSize = bufferSize;

            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(address), port);

            // Set the endpoint if the input is valid
            if (EndPoint == null) return;
            this._endPoint = EndPoint;

            // Create a new listen socket
            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

        private void connectionCallback(IAsyncResult AsyncResult)
        {
            try
            {
                // Получаем сокет клиента, который пытается подключиться
                Socket clientSocket = GetSocket().EndAccept(AsyncResult);

                // --- НАЧАЛО ИЗМЕНЕНИЙ: КОРРЕКТНОЕ РУКОПОЖАТИЕ ---

                // Увеличим буфер на случай длинных заголовков
                byte[] handshakeBuffer = new byte[2048];
                int handshakeReceived = clientSocket.Receive(handshakeBuffer);

                // Преобразуем запрос в строку
                string headerRequest = Encoding.UTF8.GetString(handshakeBuffer, 0, handshakeReceived);

                // Находим ключ "Sec-WebSocket-Key" с помощью регулярного выражения
                string webSocketKey = new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(headerRequest).Groups[1].Value.Trim();

                // "Магическая строка" из стандарта WebSocket
                string magicString = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

                // Создаем ключ ответа путем хеширования
                string acceptKey = Convert.ToBase64String(
                    System.Security.Cryptography.SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(webSocketKey + magicString)
                    )
                );

                // Формируем полный и корректный HTTP-ответ
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n" // Важны два \r\n в конце
                );

                // Отправляем ответ клиенту
                clientSocket.Send(response);

                // --- КОНЕЦ ИЗМЕНЕНИЙ ---

                // Создаем новый объект клиента и добавляем его в список
                WSServerClient client = new WSServerClient(this, clientSocket);
                _clients.Add(client);

                // Вызываем событие, что клиент подключился
                NetworkingService.instance.OnConnected(client);
                Connected?.Invoke(client);

                // Снова начинаем принимать входящие соединения
                GetSocket().BeginAccept(connectionCallback, null);
            }
            catch (Exception Exception)
            {
                // Используйте более детальное логирование ошибки
                Console.WriteLine($"Handshake or connection callback error: {Exception.ToString()}");
            }
        }

        //////TLS///////////////TLS
        /// <summary>Called when the socket is trying to accept an incomming connection</summary>
        /// <param name="AsyncResult">The async operation state</param>
        private void connectionCallbackTLS(IAsyncResult AsyncResult)
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
                Connected?.Invoke(client);


                // Start to accept incomming connections again 
                GetSocket().BeginAccept(connectionCallback, null);

            }
            catch (Exception Exception)
            {
                Console.WriteLine("An error has occured while trying to accept a connecting client.\n\n{0}", Exception.Message);
            }
        }
        /////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Called when a client disconnectes, calls event OnClientDisconnected</summary>
        /// <param name="Client">The client that disconnected</param>
        public void ClientDisconnect(WSServerClient Client)
        {
            // Remove the client from the connected clients list
            _clients.Remove(Client);
            NetworkingService.instance.OnDisconnected(Client);
            Disconnected?.Invoke(Client);
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
            foreach (var client in _clients)
            {
                client.SendAsync(packet);
            }
        }

        #endregion

        #region Server Events

        /// <summary>Called when a client connected</summary>
        public event Action<ISocketRealization> Connected;
        
        /// <summary>Called when a client disconnected</summary>
        public event Action<ISocketRealization> Disconnected;

        #endregion
    }
}