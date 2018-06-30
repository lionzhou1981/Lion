using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    #region Delegate
    public delegate void ConnectedEventHandle();
    public delegate void DisconnectedEventHandle();
    public delegate string ReceivingEventHandle(ref byte[] _binary);
    public delegate void ReceivedEventHandle(JToken _token);
    public delegate void LogEventHandle(params string[] _text);
    public delegate void BookStartedEventHandle(string _symbol);
    public delegate void BookInsertEventHandle(BookItem _bookItem);
    public delegate void BookUpdateEventHandle(BookItem _bookItem);
    public delegate void BookDeleteEventHandle(BookItem _bookItem);
    public delegate void OrderStartedEventHandle();
    public delegate void OrderInsertEventHandle(OrderItem _orderItem);
    public delegate void OrderUpdateEventHandle(OrderItem _orderItem);
    #endregion

    #region Old
    public delegate void WebSocketErrorEventHandle(string _code, string _message);
    public delegate void BookChangedEventHandle(string _side, decimal _price, decimal _amount);
    public delegate void BookUpdatedEventHandle(Dictionary<decimal, decimal> _asks, Dictionary<decimal, decimal> _bids);
    #endregion

    public abstract class MarketBase
    {
        public string Name = "";
        public string WebSocket = "";

        public Books Books = new Books();
        public Orders Orders = new Orders();
        public Balance Balance = new Balance();

        protected bool Running = false;

        private ClientWebSocket webSocket = null;
        private Thread webSocketThread;

        #region Event
        public event ConnectedEventHandle OnConnectedEvent = null;
        public event DisconnectedEventHandle OnDisconnectedEvent = null;
        public event ReceivingEventHandle OnReceivingEvent = null;
        public event ReceivedEventHandle OnReceivedEvent = null;
        public event LogEventHandle OnLogEvent = null;
        public event BookStartedEventHandle OnBookStartedEvent = null;
        public event BookInsertEventHandle OnBookInsertEvent = null;
        public event BookUpdateEventHandle OnBookUpdateEvent = null;
        public event BookDeleteEventHandle OnBookDeleteEvent = null;
        public event OrderStartedEventHandle OnOrderStartedEvent = null;
        public event OrderInsertEventHandle OnOrderInsertEvent = null;
        public event OrderUpdateEventHandle OnOrderUpdateEvent = null;
        #endregion

        #region Event Method
        internal virtual void OnConnected() { this.OnConnectedEvent?.Invoke(); }
        internal virtual void OnDisconnected() { this.OnDisconnectedEvent?.Invoke(); }
        internal virtual string OnReceiving(ref byte[] _binary) { return this.OnReceivingEvent?.Invoke(ref _binary); }
        internal virtual void OnReceived(JToken _token) { this.OnReceivedEvent?.Invoke(_token); }
        internal virtual void OnLog(params string[] _text) { this.OnLogEvent?.Invoke(_text); }
        internal virtual void OnBookStarted(string _symbol) { this.OnBookStartedEvent?.Invoke(_symbol); }
        internal virtual void OnBookInsert(BookItem _bookItem) { this.OnBookInsertEvent?.Invoke(_bookItem); }
        internal virtual void OnBookUpdate(BookItem _bookItem) { this.OnBookUpdateEvent?.Invoke(_bookItem); }
        internal virtual void OnBookDelete(BookItem _bookItem) { this.OnBookDeleteEvent?.Invoke(_bookItem); }
        internal virtual void OnOrderStarted() { this.OnOrderStartedEvent?.Invoke(); }
        internal virtual void OnOrderInsert(OrderItem _orderItem) { this.OnOrderInsertEvent?.Invoke(_orderItem); }
        internal virtual void OnOrderUpdate(OrderItem _orderItem) { this.OnOrderUpdateEvent?.Invoke(_orderItem); }
        #endregion

        #region Start
        public virtual void Start()
        {
            this.Running = true;
            this.webSocketThread = new Thread(new ThreadStart(this.StartWebSocket));
            this.webSocketThread.Start();
        }
        #endregion

        #region StartWebSocket
        private void StartWebSocket()
        {
            string _bufferedText = "";
            int _bufferedStart = 0;
            int _bufferedLevel = 0;

            while (this.Running)
            {
                Thread.Sleep(10);
                if (this.webSocket == null || this.webSocket.State != WebSocketState.Open)
                {
                    #region 建立连接
                    this.webSocket = new ClientWebSocket();
                    _bufferedText = "";

                    Task _task = this.webSocket.ConnectAsync(new Uri(this.WebSocket), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(1000); }

                    if (_task.Status != TaskStatus.RanToCompletion || this.webSocket.State != WebSocketState.Open)
                    {
                        this.Clear();
                        continue;
                    }

                    this.OnConnected();
                    #endregion
                }
                else
                {
                    #region 接收数据
                    byte[] _buffer = new byte[16384];
                    Task<WebSocketReceiveResult> _task = this.webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(10); }

                    try
                    {
                        if (_task.Status != TaskStatus.RanToCompletion
                            || _task.Result == null
                            || _task.Result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        this.Clear();
                        continue;
                    }
                    #endregion

                    #region 处理数据
                    _bufferedText += this.OnReceivingEvent == null ? Encoding.UTF8.GetString(_buffer, 0, _task.Result.Count) : this.OnReceiving(ref _buffer);

                    while (_bufferedText.Length > 0)
                    {
                        for (int i = _bufferedStart; i < _bufferedText.Length; i++)
                        {
                            if (_bufferedText[i] == '{') { _bufferedLevel++; } else if (_bufferedText[i] == '}') { _bufferedLevel--; }
                            if (_bufferedLevel != 0) { continue; }

                            string _test = "";
                            JObject _json = null;

                            try
                            {
                                _test = _bufferedText.Substring(0, i + 1);
                                _json = JObject.Parse(_test);
                            }
                            catch (Exception _ex)
                            {
                                this.Log($"Receive decode failed - {_ex.Message} - {_test}");
                            }

                            try
                            {
                                this.OnReceived(_json);
                            }
                            catch (Exception _ex)
                            {
                                this.Log($"Received failed - {_ex.Message} - {_test}");
                            }

                            _bufferedText = _bufferedText.Substring(i + 1);
                            _bufferedStart = 0;
                            _bufferedLevel = 0;
                            break;
                        }
                        if (_bufferedLevel > 0) { _bufferedStart = _bufferedText.Length; break; }
                    }
                    #endregion
                }
            }

        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.Running = false;
        }
        #endregion

        #region Clear
        protected virtual void Clear()
        {
            this.OnDisconnected();

            try { this.webSocket?.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).Wait(); } catch { }
            this.webSocket?.Dispose();
            this.webSocket = null;

            this.Balance?.Clear();
            this.Books?.Clear();
            this.Orders?.Clear();
        }
        #endregion

        #region Send
        protected void Send(JObject _json)
        {
            this.Send(_json.ToString(Newtonsoft.Json.Formatting.None));
        }

        protected void Send(string _text)
        {
            if (this.webSocket == null || this.webSocket.State != WebSocketState.Open) { return; }

            this.webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(_text)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }
        #endregion

        protected abstract void ReceivedDepth(string _symbol, string _type, JToken _token);

        protected void Log(string _text) => this.OnLog(this.Name, _text);

        #region Old
        public event WebSocketErrorEventHandle OnErrorEvent = null;
        public event BookChangedEventHandle OnBookChangedEvent = null;
        public event BookUpdatedEventHandle OnBookUpdatedEvent = null;

        internal virtual void OnError(string _code, string _message) { if (this.OnErrorEvent != null) { this.OnErrorEvent(_code, _message); } }
        internal virtual void OnBookChanged(string _side, decimal _price, decimal _amount) { if (this.OnBookChangedEvent != null) { this.OnBookChangedEvent(_side, _price, _amount); } }
        internal virtual void OnBookUpdated(Dictionary<decimal,decimal> _asks, Dictionary<decimal, decimal> _bids) { if (this.OnBookUpdatedEvent != null) { this.OnBookUpdatedEvent(_asks,_bids); } }
        #endregion
    }
}