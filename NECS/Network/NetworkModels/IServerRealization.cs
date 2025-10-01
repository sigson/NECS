using System;
using System.Net.Sockets;

namespace NECS.Network.NetworkModels
{
    public interface IServerRealization
    {
        /// <summary>
        /// Порт сервера
        /// </summary>
        int Port { get; }
        
        /// <summary>
        /// Размер буфера
        /// </summary>
        int BufferSize { get; }
        
        /// <summary>
        /// Адрес сервера
        /// </summary>
        string Address { get; }
        
        /// <summary>
        /// Начать прослушивание подключений
        /// </summary>
        void Listen();
        void StopListen();
        
        /// <summary>
        /// Отправить пакет всем подключенным клиентам
        /// </summary>
        /// <param name="packet">Данные для отправки</param>
        void Broadcast(byte[] packet);

        event Action<ISocketRealization> Connected;
        event Action<ISocketRealization> Disconnected;
    }
}