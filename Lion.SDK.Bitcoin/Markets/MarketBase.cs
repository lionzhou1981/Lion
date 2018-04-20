using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public delegate void WebSocketConnectedEventHandle();
    public delegate void WebSocketDisconnectedEventHandle();
    public delegate void WebSocketReceivedEventHandle(JToken _token);

    public class MarketBase
    {
        public event WebSocketConnectedEventHandle OnWebSocketConnectedEvent = null;
        public event WebSocketDisconnectedEventHandle OnWebSocketDisconnectedEvent = null;
        public event WebSocketReceivedEventHandle OnWebSocketReceivedEvent = null;

        internal virtual void OnWebSocketConnected() { if (this.OnWebSocketConnectedEvent != null) { this.OnWebSocketConnectedEvent(); } }
        internal virtual void OnWebSocketDisconnected() { if (this.OnWebSocketDisconnectedEvent != null) { this.OnWebSocketDisconnectedEvent(); } }
        internal virtual void OnWebSocketReceived(JToken _token) { if (this.OnWebSocketReceivedEvent != null) { this.OnWebSocketReceivedEvent(_token); } }
    }
}