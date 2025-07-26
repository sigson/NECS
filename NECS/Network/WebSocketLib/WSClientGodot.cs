#if GODOT && !GODOT4_0_OR_GREATER
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NECS.ECS.ECSCore;
using NECS.Network.NetworkModels;

namespace NECS.Network.WebSocket
{
    public class WebSocketClient : Node, ISocketRealization
    {
        // URL для подключения
        [Export]
        public string WebsocketUrl = "wss://libwebsockets.org";

        // Экземпляр WebSocketClient
        private WebSocketClient _client = new WebSocketClient();

        public override void _Ready()
        {
            // Подключаем базовые сигналы для получения уведомлений о подключении, закрытии и ошибках
            _client.Connect("connection_closed", this, nameof(_Closed));
            _client.Connect("connection_error", this, nameof(_Closed));
            _client.Connect("connection_established", this, nameof(_Connected));
            // Этот сигнал испускается при каждом получении полного пакета
            // когда не используется Multiplayer API
            // Альтернативно, можно проверить GetPeer(1).GetAvailablePackets() в цикле
            _client.Connect("data_received", this, nameof(_OnData));

            // Инициируем подключение к указанному URL
            var protocols = new string[] { "lws-mirror-protocol" };
            var err = _client.ConnectToUrl(WebsocketUrl, protocols);
            if (err != Error.Ok)
            {
                GD.Print("Unable to connect");
                SetProcess(false);
            }
        }

        private void _Closed(bool wasClean = false)
        {
            // wasClean покажет, было ли отключение корректно уведомлено
            // удаленным пиром перед закрытием сокета
            GD.Print("Closed, clean: ", wasClean);
            SetProcess(false);
        }

        private void _Connected(string proto = "")
        {
            // Вызывается при подключении, "proto" будет выбранным WebSocket
            // суб-протоколом (который является опциональным)
            GD.Print("Connected with protocol: ", proto);
            // Вы ДОЛЖНЫ всегда использовать GetPeer(1).PutPacket для отправки данных на сервер,
            // а не PutPacket напрямую, когда не используется MultiplayerAPI
            _client.GetPeer(1).PutPacket("Test packet".ToUTF8());
        }

        private void _OnData()
        {
            // Выводим полученный пакет, вы ДОЛЖНЫ всегда использовать GetPeer(1).GetPacket
            // для получения данных от сервера, а не GetPacket напрямую, когда не
            // используется MultiplayerAPI
            var packet = _client.GetPeer(1).GetPacket();
            GD.Print("Got data from server: ", packet.GetStringFromUTF8());
        }

        public override void _Process(float delta)
        {
            // Вызывайте это в _Process или _PhysicsProcess. Передача данных и испускание
            // сигналов будет происходить только при вызове этой функции
            _client.Poll();
        }
    }
}
#endif