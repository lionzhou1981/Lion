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
    public class Huobi :  MarketBase 
    {
        #region Huobi
        public Huobi(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "HUO";
            base.WebSocket = "wss://api.huobi.pro/ws";
            base.HttpUrl= "https://api.huobi.pro";
            base.OnReceivingEvent += Huobi_OnReceivingEvent;
            base.OnReceivedEvent += Huobi_OnReceivedEvent;
        }
        #endregion

        #region Huobi_OnReceivingEvent
        private string Huobi_OnReceivingEvent(ref byte[] _binary)
        {
            try
            {
                byte[] _buffer = GZip.Decompress(_binary);
                _binary = new byte[0];
                return Encoding.UTF8.GetString(_buffer);
            }
            catch
            {
                return "";
            }
        }
        #endregion

        #region Huobi_OnReceivedEvent
        private void Huobi_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;

            #region Ping -> Pong
            if (_json.Property("ping") != null)
            {
                this.Send(new JObject() { ["pong"] = _json["ping"].Value<long>() });
                return;
            }
            #endregion

            #region Error -> Log
            if (_json.Property("err-code") != null)
            {
                base.OnLog("ERROR", _json.ToString(Newtonsoft.Json.Formatting.None));
                return;
            }
            #endregion

            #region Depth
            if (_json.Property("ch") != null)
            {
                string[] _command = _json["ch"].Value<string>().Split('.');
                switch (_command[2])
                {
                    case "ticker": this.ReceivedTicker(_command[1], _json["tick"].Value<JObject>()); break;
                    case "depth": this.ReceivedDepth(_command[1], _command[3], _json["tick"].Value<JObject>()); break;
                }
                return;
            }
            #endregion

            this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None));
        }
        #endregion

        #region SubscribeTicker

        public override void SubscribeTicker(string _pair)
        {
            string _id = $"market.{_pair}.kline.1min";

            JObject _json = new JObject();
            _json["sub"] = _id;
            _json["id"] = DateTime.UtcNow.Ticks;

            this.Send(_json);
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            Ticker _ticker = new Ticker();
            _ticker.Pair = _symbol;
            //_ticker
        }
        #endregion

        #region SubscribeDepth
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_symbol"></param>
        /// <param name="_values">0:type(step0-5)</param>
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            string _id = $"market.{_pair}.depth.{_values[0]}";

            JObject _json = new JObject();
            _json["sub"] = _id;
            _json["id"] = DateTime.UtcNow.Ticks;

            if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
            if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

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
            foreach(KeyValuePair<string, BookItem> _item in _bidItems)
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

            this.Books[_symbol, MarketSide.Bid] = _bidList;
            this.Books[_symbol, MarketSide.Ask] = _askList;
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method,ref string _url, object[] _keyValues)
        {
            string _time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T").Replace(":", "%3A");

            if (_method == "GET")
            {
                #region GET
                IList<object> _result = new List<object>();
                string _query = "AccessKeyId=" + base.Key;
                _query += "&SignatureMethod=HmacSHA256";
                _query += "&SignatureVersion=2";
                _query += "&Timestamp=" + _time;

                _result.Add("AccessKeyId");
                _result.Add(base.Key);
                _result.Add("SignatureMethod");
                _result.Add("HmacSHA256");
                _result.Add("SignatureVersion");
                _result.Add("2");
                _result.Add("Timestamp");
                _result.Add(_time);

                for (int i = 0; i < (_keyValues.Length - 1); i += 2)
                {
                    if (_keyValues[i + 1] == null) { continue; }
                    _query += "&" + _keyValues[i] + "=" + _keyValues[i + 1];

                    _result.Add(_keyValues[i]);
                    _result.Add(_keyValues[i + 1]);
                }
                string _sign = "GET\napi.huobi.pro\n" + _url + "\n" + _query;
                string _signed = SHA.EncodeHMACSHA256ToBase64(_sign, base.Secret).Replace("+", "%2B").Replace("=", "%3D");

                _result.Add("Signature");
                _result.Add(_signed);
                return _result.ToArray();
                #endregion
            }
            else
            {
                #region POST
                string _query = "AccessKeyId=" + base.Key;
                _query += "&SignatureMethod=HmacSHA256";
                _query += "&SignatureVersion=2";
                _query += "&Timestamp=" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T").Replace(":", "%3A");
                string _sign = "GET\napi.huobi.pro\n" + _url + "\n" + _query;
                string _signed = SHA.EncodeHMACSHA256ToBase64(_sign, base.Secret).Replace("+", "%2B").Replace("=", "%3D");
                _url = _query + "&Signature=" + _signed;

                _http.Headers.Add("Content-Type", "application/json");
                return _keyValues;
                #endregion
            }
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;

            if (_json.Property("status") == null || _json["status"].Value<string>() != "ok")
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json;
        }
        #endregion

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/market/detail/merged?symbol={_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false);
            if (_token == null || _token["status"] + "" != "ok") { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.Last = _token["tick"]["close"].Value<decimal>();
            _ticker.BidPrice = _token["tick"]["bid"][0].Value<decimal>();
            _ticker.AskPrice = _token["tick"]["ask"][0].Value<decimal>();
            _ticker.Open24H = _token["tick"]["open"].Value<decimal>();
            _ticker.High24H = _token["tick"]["high"].Value<decimal>();
            _ticker.Low24H = _token["tick"]["low"].Value<decimal>();
            _ticker.Volume24H = _token["tick"]["vol"].Value<decimal>();

            return _ticker;
        }
        #endregion

        #region GetDepths
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:step0-5</param>
        /// <returns></returns>
        public override Books GetDepths(string _pair, params string[] _values)
        {
            string _url = $"/market/depth?symbol={_values[0]}&type={_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            BookItems _bidList = new BookItems(MarketSide.Bid);
            JArray _bids = _token["tick"]["bids"].Value<JArray>();
            for (int i = 0; i < _bids.Count; i++)
            {
                decimal _price = _bids[i][0].Value<decimal>();
                decimal _amount = _bids[i][1].Value<decimal>();
                _bidList.Insert(_price.ToString(), _price, _amount);
            }
            BookItems _askList = new BookItems(MarketSide.Ask);
            JArray _asks = _token["tick"]["asks"].Value<JArray>();
            for (int i = 0; i < _asks.Count; i++)
            {
                decimal _price = _asks[i][0].Value<decimal>();
                decimal _amount = _asks[i][1].Value<decimal>();
                _askList.Insert(_price.ToString(), _price, _amount);
            }

            Books _books = new Books();
            _books[_pair, MarketSide.Ask] = _askList;
            _books[_pair, MarketSide.Bid] = _bidList;

            return _books;
        }
        #endregion

        #region GetTrades
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_pair"></param>
        /// <param name="_values">0:size</param>
        /// <returns></returns>
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            string _url = $"/market/history/trade?symbol{_pair}";
            if (_values.Length > 0) { _url += $"&size={_values[0]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<Trade> _result = new List<Trade>();
            JArray _trades = _token["data"].Value<JArray>();
            foreach (JToken _item in _trades)
            {
                Trade _trade = new Trade();
                _trade.Id = _item["id"].Value<string>();
                _trade.Pair = _pair;
                _trade.Side = _item["direction"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
                _trade.Price = _item["price"].Value<decimal>();
                _trade.Amount = _item["amount"].Value<decimal>();
                _trade.DateTime = DateTimePlus.JSTime2DateTime(_item["ts"].Value<long>() / 1000);

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
        /// <param name="_values">0:size</param>
        /// <returns></returns>
        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            string _typeText = "";
            switch (_type)
            {
                case KLineType.M1: _typeText = "1min"; break;
                case KLineType.M5: _typeText = "5min"; break;
                case KLineType.M15: _typeText = "15min"; break;
                case KLineType.M30: _typeText = "30min"; break;
                case KLineType.H1: _typeText = "60min"; break;
                case KLineType.D1: _typeText = "1day"; break;
                case KLineType.D7: _typeText = "1week"; break;
                case KLineType.MM: _typeText = "1mon"; break;
                case KLineType.YY: _typeText = "1year"; break;
                default: throw new Exception($"KLine type:{_type.ToString()} not supported.");
            }

            string _url = $" /market/history/kline?symbol={_pair}&period={_typeText}";
            if (_values.Length > 0) { _url += $"&size={_values[0]}"; }

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            IList<KLine> _result = new List<KLine>();
            JArray _trades = _token["data"].Value<JArray>();
            foreach (JToken _item in _trades)
            {
                KLine _line = new KLine();
                _line.DateTime = DateTimePlus.JSTime2DateTime(_token["id"].Value<long>() / 1000);
                _line.Pair = _pair;
                _line.Open = _item["open"].Value<decimal>();
                _line.Close = _item["close"].Value<decimal>();
                _line.High = _item["high"].Value<decimal>();
                _line.Low = _item["low"].Value<decimal>();
                _line.Count = _item["count"].Value<decimal>();
                _line.Volume = _item["vol"].Value<decimal>();
                _line.Volume2 = _item["amount"].Value<decimal>();

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
            foreach (JToken _item in _token.Value<JArray>())
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
    }
}
