using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Linq;
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
        private IList<string> depths;

        public Fundings Fundings;
        public int BookSize = 0;

        public override string WebSocket
        {
            get
            {
                string _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString();
                string _sign = "GET/realtime" + _nonce;
                _sign = SHA.EncodeHMACSHA256(_sign, base.Secret).ToLower();
                return $"wss://www.bitmex.com/realtime?api-nonce={_nonce}&api-signature={_sign}&api-key={base.Key}";
            }
        }

        #region Bitmex
        public Bitmex(string _key, string _secret) : base(_key, _secret)
        {
            this.depths = new List<string>();
            this.Fundings = new Fundings();

            base.Name = "BMX";
            base.WebSocket = "";
            base.HttpUrl = "https://www.bitmex.com";
            base.OnReceivedEvent += Bitmex_OnReceivedEvent; ;
        }
        #endregion

        #region Start
        public override void Start()
        {
            base.Start();
        }
        #endregion

        #region Clear
        protected override void Clear()
        {
            base.Clear();

            this.depths?.Clear();
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

        public override void SubscribeTicker(string _pair)
        {
            throw new NotImplementedException();
        }

        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            throw new NotImplementedException();
        }

        #region SubscribeDepth
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">Null</param>
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            JObject _json = new JObject();
            _json.Add("op", "subscribe");
            _json.Add("args", new JArray($"orderBookL2:{_pair}"));

            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

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
                BookItems _asks = new BookItems(MarketSide.Ask);
                BookItems _bids = new BookItems(MarketSide.Bid);

                foreach (JObject _item in _list)
                {
                    MarketSide _side = _item["side"].Value<string>().ToUpper() == "BUY" ? MarketSide.Bid : MarketSide.Ask;
                    string _id = _item["id"].Value<string>();
                    decimal _price = _item["price"].Value<decimal>();
                    decimal _amount = _item["size"].Value<decimal>() / _price;

                    BookItem _bookItem = new BookItem(_symbol, _side, _price, _amount, _id);
                    if (_side == MarketSide.Bid) { _bids.TryAdd(_id, _bookItem); }
                    if (_side == MarketSide.Ask) { _asks.TryAdd(_id, _bookItem); }
                }

                this.Books[_symbol, MarketSide.Ask] = _asks;
                this.Books[_symbol, MarketSide.Bid] = _bids;

                if (this.BookSize > 0)
                {
                    this.Books[_symbol, MarketSide.Ask].Resize(this.BookSize);
                    this.Books[_symbol, MarketSide.Bid].Resize(this.BookSize);
                }

                this.depths.Add(_symbol);
                this.OnBookStarted(_symbol);
            }
            else if (_type == "insert")
            {
                foreach (JObject _item in _list)
                {
                    _symbol = _item["symbol"].Value<string>().ToUpper();
                    if (!this.depths.Contains(_symbol)) { continue; }

                    MarketSide _side = _item["side"].Value<string>().ToUpper() == "BUY" ? MarketSide.Bid : MarketSide.Ask;
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
                    if (!this.depths.Contains(_symbol)) { continue; }

                    MarketSide _side = _item["side"].Value<string>().ToUpper() == "BUY" ? MarketSide.Bid : MarketSide.Ask;
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
                    if (!this.depths.Contains(_symbol)) { continue; }

                    MarketSide _side = _item["side"].Value<string>().ToUpper() == "BUY" ? MarketSide.Bid : MarketSide.Ask;
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
                bool _changed = _available != this.Balances["XBT"].Free;
                this.Balances["XBT"].Free = _available;
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
                        JToken _result = this.HttpCall(HttpCallMethod.Get, "GET", "/instrument", false, "symbol", _item["symbol"].Value<string>());
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

                    OrderItem _order = new OrderItem();
                    _order.Id = _id;
                    _order.Pair = _symbol;
                    _order.Side = _side.ToLower() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                    _order.Price = _price;
                    _order.Amount = _amount;
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

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            string _query = "";
            JObject _json = new JObject();
            for (int i = 0; i < _keyValues.Length - 1; i += 2)
            {
                _query += _query == "" ? "" : "&";
                _query += _keyValues[i] + "=" + System.Web.HttpUtility.UrlEncode(_keyValues[i + 1].ToString());

                Type _valueType = _keyValues[i + 1].GetType();
                if (_valueType == typeof(int)) { _json[_keyValues[i]] = (int)_keyValues[i + 1]; }
                else if (_valueType == typeof(bool)) { _json[_keyValues[i]] = (bool)_keyValues[i + 1]; }
                else if (_valueType == typeof(decimal)) { _json[_keyValues[i]] = (decimal)_keyValues[i + 1]; }
                else if (_valueType == typeof(long)) { _json[_keyValues[i]] = (long)_keyValues[i + 1]; }
                else if (_valueType == typeof(JArray)) { _json[_keyValues[i]] = (JArray)_keyValues[i + 1]; }
                else { _json[_keyValues[i]] = _keyValues[i + 1].ToString(); }
            }

            string _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddHours(1)).ToString();
            string _sign = _method;
            if (_method == "GET")
            {
                _url += _query == "" ? "" : "?";
                _url += _query;
                _sign += _url + _nonce;
            }
            else
            {
                _sign += _url + _nonce + _json.ToString(Newtonsoft.Json.Formatting.None);
            }
            Console.WriteLine(_sign);
            _sign = SHA.EncodeHMACSHA256(_sign, base.Secret).ToLower();

            _http.Headers.Add("accept", "application/json");
            _http.Headers.Add("api-key", base.Key);
            _http.Headers.Add("api-signature", _sign);
            _http.Headers.Add("api-expires", _nonce);
            if (_method == "POST") { _http.ContentType="application/json"; }

            return _keyValues;
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            return _token;
        }
        #endregion

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/api/v1/instrument?symbol={_pair}&count=1&reverse=false";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.LastPrice = _token["lastPrice"].Value<decimal>();
            _ticker.BidPrice = _token["bidPrice"].Value<decimal>();
            _ticker.AskPrice = _token["askPrice"].Value<decimal>();
            _ticker.High24H = _token["highPrice"].Value<decimal>();
            _ticker.Low24H = _token["lowPrice"].Value<decimal>();
            _ticker.Volume24H = _token["volume24h"].Value<decimal>();

            return _ticker;
        }
        #endregion

        #region GetDepths
        /// <summary>
        /// GetDepths
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:limit 1:group</param>
        /// <returns></returns>
        public override Books GetDepths(string _pair, params string[] _values)
        {
            string _url = $"/api/v1/orderBook/L2?symbol={_pair}&depth={_values[0]}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            BookItems _bidList = new BookItems(MarketSide.Bid);
            BookItems _askList = new BookItems(MarketSide.Ask);
            foreach (JToken _item in _token)
            {
                string _id = _item["id"].Value<string>();
                decimal _price = _item["price"].Value<decimal>();
                decimal _amount = _item["amount"].Value<decimal>();
                string _side = _item["side"].Value<string>().ToLower();
                if(_side== "Sell")
                {
                    _askList.Insert(_price.ToString(), _price, _amount);
                }
                else
                {
                    _bidList.Insert(_price.ToString(), _price, _amount);
                }
            }
            Books _books = new Books();
            _books[_pair, MarketSide.Bid] = _bidList;
            _books[_pair, MarketSide.Ask] = _askList;

            return _books;
        }
        #endregion

        #region GetTrades
        /// <summary>
        /// GetTrades
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:count 1:start</param>
        /// <returns></returns>
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            string _url = $"/api/v1/trade?symbol={_pair}&count={_values[0]}&reverse=true";
            if (_values.Length > 1) { _url += $"&last={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item["trdMatchID"].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item["side"].Value<string>().ToLower() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                _trade.Price = _item["price"].Value<decimal>();
                _trade.Amount = _item["size"].Value<decimal>();
                _trade.DateTime = _item["DateTime"].Value<DateTime>();

                _result.Add(_trade);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetKLines
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_type"></param>
        /// <param name="_values">0:limit 1:start</param>
        /// <returns></returns>
        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            string _typeText = "";
            switch (_type)
            {
                case KLineType.M1: _typeText = "1m"; break;
                case KLineType.M5: _typeText = "5m"; break;
                case KLineType.H1: _typeText = "h"; break;
                case KLineType.D1: _typeText = "1d"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $"/api/v1/trade/bucketed?binSize={_typeText}&partial=true&symbol={_pair}&count={_values[0]}&reverse=true";
            if (_values.Length > 1) { _url += $"&start={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = _item["timestamp"].Value<DateTime>();
                _line.Pair = _pair;
                _line.Open = _item["open"].Value<decimal>();
                _line.Close = _item["close"].Value<decimal>();
                _line.High = _item["high"].Value<decimal>();
                _line.Low = _item["low"].Value<decimal>();
                _line.Volume = _item["volume"].Value<decimal>();
                _line.Volume2 = _item["homeNotional"].Value<decimal>();
                _line.Count = _item["trades"].Value<decimal>();

                _result.Add(_line);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances()
        {
            string _url = "/api/v1/user/margin";
            JToken _token = base.HttpCall(HttpCallMethod.Json, "GET", _url, true);
            if (_token == null) { return null; }

            string _symbol = _token["currency"].Value<string>();
            decimal _total = _token["walletBalance"].Value<decimal>() * 0.00000001M;
            decimal _free = _token["availableMargin"].Value<decimal>() * 0.00000001M;

            Balances _balances = new Balances();
            _balances[_symbol] = new BalanceItem()
            {
                Symbol = _symbol,
                Free = _free,
                Lock = _total - _free
            };

            return _balances;
        }
        #endregion

        #region OrderCreate
        public string OrderCreate(string _symbol, OrderType _type, MarketSide _side, decimal _amount, decimal _price = 0M)
        {
            string _url = "/api/v1/order";

            IList<object> _values = new List<object>();
            _values.Add("symbol");
            _values.Add(_symbol);
            _values.Add("side");
            _values.Add(_side == MarketSide.Bid ? "Buy" : "Sell");
            _values.Add("orderQty");
            _values.Add(_amount.ToString().Split('.')[0]);
            _values.Add("ordType");
            switch (_type)
            {
                case OrderType.Limit:
                    _values.Add("Limit");
                    _values.Add("price");
                    _values.Add(_price.ToString());
                    break;
                case OrderType.Market:
                    _values.Add("Market");
                    break;
            }

            JToken _token = base.HttpCall(HttpCallMethod.Json, "POST", _url, true, _values.ToArray());
            if (_token == null) { return null; }
            Console.WriteLine(_token.ToString(Newtonsoft.Json.Formatting.None));
            return _token["orderID"].Value<string>();
        }
        #endregion

        #region OrderCancel
        public bool OrderCancel(string _orderId)
        {
            string _url = "/api/v1/order";

            JToken _token = base.HttpCall(HttpCallMethod.Json, "DELETE", _url, true, "orderID", _orderId);
            if (_token == null) { return false; }

            return _token["ordStatus"].Value<string>() == "Canceled";
        }
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
