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
        private string key;
        private string secret;
        private IList<string> depthList;

        public Fundings Fundings;
        public int BookSize = 0;

        #region Bitmex
        public Bitmex(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;
            this.depthList = new List<string>();

            base.Name = "BMX";
            base.WebSocket = "";
            base.OnReceivedEvent += Bitmex_OnReceivedEvent; ;
            this.Fundings = new Fundings();
        }
        #endregion

        #region Start
        public override void Start()
        {
            string _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString();
            string _sign = "GET/realtime" + _nonce;
            _sign = SHA.EncodeHMACSHA256(_sign, this.secret).ToLower();
            base.WebSocket = $"wss://www.bitmex.com/realtime?api-nonce={_nonce}&api-signature={_sign}&api-key={this.key}";

            base.Start();
        }
        #endregion

        #region Clear
        protected override void Clear()
        {
            base.Clear();

            this.depthList?.Clear();
            this.Fundings?.Clear();
        }
        #endregion

        #region Bitmex_OnReceivedEvent
        private void Bitmex_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;

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

            switch (_table)
            {
                case "orderBookL2": this.ReceivedDepth("", _action, _list); break;
                case "margin": this.ReceivedMergin(_action, _list); break;
                case "instrument": this.ReceiveInstrument(_action, _list); break;
                case "order": this.ReceiveOrder(_action, _list); break;
                default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
            }
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, string _type = "")
        {
            JObject _json = new JObject();
            _json.Add("op", "subscribe");
            _json.Add("args", new JArray($"orderBookL2:{_symbol}"));

            if (this.Books[_symbol, "BID"] == null) { this.Books[_symbol, "BID"] = new BookItems("BID"); }
            if (this.Books[_symbol, "ASK"] == null) { this.Books[_symbol, "ASK"] = new BookItems("ASK"); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JArray _list = (JArray)_token;

            if (_type == "partial")
            {
                _symbol = _list[0]["symbol"].Value<string>();
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

                if (this.BookSize > 0)
                {
                    this.Books[_symbol, "ASK"].Resize(this.BookSize);
                    this.Books[_symbol, "BID"].Resize(this.BookSize);
                }

                this.depthList.Add(_symbol);
                this.OnBookStarted(_symbol);
            }
            else if (_type == "insert")
            {
                foreach (JObject _item in _list)
                {
                    _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.depthList.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();
                    decimal _price = _item["price"].Value<decimal>();
                    decimal _amount = _item["size"].Value<decimal>() / _price;

                    BookItem _bookItem = this.Books[_symbol, _side].Insert(_id, _price, _amount);
                    this.OnBookInsert(_bookItem);

                    if (this.BookSize > 0 && this.Books[_symbol, _side].Count > this.BookSize * 2)
                    {
                        this.Books[_symbol, _side].Resize(this.BookSize);
                    }
                }
            }
            else if (_type == "update")
            {
                foreach (JObject _item in _list)
                {
                    _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.depthList.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();
                    decimal _amount = _item["size"].Value<decimal>();

                    BookItem _bookItem = this.Books[_symbol, _side][_id];
                    if (_bookItem == null && this.BookSize > 0)
                    {
                        return;
                    }
                    else if (_bookItem == null)
                    {
                        this.Log("Book update failed 1 - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                        return;
                    }

                    _amount = _amount / _bookItem.Price;
                    _bookItem = this.Books[_symbol, _side].Update(_id, _amount);
                    if (_bookItem != null)
                    {
                        this.OnBookUpdate(_bookItem);
                    }
                    else if (this.BookSize == 0)
                    {
                        this.Log("Book update failed 2 - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                    }
                }
            }
            else if (_type == "delete")
            {
                foreach (JObject _item in _list)
                {
                    _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.depthList.Contains(_symbol)) { continue; }

                    string _side = _item["side"].Value<string>().ToUpper() == "BUY" ? "BID" : "ASK";
                    string _id = _item["id"].Value<string>();

                    BookItem _bookItem = this.Books[_symbol, _side].Delete(_id);
                    if (_bookItem == null)
                    {
                        this.OnBookDelete(_bookItem);
                    }
                    else if (this.BookSize == 0)
                    {
                        this.Log("Book delete failed - " + _item.ToString(Newtonsoft.Json.Formatting.None));
                    }
                }
            }
        }
        #endregion

        #region SubscribeMargin
        public void SubscribeMargin()
        {
            JObject _json = new JObject();
            _json.Add("op", "subscribe");
            _json.Add("args", new JArray($"margin"));
            this.Send(_json);
        }
        #endregion

        #region ReceivedMergin
        private void ReceivedMergin(string _action, JArray _list)
        {
            foreach (JObject _item in _list)
            {
                if (_item.Property("currency") == null || _item["currency"].Value<string>() != "XBt") { continue; }
                if (_item.Property("availableMargin") == null) { continue; }

                decimal _available = _item["availableMargin"].Value<decimal>() * 0.00000001M;
                bool _changed = _available != this.Balance["XBT"];
                this.Balance["XBT"] = _available;
            }
        }
        #endregion

        #region SubscribeInstrument
        public void SubscribeInstrument()
        {
            JObject _json = new JObject();
            _json.Add("op", "subscribe");
            _json.Add("args", new JArray($"instrument"));
            this.Send(_json);
        }
        #endregion

        #region ReceiveInstrument
        private void ReceiveInstrument(string _action, JArray _list)
        {
            foreach (JObject _item in _list)
            {
                if (_item.Property("symbol") == null
                    || _item["symbol"].Type == JTokenType.Null
                    || _item.Property("fundingRate") == null
                    || _item["fundingRate"].Type == JTokenType.Null) { continue; }

                string _symbol = _item["symbol"].Value<string>();
                decimal _rate = _item["fundingRate"].Value<decimal>();

                if (_action == "partial" || _action == "insert")
                {
                    FundingItem _funding = new FundingItem();
                    _funding.Rate = _rate;
                    _funding.Time = _item["fundingTimestamp"].Value<DateTime>();
                    if (!this.Fundings.TryAdd(_symbol, _funding))
                    {
                        this.Log($"Receive_Instrument: {_item.ToString(Newtonsoft.Json.Formatting.None)}");
                    }
                }
                else if (_action == "update")
                {
                    FundingItem _funding;
                    if (this.Fundings.TryGetValue(_symbol, out _funding))
                    {
                        JToken _result = this.Call("GET", "/instrument", "symbol", _item["symbol"].Value<string>());
                        if (_result is JArray)
                        {
                            _funding.Rate = _result[0]["fundingRate"].Value<decimal>();
                            _funding.Time = _result[0]["fundingTimestamp"].Value<DateTime>();
                        }
                    }
                    else
                    {
                        this.Log($"Receive_Instrument: {_item.ToString(Newtonsoft.Json.Formatting.None)}");
                    }
                }
            }
        }
        #endregion

        #region SubscribeOrder
        public void SubscribeOrder()
        {
            JObject _json = new JObject();
            _json.Add("op", "subscribe");
            _json.Add("args", new JArray($"order"));
            this.Send(_json);
        }
        #endregion

        #region ReceiveOrder
        private void ReceiveOrder(string _action, JArray _list)
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
                    decimal _amount = (_item.Property("orderQty") == null || _item["orderQty"].Type == JTokenType.Null) ? 0M : _item["orderQty"].Value<decimal>();
                    decimal _amountFilled = (_item.Property("cumQty") == null || _item["cumQty"].Type == JTokenType.Null) ? 0M : _item["cumQty"].Value<decimal>();
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

                    if (_item.Property("orderQty") != null && _item["orderQty"].Value<decimal?>() != null)
                    {
                        _order.Amount = _item["orderQty"].Value<decimal>();
                    }
                    if (_item.Property("cumQty") != null && _item["cumQty"].Value<decimal?>() != null)
                    {
                        _order.FilledAmount = _item["cumQty"].Value<decimal>();
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



        private static string httpUrl = "https://www.bitmex.com/api/v1";

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
            Console.WriteLine(_jtoken.ToString(Newtonsoft.Json.Formatting.None));
            JObject _json = _jtoken is JObject ? (JObject)_jtoken : null;
            if (_json == null && _jtoken is JArray)
            {
                JArray _list = (JArray)_jtoken;
                _json = _list.Count == 1 ? (JObject)_list[0] : null;
            }
            if (_json == null) { return false; }

            string _status = _json.Property("ordStatus") == null ? "" : _json["ordStatus"].Value<string>();
            if (_status == "Canceled") { return true; }

            this.Log(_jtoken.ToString(Newtonsoft.Json.Formatting.None));
            return false;
        }
        #endregion

        #region MarketTicker

        #endregion
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
