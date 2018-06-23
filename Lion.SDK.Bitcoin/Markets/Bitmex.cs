using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Bitmex : MarketBase
    {
        private static string httpUrl = "https://www.bitmex.com/api/v1";
        private static string wsUrl = "wss://www.bitmex.com/realtime";

        public Fundings Fundings;
        public IList<string> BookInitialized;

        private string key;
        private string secret;
        private string[] listens;
        private bool running = false;

        private ClientWebSocket webSocket = null;
        private Thread webSocketThread;

        #region Bitmex
        // instrument,margin,order,orderBookL2:XBTUSD
        public Bitmex(string _key, string _secret,params string[] _listens)
        {
            this.key = _key;
            this.secret = _secret;
            this.listens = _listens;
            this.Books = new Books();
            this.BookInitialized = new List<string>();
            this.Fundings = new Fundings();
        }
        #endregion

        #region Start
        public void Start()
        {
            this.running = true;
            this.webSocketThread = new Thread(new ThreadStart(this.StartThread));
            this.webSocketThread.Start();
        }
        #endregion

        #region StartThread
        private void StartThread()
        {
            string _buffered = "";
            int _bufferedStart = 0;
            int _bufferedLevel = 0;

            while (this.running)
            {
                Thread.Sleep(10);
                if (this.webSocket == null || this.webSocket.State != WebSocketState.Open)
                {
                    #region Connect
                    this.webSocket = new ClientWebSocket();
                    _buffered = "";

                    string _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString();
                    string _sign = "GET/realtime" + _nonce;
                    _sign = SHA.EncodeHMACSHA256(_sign, this.secret).ToLower();

                    Task _task = this.webSocket.ConnectAsync(new Uri($"{Bitmex.wsUrl}?api-nonce={_nonce}&api-signature={_sign}&api-key={this.key}"), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(1000); }

                    if (_task.Status != TaskStatus.RanToCompletion || this.webSocket.State != WebSocketState.Open)
                    {
                        this.Clear();
                        continue;
                    }
                    this.Log("Websocket connected");

                    JObject _json = new JObject();
                    _json.Add("op", "subscribe");
                    _json.Add("args", new JArray(this.listens));
                    this.Send(_json);
                    #endregion
                }
                else
                {
                    #region Receiving
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

                    #region Received
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

            this.Clear();
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

            try { this.webSocket?.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None).Wait(); } catch { };
            this.webSocket?.Dispose();
            this.webSocket = null;

            this.Balance?.Clear();
            this.Books?.Clear();
            this.BookInitialized?.Clear();
            this.Orders?.Clear();
            this.Fundings?.Clear();
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
            if (_json.Property("success") != null)
            {
                if (!_json["success"].Value<bool>())
                {
                    this.Log("Receive failed - " + _json.ToString(Newtonsoft.Json.Formatting.None));
                }
                return;
            }

            string _table = _json.Property("table") == null ? "" : _json["table"].Value<string>();
            string _action = _json.Property("action") == null ? "" : _json["action"].Value<string>();
            JArray _list = _json.Property("data") == null ? null : _json["data"].Value<JArray>();

            if (_table == "orderBookL2" && _list != null)
            {
                this.Receive_Book(_action, _list);
                return;
            }
            if (_table == "margin" && _list != null)
            {
                this.Receive_Mergin(_action, _list);
                return;
            }
            if (_table == "instrument" && _list != null)
            {
                this.Receive_Instrument(_action, _list);
                return;
            }
            if (_table == "order" && _list != null)
            {
                this.Receive_Order(_action, _list);
                return;
            }
            this.Log("RX - "+_json.ToString(Newtonsoft.Json.Formatting.None));
        }
        #endregion

        #region Receive_Book
        private void Receive_Book(string _action, JArray _list)
        {
            if (_action == "partial")
            {
                string _symbol = _list[0]["symbol"].Value<string>();
                BookItems _asks = new BookItems("ASK");
                BookItems _bids = new BookItems("BID");

                foreach (JObject _item in _list)
                {
                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();
                    decimal _price = _item["price"].Value<decimal>();
                    decimal _amount = _item["size"].Value<decimal>() / _price;

                    BookItem _bookItem = new BookItem(_symbol, _side, _price, _amount, _id);
                    if (_side == "BID") { _bids.TryAdd(_id, _bookItem); }
                    if (_side == "ASK") { _asks.TryAdd(_id, _bookItem); }

                }

                this.Books[_symbol, "ASK"] = _asks;
                this.Books[_symbol, "BID"] = _bids;
                this.BookInitialized.Add(_symbol);
                this.OnBookStarted(_symbol);
            }
            else if (_action == "insert")
            {
                foreach (JObject _item in _list)
                {
                    string _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.BookInitialized.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();
                    decimal _price = _item["price"].Value<decimal>();
                    decimal _amount = _item["size"].Value<decimal>() / _price;

                    BookItem _bookItem = this.Books[_symbol, _side].Insert(_id, _price, _amount);
                    this.OnBookInsert(_bookItem);
                }
            }
            else if (_action == "update")
            {
                foreach (JObject _item in _list)
                {
                    string _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.BookInitialized.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();
                    decimal _amount = _item["size"].Value<decimal>();

                    BookItem _bookItem = this.Books[_symbol, _side][_id];
                    if (_bookItem == null)
                    {
                        this.Log("Book update failed 1 - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                        return;
                    }

                    _amount = _amount / _bookItem.Price;
                    _bookItem = this.Books[_symbol, _side].Update(_id, _amount);
                    if (_bookItem == null)
                    {
                        this.Log("Book update failed 2 - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                    }
                    else
                    {
                        this.OnBookUpdate(_bookItem);
                    }
                }
            }
            else if (_action == "delete")
            {
                foreach (JObject _item in _list)
                {
                    string _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.BookInitialized.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();

                    BookItem _bookItem = this.Books[_symbol, _side].Delete(_id);
                    if (_bookItem==null)
                    {
                        this.Log("Book delete failed - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                    }
                    else
                    {
                        this.OnBookDelete(_bookItem);
                    }
                }
            }
        }
        #endregion

        #region Receive_Mergin
        private void Receive_Mergin(string _action, JArray _list)
        {
            foreach (JObject _item in _list)
            {
                if (_item.Property("currency") == null || _item["currency"].Value<string>() != "XBt") { continue; }
                if (_item.Property("availableMargin") == null) { continue; }

                decimal _available= _item["availableMargin"].Value<decimal>() * 0.00000001M;
                bool _changed = _available != this.Balance["XBT"];
                this.Balance["XBT"] = _available;
            }
        }
        #endregion

        #region Receive_Instrument
        private void Receive_Instrument(string _action, JArray _list)
        {
            foreach (JObject _item in _list)
            {
                if (_item.Property("symbol") == null
                    || _item["symbol"].Type == JTokenType.Null
                    || _item.Property("fundingRate") == null
                    || _item["fundingRate"].Type == JTokenType.Null) { continue; }

                string _symbol = _item["symbol"].Value<string>();
                decimal _rate = _item["fundingRate"].Value<decimal>();
                DateTime _time = _item["fundingTimestamp"].Value<DateTime>();

                this.Fundings.AddOrUpdate(_symbol, new FundingItem() { Rate = _rate, Time = _time }, (k, v) => { v.Rate = _rate; v.Time = _time; return v; });
            }
        }
        #endregion

        #region Receive_Order
        private void Receive_Order(string _action, JArray _list)
        {
            if (_action == "partial" || _action == "insert")
            {
                #region Init or Insert
                Orders _orders = _action == "partial" ? new Orders() : this.Orders;

                foreach (JObject _item in _list)
                {
                    string _id = _item["orderID"].Value<string>();
                    string _symbol = _item["symbol"].Value<string>();
                    string _side = _item["side"].Value<string>();
                    decimal _price = (_item.Property("price") == null || _item["price"].Type == JTokenType.Null) ? 0M : _item["price"].Value<decimal>();
                    decimal _priceFilled = (_item.Property("avgPx") == null || _item["avgPx"].Type == JTokenType.Null) ? 0M : _item["avgPx"].Value<decimal>();
                    decimal _amount = (_item.Property("simpleOrderQty") == null || _item["simpleOrderQty"].Type == JTokenType.Null) ? 0M : _item["simpleOrderQty"].Value<decimal>();
                    decimal _amountFilled = (_item.Property("simpleCumQty") == null || _item["simpleCumQty"].Type == JTokenType.Null) ? 0M : _item["simpleCumQty"].Value<decimal>();
                    DateTime _createTime = (_item.Property("transactTime") == null || _item["transactTime"].Type == JTokenType.Null) ? DateTime.UtcNow : _item["transactTime"].Value<DateTime>();

                    OrderItem _order = new OrderItem(_id, _symbol, _side, _price, _amount);
                    _order.FilledAmount = _amountFilled;
                    _order.CreateTime = _createTime;

                    _orders.AddOrUpdate(_id, _order, (k, v) => _order);
                    if (_action == "insert") { this.OnOrderInsert(_order); }
                }

                if (_action == "partial")
                {
                    this.Orders = _orders;
                    this.OnOrderStarted();
                }
                #endregion
            }
            else if (_action == "update")
            {
                #region Update
                foreach (JObject _item in _list)
                {
                    string _id = _item["orderID"].Value<string>();
                    string _status = (_item.Property("ordStatus") == null || _item["ordStatus"].Type == JTokenType.Null) ? "" : _item["ordStatus"].Value<string>();

                    OrderItem _order;
                    if (!this.Orders.TryGetValue(_id, out _order))
                    {
                        this.Log("Order update failed - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                        continue;
                    }

                    if (_item.Property("simpleOrderQty") != null && _item["simpleOrderQty"].Value<decimal?>() != null)
                    {
                        _order.Amount = _item["simpleOrderQty"].Value<decimal>();
                    }
                    if (_item.Property("simpleCumQty") != null && _item["simpleCumQty"].Value<decimal?>() != null)
                    {
                        _order.FilledAmount = _item["simpleCumQty"].Value<decimal>();
                    }
                    if (_item.Property("avgPx") != null && _item["avgPx"].Value<decimal?>() != null)
                    {
                        _order.FilledPrice = _item["avgPx"].Value<decimal>();
                    }
                    if (_item.Property("ordStatus") != null && _item["ordStatus"].Type != JTokenType.Null)
                    {
                        switch (_item["ordStatus"].Value<string>().ToLower())
                        {
                            case "new": _order.Status = OrderStatus.New; continue;
                            case "filled": _order.Status = OrderStatus.Filled; continue;
                            case "canceled": _order.Status = OrderStatus.Canceled; continue;
                            default: _order.Status = OrderStatus.Filling; continue;
                        }
                    }

                    this.OnOrderUpdate(_order);
                }
                #endregion
            }
        }
        #endregion

        #region Call
        public JToken Call(string _method, string _url, params string[] _values)
        {
            try
            {
                string _data = "";
                for (int i = 0; i < _values.Length - 1; i += 2)
                {
                    _data += _data == "" ? "" : "&";
                    _data += _values[i] + "=" + System.Web.HttpUtility.UrlEncode(_values[i + 1]);
                }

                string _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString();
                string _sign = _method + "/api/v1";
                if (_method == "GET")
                {
                    _url += _data == "" ? "" : "?";
                    _url += _data;
                    _sign += _url + _nonce;
                }
                else
                {
                    _sign += _url + _nonce + _data;
                }
                _sign = SHA.EncodeHMACSHA256(_sign, this.secret).ToLower();

                HttpClient _http = new HttpClient(10000);
                _http.BeginResponse(_method, Bitmex.httpUrl + _url, "");
                _http.Request.Accept = "application/json";
                _http.Request.Headers.Add("api-key", this.key);
                _http.Request.Headers.Add("api-signature", _sign);
                _http.Request.Headers.Add("api-nonce", _nonce);
                if (_method == "GET")
                {
                    _http.EndResponse();
                }
                else
                {
                    _http.Request.Headers.Add("x-requested-with", "XMLHttpRequest");
                    _http.Request.ContentType = "application/x-www-form-urlencoded";
                    _http.EndResponse(Encoding.UTF8.GetBytes(_data));
                }
                string _result = _http.GetResponseString(Encoding.UTF8);
                if (_result[0] == '[')
                {
                    return JArray.Parse(_result);
                }
                else
                {
                    return JObject.Parse(_result);
                }
            }
            catch (Exception _ex)
            {
                this.Log($"CALL - {_ex.Message} - {_method} {_url} {string.Join(",", _values)}");
                return null;
            }
        }
        #endregion

        #region OrderCreate
        public string OrderCreate(string _symbol, string _side, decimal _price, decimal _amount, string _type = "Limit")
        {
            JObject _result = (JObject)this.Call(
                "POST", "/order",
                "symbol", _symbol,
                "side", _side == "BID" ? "Buy" : "Sell",
                "orderQty", Math.Round(_amount, 0, MidpointRounding.AwayFromZero).ToString(),
                "price", _price.ToString("0.0"),
                "ordType", _type);
            if (_result == null) { return ""; }
            if (_result.Property("error") != null)
            {
                this.Log("Order create failed - " + _result?.ToString(Newtonsoft.Json.Formatting.None));
                return "";
            }
            return _result["orderID"].Value<string>();
        }
        #endregion

        #region OrderCancel
        public bool OrderCancel(string _orderId)
        {
            JToken _jtoken = this.Call("DELETE", "/order", "orderID", _orderId);
            if (_jtoken is JObject)
            {
                JObject _json = (JObject)_jtoken;
                string _status = _json.Property("ordStatus") == null ? "" : _json["ordStatus"].Value<string>();
                if (_status == "Canceled") { return true; }
                if (_json.Property("error") != null)
                {
                    this.Log("Order cancel failed - " + _json.ToString(Newtonsoft.Json.Formatting.None));
                    return false;
                }
            }
            return false;
        }
        #endregion

        private void Log(string _text) => this.OnLog("Bitmex", _text);
    }

    #region Fundings
    public class Fundings : ConcurrentDictionary<string, FundingItem>
    {
        public new FundingItem this[string _symbol]
        {
            get
            {
                FundingItem _item;
                if (this.TryGetValue(_symbol, out _item)) { return _item; }
                return null;
            }
            set
            {
                this.AddOrUpdate(_symbol, value, (k, v) => value);
            }
        }
    }

    public class FundingItem
    {
        public decimal Rate;
        public DateTime Time;
    }
    #endregion
}
