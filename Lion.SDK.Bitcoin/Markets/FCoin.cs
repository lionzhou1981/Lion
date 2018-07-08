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
        #region FCoin
        public FCoin(string _key, string _secret)
        {
            this.Key = _key;
            this.Secret = _secret;

            base.Name = "FCN";
            base.WebSocket = "wss://api.fcoin.com/v2/ws";
            base.HttpUrl = "https://api.fcoin.com";
            base.OnReceivedEvent += FCoin_OnReceivedEvent;
        }
        #endregion

        #region FCoin_OnReceivedEvent
        private void FCoin_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _type = _json.Property("type") == null ? "" : _json["type"].Value<string>();

            string[] _command = _type.Split('.');
            switch (_command[0])
            {
                case "depth": this.ReceivedDepth(_command[2], _command[1], _json); break;
                default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
            }
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _pair,string _type)
        {
            JObject _json = new JObject();
            _json.Add("type", "hello");
            _json.Add("ts", DateTimePlus.DateTime2JSTime(DateTime.UtcNow));
            _json.Add("cmd", "sub");
            _json.Add("args", new JArray("depth.L20.btcusdt"));

            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _pair, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            #region Bid
            IList<KeyValuePair<string, BookItem>> _bidItems = this.Books[_pair, MarketSide.Bid].ToList();
            BookItems _bidList = new BookItems(MarketSide.Bid);
            JArray _bids = _json["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i += 2)
            {
                decimal _price = _bids[i].Value<decimal>();
                decimal _amount = _bids[i + 1].Value<decimal>();

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
            IList<KeyValuePair<string, BookItem>> _askItems = this.Books[_pair, MarketSide.Ask].ToList();
            JArray _asks = _json["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i += 2)
            {
                decimal _price = _asks[i].Value<decimal>();
                decimal _amount = _asks[i + 1].Value<decimal>();

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

            this.Books[_pair, MarketSide.Ask] = _askList;
            this.Books[_pair, MarketSide.Bid] = _bidList;
        }
        #endregion

        #region SubscribeBalance
        public void SubscribeBalance()
        {
            base.HttpBalanceMonitor();
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            Dictionary<string, string> _list = new Dictionary<string, string>();
            for (int i = 0; i < _keyValues.Length - 1; i += 2)
            {
                _list.Add(_keyValues[i].ToString(), _keyValues[i + 1].ToString());
            }
            KeyValuePair<string, string>[] _sorted = _list.ToArray().OrderBy(c => c.Key).ToArray();

            string _ts = (DateTimePlus.DateTime2JSTime(DateTime.UtcNow) * 1000).ToString();
            string _sign = _method + this.HttpUrl + _url + _ts;
            for (int i = 0; i < _sorted.Length; i++)
            {
                _sign += i == 0 ? "" : "&";
                _sign += _sorted[i].Key + "=" + _sorted[i].Value;
            }
            _sign = Base64.Encode(Encoding.UTF8.GetBytes(_sign));
            _sign = SHA.EncodeHMACSHA1ToBase64(_sign, base.Secret);

            _http.Headers.Add("FC-ACCESS-KEY", base.Key);
            _http.Headers.Add("FC-ACCESS-SIGNATURE", _sign);
            _http.Headers.Add("FC-ACCESS-TIMESTAMP", _ts);

            return _keyValues;
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;

            if (_json.Property("status") == null || _json["status"].Value<int>() != 0)
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"];
        }
        #endregion

        #region GetServerTime
        public DateTime? GetServerTime()
        {
            string _url = $"/v2/public/server-time";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            return DateTimePlus.JSTime2DateTime(_token.Value<long>() / 1000);
        }
        #endregion

        #region GetSymbols
        public string[] GetSymbols()
        {
            string _url = $"/v2/public/currencies";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            return _token.Value<string[]>();
        }
        #endregion

        #region GetPairs
        public Pairs GetPairs()
        {
            string _url = $"/v2/public/symbols";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            Pairs _pairs = new Pairs();
            foreach (JToken _item in _token.Value<JArray>())
            {
                _pairs[_item["name"].Value<string>()] = new PairItem()
                {
                    Code = _item["name"].Value<string>(),
                    SymbolFrom = _item["base_currency"].Value<string>(),
                    SymbolTo = _item["quote_currency"].Value<string>(),
                    PriceDecimal = _item["price_decimal"].Value<int>(),
                    AmountDecimal = _item["amount_decimal"].Value<int>()
                };
            }
            return _pairs;
        }
        #endregion

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/v2/market/ticker/{_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.Last = _token["ticker"][0].Value<decimal>();
            _ticker.LastAmount = _token["ticker"][1].Value<decimal>();
            _ticker.BidPrice = _token["ticker"][2].Value<decimal>();
            _ticker.BidAmount = _token["ticker"][3].Value<decimal>();
            _ticker.AskPrice = _token["ticker"][4].Value<decimal>();
            _ticker.AskAmount = _token["ticker"][5].Value<decimal>();
            _ticker.Open24H = _token["ticker"][6].Value<decimal>();
            _ticker.High24H = _token["ticker"][7].Value<decimal>();
            _ticker.Low24H = _token["ticker"][8].Value<decimal>();
            _ticker.Volume24H = _token["ticker"][9].Value<decimal>();
            _ticker.Volume24H2 = _token["ticker"][10].Value<decimal>();

            return _ticker;
        }
        #endregion

        #region GetDepths
        public override Books GetDepths(string _pair, params string[] _values)
        {
            string _url = $"/v2/market/depth/{_values[0]}/{_pair}";

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
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            string _url = $"/v2/market/trades/{_pair}";
            if (_values.Length > 0) { _url += $"?limit={_values[0]}"; }
            if (_values.Length > 1) { _url += $"&before={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token.Value<JArray>();
            foreach(JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item["id"].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item["side"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                _trade.Price = _item["price"].Value<decimal>();
                _trade.Amount = _item["amount"].Value<decimal>();
                _trade.DateTime = DateTimePlus.JSTime2DateTime(_item["ts"].Value<long>() / 1000);

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
                case KLineType.M1: _typeText = "M1"; break;
                case KLineType.M5: _typeText = "M5"; break;
                case KLineType.M15: _typeText = "M15"; break;
                case KLineType.M30: _typeText = "M30"; break;
                case KLineType.H1: _typeText = "H1"; break;
                case KLineType.H4: _typeText = "H4"; break;
                case KLineType.H6: _typeText = "H6"; break;
                case KLineType.D1: _typeText = "D1"; break;
                case KLineType.D7: _typeText = "W1"; break;
                case KLineType.MM: _typeText = "WN"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $"/v2/market/candles/{_typeText}/{_pair}";
            if (_values.Length > 0) { _url += $"?limit={_values[0]}"; }
            if (_values.Length > 1) { _url += $"&before={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = DateTimePlus.JSTime2DateTime(_item["id"].Value<long>() / 1000);
                _line.Pair = _pair;
                _line.Open = _item["open"].Value<decimal>();
                _line.Close = _item["close"].Value<decimal>();
                _line.High = _item["high"].Value<decimal>();
                _line.Low = _item["low"].Value<decimal>();
                _line.Count = _item["count"].Value<decimal>();
                _line.Volume = _item["base_vol"].Value<decimal>();
                _line.Volume2 = _item["quote_vol"].Value<decimal>();

                _result.Add(_line);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances()
        {
            string _url = "/v2/accounts/balance";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach(JToken _item in _token.Value<JArray>())
            {
                _balances[_item["currency"].Value<string>()] = new BalanceItem()
                {
                    Symbol = _item["currency"].Value<string>(),
                    Free = _item["available"].Value<decimal>(),
                    Lock = _item["frozen"].Value<decimal>()
                };
            }
            return _balances;
        }
        #endregion

        #region MakeOrder
        public string MakeOrder(string _pair, OrderType _type, MarketSide _side, decimal _amount, decimal _price = 0M)
        {
            IList<string> _values = new List<string>();
            _values.Add("symbol");
            _values.Add(_pair);
            _values.Add("side");
            _values.Add(_side == MarketSide.Bid ? "buy" : "sell");
            _values.Add("type");
            _values.Add(_type == OrderType.Market ? "market" : "limit");
            _values.Add("amount");
            _values.Add(_amount.ToString());
            if(_type== OrderType.Limit)
            {
                _values.Add("price");
                _values.Add(_price.ToString());
            }

            JToken _token = base.HttpCall(HttpCallMethod.PostJson, "POST", "/v2/orders", true, _values.ToArray());
            if (_token == null) { return ""; }

            return _token["id"].Value<string>();
        }
        #endregion
    }
}
