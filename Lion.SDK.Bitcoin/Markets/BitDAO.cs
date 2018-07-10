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
    public class BitDAO : MarketBase
    {
        #region BitDAO
        public BitDAO(string _key, string _secret)
        {
            base.Key = _key;
            base.Secret = _secret;

            base.Name = "BTD";
            base.WebSocket = "wss://api.bitdao.com/wsv1";
            base.HttpUrl = "https://api.bitdao.com/v1";
            base.OnReceivedEvent += BitDAO_OnReceivedEvent;
        }
        #endregion

        #region BitDAO_OnReceivedEvent
        private void BitDAO_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _channel = _json.Property("channel") == null ? "" : _json["channel"].Value<string>();

            if (_channel != "")
            {
                switch (_channel)
                {
                    case "depth":
                        this.ReceivedDepth(
                            _json["pair"].Value<string>(),
                            ((JObject)_json["data"]).Property("change") == null ? "START" : "UPDATE",
                            _json["data"]); break;

                    default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _pair, int _limit = 10, int _prec = 0)
        {
            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = "depth";
            _json["pair"] = _pair;
            _json["depth"] = _limit;
            _json["prec"] = _prec;
            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _pair, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            if (_type == "START")
            {
                #region START
                JArray _askArray = _json["asks"].Value<JArray>();
                JArray _bidArray = _json["bids"].Value<JArray>();

                BookItems _asks = new BookItems(MarketSide.Ask);
                BookItems _bids = new BookItems(MarketSide.Bid);

                foreach (JArray _item in _askArray)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_pair, MarketSide.Ask, _price, _size, _id);
                    _asks.TryAdd(_id, _bookItem);
                }

                foreach (JArray _item in _bidArray)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_pair, MarketSide.Bid, _price, _size, _id);
                    _bids.TryAdd(_id, _bookItem);
                }

                this.Books[_pair, MarketSide.Ask] = _asks;
                this.Books[_pair, MarketSide.Bid] = _bids;

                this.OnBookStarted(_pair);
                #endregion
            }
            else if (_type == "UPDATE")
            {
                #region UPDATE
                JArray _array = _json["change"].Value<JArray>();
                foreach (JArray _item in _array)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = Math.Abs(_price).ToString();

                    if (_price > 0)
                    {
                        if (_size > 0)
                        {
                            BookItem _bookItem = this.Books[_pair, MarketSide.Bid].Update(_id, _size);
                            if (_bookItem == null)
                            {
                                _bookItem = this.Books[_pair, MarketSide.Bid].Insert(_id, Math.Abs(_price), _size);
                                this.OnBookInsert(_bookItem);
                            }
                            else
                            {
                                this.OnBookUpdate(_bookItem);
                            }
                        }
                        else
                        {
                            BookItem _bookItem = this.Books[_pair, MarketSide.Bid].Delete(_id);
                            if (_bookItem != null)
                            {
                                this.OnBookDelete(_bookItem);
                            }
                        }
                    }
                    else if (_price < 0)
                    {
                        if (_size > 0)
                        {
                            BookItem _bookItem = this.Books[_pair, MarketSide.Ask].Update(_id, _size);
                            if (_bookItem == null)
                            {
                                _bookItem = this.Books[_pair, MarketSide.Ask].Insert(_id, Math.Abs(_price), _size);
                                this.OnBookInsert(_bookItem);
                            }
                            else
                            {
                                this.OnBookUpdate(_bookItem);
                            }
                        }
                        else
                        {
                            BookItem _bookItem = this.Books[_pair, MarketSide.Ask].Delete(_id);
                            if (_bookItem != null)
                            {
                                this.OnBookDelete(_bookItem);
                            }
                        }
                    }
                }
                #endregion
            }
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            Dictionary<string, string> _list = new Dictionary<string, string>();
            for(int i = 0; i < _keyValues.Length; i += 2)
            {
                _list.Add(_keyValues[i].ToString(), _keyValues[i + 1].ToString());
            }
            string _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1)).ToString();
            _list.Add("api_key", base.Key);
            _list.Add("auth_nonce", _time);
            KeyValuePair<string, string>[] _sorted = _list.OrderBy(c => c.Key).ToArray();

            string _sign = "";
            foreach (KeyValuePair<string, string> _item in _sorted) { _sign += _item.Value; }
            _list.Add("auth_sign", MD5.Encode(_sign + base.Secret).ToLower());

            IList<string> _keyValueList = new List<string>();
            foreach (KeyValuePair<string, string> _item in _list)
            {
                _keyValueList.Add(_item.Key);
                _keyValueList.Add(_item.Value);
            }
            return _keyValueList.ToArray();
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
            string _url = $"/v1/ticker?pair={_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.Last = _token["last"].Value<decimal>();
            _ticker.BidPrice = _token["buy"].Value<decimal>();
            _ticker.AskPrice = _token["sell"].Value<decimal>();
            _ticker.High24H = _token["high"].Value<decimal>();
            _ticker.Low24H = _token["low"].Value<decimal>();
            _ticker.Volume24H = _token["vol"].Value<decimal>();
            _ticker.Change24H = _token["dchange"].Value<decimal>();
            _ticker.ChangeRate24H = _token["dchange_pec"].Value<decimal>();

            return _ticker;
        }
        #endregion

        #region GetDepths
        public override Books GetDepths(string _pair, params string[] _values)
        {
            string _url = $"/v1/api/depth?pair={_pair}&depth={_values[0]}&prec={_values[1]}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            BookItems _bidList = new BookItems(MarketSide.Bid);
            JArray _bids = _token["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i++)
            {
                decimal _price = _bids[i][0].Value<decimal>();
                decimal _amount = _bids[i][1].Value<decimal>();
                _bidList.Insert(_price.ToString(), _price, _amount);
            }

            BookItems _askList = new BookItems(MarketSide.Ask);
            JArray _asks = _token["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i++)
            {
                decimal _price = _asks[i][0].Value<decimal>();
                decimal _amount = _asks[i][1].Value<decimal>();
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
            string _url = $"/v1/api/trades?pair={_pair}&last={_values[0]}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item[0].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item[4].Value<bool>() ? MarketSide.Bid : MarketSide.Ask;
                _trade.Price = _item[1].Value<decimal>();
                _trade.Amount = _item[2].Value<decimal>();
                _trade.DateTime = DateTimePlus.JSTime2DateTime(_item[3].Value<long>() / 1000);

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
                case KLineType.M1: _typeText = "1"; break;
                case KLineType.H1: _typeText = "60"; break;
                case KLineType.D1: _typeText = "D"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $"/v1/api/kline?pair={_pair}&type={_typeText}";
            if (_values.Length > 0) { _url += $"&last={_values[0]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token.Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = DateTimePlus.JSTime2DateTime(_token[0].Value<long>() / 1000);
                _line.Pair = _pair;
                _line.Open = _item[1].Value<decimal>();
                _line.Close = _item[4].Value<decimal>();
                _line.High = _item[2].Value<decimal>();
                _line.Low = _item[3].Value<decimal>();
                _line.Volume = _item[5].Value<decimal>();

                _result.Add(_line);
            }

            return _result.ToArray();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances()
        {
            string _url = "/v1/auth/wallet";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token["free"])
            {
                JProperty _property = (JProperty)_item;
                BalanceItem _balance = new BalanceItem()
                {
                    Symbol = _property.Name,
                    Free = _item[_property.Name].Value<decimal>()
                };
            }
            foreach (JToken _item in _token["freezed"])
            {
                JProperty _property = (JProperty)_item;
                _balances[_property.Name].Lock = _item[_property.Name].Value<decimal>();
            }
            this.Balances = _balances;
            return _balances;
        }
        #endregion

        #region MarketTicker
        public JObject MarketTicker(string _symbol)
        {
            string _url = "/bb/api/ticker?pair=" + _symbol;
            JToken _json = this.HttpCall(HttpCallMethod.Get, "GET", _url);

            if (_json == null) { return null; }
            if (_json["code"].Value<int>() != 0) { this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None)); return null; }

            return _json["data"].Value<JObject>();
        }
        #endregion

        #region MarketKLine
        public JArray MarketKLine(string _symbol, string _type = "60", DateTime? _start = null, DateTime? _end = null)
        {
            _start = _start == null ? DateTime.UtcNow.AddDays(-1) : _start;
            _end = _end == null ? DateTime.UtcNow : _end;

            string _url = $"/bb/api/ticker?pair={_symbol}&type={_type}&time_start={DateTimePlus.DateTime2JSTime((DateTime)_start)}&time_end={DateTimePlus.DateTime2JSTime((DateTime)_end)}";
            JToken _json = this.HttpCall(HttpCallMethod.Get, "GET", _url);

            if (_json == null) { return null; }
            if (_json["code"].Value<int>() != 0) { this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None)); return null; }

            return _json["data"].Value<JArray>();
        }
        #endregion

        #region OrderCreate
        public string OrderCreate(string _symbol, OrderType _type, MarketSide _side, decimal _amount, decimal _price = 0M)
        {
            string _url = "/api/v1/order";

            IList<string> _values = new List<string>();
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

            return _token["orderID"].Value<string>();
        }
        #endregion

        #region OrderCancel
        public bool OrderCancel(string _orderId)
        {
            string _url = "/api/v1/cancel/order";

            JToken _token = base.HttpCall(HttpCallMethod.Json, "DELETE", _url, true, "orderID", _orderId);
            if (_token == null) { return false; }

            return _token["ordStatus"].Value<string>() == "Canceled";
        }
        #endregion

        #region OrderStatus
        public JObject OrderStatus(string _symbol, string _id)
        {
            string _url = "/api/v1/cancel/order";
            JToken _json = this.HttpCall(HttpCallMethod.Form, "POST", _url, true,
                "pair", _symbol,
                "order_id", _id
                );

            if (_json == null) { return null; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"].Value<JObject>();
        }
        #endregion

        #region MiningDifficulty
        public JObject MiningDifficulty()
        {
            string _url = "/v1/order/mining/difficulty";
            JToken _json = this.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_json == null) { return null; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"].Value<JObject>();
        }
        #endregion
    }
}