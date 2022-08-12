using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.CryptoCurrency.Market
{
    public class Binance : MarketBase
    {
        ConcurrentQueue<JObject> QueueDepth = new ConcurrentQueue<JObject>();
        long LastUpdateId = 0;
        WebClientPlus WebClient = new WebClientPlus(10000);
        string SnapshotUrl = "https://www.binance.com/api/v1/depth";
        private List<string> ListPair;

        #region Binance
        public Binance(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "BIN";
            base.WebSocket = $"wss://stream.binance.com:9443/stream?streams=";
            base.HttpUrl = "https://api.binance.com";
            base.OnReceivedEvent += Binance_OnReceivedEvent;

            ListPair = new List<string>();
        }
        #endregion

        #region Binance_OnReceivedEvent
        private void Binance_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token["data"];
            if (_token["stream"].Value<string>().Contains("ticker"))
            {
                this.ReceivedTicker(_json["s"].Value<string>(), _json);
            }
            if (_token["stream"].Value<string>().Contains("depth"))
            {
                this.ReceivedDepth(_json["s"].Value<string>(), "", _json);
            }
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _symbols)
        {
            string _url = "/api/v3/account";
            string _timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1)).ToString() + DateTime.UtcNow.Millisecond.ToString();
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, "timestamp", _timestamp);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token["balances"])
            {
                _balances[_item["asset"].ToString()] = new BalanceItem()
                {
                    Symbol = _item["asset"].ToString(),
                    Free = _item["free"].Value<decimal>(),
                    Lock = _item["locked"].Value<decimal>()
                };
            }
            foreach (string _symbol in _symbols)
            {
                if (!_balances.ContainsKey(_symbol.ToUpper()))
                {
                    _balances[_symbol.ToUpper()] = new BalanceItem()
                    {
                        Symbol = _symbol.ToUpper(),
                        Free = 0M,
                        Lock = 0M
                    };
                }
            }
            return _balances;
        }
        #endregion

        #region GetDepths
        public override Books GetDepths(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GetKLines
        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = "/api/v3/ticker/24hr";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false, "symbol", _pair);

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.LastPrice = _token["lastPrice"].Value<decimal>();
            _ticker.LastAmount = _token["lastQty"].Value<decimal>();
            _ticker.AskPrice = _token["askPrice"].Value<decimal>();
            _ticker.AskAmount = _token["askQty"].Value<decimal>();
            _ticker.BidPrice = _token["bidPrice"].Value<decimal>();
            _ticker.BidAmount = _token["bidQty"].Value<decimal>();
            _ticker.Open24H = _token["openPrice"].Value<decimal>();
            _ticker.High24H = _token["highPrice"].Value<decimal>();
            _ticker.Low24H = _token["lowPrice"].Value<decimal>();
            _ticker.Volume24H = _token["volume"].Value<decimal>();
            _ticker.Volume24H2 = _token["quoteVolume"].Value<decimal>();
            _ticker.Change24H = _token["priceChange"].Value<decimal>();
            _ticker.ChangeRate24H = _token["priceChangePercent"].Value<decimal>();
            _ticker.DateTime = DateTimePlus.UnixTime2DateTime(_token["closeTime"].Value<long>());

            return _ticker;
        }
        #endregion

        #region GetTrades
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region SubscribeDepth
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            base.WebSocket += $"{_pair.ToLower()}@depth/";
            this.SnapshotUrl += $"?symbol={_pair}&limit=10";

            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }
        }
        public override void SubscribeDepth(JToken _token, params object[] _values)
        {
            foreach (string _pair in _token)
            {
                this.ListPair.Add(_pair);
            }
        }
        #endregion

        #region SubscribeTicker
        public override void SubscribeTicker(string _pair)
        {
            base.WebSocket += $"{_pair.ToLower()}@ticker/";
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            Dictionary<string, string> _list = new Dictionary<string, string>();
            for (int i = 0; i < _keyValues.Length; i += 2)
            {
                _list.Add(_keyValues[i].ToString(), _keyValues[i + 1].ToString());
            }
            //string _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1)).ToString() + DateTime.UtcNow.Millisecond.ToString();
            //_list.Add("timestamp", _time);

            string _sign = "";
            foreach (var _item in _list)
            {
                _sign += $"{_item.Key}={_item.Value}&";
            }
            if (_sign != "")
            {
                _sign = _sign.Remove(_sign.Length - 1);
            }
            _list.Add("signature", SHA.EncodeHMACSHA256ToHex(_sign, base.Secret).ToLower());

            IList<string> _keyValueList = new List<string>();
            foreach (KeyValuePair<string, string> _item in _list)
            {
                _keyValueList.Add(_item.Key);
                _keyValueList.Add(_item.Value);
            }
            _http.Headers.Add("X-MBX-APIKEY", base.Key);
            _http.Headers.Add("MediaType", "application/x-www-form-urlencoded");

            return _keyValueList.ToArray();
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;
            if (_json.Property("code") != null && _json.Property("msg") != null)
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _token;
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            try
            {
                //this.OnLog($"ReceivedDepth:book.count={this.Books[_symbol, MarketSide.Bid].Count}:{this.Books[_symbol, MarketSide.Ask].Count}");
                //this.OnLog(_token.ToString());
                JObject _json = (JObject)_token;
                this.QueueDepth.Enqueue(_json);
                long _U = _json["U"].Value<long>();
                long _u = _json["u"].Value<long>();
                //this.OnLog($"ReceivedDepth:U={_U},u={_u},e={_json["e"]}");
                if (this.Books[_symbol, MarketSide.Bid].Count < 1 && this.Books[_symbol, MarketSide.Ask].Count < 1)
                {
                    string _snapshot = this.WebClient.DownloadString(this.SnapshotUrl);
                    JObject _jsonSnapshot = JObject.Parse(_snapshot);
                    this.LastUpdateId = _jsonSnapshot["lastUpdateId"].Value<long>();
                    //this.OnLog($"ReceivedDepth:lastUpdateId={this.LastUpdateId}");
                    if (this.LastUpdateId + 1 > _u)
                    {
                        #region Bid
                        JArray _bids = _jsonSnapshot["bids"].Value<JArray>();
                        foreach (JArray _item in _bids)
                        {
                            decimal _price = _item[0].Value<decimal>();
                            decimal _amount = _item[1].Value<decimal>();
                            string _id = Math.Abs(_price).ToString();

                            if (_amount > 0M)
                            {
                                BookItem _bookItem = this.Books[_symbol, MarketSide.Bid].Update(_id, _amount);
                                if (_bookItem == null)
                                {
                                    _bookItem = this.Books[_symbol, MarketSide.Bid].Insert(_id, Math.Abs(_price), _amount);
                                    this.OnBookInsert(_bookItem);
                                }
                                else
                                {
                                    this.OnBookUpdate(_bookItem);
                                }
                            }
                            else
                            {
                                BookItem _bookItem = this.Books[_symbol, MarketSide.Bid].Delete(_id);
                                if (_bookItem != null)
                                {
                                    this.OnBookDelete(_bookItem);
                                }
                            }
                        }
                        #endregion
                        #region Ask
                        JArray _asks = _jsonSnapshot["asks"].Value<JArray>();
                        foreach (JArray _item in _asks)
                        {
                            decimal _price = _item[0].Value<decimal>();
                            decimal _amount = _item[1].Value<decimal>();
                            string _id = Math.Abs(_price).ToString();

                            if (_amount > 0M)
                            {
                                BookItem _bookItem = this.Books[_symbol, MarketSide.Ask].Update(_id, _amount);
                                if (_bookItem == null)
                                {
                                    _bookItem = this.Books[_symbol, MarketSide.Ask].Insert(_id, Math.Abs(_price), _amount);
                                    this.OnBookInsert(_bookItem);
                                }
                                else
                                {
                                    this.OnBookUpdate(_bookItem);
                                }
                            }
                            else
                            {
                                BookItem _bookItem = this.Books[_symbol, MarketSide.Ask].Delete(_id);
                                if (_bookItem != null)
                                {
                                    this.OnBookDelete(_bookItem);
                                }
                            }
                        }
                        #endregion
                        return;
                    }
                }

                if (_U <= this.LastUpdateId + 1 && _u >= this.LastUpdateId + 1)
                {
                    this.Books.Timestamp = _token["E"].Value<long>();
                    //this.OnLog($"ReceivedDepth:true");
                    #region Bid
                    JArray _b = _token["b"].Value<JArray>();
                    foreach (JArray _item in _b)
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
                        string _id = Math.Abs(_price).ToString();

                        if (_amount > 0M)
                        {
                            BookItem _bookItem = this.Books[_symbol, MarketSide.Bid].Update(_id, _amount);
                            if (_bookItem == null)
                            {
                                _bookItem = this.Books[_symbol, MarketSide.Bid].Insert(_id, Math.Abs(_price), _amount);
                                this.OnBookInsert(_bookItem);
                            }
                            else
                            {
                                this.OnBookUpdate(_bookItem);
                            }
                        }
                        else
                        {
                            BookItem _bookItem = this.Books[_symbol, MarketSide.Bid].Delete(_id);
                            if (_bookItem != null)
                            {
                                this.OnBookDelete(_bookItem);
                            }
                        }
                    }
                    #endregion

                    #region Ask
                    JArray _a = _token["a"].Value<JArray>();
                    foreach (JArray _item in _a)
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
                        string _id = Math.Abs(_price).ToString();

                        if (_amount > 0M)
                        {
                            BookItem _bookItem = this.Books[_symbol, MarketSide.Ask].Update(_id, _amount);
                            if (_bookItem == null)
                            {
                                _bookItem = this.Books[_symbol, MarketSide.Ask].Insert(_id, Math.Abs(_price), _amount);
                                this.OnBookInsert(_bookItem);
                            }
                            else
                            {
                                this.OnBookUpdate(_bookItem);
                            }
                        }
                        else
                        {
                            BookItem _bookItem = this.Books[_symbol, MarketSide.Ask].Delete(_id);
                            if (_bookItem != null)
                            {
                                this.OnBookDelete(_bookItem);
                            }
                        }
                    }
                    #endregion
                    this.LastUpdateId = _u;
                }
                else
                {
                    //this.OnLog($"ReceivedDepth:false");
                }

                this.Books[_symbol, MarketSide.Ask].OrderBy(b => b.Value.Price);
                for (int i = this.Books[_symbol, MarketSide.Ask].Count - 1; i >= 0; i--)
                {
                    if (this.Books[_symbol, MarketSide.Ask].Count <= 10) { break; }
                    this.Books[_symbol, MarketSide.Ask].TryRemove(this.Books[_symbol, MarketSide.Ask].ElementAt(i).Key, out BookItem _remove);
                }

                this.Books[_symbol, MarketSide.Bid].OrderByDescending(b => b.Value.Price);
                for (int i = this.Books[_symbol, MarketSide.Bid].Count - 1; i >= 0; i--)
                {
                    if (this.Books[_symbol, MarketSide.Bid].Count <= 10) { break; }
                    this.Books[_symbol, MarketSide.Bid].TryRemove(this.Books[_symbol, MarketSide.Bid].ElementAt(i).Key, out BookItem _remove);
                }

            }
            catch (Exception _ex)
            {
                this.OnLog($"ReceivedDepth---{_ex.ToString()}");
            }
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            try
            {
                Ticker _ticker = new Ticker();
                _ticker.Pair = _symbol;
                _ticker.Open24H = _token["o"].Value<decimal>();
                _ticker.LastPrice = _token["c"].Value<decimal>();
                _ticker.High24H = _token["h"].Value<decimal>();
                _ticker.Low24H = _token["l"].Value<decimal>();
                _ticker.Volume24H = _token["v"].Value<decimal>();
                _ticker.Volume24H2 = _token["q"].Value<decimal>();
                _ticker.AskPrice = _token["a"].Value<decimal>();
                _ticker.AskAmount = _token["A"].Value<decimal>();
                _ticker.BidPrice = _token["b"].Value<decimal>();
                _ticker.BidAmount = _token["B"].Value<decimal>();
                _ticker.DateTime = DateTime.UtcNow;

                base.Tickers[_symbol] = _ticker;
            }
            catch (Exception _ex)
            {
                this.OnLog($"ReceivedTicker---{_ex.ToString()}");
            }
        }
        #endregion

        #region OrderCreate
        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0M)
        {
            string _url = "/api/v3/order";

            IList<object> _values = new List<object>();
            _values.Add("symbol");
            _values.Add(_pair.ToUpper());
            _values.Add("side");
            _values.Add(_side == MarketSide.Bid ? "BUY" : "SELL");
            _values.Add("type");
            switch (_type)
            {
                case OrderType.Limit:
                    _values.Add("LIMIT");
                    _values.Add("price");
                    _values.Add(_price.ToString());
                    _values.Add("timeInForce");
                    _values.Add("GTC");
                    break;
                case OrderType.Market:
                    _values.Add("MARKET");
                    break;
            }
            _values.Add("quantity");
            _values.Add(_amount);
            _values.Add("timestamp");
            long _timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1));
            _values.Add(_timestamp.ToString() + DateTime.UtcNow.Millisecond.ToString());

            JToken _token = base.HttpCall(HttpCallMethod.Form, "POST", _url, true, _values.ToArray());
            if (_token == null || _token.ToString().Trim() == "{}") { return null; }
            this.OnLog(_token.ToString(Newtonsoft.Json.Formatting.None));

            OrderItem _item = new OrderItem();
            _item.Id = _token["orderId"].Value<string>();
            _item.Pair = _token["symbol"].Value<string>();
            _item.Side = _token["side"].Value<string>() == "SELL" ? MarketSide.Ask : MarketSide.Bid;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["origQty"].Value<decimal>();
            _item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["transactTime"].Value<string>().Remove(10)));
            return _item;
        }
        #endregion

        #region OrderDetail
        public override OrderItem OrderDetail(string _id, params string[] _values)
        {
            string _url = "/api/v3/order";

            IList<object> _list = new List<object>();
            _list.Add("symbol");
            string _pair = _values[0].Replace("_", "").ToUpper();
            _list.Add(_pair);
            _list.Add("orderId");
            _list.Add(_id);
            _list.Add("timestamp");
            long _timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1));
            _list.Add(_timestamp.ToString() + DateTime.UtcNow.Millisecond.ToString());

            JToken _token = this.HttpCall(HttpCallMethod.Get, "GET", _url, true, "symbol", _pair, "orderId", _id, "timestamp", _timestamp.ToString() + DateTime.UtcNow.Millisecond.ToString());
            if (_token == null) { return null; }

            OrderItem _order = new OrderItem();
            _order.Id = _token["orderId"].Value<string>();
            _order.Pair = _token["symbol"].Value<string>();
            _order.Side = _token["side"].Value<string>().ToUpper() == "BUY" ? MarketSide.Bid : MarketSide.Ask;
            _order.Amount = _token["origQty"].Value<decimal>();
            _order.Price = _token["price"].Value<decimal>();
            string _status = _token["status"].Value<string>();
            switch (_status)
            {
                case "NEW": _order.Status = OrderStatus.New; break;
                case "PARTIALLY_FILLED": _order.Status = OrderStatus.Filling; break;
                case "FILLED": _order.Status = OrderStatus.Filled; break;
                case "CANCELED": _order.Status = OrderStatus.Canceled; break;
            }
            _order.FilledAmount = _token["executedQty"].Value<decimal>();
            _order.FilledVolume = _token["cummulativeQuoteQty"].Value<decimal>();

            return _order;
        }
        #endregion

        #region Start
        public override void Start()
        {
            this.Running = true;

            foreach (string _pair in this.ListPair)
            {
                Thread _thread = new Thread(new ParameterizedThreadStart(this.BooksRunner));
                this.Threads.Add($"GetBooks_{_pair}", _thread);
                _thread.Start(_pair);
            }
        }
        #endregion

        #region BooksRunner
        private void BooksRunner(object _object)
        {
            string _pair = _object.ToString();
            string _url = $"/api/v1/depth?symbol={_pair}&limit=10";

            this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask);
            this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid);

            string _result = "";
            while (this.Running)
            {
                Thread.Sleep(1000);

                try
                {
                    WebClientPlus _client = new WebClientPlus(5000);
                    _result = _client.DownloadString($"{base.HttpUrl}{_url}");
                    _client.Dispose();

                    JObject _json = JObject.Parse(_result);

                    BookItems _asks = new BookItems(MarketSide.Ask);
                    BookItems _bids = new BookItems(MarketSide.Bid);

                    this.Books.Timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow);

                    foreach (var _item in _json["asks"])
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["bids"])
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
                        _bids.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }

                    this.Books[_pair, MarketSide.Ask] = _asks;
                    this.Books[_pair, MarketSide.Bid] = _bids;
                }
                catch (Exception _ex)
                {
                    this.OnLog($"GetBooks Error:{_result}");
                    this.OnLog($"GetBooks Error:{_ex.ToString()}");
                }
            }
        }
        #endregion

    }
}
