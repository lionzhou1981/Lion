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
    public class FCoin : MarketBase
    {
        private static string httpUrl = "https://api.fcoin.com/v2";
        private static string wsUrl = "wss://api.fcoin.com/v2/ws";

        private string key;
        private string secret;
        private string[] listens;
        private bool running = false;

        private ClientWebSocket webSocket = null;
        private Thread webSocketThread;
        private Thread balanceThread;

        #region FCoin
        // depth.L20.btcusdt
        public FCoin(string _key, string _secret, params string[] _listens)
        {
            this.key = _key;
            this.secret = _secret;
            this.listens = _listens;
            this.Books = new Books();
        }
        #endregion

        #region Start
        public void Start()
        {
            this.running = true;
            this.webSocketThread = new Thread(new ThreadStart(this.StartWebSocket));
            this.webSocketThread.Start();

            this.balanceThread = new Thread(new ThreadStart(this.StartBalance));
            this.balanceThread.Start();
        }
        #endregion

        #region StartWebSocket
        private void StartWebSocket()
        {
            string _buffered = "";
            int _bufferedStart = 0;
            int _bufferedLevel = 0;

            while (this.running)
            {
                Thread.Sleep(10);
                if (this.webSocket == null || this.webSocket.State != WebSocketState.Open)
                {
                    #region 建立连接
                    this.webSocket = new ClientWebSocket();
                    _buffered = "";

                    Task _task = this.webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(1000); }

                    if (_task.Status != TaskStatus.RanToCompletion || this.webSocket.State != WebSocketState.Open)
                    {
                        this.Clear();
                        continue;
                    }

                    this.Log("Websocket connected");
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
                    _buffered += Encoding.UTF8.GetString(_buffer, 0, _task.Result.Count);

                    while (_buffered.Length > 0)
                    {
                        for (int i = _bufferedStart; i < _buffered.Length; i++)
                        {
                            if (_buffered[i] == '{') { _bufferedLevel++; } else if (_buffered[i] == '}') { _bufferedLevel--; }
                            if (_bufferedLevel != 0) { continue; }

                            string _test = "";
                            JObject _json = null;

                            try
                            {
                                _test = _buffered.Substring(0, i + 1);
                                _json = JObject.Parse(_test);
                            }
                            catch (Exception _ex)
                            {
                                this.Log($"Receive decode failed - {_ex.Message} - {_test}");
                            }

                            try
                            {
                                this.Receive(_json);
                            }
                            catch (Exception _ex)
                            {
                                this.Log($"Received failed - {_ex.Message} - {_test}");
                            }

                            _buffered = _buffered.Substring(i + 1);
                            _bufferedStart = 0;
                            _bufferedLevel = 0;
                            break;
                        }
                        if (_bufferedLevel > 0) { _bufferedStart = _buffered.Length; break; }
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region StartBalance
        private void StartBalance()
        {
            int _loop = 100;
            while (this.running)
            {
                if (_loop > 0) { _loop--; Thread.Sleep(100); continue; }
                _loop = 100;

                JObject _json = this.Call("GET", "/accounts/balance");
                if (_json == null) { continue; }

                int _status = _json.Property("status") == null ? -1 : _json["status"].Value<int>();
                if (_status != 0)
                {
                    this.Log("Balance failed - " + _json.ToString(Newtonsoft.Json.Formatting.None));
                    continue;
                }

                foreach (JObject _item in _json["data"])
                {
                    this.Balance[_item["currency"].Value<string>().ToUpper()] = _item["available"].Value<decimal>();
                }
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.running = false;
        }
        #endregion

        #region Clear
        private void Clear()
        {
            this.Log("Websocket stopped");

            try { this.webSocket?.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).Wait(); } catch { }
            this.webSocket?.Dispose();
            this.webSocket = null;

            this.Balance?.Clear();
            this.Books?.Clear();
            this.Orders?.Clear();
        }
        #endregion

        #region Send
        private void Send(JObject _json)
        {
            this.Send(_json.ToString(Newtonsoft.Json.Formatting.None));
        }

        private void Send(string _text)
        {
            if (this.webSocket == null || this.webSocket.State != WebSocketState.Open) { return; }

            this.webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(_text)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }
        #endregion

        #region Receive
        private void Receive(JObject _json)
        {
            if (_json.Property("type") != null && _json["type"].Value<string>() == "hello")
            {
                this.Books.Clear();

                JObject _sub = new JObject();
                _json.Add("cmd", "sub");
                _json.Add("args", new JArray(this.listens));
                this.Send(_json);
                return;
            }

            string _type = _json.Property("type") == null ? "" : _json["type"].Value<string>();
            if (_type == "") { return; }

            string[] _command = _type.Split('.');
            if (_command[0] == "depth")
            {
                this.Receive_Depth(_command[2], _json);
                return;
            }

            this.Log("RX - " + _json.ToString(Newtonsoft.Json.Formatting.None));
        }
        #endregion

        #region Receive_Depth
        private void Receive_Depth(string _symbol, JObject _json)
        {
            BookItems _askList = new BookItems("ASK");
            BookItems _bidList = new BookItems("BID");

            JArray _asks = _json["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i += 2)
            {
                decimal _price = _asks[i].Value<decimal>();
                decimal _amount = _asks[i + 1].Value<decimal>();
                BookItem _item = new BookItem(_symbol, "ASK", _price, _amount);
                _askList.TryAdd(_item.Id, _item);
            }

            JArray _bids = _json["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i += 2)
            {
                decimal _price = _asks[i].Value<decimal>();
                decimal _amount = _asks[i + 1].Value<decimal>();
                BookItem _item = new BookItem(_symbol, "BID", _price, _amount);
                _bidList.TryAdd(_item.Id, _item);
            }

            this.Books[_symbol, "ASK"] = _askList;
            this.Books[_symbol, "BID"] = _bidList;
        }
        #endregion

        #region Call
        public JObject Call(string _method, string _url, params string[] _values)
        {
            try
            {
                JObject _json = new JObject();
                Dictionary<string, string> _list = new Dictionary<string, string>();
                for (int i = 0; i < _values.Length - 1; i += 2)
                {
                    _list.Add(_values[i], _values[i + 1]);
                    _json.Add(_values[i], _values[i + 1]);
                }
                KeyValuePair<string, string>[] _sorted = _list.ToArray().OrderBy(c => c.Key).ToArray();

                string _ts = (DateTimePlus.DateTime2JSTime(DateTime.UtcNow) * 1000).ToString();
                string _sign = _method + FCoin.httpUrl + _url + _ts;
                for (int i = 0; i < _sorted.Length; i++)
                {
                    _sign += i == 0 ? "" : "&";
                    _sign += _sorted[i].Key + "=" + _sorted[i].Value;
                }
                _sign = Base64.Encode(Encoding.UTF8.GetBytes(_sign));
                _sign = SHA.EncodeHMACSHA1ToBase64(_sign, this.secret);

                HttpClient _http = new HttpClient(10000);
                _http.BeginResponse(_method, FCoin.httpUrl + _url, "");
                _http.Request.ContentType = "application/json";
                _http.Request.Headers.Add("FC-ACCESS-KEY", this.key);
                _http.Request.Headers.Add("FC-ACCESS-SIGNATURE", _sign);
                _http.Request.Headers.Add("FC-ACCESS-TIMESTAMP", _ts);
                _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None)));
                string _result = _http.GetResponseString(Encoding.UTF8);

                return JObject.Parse(_result);
            }
            catch (Exception _ex)
            {
                this.Log($"CALL - {_ex.Message} - {_method} {_url} {string.Join(",", _values)}");
                return null;
            }
        }
        #endregion

        #region OrderCreateMarket
        public string OrderCreateMarket(string _symbol, string _side, decimal _amount)
        {
            JObject _result = this.Call(
                "POST", "/orders",
                "symbol", _symbol,
                "side", _side == "BID" ? "buy" : "sell",
                "type", "market",
                "amount", _amount.ToString());

            if (_result == null) { return ""; }
            if (_result.Property("status") == null || _result["status"].Value<int>() != 0)
            {
                this.Log($"Order create failed - {_side} {_amount}");
                this.Log($"Order create failed - {_result.ToString(Newtonsoft.Json.Formatting.None)}");
                return "";
            }
            return _result["data"].Value<string>();
        }
        #endregion

        private void Log(string _text) => this.OnLog("FCoin", _text);
    }
}
