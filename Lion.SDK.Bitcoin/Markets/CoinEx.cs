﻿using System;
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
    public class CoinEx : MarketBase
    {
        private List<string> ListPair;
        private long commandId = 1;

        #region CoinEx
        public CoinEx(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "CEX";
            base.WebSocket = "wss://socket.coinex.com/";
            base.HttpUrl = "https://api.coinex.com";
            base.OnReceivedEvent += CoinEx_OnReceivedEvent;

            this.ListPair = new List<string>();
        }
        #endregion

        #region CoinEx_OnReceivedEvent
        private void CoinEx_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _type = _json.Property("method") == null ? "" : _json["method"].Value<string>();

            switch (_type)
            {
                case "depth.update":
                    this.ReceivedDepth(
                        _json["params"][2].Value<string>(),
                        _json["params"][0].Value<bool>() ? "FULL" : "UPDATE",
                        _json["params"][1].Value<JObject>());
                    break;
                case "state.update":
                    this.ReceivedTicker("", _json["params"]);
                    break;
                default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
            }
        }
        #endregion

        #region Send
        private void Send(string _method, JToken _params)
        {
            JObject _json = new JObject();
            _json["method"] = _method;
            _json["params"] = _params;
            _json["id"] = this.commandId++;

            this.Send(_json);
        }
        #endregion

        #region SubscribeDepth
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:limit(5/10/20) 1:interval('0','0.1','0.01',...)</param>
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

            this.Send("depth.subscribe", new JArray(_pair, _values[0], _values[1]));
        }
        public override void SubscribeDepth(JToken _token, params object[] _values)
        {
            foreach (string _pair in _token)
            {
                this.ListPair.Add(_pair);
            }
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            if (_type == "FULL")
            {
                #region Bid
                IList<KeyValuePair<string, BookItem>> _bidItems = this.Books[_symbol, MarketSide.Bid].ToList();
                BookItems _bidList = new BookItems(MarketSide.Bid);
                JArray _bids = _json["bids"].Value<JArray>();
                for (int i = 0; i < _bids.Count; i++)
                {
                    decimal _price = _bids[i][0].Value<decimal>();
                    decimal _amount = _bids[i][1].Value<decimal>();

                    KeyValuePair<string, BookItem>[] _temps = _bidItems.Where(c => c.Key == _price.ToString()).ToArray();
                    if (_temps.Length == 0)
                    {
                        this.OnBookInsert(_bidList.Insert(_price.ToString(), _price, _amount));
                    }
                    else
                    {
                        BookItem _item = _temps[0].Value;
                        if (_item.Amount != _amount)
                        {
                            _item.Amount = _amount;
                            this.OnBookUpdate(_item);
                        }
                        _bidList.Insert(_item.Id, _item.Price, _amount);
                        _bidItems.Remove(_temps[0]);
                    }
                }
                foreach (KeyValuePair<string, BookItem> _item in _bidItems)
                {
                    this.OnBookDelete(_item.Value);
                }
                #endregion

                #region Ask
                BookItems _askList = new BookItems(MarketSide.Ask);
                IList<KeyValuePair<string, BookItem>> _askItems = this.Books[_symbol, MarketSide.Ask].ToList();
                JArray _asks = _json["asks"].Value<JArray>();
                for (int i = 0; i < _asks.Count; i++)
                {
                    decimal _price = _asks[i][0].Value<decimal>();
                    decimal _amount = _asks[i][1].Value<decimal>();

                    KeyValuePair<string, BookItem>[] _temps = _askItems.Where(c => c.Key == _price.ToString()).ToArray();
                    if (_temps.Length == 0)
                    {
                        this.OnBookInsert(_askList.Insert(_price.ToString(), _price, _amount));
                    }
                    else
                    {
                        BookItem _item = _temps[0].Value;
                        if (_item.Amount != _amount)
                        {
                            _item.Amount = _amount;
                            this.OnBookUpdate(_item);
                        }
                        _askList.Insert(_item.Id, _item.Price, _amount);
                        _askItems.Remove(_temps[0]);
                    }
                }
                foreach (KeyValuePair<string, BookItem> _item in _askItems)
                {
                    this.OnBookDelete(_item.Value);
                }
                #endregion

                this.Books[_symbol, MarketSide.Ask] = _askList;
                this.Books[_symbol, MarketSide.Bid] = _bidList;
            }
            else if (_type == "UPDATE")
            {
                #region Bid
                if (_json.Property("bids") != null)
                {
                    JArray _bids = _json["bids"].Value<JArray>();
                    BookItems _bidList = this.Books[_symbol, MarketSide.Bid];
                    for (int i = 0; i < _bids.Count; i++)
                    {
                        decimal _price = _bids[i][0].Value<decimal>();
                        decimal _amount = _bids[i][1].Value<decimal>();

                        if (_amount == 0)
                        {
                            BookItem _item = _bidList.Delete(_price.ToString());
                            if (_item != null)
                            {
                                this.OnBookDelete(_item);
                            }
                        }
                        else
                        {
                            BookItem _item = _bidList.Update(_price.ToString(), _amount);
                            if (_item != null)
                            {
                                this.OnBookUpdate(_item);
                            }
                            else
                            {
                                this.OnBookInsert(_bidList.Insert(_price.ToString(), _price, _amount));
                            }
                        }
                    }
                }
                #endregion

                #region Ask
                if (_json.Property("asks") != null)
                {
                    BookItems _askList = this.Books[_symbol, MarketSide.Ask];
                    JArray _asks = _json["asks"].Value<JArray>();
                    for (int i = 0; i < _asks.Count; i++)
                    {
                        decimal _price = _asks[i][0].Value<decimal>();
                        decimal _amount = _asks[i][1].Value<decimal>();

                        if (_amount == 0)
                        {
                            BookItem _item = _askList.Delete(_price.ToString());
                            if (_item != null)
                            {
                                this.OnBookDelete(_item);
                            }
                        }
                        else
                        {
                            BookItem _item = _askList.Update(_price.ToString(), _amount);
                            if (_item != null)
                            {
                                this.OnBookUpdate(_item);
                            }
                            else
                            {
                                this.OnBookInsert(_askList.Insert(_price.ToString(), _price, _amount));
                            }
                        }
                    }
                }
                #endregion
            }
        }
        #endregion

        #region SubscribeTicker
        public override void SubscribeTicker(string _pair)
        {
            this.SubscribeTicker(new JArray(_pair));
        }
        public void SubscribeTicker(JArray _json)
        {
            this.Send("state.subscribe", _json);
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            JArray _array = (JArray)_token;
            foreach (JObject _item in _array)
            {
                string _pair = ((JProperty)_item.First).Name;

                Ticker _ticker = new Ticker();
                _ticker.Pair = _pair;
                _ticker.LastPrice = _item[_pair]["last"].Value<decimal>();
                _ticker.High24H = _item[_pair]["high"].Value<decimal>();
                _ticker.Low24H = _item[_pair]["low"].Value<decimal>();
                _ticker.Open24H = _item[_pair]["open"].Value<decimal>();
                _ticker.Volume24H = _item[_pair]["volume"].Value<decimal>();
                _ticker.Volume24H2 = _item[_pair]["deal"].Value<decimal>();
                _ticker.DateTime = DateTime.UtcNow;

                this.Tickers[_pair] = _ticker;
            }
        }
        #endregion

        #region SubscribeBalance
        public void SubscribeBalance()
        {
            base.HttpMonitorBalance();
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            IList<object> _result = new List<object>();

            SortedDictionary<string, string> _list = new SortedDictionary<string, string>();
            for (int i = 0; i < _keyValues.Length; i += 2)
            {
                _list.Add(_keyValues[i].ToString(), _keyValues[i + 1].ToString());

                _result.Add(_keyValues[i]);
                _result.Add(_keyValues[i + 1]);
            }

            string _sign = "";
            long _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1)) * 1000;
            _list.Add("access_id", base.Key);
            _list.Add("tonce", _time.ToString());

            _result.Add("access_id");
            _result.Add(base.Key);
            _result.Add("tonce");
            _result.Add(_time.ToString());

            foreach (KeyValuePair<string, string> _item in _list)
            {
                _sign += _sign == "" ? "" : "&";
                _sign += $"{_item.Key}={_item.Value}";
            }
            _sign += $"&secret_key={base.Secret}";
            _sign = MD5.Encode(_sign).ToUpper();

            _http.Headers.Add("authorization", _sign);

            return _result.ToArray();
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;

            if (_json.Property("code") == null || _json["code"].Value<int>() != 0)
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"];
        }
        #endregion

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/v1/market/ticker?market={_pair.ToUpper()}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false);
            if (_token == null) { return null; }

            try
            {
                JObject _value = _token["ticker"].Value<JObject>();

                Ticker _ticker = new Ticker();
                _ticker.Pair = _pair;
                _ticker.LastPrice = _value["last"].Value<decimal>();
                _ticker.BidPrice = _value["buy"].Value<decimal>();
                _ticker.BidAmount = _value["buy_amount"].Value<decimal>();
                _ticker.AskPrice = _value["sell"].Value<decimal>();
                _ticker.AskAmount = _value["sell_amount"].Value<decimal>();
                _ticker.Open24H = _value["open"].Value<decimal>();
                _ticker.High24H = _value["high"].Value<decimal>();
                _ticker.Low24H = _value["low"].Value<decimal>();
                _ticker.Volume24H = _value["vol"].Value<decimal>();

                return _ticker;
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_token.ToString(Newtonsoft.Json.Formatting.None));
                Console.WriteLine(_ex.ToString());
                return null;
            }
        }
        #endregion

        #region GetDepths
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:merge 1:limit</param>
        /// <returns></returns>
        public override Books GetDepths(string _pair, params string[] _values)
        {
            string _url = $"/v1/market/depth?market={_pair}&merge={_values[0]}";
            if (_values.Length > 1) { _url += $"&limit={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            BookItems _bidList = new BookItems(MarketSide.Bid);
            JArray _bids = _token["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i += 2)
            {
                decimal _price = _bids[i].Value<decimal>();
                decimal _amount = _bids[i + 1].Value<decimal>();
                _bidList.Insert(_price.ToString(), _price, _amount);
            }
            BookItems _askList = new BookItems(MarketSide.Ask);
            JArray _asks = _token["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i += 2)
            {
                decimal _price = _asks[i].Value<decimal>();
                decimal _amount = _asks[i + 1].Value<decimal>();
                _askList.Insert(_price.ToString(), _price, _amount);
            }

            Books _books = new Books();
            _books[_pair, MarketSide.Bid] = _bidList;
            _books[_pair, MarketSide.Ask] = _askList;

            return _books;
        }
        #endregion

        #region GetTrades
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:last_id</param>
        /// <returns></returns>
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            string _url = $"/v1/market/deals?market={_pair}";
            if (_values.Length > 0) { _url += $"&last_id={_values[0]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item["id"].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item["type"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                _trade.Price = _item["price"].Value<decimal>();
                _trade.Amount = _item["amount"].Value<decimal>();
                _trade.DateTime = DateTimePlus.JSTime2DateTime(_item["date"].Value<long>());

                _result.Add(_trade);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetKLines
        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            string _typeText = "";
            switch (_type)
            {
                case KLineType.M1: _typeText = "1min"; break;
                case KLineType.M5: _typeText = "5min"; break;
                case KLineType.M15: _typeText = "15min"; break;
                case KLineType.M30: _typeText = "30min"; break;
                case KLineType.H1: _typeText = "1hour"; break;
                case KLineType.H4: _typeText = "4hour"; break;
                case KLineType.H6: _typeText = "6hour"; break;
                case KLineType.D1: _typeText = "1day"; break;
                case KLineType.D7: _typeText = "1week"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $"/v1/market/kline?market={_pair}&type={_typeText}";
            if (_values.Length > 0) { _url += $"?limit={_values[0]}"; }
            if (_values.Length > 1) { _url += $"&before={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = DateTimePlus.JSTime2DateTime(_item[0].Value<long>());
                _line.Pair = _pair;
                _line.Open = _item[1].Value<decimal>();
                _line.Close = _item[2].Value<decimal>();
                _line.High = _item[3].Value<decimal>();
                _line.Low = _item[4].Value<decimal>();
                _line.Volume = _item[5].Value<decimal>();
                _line.Volume2 = _item[6].Value<decimal>();

                _result.Add(_line);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _pairs)
        {
            string _url = "/v1/balance/";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token)
            {
                JProperty _property = (JProperty)_item;
                _balances[_property.Name] = new BalanceItem()
                {
                    Symbol = _property.Name,
                    Free = _property.Value["available"].Value<decimal>(),
                    Lock = _property.Value["frozen"].Value<decimal>()
                };
            }
            return _balances;
        }
        #endregion

        #region GetMiningStatus
        public MiningStatus GetMiningStatus()
        {
            string _url = "/v1/order/mining/difficulty";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            MiningStatus _status = new MiningStatus();
            _status.Maximum = _token["difficulty"].Value<decimal>();
            _status.Current = _token["prediction"].Value<decimal>();
            _status.DateTime = DateTimePlus.JSTime2DateTime(_token["update_time"].Value<long>());
            return _status;
        }
        #endregion

        #region OrderCreate
        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0M)
        {
            string _url = "/v1/order";
            _url += _type == OrderType.Market ? "/market" : "/limit";

            IList<object> _values = new List<object>();
            _values.Add("amount");
            _values.Add(_amount.ToString());
            _values.Add("type");
            _values.Add(_side == MarketSide.Ask ? "sell" : "buy");
            _values.Add("market");
            _values.Add(_pair.ToUpper());
            if (_type == OrderType.Limit)
            {
                _values.Add("price");
                _values.Add(_price.ToString());
            }

            JToken _token = base.HttpCall(HttpCallMethod.Json, "POST", _url, true, _values.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["order_id"].Value<string>();
            _item.Pair = _token["pair"].Value<string>();
            _item.Side = _token["side"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["start_amount"].Value<decimal>();
            _item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["ordered_at"].Value<string>().Remove(10)));
            return _item;
        }
        #endregion

        #region OrderDetail
        public override OrderItem OrderDetail(string _id, params string[] _values)
        {
            string _url = "/v1/order/status";

            IList<object> _value = new List<object>();
            _value.Add("id");
            _value.Add(_id);
            _value.Add("market");
            _value.Add(_values[0].ToUpper());

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, _value.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["id"].Value<string>();
            _item.Pair = _token["market"].Value<string>();
            _item.Side = _token["type"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["amount"].Value<decimal>();
            _item.FilledAmount = _token["deal_amount"].Value<decimal>();
            _item.FilledPrice = _token["avg_price"].Value<decimal>();
            _item.FilledVolume = _token["deal_money"].Value<decimal>();
            string _status = _token["status"].Value<string>();
            switch (_status)
            {
                case "not_deal": _item.Status = OrderStatus.New; break;
                case "part_deal": _item.Status = OrderStatus.Filling; break;
                case "done": _item.Status = OrderStatus.Filled; break;
            }
            _item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["create_time"].Value<string>().Remove(10)));
            return _item;
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
            string _url = $"/v1/market/depth?market={_pair}&limit=10&merge=0";

            this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask);
            this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid);

            string _result = "";
            while (this.Running)
            {
                Thread.Sleep(1000);

                try
                {
                    WebClientPlus _client = new WebClientPlus(10000);
                    _result = _client.DownloadString($"{this.HttpUrl}{_url}");
                    _client.Dispose();

                    JObject _json = JObject.Parse(_result);

                    BookItems _asks = new BookItems(MarketSide.Ask);
                    BookItems _bids = new BookItems(MarketSide.Bid);

                    this.Books.Timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow);

                    foreach (var _item in _json["data"]["asks"])
                    {
                        decimal _price = decimal.Parse(_item[0].Value<string>(), System.Globalization.NumberStyles.Float);
                        decimal _amount = decimal.Parse(_item[1].Value<string>(), System.Globalization.NumberStyles.Float);
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["data"]["bids"])
                    {
                        decimal _price = decimal.Parse(_item[0].Value<string>(), System.Globalization.NumberStyles.Float);
                        decimal _amount = decimal.Parse(_item[1].Value<string>(), System.Globalization.NumberStyles.Float);
                        _bids.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }

                    BookItems _asksLimit = new BookItems(MarketSide.Ask);
                    BookItems _bidsLimit = new BookItems(MarketSide.Bid);
                    foreach (var _item in _asks.OrderBy(b => b.Value.Price).Take(10))
                    {
                        decimal _price = _item.Value.Price;
                        decimal _amount = _item.Value.Amount;
                        _asksLimit.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _bids.OrderByDescending(b => b.Value.Price).Take(10))
                    {
                        decimal _price = _item.Value.Price;
                        decimal _amount = _item.Value.Amount;
                        _bidsLimit.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }

                    this.Books[_pair, MarketSide.Ask] = _asksLimit;
                    this.Books[_pair, MarketSide.Bid] = _bidsLimit;
                }
                catch (Exception _ex)
                {
                    this.OnLog($"GetBooks Error:{_result}");
                    this.OnLog($"GetBooks Error:{_ex.ToString()}");
                }
            }
        }
        #endregion

        #region OrderCancel
        public OrderItem OrderCancel(string _pair, string _orderId)
        {
            string _url = "/v1/order/pending";

            IList<object> _value = new List<object>();
            _value.Add("id");
            _value.Add(_orderId);
            _value.Add("market");
            _value.Add(_pair.ToUpper());

            JToken _token = base.HttpCall(HttpCallMethod.Get, "DELETE", _url, true, _value.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["id"].Value<string>();
            _item.Pair = _token["market"].Value<string>();
            _item.Side = _token["type"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["amount"].Value<decimal>();
            _item.FilledAmount = _token["deal_amount"].Value<decimal>();
            _item.FilledPrice = _token["avg_price"].Value<decimal>();
            _item.FilledVolume = _token["deal_money"].Value<decimal>();
            string _status = _token["status"].Value<string>();
            switch (_status)
            {
                case "not_deal": _item.Status = OrderStatus.New; break;
                case "part_deal": _item.Status = OrderStatus.Filling; break;
                case "done": _item.Status = OrderStatus.Filled; break;
            }
            _item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["create_time"].Value<string>().Remove(10)));
            return _item;
        }
        #endregion
    }
}
