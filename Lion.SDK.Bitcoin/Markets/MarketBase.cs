﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        public string HttpUrl = "";

        public Books Books = new Books();
        public Orders Orders = new Orders();
        public Balances Balances = new Balances();

        protected bool Running = false;
        protected string Key;
        protected string Secret;

        private ClientWebSocket webSocket = null;
        private Thread threadWebSocket;
        private Thread threadBalance;

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

            this.threadWebSocket = new Thread(new ThreadStart(this.StartWebSocket));
            this.threadWebSocket.Start();
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
                        char _startAt = _bufferedText[0] == '{' ? '{' : '[';
                        char _endAt = _bufferedText[0] == '{' ? '}' : ']';

                        for (int i = _bufferedStart; i < _bufferedText.Length; i++)
                        {
                            if (_bufferedText[i] == _startAt) { _bufferedLevel++; } else if (_bufferedText[i] == _endAt) { _bufferedLevel--; }
                            if (_bufferedLevel != 0) { continue; }

                            string _test = "";
                            JToken _json = null;

                            try
                            {
                                _test = _bufferedText.Substring(0, i + 1);
                                _json = JToken.Parse(_test);
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

            this.Balances?.Clear();
            this.Books?.Clear();
            this.Orders?.Clear();
        }
        #endregion

        #region Send
        public void Send(JObject _json)
        {
            this.Send(_json.ToString(Newtonsoft.Json.Formatting.None));
        }

        public void Send(string _text)
        {
            if (this.webSocket == null || this.webSocket.State != WebSocketState.Open) { return; }

            this.webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(_text)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }
        #endregion

        #region HttpCall
        protected JToken HttpCall(HttpCallMethod _callMethod, string _httpMethod, string _url, bool _auth = false, params object[] _keyValues)
        {
            string _result = "";
            try
            {
                HttpClient _http = new HttpClient(5000);
                _http.UserAgent = " User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36";
                if (_auth) { _keyValues = HttpCallAuth(_http, _httpMethod,ref _url, _keyValues); }

                switch (_callMethod)
                {
                    case HttpCallMethod.Get:
                        #region Get
                        {
                            string _query = "";
                            for (int i = 0; i < _keyValues.Length; i += 2)
                            {
                                _query += _query == "" ? "?" : "&";
                                _query += $"{_keyValues[i].ToString()}={HttpUtility.UrlEncode(_keyValues[i + 1].ToString())}";
                            }

                            _http.BeginResponse(_httpMethod, $"{this.HttpUrl}{_url}", "");
                            _http.EndResponse(_query);
                            _result = _http.GetResponseString(Encoding.UTF8);
                            break;
                        }
                    #endregion
                    case HttpCallMethod.Json:
                        #region PostJson
                        {
                            JObject _json = new JObject();
                            for (int i = 0; i < _keyValues.Length; i += 2)
                            {
                                Type _valueType = _keyValues[i + 1].GetType();
                                if (_valueType == typeof(int)) { _json[_keyValues[i]] = (int)_keyValues[i + 1]; }
                                else if (_valueType == typeof(bool)) { _json[_keyValues[i]] = (bool)_keyValues[i + 1]; }
                                else if (_valueType == typeof(long)) { _json[_keyValues[i]] = (long)_keyValues[i + 1]; }
                                else if (_valueType == typeof(JArray)) { _json[_keyValues[i]] = (JArray)_keyValues[i + 1]; }
                                else { _json[_keyValues[i]] = _keyValues[i + 1].ToString(); }
                            }

                            _http.BeginResponse(_httpMethod, $"{this.HttpUrl}{_url}", "");
                            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None)));
                            _result = _http.GetResponseString(Encoding.UTF8);
                            break;
                        }
                    #endregion
                    case HttpCallMethod.Form:
                        string _form = "";
                        for (int i = 0; i < _keyValues.Length; i += 2)
                        {
                            _form += _form == "" ? "" : "&";
                            _form += $"{_keyValues[i].ToString()}={HttpUtility.UrlEncode(_keyValues[i + 1].ToString())}";
                        }
                        _http.BeginResponse(_httpMethod, $"{this.HttpUrl}{_url}", "");
                        _http.EndResponse(Encoding.UTF8.GetBytes(_form));
                        _result = _http.GetResponseString(Encoding.UTF8);
                        break;
                }
            }
            catch (Exception _ex)
            {
                this.OnLog("HTTP", $"{_url} HttpFailed\r\n{string.Join(",", _keyValues)}\r\n{_ex.Message}\r\n{_result}");
                return null;
            }
            try
            {
                return this.HttpCallResult(JToken.Parse(_result));
            }
            catch (Exception _ex)
            {
                this.OnLog("HTTP", $"{_url} JsonFailed\r\n{string.Join(",", _keyValues)}\r\n{_ex.Message}\r\n{_result}");
                return null;
            }
        }
        #endregion

        #region HttpBalanceMonitor
        protected void HttpBalanceMonitor(object _delay = null)
        {
            if (this.threadBalance == null)
            {
                this.threadBalance = new Thread(this.HttpBalanceMonitor);
                this.threadBalance.Start();
                return;
            }

            int _loopDelay = (_delay == null ? 10000 : (int)_delay) / 100;
            int _loop = _loopDelay;
            while (this.Running)
            {
                if (_loop > 0) { _loop--; Thread.Sleep(100); continue; }
                _loop = _loopDelay;

                Balances _balances = this.GetBalances();
                if (_balances != null)
                {
                    this.Balances = _balances;
                }
            }
        }
        #endregion

        protected abstract void ReceivedDepth(string _symbol, string _type, JToken _token);
        protected abstract object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues);
        protected abstract JToken HttpCallResult(JToken _token);
        public abstract Ticker GetTicker(string _symbol);
        public abstract Books GetDepths(string _pair, params string[] _values);
        public abstract Trade[] GetTrades(string _pair, params string[] _values);
        public abstract KLine[] GetKLines(string _pair, KLineType _type, params string[] _values);
        public abstract Balances GetBalances();

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