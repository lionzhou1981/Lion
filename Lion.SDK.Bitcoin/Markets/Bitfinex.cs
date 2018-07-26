using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Bitfinex :  MarketBase 
    {
        private ConcurrentDictionary<string, string> Channels;

        #region Bitfinex
        public Bitfinex(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "BFX";
            base.WebSocket = "wss://api.bitfinex.com/ws/2";
            base.HttpUrl = "https://api.bitfinex.com";
            base.OnReceivedEvent += Bitfinex_OnReceivedEvent;

            this.Channels = new ConcurrentDictionary<string, string>();
        }
        #endregion

        #region Bitfinex_OnReceivedEvent
        private void Bitfinex_OnReceivedEvent(JToken _token)
        {
            if (_token is JObject)
            {
                JObject _item = (JObject)_token;
                switch (_item["event"].Value<string>())
                {
                    case "ping": this.Send("{\"event\":\"pong\"}"); break;
                    case "subscribed":
                        string _value = $"{_item["channel"].Value<string>()}.{_item["pair"].Value<string>()}.{_item["prec"].Value<string>()}.{_item["freq"].Value<string>()}.{_item["len"].Value<string>()}";
                        this.Channels.AddOrUpdate(_item["chanId"].Value<string>(), _value, (k, v) => _value);
                        break;
                    case "unsubscribed": this.Channels.TryRemove(_item["chanId"].Value<string>(), out string _out); break;
                    default: this.OnLog("RECV", _item.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
            else if (_token is JArray)
            {
                JArray _array = (JArray)_token;
                string _channelId = _array[0].Value<string>();

                if (_array.Count == 2 && _array[1].Type == JTokenType.Array)
                {
                    this.ReceiveSubscribe(_channelId, _array[1].Value<JArray>());
                }
                else if (_array.Count != 2)
                {
                    this.ReceiveSubscribe(_channelId, _array);
                }
            }
        }
        #endregion

        #region ReceiveSubscribe
        public void ReceiveSubscribe(string _channelId, JArray _item)
        {
            if (!this.Channels.TryGetValue(_channelId, out string _value))
            {
                this.OnLog("RECV", $"Channel not found {_channelId}");
                return;
            }

            string[] _channel = _value.Split('.');
            if (_channel[0] == "book")
            {
                this.ReceivedDepth(_channel[1], _item[0].Type == JTokenType.Array ? "START" : "UPDATE", _item);
            }
        }
        #endregion

        #region Clear
        protected override void Clear()
        {
            base.Clear();

            this.Channels?.Clear();
        }
        #endregion

        #region SubscribeDepth
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:prec(P0) 1:freq(F0) 2:len(25,100)</param>
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = "book";
            _json["pair"] = _pair;
            _json["prec"] = _values[0].ToString();
            _json["freq"] = _values[1].ToString();
            _json["len"] = (int)_values[2];

            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JArray _list = (JArray)_token;

            if (_type == "START")
            {
                BookItems _asks = new BookItems(MarketSide.Ask);
                BookItems _bids = new BookItems(MarketSide.Bid);

                foreach (JArray _item in _list)
                {
                    decimal _price = _item[0].Value<decimal>();
                    int _count =  _item[1].Value<int>();
                    decimal _amount =_item[2].Value<decimal>();
                    MarketSide _side = _amount > 0 ? MarketSide.Bid : MarketSide.Ask;
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_symbol, _side, _price, Math.Abs(_amount), _id);
                    if (_side == MarketSide.Bid) { _bids.TryAdd(_id, _bookItem); }
                    if (_side == MarketSide.Ask) { _asks.TryAdd(_id, _bookItem); }
                }

                this.Books[_symbol, MarketSide.Ask] = _asks;
                this.Books[_symbol, MarketSide.Bid] = _bids;
                this.OnBookStarted(_symbol);
            }
            else
            {
                decimal _price = _list[0].Value<decimal>();
                int _count = _list[1].Value<int>();
                decimal _amount = _list[2].Value<decimal>();
                MarketSide _side = _amount > 0 ? MarketSide.Bid : MarketSide.Ask;

                BookItems _items = this.Books[_symbol, _side];
                if (_count == 0)
                {
                    BookItem _item = _items.Delete(_price.ToString());
                    if (_item != null)
                    {
                        this.OnBookDelete(_item);
                    }
                }
                else
                {
                    BookItem _item = _items.Update(_price.ToString(),Math.Abs(_amount));
                    if (_item == null)
                    {
                        _item = _items.Insert(_price.ToString(), _price, _amount);
                        this.OnBookInsert(_item);
                    }
                    else
                    {
                        this.OnBookUpdate(_item);
                    }
                }
            }
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            IList<object> _result = new List<object>();
            string _time = DateTime.Now.Ticks.ToString();
            _result.Add("request");
            _result.Add(_url);
            _result.Add("nonce");
            _result.Add(_time);

            JObject _json = new JObject();
            _json["request"] = _url;
            _json["nonce"] = _time;
            for (int i = 0; i < _keyValues.Length; i += 2)
            {
                Type _valueType = _keyValues[i + 1].GetType();
                if (_valueType == typeof(int)) { _json[_keyValues[i]] = (int)_keyValues[i + 1]; }
                else if (_valueType == typeof(bool)) { _json[_keyValues[i]] = (bool)_keyValues[i + 1]; }
                else if (_valueType == typeof(long)) { _json[_keyValues[i]] = (long)_keyValues[i + 1]; }
                else if (_valueType == typeof(JArray)) { _json[_keyValues[i]] = (JArray)_keyValues[i + 1]; }
                else { _json[_keyValues[i]] = _keyValues[i + 1].ToString(); }

                _result.Add(_keyValues[i]);
                _result.Add(_keyValues[i+1]);
            }

            string _payload = _json.ToString(Newtonsoft.Json.Formatting.None);
            string _payloadBase64 = Base64.Encode(Encoding.UTF8.GetBytes(_payload));
            string _sign = SHA.EncodeHMACSHA384(_payloadBase64, base.Secret);

            _http.Headers.Add("Content-Type", "application/json");
            _http.Headers.Add("Accept", "application/json");
            _http.Headers.Add("X-BFX-APIKEY", base.Key);
            _http.Headers.Add("X-BFX-PAYLOAD", _payloadBase64);
            _http.Headers.Add("X-BFX-SIGNATURE", _sign);

            return _result.ToArray();
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
            string _url = $"/v1/pubticker/{_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.Last = _token["last_price"].Value<decimal>();
            _ticker.BidPrice = _token["bid"].Value<decimal>();
            _ticker.AskPrice = _token["ask"].Value<decimal>();
            _ticker.High24H = _token["high"].Value<decimal>();
            _ticker.Low24H = _token["low"].Value<decimal>();
            _ticker.Volume24H = _token["volume"].Value<decimal>();

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
            string _url = $"/v1/book/{_pair}";
            if (_values.Length > 0) { _url += $"?limit_bids={_values[0]}&limit_asks={_values[1]}"; }
            if (_values.Length > 1) { _url += $"&group={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            BookItems _bidList = new BookItems(MarketSide.Bid);
            JArray _bids = _token["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i++)
            {
                decimal _price = _bids[i]["price"].Value<decimal>();
                decimal _amount = _bids[i]["amount"].Value<decimal>();
                _bidList.Insert(_price.ToString(), _price, _amount);
            }
            BookItems _askList = new BookItems(MarketSide.Ask);
            JArray _asks = _token["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i++)
            {
                decimal _price = _asks[i]["price"].Value<decimal>();
                decimal _amount = _asks[i]["amount"].Value<decimal>();
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
        /// GetTrades
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:limit 1:timestamp</param>
        /// <returns></returns>
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            string _url = $"/v1/trades/{_pair}";
            if (_values.Length > 0) { _url += $"?limit_trades={_values[0]}"; }
            if (_values.Length > 1) { _url += $"&timestamp={_values[1]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item["tid"].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item["type"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                _trade.Price = _item["price"].Value<decimal>();
                _trade.Amount = _item["amount"].Value<decimal>();
                _trade.DateTime = DateTimePlus.JSTime2DateTime(_item[0].Value<long>());

                _result.Add(_trade);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetKLines
        /// <summary>
        /// GetKLines
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_type"></param>
        /// <param name="_values">0:limit 1:sort 2:start 3:end</param>
        /// <returns></returns>
        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            string _typeText = "";
            switch (_type)
            {
                case KLineType.M1: _typeText = "1m"; break;
                case KLineType.M5: _typeText = "5m"; break;
                case KLineType.M15: _typeText = "15m"; break;
                case KLineType.M30: _typeText = "30m"; break;
                case KLineType.H1: _typeText = "1h"; break;
                case KLineType.H6: _typeText = "6h"; break;
                case KLineType.H12: _typeText = "12h"; break;
                case KLineType.D1: _typeText = "1D"; break;
                case KLineType.D7: _typeText = "7D"; break;
                case KLineType.D14: _typeText = "14D"; break;
                case KLineType.MM: _typeText = "1M"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $"/v2/candles/trade:{_typeText}:t{_pair.ToUpper()}/hist";
            if (_values.Length > 0) { _url += $"?limit={_values[0]}"; }
            if (_values.Length > 1) { _url += $"&sort={_values[1]}"; }
            if (_values.Length > 2) { _url += $"&start={_values[2]}"; }
            if (_values.Length > 4) { _url += $"&end={_values[3]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = DateTimePlus.JSTime2DateTime(_item[0].Value<long>() / 1000);
                _line.Pair = _pair;
                _line.Open = _item[1].Value<decimal>();
                _line.Close = _item[2].Value<decimal>();
                _line.High = _item[3].Value<decimal>();
                _line.Low = _item[4].Value<decimal>();
                _line.Volume = _item[5].Value<decimal>();

                _result.Add(_line);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances()
        {
            string _url = "/v1/balances";
            JToken _token = base.HttpCall(HttpCallMethod.Json, "POST", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token.Value<JArray>())
            {
                if (_item["type"].Value<string>() != "exchange") { continue; }

                decimal _free = _item["amount"].Value<decimal>();
                decimal _total = _item["amount"].Value<decimal>();
                _balances[_item["currency"].Value<string>()] = new BalanceItem()
                {
                    Symbol = _item["currency"].Value<string>(),
                    Free = _free,
                    Lock = _total - _free
                };
            }
            return _balances;
        }
        #endregion
    }
}
