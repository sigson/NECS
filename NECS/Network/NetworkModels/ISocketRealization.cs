using NECS.ECS.ECSCore;
using System;
using System.Net.Sockets;

namespace NECS.Network.NetworkModels
{
    public interface ISocketRealization
    {
        #region Properties
        
        long Id { get; set; }
        bool ProxyMode { get; set; }
        string Address { get; }
        int Port { get; }
        bool IsConnected { get; }
        bool IsConnecting { get; }
        bool IsDisposed { get; }
        bool IsSocketDisposed { get; }
        event Action<ISocketRealization, byte[]> DataReceived;
        event Action<ISocketRealization, Exception> ErrorOccurred;
        event Action<ISocketRealization> Connected;
        event Action<ISocketRealization> Disconnected;
        
        #endregion

        #region Connection Methods

        void Connect();
        void Disconnect();
        void Close();
        void Reconnect();
        void ConnectAsync();
        void DisconnectAsync();
        bool ReconnectAsync();
        
        #endregion

        #region Send Methods
        
        void Send(byte[] buffer);
        void Send(ECSEvent ecsEvent);
        void SendAsync(byte[] buffer);
        
        #endregion
    }
}