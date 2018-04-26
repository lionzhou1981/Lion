using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public delegate void WebSocketConnectedEventHandle();
    public delegate void WebSocketDisconnectedEventHandle();
    public delegate void WebSocketReceivedEventHandle(JToken _token);
    public delegate void BookChangedEventHandle(string _side, decimal _price, decimal _amount);
    public delegate void WebSocketErrorEventHandle(string _code, string _message);

    public class MarketBase
    {
        public event WebSocketConnectedEventHandle OnWebSocketConnectedEvent = null;
        public event WebSocketDisconnectedEventHandle OnWebSocketDisconnectedEvent = null;
        public event WebSocketReceivedEventHandle OnWebSocketReceivedEvent = null;
        public event BookChangedEventHandle OnBookChangedEvent = null;
        public event WebSocketErrorEventHandle OnErrorEvent = null;

        internal virtual void OnWebSocketConnected() { if (this.OnWebSocketConnectedEvent != null) { this.OnWebSocketConnectedEvent(); } }
        internal virtual void OnWebSocketDisconnected() { if (this.OnWebSocketDisconnectedEvent != null) { this.OnWebSocketDisconnectedEvent(); } }
        internal virtual void OnWebSocketReceived(JToken _token) { if (this.OnWebSocketReceivedEvent != null) { this.OnWebSocketReceivedEvent(_token); } }
        internal virtual void OnBookChanged(string _side, decimal _price, decimal _amount) { if (this.OnBookChangedEvent != null) { this.OnBookChangedEvent(_side, _price, _amount); } }
        internal virtual void OnError(string _code, string _message) { if (this.OnErrorEvent != null) { this.OnErrorEvent(_code, _message); } }
    }
}