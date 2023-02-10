using System.Net;
using System.Collections.Generic;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VTS.Networking
{
    /// <summary>
    /// Underlying VTS socket connection and response processor.
    /// </summary>
    public class VTSWebSocket
    {
        // Dependencies
        private const string VTS_WS_URL = "ws://{0}:{1}";
        private IPAddress _ip = IPAddress.Loopback;
        private int _port = 8001;
        private IWebSocket _ws = null;

        // API Callbacks
        private readonly Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();
        private readonly Dictionary<string, VTSEventCallbacks> _events = new Dictionary<string, VTSEventCallbacks>();

        private static UdpClient UDP_CLIENT = null;
        private static Task<UdpReceiveResult> UDP_RESULT = null;
        private static readonly Dictionary<int, VTSVTubeStudioAPIStateBroadcastData> PORTS = new Dictionary<int, VTSVTubeStudioAPIStateBroadcastData>();

        #region Lifecycle
        public void Initialize(IWebSocket webSocket)
        {
            _ws = webSocket;
        }
        #endregion

        #region UDP

        private void CheckPorts()
        {
            StartUDP();
            if (UDP_CLIENT != null)
            {
                if (UDP_RESULT != null)
                {
                    if (UDP_RESULT.IsCanceled || UDP_RESULT.IsFaulted)
                    {
                        // If the task faults, try again
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                    }
                    else if (UDP_RESULT.IsCompleted)
                    {
                        // Otherwise, collect the result
                        string text = Encoding.UTF8.GetString(UDP_RESULT.Result.Buffer);
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                        var response = JsonConvert.DeserializeObject<VTSVTubeStudioAPIStateBroadcastData>(text);
                        var data = response.Data;

                        if (data != null)
                        {
                            if (PORTS.ContainsKey(data.Port))
                            {
                                PORTS.Remove(data.Port);
                            }

                            PORTS.Add(data.Port, response);
                        }
                    }
                }

                if (UDP_RESULT == null)
                {
                    UDP_RESULT = UDP_CLIENT.ReceiveAsync();
                }
            }
        }

        private void StartUDP()
        {
            try
            {
                if (UDP_CLIENT == null)
                {
                    // This configuration should prevent the UDP client from blocking other connections to the port
                    IPEndPoint LOCAL_PT = new IPEndPoint(IPAddress.Any, 47779);
                    UDP_CLIENT = new UdpClient();
                    UDP_CLIENT.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UDP_CLIENT.Client.Bind(LOCAL_PT);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Dictionary<int, VTSVTubeStudioAPIStateBroadcastData> GetPorts()
        {
            return new Dictionary<int, VTSVTubeStudioAPIStateBroadcastData>(PORTS);
        }

        public bool SetPort(int port)
        {
            if (PORTS.ContainsKey(port))
            {
                _port = port;

                return true;
            }

            return false;
        }

        public bool SetIPAddress(string ipString)
        {
            if (IPAddress.TryParse(ipString, out IPAddress address))
            {
                _ip = address;

                return true;
            }

            return false;
        }

        #endregion

        #region I/O

        // GOES INTO STREAMER.BOT
        private void ProcessResponses()
        {
            while (_ws?.GetNextResponse() is string data)
            {
                var response = JsonConvert.DeserializeObject<VTSMessageData<DTO>>(data);
                Type responseType = Type.GetType("VTS" + response.MessageType + "Data");
                var responseData = JsonConvert.DeserializeObject(data, responseType);

                try
                {
                    if (_events.ContainsKey(response.MessageType))
                    {
                        _events[response.MessageType].OnEvent((dynamic)responseData);
                    }
                    else if (_callbacks.ContainsKey(response.RequestID))
                    {
                        if (response.MessageType == "APIError")
                        {
                            _callbacks[response.RequestID].OnError((dynamic)responseData);
                        }
                        else
                        {
                            _callbacks[response.RequestID].OnSuccess((dynamic)responseData);
                        }

                        _callbacks.Remove(response.RequestID);
                    }
                }
                catch (Exception e)
                {
                    VTSErrorData error = new VTSErrorData(e.Message)
                    {
                        RequestID = response.RequestID
                    };

                    _events[response.MessageType].OnError(error);
                }
            }
        }

        #endregion

        #region I/O      

        public void Connect(Action onConnect, Action onDisconnect, Action onError)
        {
            if (_ws != null)
            {
                _ws.Start(string.Format(VTS_WS_URL, _ip.ToString(), _port), onConnect, onDisconnect, onError);
            }
            else
            {
                onError();
            }
        }

        public void Disconnect()
        {
            _ws?.Stop();
        }

        public void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError) where T : VTSMessageData<DTO> where K : VTSMessageData<DTO>
        {
            if (_ws != null)
            {
                try
                {
                    _callbacks.Add(request.RequestID, new VTSCallbacks((k) => onSuccess((K)k), onError));
                    // make sure to remove null properties
                    _ws.Send(JsonConvert.SerializeObject(request));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    VTSErrorData error = new VTSErrorData();
                    error.Data.ErrorID = ErrorID.InternalServerError;
                    error.Data.Message = e.Message;
                    onError(error);
                }
            }
            else
            {
                VTSErrorData error = new VTSErrorData();
                error.Data.ErrorID = ErrorID.InternalServerError;
                error.Data.Message = "No websocket data";
                onError(error);
            }
        }

        public void SendEventSubscription<T, K>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe) where T : VTSEventSubscriptionRequestData where K : VTSEventData
        {
            Send<T, VTSEventSubscriptionResponseData>(
                request,
                (s) =>
                {
                    // add event or remove event from register
                    var data = request.Data;

                    if (_events.ContainsKey(data.EventName))
                    {
                        _events.Remove(data.EventName);
                    }

                    if (data.Subscribe)
                    {
                        _events.Add(data.EventName, new VTSEventCallbacks((k) => onEvent((K)k), onError, resubscribe));
                    }

                    onSubscribe(s);
                },
                onError);
        }

        public void ResubscribeToEvents()
        {
            foreach (VTSEventCallbacks callback in _events.Values)
            {
                callback.Resubscribe();
            }
        }

        #endregion

        private struct VTSCallbacks
        {
            public Action<VTSMessageData<DTO>> OnSuccess;
            public Action<VTSErrorData> OnError;
            public VTSCallbacks(Action<VTSMessageData<DTO>> onSuccess, Action<VTSErrorData> onError)
            {
                OnSuccess = onSuccess;
                OnError = onError;
            }
        }

        private struct VTSEventCallbacks
        {
            public Action<VTSEventData> OnEvent;
            public Action<VTSErrorData> OnError;
            public Action Resubscribe;
            public VTSEventCallbacks(Action<VTSEventData> onEvent, Action<VTSErrorData> onError, Action resubscribe)
            {
                OnEvent = onEvent;
                OnError = onError;
                Resubscribe = resubscribe;
            }
        }
    }
}
