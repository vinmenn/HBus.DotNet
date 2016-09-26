using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using HBus.Server.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HBus.Server.Processors
{
    /// <summary>
    /// Websocket endpoint
    /// </summary>
    public class WebsocketProcessor : BaseProcessor
    {
        private const string Channel = "ws";
        private readonly WebSocketServer _ws;
        private readonly IList<IWebSocketConnection> _clients;

        private class SourceInfo
        {
            public string Source { get; set; }
            public int Address { get; set; }
            public string Type { get; set; }
            public string Channel { get; set; }
        }
        private class Subscription
        {
            public string Name { get; set; }
            public string MessageType { get; set; }
            public string Subscriber { get; set; }
            public SourceInfo[] Sources{ get; set; }
        }
        public WebsocketProcessor(string uri)
        {
            //Websocket clients container
            _clients = new List<IWebSocketConnection>();

            //Websocket server start
            _ws = new WebSocketServer(uri);
            _ws.Start(socket =>
            {
                socket.OnOpen = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = OnWsMessage;
            });

            //Event from ep source
            OnSourceEvent = (@event, point) =>
            {
//                if (@event.Type != Channel && !string.IsNullOrEmpty(@event.Type)) return;

                var json = JsonConvert.SerializeObject(@event, Formatting.Indented);

                foreach (var client in _clients)
                {
                    client.Send(json);
                }
            };

            //Error from ep source
            OnSourceError = (exception, sender) =>
            {
                //Close websocket clients
                foreach (var client in _clients)
                    client.Close();

                Log.Debug("error sent to websocket clients");
            };

            //Close connection with ep source
            OnSourceClose = (sender) =>
            {
                //Close websocket clients
                foreach (var client in _clients)
                    client.Close();

                _ws.Dispose();

                Log.Debug("Websocket closed on source close");
            };
        }

        #region Websocket events
        /// <summary>
        /// Open request from websocket
        /// </summary>
        /// <param name="socket"></param>
        private void OnOpen(IWebSocketConnection socket)
        {
            var ip = socket.ConnectionInfo.ClientIpAddress;

            //add new ip client
            if (_clients.All(c => c.ConnectionInfo.ClientIpAddress != ip))
                _clients.Add(socket);

            Log.Debug(string.Format("Websocket client {0} connected", ip));
        }

        /// <summary>
        /// Close request from websocket
        /// </summary>
        /// <param name="socket"></param>
        private void OnClose(IWebSocketConnection socket)
        {
            //if (socket.ConnectionInfo.SubProtocol != "hbus-protocol") return;
            if (_clients.Any(c => c.ConnectionInfo.ClientIpAddress == socket.ConnectionInfo.ClientIpAddress))
            {
                _clients.Remove(socket);
            }
            Log.Debug(string.Format("Websocket client {0} disconnected", socket.ConnectionInfo.ClientIpAddress));
        }

        /// <summary>
        /// Manage subscriptions from client requests
        /// </summary>
        /// <param name="message"></param>
        private void OnWsMessage(string message)
        {
            Log.Debug(string.Format("Websocket message {0}", message)); // socket.ConnectionInfo.ClientIpAddress, message));
            Event evt;
            try
            {
                dynamic obj = JObject.Parse(message);
                var type = obj.MessageType.ToString() as string;
                switch (type)
                {
                    case "page":
                        var request = JsonConvert.DeserializeObject<Subscription>(message);

                        foreach (var source in request.Sources)
                        {
                            evt = new Event()
                            {
                                Name = source.Type + "-subscribe",
                                Source = source.Source,
                                Channel = source.Channel,
                                Subscriber = request.Subscriber,
                                Address = source.Address.ToString(),
                                //Data = source.Data
                            };
                            Send(evt, this);
                        }
                        break;
                    case "event":
                        evt = JsonConvert.DeserializeObject<Event>(message);
                        Send(evt, this);
                        break;
                }
            }
            catch (Exception ex)
            {
                Error(ex, this);
            }
        }

        #endregion
    }
}
