#if GODOT && !GODOT4_0_OR_GREATER
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using NECS.Network.NetworkModels;
using Newtonsoft.Json;

namespace NECS.Network.WebSocket
{
    public class WSClientGodot : Node, ISocketRealization
    {
        #region Fields
        
        // URL для подключения
        [Export]
        public string WebsocketUrl = "ws://localhost:8080";
        
        // Godot WebSocket клиент
        private Godot.WebSocketClient _client;
        
        // Состояние подключения
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private bool _isDisposed = false;
        
        // Данные подключения
        private string _address;
        private int _port;
        private long _id;
        
        // Протоколы WebSocket
        private string[] _protocols = new string[] { };
        
        #endregion
        
        #region Events
        
        public event Action<ISocketRealization, byte[]> DataReceived;
        public event Action<ISocketRealization, Exception> ErrorOccurred;
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;
        
        #endregion
        
        #region Properties
        
        public long Id 
        { 
            get => _id; 
            set => _id = value; 
        }

        public string Address => _address;

        public int Port => _port;

        public bool IsConnected => _isConnected && _client != null && _client.GetPeer(1).IsConnectedToHost();

        public bool IsConnecting => _isConnecting;

        public bool IsDisposed => _isDisposed;

        public bool IsSocketDisposed => _isDisposed || _client == null;
        
        #endregion
        
        #region Godot Lifecycle

        public override void _Ready()
        {
            
        }
        
        public override void _Process(float delta)
        {
            if (_client != null && (_isConnected || _isConnecting))
            {
                _client.Poll();
            }
        }
        
        public override void _ExitTree()
        {
            Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        public  void InitializeClient(string host, int port, int bufferSize = 1024)
        {
            WebsocketUrl = $"ws://{host}:{port}";
            ParseUrl(WebsocketUrl);

            if (_client != null)
            {
                CleanupClient();
            }
            
            _client = new Godot.WebSocketClient();
            
            // Подключаем сигналы
            _client.Connect("connection_closed", this, nameof(_OnConnectionClosed));
            _client.Connect("connection_error", this, nameof(_OnConnectionError));
            _client.Connect("connection_established", this, nameof(_OnConnectionEstablished));
            _client.Connect("data_received", this, nameof(_OnDataReceived));
            _client.Connect("server_close_request", this, nameof(_OnServerCloseRequest));
        }
        
        private void CleanupClient()
        {
            if (_client == null) return;
            
            try
            {
                // Отключаем сигналы
                if (_client.IsConnected("connection_closed", this, nameof(_OnConnectionClosed)))
                    _client.Disconnect("connection_closed", this, nameof(_OnConnectionClosed));
                    
                if (_client.IsConnected("connection_error", this, nameof(_OnConnectionError)))
                    _client.Disconnect("connection_error", this, nameof(_OnConnectionError));
                    
                if (_client.IsConnected("connection_established", this, nameof(_OnConnectionEstablished)))
                    _client.Disconnect("connection_established", this, nameof(_OnConnectionEstablished));
                    
                if (_client.IsConnected("data_received", this, nameof(_OnDataReceived)))
                    _client.Disconnect("data_received", this, nameof(_OnDataReceived));
                    
                if (_client.IsConnected("server_close_request", this, nameof(_OnServerCloseRequest)))
                    _client.Disconnect("server_close_request", this, nameof(_OnServerCloseRequest));
                
                // Закрываем соединение
                if (_isConnected)
                {
                    _client.DisconnectFromHost();
                }
                
                _client = null;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error cleaning up WebSocket client: {ex.Message}");
            }
        }
        
        private void ParseUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                _address = uri.Host;
                _port = uri.Port;
                
                // Если порт не указан явно, используем стандартные
                if (_port == -1)
                {
                    _port = uri.Scheme == "wss" ? 443 : 80;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to parse URL: {ex.Message}");
                _address = "localhost";
                _port = 8080;
            }
        }
        
        #endregion
        
        #region Signal Handlers
        
        private void _OnConnectionClosed(bool wasClean = false)
        {
            _isConnected = false;
            _isConnecting = false;
            
            GD.Print($"WebSocket connection closed. Clean: {wasClean}");
            
            Disconnected?.Invoke(this);
            
            if (!wasClean)
            {
                ErrorOccurred?.Invoke(this, new Exception("Connection closed unexpectedly"));
            }
        }
        
        private void _OnConnectionError()
        {
            _isConnected = false;
            _isConnecting = false;
            
            GD.PrintErr("WebSocket connection error occurred");
            
            var error = new Exception("WebSocket connection error");
            ErrorOccurred?.Invoke(this, error);
        }
        
        private void _OnConnectionEstablished(string protocol = "")
        {
            _isConnected = true;
            _isConnecting = false;
            
            GD.Print($"WebSocket connected with protocol: {protocol}");
            
            Connected?.Invoke(this);
        }
        
        private void _OnDataReceived()
        {
            try
            {
                if (!_isConnected)
                {
                    _OnConnectionEstablished();
                }
            }
            catch (Exception ex)
            {
                NLogger.Error($"Error receiving data: {ex.Message}");
            }
            try
            {
                var peer = _client.GetPeer(1);

                // Получаем все доступные пакеты
                while (peer.GetAvailablePacketCount() > 0)
                {
                    var packet = peer.GetPacket();
                    var result = NetworkPacketBuilderService.instance.UnpackNetworkPacket(packet);
                    if (result.Item2)
                    {
                        DataReceived?.Invoke(this, result.Item1);
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error receiving data: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        
        private void _OnServerCloseRequest(int code, string reason)
        {
            GD.Print($"Server requested close. Code: {code}, Reason: {reason}");
            _client.DisconnectFromHost(code, reason);
        }
        
        #endregion
        
        #region Connection Methods

        public void Connect()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(WSClientGodot));
            if (_isConnected || _isConnecting) return;
            
            _isConnecting = true;

            if (_client == null)
            {
                NLogger.LogError("Network client is not initialized");
                //InitializeClient();
            }
            
            var err = _client.ConnectToUrl(WebsocketUrl, _protocols);
            
            if (err != Error.Ok)
            {
                _isConnecting = false;
                var exception = new Exception($"Failed to connect to {WebsocketUrl}. Error: {err}");
                ErrorOccurred?.Invoke(this, exception);
                throw exception;
            }
        }
        
        public void Disconnect()
        {
            if (_client == null || !_isConnected) return;
            
            _client.DisconnectFromHost(1000, "Client disconnect");
            _isConnected = false;
            _isConnecting = false;
        }
        
        public void Close()
        {
            Disconnect();
            CleanupClient();
        }
        
        public void Reconnect()
        {
            Disconnect();
            CallDeferred(nameof(Connect));
        }
        
        public void ConnectAsync()
        {
            CallDeferred(nameof(Connect));
        }
        
        public void DisconnectAsync()
        {
            CallDeferred(nameof(Disconnect));
        }
        
        public bool ReconnectAsync()
        {
            try
            {
                CallDeferred(nameof(Reconnect));
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #region Send Methods
        
        public void Send(byte[] buffer)
        {
            if (!IsConnected)
            {
                var error = new InvalidOperationException("WebSocket is not connected");
                ErrorOccurred?.Invoke(this, error);
                throw error;
            }
            
            try
            {
                var peer = _client.GetPeer(1);
                var err = peer.PutPacket(buffer);
                
                if (err != Error.Ok)
                {
                    var exception = new Exception($"Failed to send data. Error: {err}");
                    ErrorOccurred?.Invoke(this, exception);
                    throw exception;
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }
        
        public void Send(ECSEvent ecsEvent)
        {
            if (ecsEvent == null)
                throw new ArgumentNullException(nameof(ecsEvent));
            
            try
            {
                // Сериализуем событие в JSON
                // var json = JsonConvert.SerializeObject(ecsEvent);
                // var buffer = Encoding.UTF8.GetBytes(json);
                Send(ecsEvent.GetNetworkPacket());
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }
        
        public void SendAsync(byte[] buffer)
        {
            if (!IsConnected)
            {
                ErrorOccurred?.Invoke(this, new InvalidOperationException("WebSocket is not connected"));
                return;
            }
            
            // В Godot все операции WebSocket уже асинхронные
            // Используем CallDeferred для отложенного вызова
            CallDeferred(nameof(_DeferredSend), buffer);
        }
        
        private void _DeferredSend(byte[] buffer)
        {
            try
            {
                Send(buffer);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetUrl(string url)
        {
            WebsocketUrl = url;
            ParseUrl(url);
        }
        
        public void SetProtocols(string[] protocols)
        {
            _protocols = protocols ?? new string[] { };
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            Close();
            
            DataReceived = null;
            ErrorOccurred = null;
            Connected = null;
            Disconnected = null;
        }
        
        #endregion
    }
}
#endif