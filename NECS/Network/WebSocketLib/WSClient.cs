using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NECS.ECS.ECSCore;
using NECS.Network.NetworkModels;

namespace WebSocketRealization
{
    public class WSClient : ISocketRealization, IDisposable
    {
        #region Private Fields
        
        private readonly string _connectionString;
        private readonly ConcurrentQueue<WebSocketMessage> _incomingMessages;
        private readonly ConcurrentQueue<WebSocketMessage> _outgoingMessages;
        private readonly ConcurrentQueue<ConnectionCommand> _connectionCommands;
        private readonly AutoResetEvent _messageAvailableEvent;
        private readonly AutoResetEvent _eventTrigger;
        private readonly Thread _eventLoopThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        private volatile ClientWebSocket _webSocket;
        private volatile int _isConnected; // 0 = false, 1 = true
        private volatile int _isConnecting; // 0 = false, 1 = true
        private volatile int _isDisposed; // 0 = false, 1 = true
        private volatile int _isSocketDisposed; // 0 = false, 1 = true

        public event Action<ISocketRealization, byte[]> DataReceived;
        public event Action<ISocketRealization, Exception> ErrorOccurred;
        public event Action<ISocketRealization> Connected;
        public event Action<ISocketRealization> Disconnected;

        #endregion

        #region Properties

        public long Id { get; set; }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public bool IsConnected => _isConnected == 1;
        public bool IsConnecting => _isConnecting == 1;
        public bool IsDisposed => _isDisposed == 1;
        public bool IsSocketDisposed => _isSocketDisposed == 1;

        #endregion

        #region Events

        #endregion

        #region Constructor

        public WSClient(string host, int port, int bufferSize = 1024)
        {
            string connectionString = $"ws://{host}:{port}";
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            
            var uri = new Uri(_connectionString);
            Address = uri.Host;
            Port = uri.Port;
            
            _incomingMessages = new ConcurrentQueue<WebSocketMessage>();
            _outgoingMessages = new ConcurrentQueue<WebSocketMessage>();
            _connectionCommands = new ConcurrentQueue<ConnectionCommand>();
            _messageAvailableEvent = new AutoResetEvent(false);
            _eventTrigger = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();
            
            _eventLoopThread = new Thread(EventLoop)
            {
                IsBackground = true,
                Name = "WebSocket-EventLoop"
            };
            _eventLoopThread.Start();
        }

        #endregion

        #region Connection Methods

        public void Connect()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(WSClient));

            _connectionCommands.Enqueue(new ConnectionCommand { Action = ConnectionAction.Connect });
            _eventTrigger.Set();
            
            // Spin-wait for connection completion
            var timeout = Environment.TickCount + 30000; // 30 seconds
            while (Environment.TickCount < timeout && !IsConnected && !IsDisposed)
            {
                if (IsConnected || IsDisposed)
                    break;
                Thread.Sleep(10);
            }
            
            if (!IsConnected && !IsDisposed)
                throw new TimeoutException("Connection timeout");
        }

        public void Disconnect()
        {
            if (IsDisposed)
                return;

            _connectionCommands.Enqueue(new ConnectionCommand { Action = ConnectionAction.Disconnect });
            _eventTrigger.Set();
            
            // Spin-wait for disconnection completion
            var timeout = Environment.TickCount + 10000; // 10 seconds
            while (Environment.TickCount < timeout && IsConnected && !IsDisposed)
            {
                if (!IsConnected || IsDisposed)
                    break;
                Thread.Sleep(10);
            }
        }

        public void Close()
        {
            Disconnect();
        }

        public void Reconnect()
        {
            Disconnect();
            Thread.Sleep(1000);
            Connect();
        }

        public void ConnectAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    Connect();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            });
        }

        public void DisconnectAsync()
        {
            Task.Run(() =>
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
            var tcs = new TaskCompletionSource<bool>();
            
            Task.Run(() =>
            {
                try
                {
                    Reconnect();
                    tcs.SetResult(true);
                }
                catch
                {
                    tcs.SetResult(false);
                }
            });
            
            return tcs.Task.Result;
        }

        #endregion

        #region Send Methods

        public void Send(byte[] buffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(WSClient));
            
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var message = new WebSocketMessage
            {
                Data = buffer,
                Type = WebSocketMessageType.Binary
            };

            _outgoingMessages.Enqueue(message);
            _eventTrigger.Set();
        }

        public void Send(ECSEvent ecsEvent)
        {
            if (ecsEvent == null)
                throw new ArgumentNullException(nameof(ecsEvent));

            var buffer = ecsEvent.GetNetworkPacket();
            
            var message = new WebSocketMessage
            {
                Data = buffer,
                Type = WebSocketMessageType.Text
            };

            _outgoingMessages.Enqueue(message);
            _eventTrigger.Set();
        }

        public void SendAsync(byte[] buffer)
        {
            Task.Run(() =>
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

        #endregion

        #region Message Retrieval Methods

        public bool TryGetMessage(out string message)
        {
            message = null;
            if (_incomingMessages.TryDequeue(out var webSocketMessage))
            {
                message = Encoding.UTF8.GetString(webSocketMessage.Data);
                return true;
            }
            return false;
        }

        public string GetMessage(int timeoutMs = -1)
        {
            var endTime = timeoutMs > 0 ? Environment.TickCount + timeoutMs : int.MaxValue;
            
            while (Environment.TickCount < endTime && !IsDisposed)
            {
                if (TryGetMessage(out var message))
                    return message;
                
                if (timeoutMs > 0)
                {
                    var remainingTime = endTime - Environment.TickCount;
                    if (remainingTime <= 0)
                        break;
                    _messageAvailableEvent.WaitOne(Math.Min(remainingTime, 100));
                }
                else
                {
                    _messageAvailableEvent.WaitOne(100);
                }
            }
            
            return null;
        }

        #endregion

        #region Event Loop

        private void EventLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Process connection commands
                    while (_connectionCommands.TryDequeue(out var command))
                    {
                        ProcessConnectionCommand(command);
                    }
                    
                    // Process outgoing messages
                    while (_outgoingMessages.TryDequeue(out var message) && IsConnected)
                    {
                        ProcessOutgoingMessage(message);
                    }
                    
                    // Process incoming messages
                    if (IsConnected && _webSocket != null)
                    {
                        ProcessIncomingMessages();
                    }
                    
                    // Wait for next event or timeout
                    _eventTrigger.WaitOne(50);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    
                    if (IsConnected)
                    {
                        Interlocked.Exchange(ref _isConnected, 0);
                        Interlocked.Exchange(ref _isSocketDisposed, 1);
                        Disconnected?.Invoke(this);
                    }
                }
            }
        }

        private void ProcessConnectionCommand(ConnectionCommand command)
        {
            switch (command.Action)
            {
                case ConnectionAction.Connect:
                    HandleConnect();
                    break;
                case ConnectionAction.Disconnect:
                    HandleDisconnect();
                    break;
            }
        }

        private void HandleConnect()
        {
            if (IsConnected || IsConnecting)
                return;

            try
            {
                Interlocked.Exchange(ref _isConnecting, 1);
                
                var oldSocket = Interlocked.Exchange(ref _webSocket, null);
                oldSocket?.Dispose();
                
                var newSocket = new ClientWebSocket();
                var connectTask = newSocket.ConnectAsync(new Uri(_connectionString), _cancellationTokenSource.Token);
                
                connectTask.Wait(_cancellationTokenSource.Token);
                
                Interlocked.Exchange(ref _webSocket, newSocket);
                Interlocked.Exchange(ref _isConnected, 1);
                Interlocked.Exchange(ref _isConnecting, 0);
                Interlocked.Exchange(ref _isSocketDisposed, 0);
                
                Connected?.Invoke(this);
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _isConnecting, 0);
                Interlocked.Exchange(ref _isConnected, 0);
                Interlocked.Exchange(ref _isSocketDisposed, 1);
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        private void HandleDisconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                var currentSocket = _webSocket;
                if (currentSocket?.State == WebSocketState.Open)
                {
                    var closeTask = currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancellationTokenSource.Token);
                    closeTask.Wait(TimeSpan.FromSeconds(5));
                }
                
                Interlocked.Exchange(ref _webSocket, null);
                currentSocket?.Dispose();
                
                Interlocked.Exchange(ref _isConnected, 0);
                Interlocked.Exchange(ref _isSocketDisposed, 1);
                
                Disconnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _isConnected, 0);
                Interlocked.Exchange(ref _isSocketDisposed, 1);
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        private void ProcessOutgoingMessage(WebSocketMessage message)
        {
            try
            {
                var currentSocket = _webSocket;
                if (currentSocket?.State == WebSocketState.Open)
                {
                    var sendTask = currentSocket.SendAsync(
                        new ArraySegment<byte>(message.Data),
                        message.Type,
                        true,
                        _cancellationTokenSource.Token);
                    
                    sendTask.Wait(_cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this,ex);
            }
        }

        private void ProcessIncomingMessages()
        {
            try
            {
                var currentSocket = _webSocket;
                if (currentSocket?.State != WebSocketState.Open)
                    return;

                var buffer = new ArraySegment<byte>(new byte[4096]);
                var receiveTask = currentSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                
                // Non-blocking check for available data
                if (receiveTask.Wait(1))
                {
                    var result = receiveTask.Result;
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Interlocked.Exchange(ref _isConnected, 0);
                        Interlocked.Exchange(ref _isSocketDisposed, 1);
                        Disconnected?.Invoke(this);
                        return;
                    }
                    
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                        
                        while (!result.EndOfMessage)
                        {
                            var nextReceiveTask = currentSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                            nextReceiveTask.Wait(_cancellationTokenSource.Token);
                            result = nextReceiveTask.Result;
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        
                        var messageData = ms.ToArray();
                        var webSocketMessage = new WebSocketMessage
                        {
                            Data = messageData,
                            Type = result.MessageType
                        };
                        
                        _incomingMessages.Enqueue(webSocketMessage);
                        _messageAvailableEvent.Set();
                        
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            //var messageText = Encoding.UTF8.GetString(messageData);
                            DataReceived?.Invoke(this, messageData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            _cancellationTokenSource.Cancel();
            
            // Enqueue disconnect command
            _connectionCommands.Enqueue(new ConnectionCommand { Action = ConnectionAction.Disconnect });
            _eventTrigger.Set();
            
            // Wait for event loop to finish
            _eventLoopThread?.Join(TimeSpan.FromSeconds(5));
            
            _webSocket?.Dispose();
            _messageAvailableEvent?.Dispose();
            _eventTrigger?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region Helper Classes

        private class WebSocketMessage
        {
            public byte[] Data { get; set; }
            public WebSocketMessageType Type { get; set; }
        }

        private class ConnectionCommand
        {
            public ConnectionAction Action { get; set; }
        }

        private enum ConnectionAction
        {
            Connect,
            Disconnect
        }

        #endregion
    }
}