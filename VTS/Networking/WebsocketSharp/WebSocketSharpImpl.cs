using System;
using System.Collections.Concurrent;
using WebSocketSharp;

// GOES INTO STREAMER.bot
namespace VTS.Networking.Impl
{
    public class WebSocketSharpImpl : IWebSocket
    {
        private WebSocket _socket;
        private ConcurrentQueue<string> _intakeQueue = new ConcurrentQueue<string>();

        private bool _attemptReconnect = false;

        private Action _onConnect = () => { };
        private Action _onDisconnect = () => { };
        private Action _onError = () => { };
        private string _url = "";

        public WebSocketSharpImpl()
        {
            _intakeQueue = new ConcurrentQueue<string>();
        }

        public string GetNextResponse()
        {
            _intakeQueue.TryDequeue(out string response);

            return response;
        }

        public bool IsConnecting()
        {
            return _socket?.ReadyState == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen()
        {
            return _socket?.ReadyState == WebSocketState.Open;
        }

        public void Send(string message)
        {
            _socket.SendAsync(message, (success) => { });
        }

        public void Start(string URL, Action onConnect, Action onDisconnect, Action onError)
        {
            _url = URL;

            _socket?.Close();

            _socket = new WebSocket(_url);
            _socket.WaitTime = TimeSpan.FromSeconds(10);
            _onConnect = onConnect;
            _onDisconnect = onDisconnect;
            _onError = onError;
            _socket.OnMessage += (sender, e) =>
            {
                if (e != null && e.IsText)
                {
                    _intakeQueue.Enqueue(e.Data);
                }
            };
            _socket.OnOpen += (sender, e) =>
            {
                _onConnect();
                Console.WriteLine(string.Format("{0} Socket open!", _socket.Url.Host));
                _attemptReconnect = true;
            };
            _socket.OnError += (sender, e) =>
            {
                Console.WriteLine(string.Format("{0} Socket error...", _socket.Url.Host));
                if (e != null)
                {
                    Console.WriteLine(string.Format("'{0}', {1}", e.Message, e.Exception));
                }
                _onError();
            };
            _socket.OnClose += (sender, e) =>
            {
                Console.WriteLine(string.Format("{0} Socket closing: {1}, '{2}', {3}", _socket.Url.Host, e.Code, e.Reason, e.WasClean));
                _onDisconnect();
                if (_attemptReconnect && !e.WasClean)
                {
                    Reconnect();
                }
            };

            _socket.ConnectAsync();
        }

        public void Stop()
        {
            _attemptReconnect = false;
            if (_socket != null && _socket.IsAlive)
            {
                _socket.Close();
            }
        }

        private void Reconnect()
        {
            Start(_url, _onConnect, _onDisconnect, _onError);
        }
    }
}
