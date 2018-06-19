using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public delegate void LogEventHandle(params string[] _text);
    public delegate void BalanceChangedEventHandle(decimal _balanceAvailable);




    public delegate void WebSocketConnectedEventHandle();
    public delegate void WebSocketDisconnectedEventHandle();
    public delegate void WebSocketReceivedEventHandle(JToken _token);
    public delegate void WebSocketErrorEventHandle(string _code, string _message);
    public delegate void BookChangedEventHandle(string _side, decimal _price, decimal _amount);
    public delegate void BookUpdatedEventHandle(Dictionary<decimal, decimal> _asks, Dictionary<decimal, decimal> _bids);

    public class MarketBase
    {
        public decimal BalanceAvailable = 0M;

        public event LogEventHandle OnLogEvent = null;
        public event BalanceChangedEventHandle OnBalanceChangedEvent = null;
        internal virtual void OnLog(params string[] _text) { if (this.OnLogEvent != null) { this.OnLogEvent(_text); } }
        internal virtual void OnBalanceChanged(decimal _balanceAvailable) { if (this.OnBalanceChangedEvent != null) { this.OnBalanceChanged(_balanceAvailable); } }







        public event WebSocketConnectedEventHandle OnWebSocketConnectedEvent = null;
        public event WebSocketDisconnectedEventHandle OnWebSocketDisconnectedEvent = null;
        public event WebSocketReceivedEventHandle OnWebSocketReceivedEvent = null;
        public event WebSocketErrorEventHandle OnErrorEvent = null;
        public event BookChangedEventHandle OnBookChangedEvent = null;
        public event BookUpdatedEventHandle OnBookUpdatedEvent = null;

        internal virtual void OnWebSocketConnected() { if (this.OnWebSocketConnectedEvent != null) { this.OnWebSocketConnectedEvent(); } }
        internal virtual void OnWebSocketDisconnected() { if (this.OnWebSocketDisconnectedEvent != null) { this.OnWebSocketDisconnectedEvent(); } }
        internal virtual void OnWebSocketReceived(JToken _token) { if (this.OnWebSocketReceivedEvent != null) { this.OnWebSocketReceivedEvent(_token); } }
        internal virtual void OnError(string _code, string _message) { if (this.OnErrorEvent != null) { this.OnErrorEvent(_code, _message); } }
        internal virtual void OnBookChanged(string _side, decimal _price, decimal _amount) { if (this.OnBookChangedEvent != null) { this.OnBookChangedEvent(_side, _price, _amount); } }
        internal virtual void OnBookUpdated(Dictionary<decimal,decimal> _asks, Dictionary<decimal, decimal> _bids) { if (this.OnBookUpdatedEvent != null) { this.OnBookUpdatedEvent(_asks,_bids); } }
    }
}