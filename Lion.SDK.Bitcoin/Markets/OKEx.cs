using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class OKEx : MarketBase
    {
        private string PassPhrase = "";
        private List<string> ListPair;

        #region OKEx
        public OKEx(string _key, string _secret, string _passPhrase = "123456") : base(_key, _secret)
        {
            base.Name = "OKEx";
            base.WebSocket = $"";
            base.HttpUrl = "https://www.okex.com";
            base.OnReceivedEvent += OKEx_OnReceivedEvent;

            this.ListPair = new List<string>();
            this.PassPhrase = _passPhrase;
        }
        #endregion

        #region OKEx_OnReceivedEvent
        private void OKEx_OnReceivedEvent(JToken _token)
        {
            //JObject _json = (JObject)_token["data"];
            //if (_token["stream"].Value<string>().Contains("ticker"))
            //{
            //    this.ReceivedTicker(_json["s"].Value<string>(), _json);
            //}
            //if (_token["stream"].Value<string>().Contains("depth"))
            //{
            //    this.ReceivedDepth(_json["s"].Value<string>(), "", _json);
            //}
        }
        #endregion

        public override Balances GetBalances(params object[] _symbols)
        {
            string _url = "/api/spot/v3/accounts";
            JToken _token = base.HttpCall(HttpCallMethod.Json, "GET", _url, true);
            if (_token == null) { return null; }

            JArray _jArray = JArray.Parse(_token.ToString());

            Balances _balances = new Balances();
            foreach (JToken _item in _jArray)
            {
                _balances[_item["currency"].ToString()] = new BalanceItem()
                {
                    Symbol = _item["currency"].ToString(),
                    Free = _item["available"].ToString() == "" ? 0 : _item["available"].Value<decimal>(),
                    Lock = _item["hold"].ToString() == "" ? 0 : _item["hold"].Value<decimal>()
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

        public override Books GetDepths(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/api/spot/v3/instruments/{_pair}/ticker";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, false);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.LastPrice = _token["last"].Value<decimal>();
            _ticker.BidPrice = _token["best_bid"].Value<decimal>();
            _ticker.AskPrice = _token["best_ask"].Value<decimal>();
            _ticker.Open24H = _token["open_24h"].Value<decimal>();
            _ticker.High24H = _token["high_24h"].Value<decimal>();
            _ticker.Low24H = _token["low_24h"].Value<decimal>();
            _ticker.Volume24H = _token["quote_volume_24h"].Value<decimal>();

            return _ticker;
        }

        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0)
        {
            string _url = "/api/spot/v3/orders";

            IList<object> _values = new List<object>();
            if (_type == OrderType.Limit)
            {
                _values.Add("price");
                _values.Add(_price.ToString());
                _values.Add("size");
                _values.Add(_amount.ToString());
            }
            else
            {
                _values.Add("type");
                _values.Add("market");
                _values.Add(_side == MarketSide.Ask ? "size" : "notional");
                _values.Add(_amount.ToString());
                //if (_side == MarketSide.Ask)
                //{
                //    _values.Add("size");
                //    _values.Add(_amount.ToString());
                //    _values.Add("notional");
                //    _values.Add("0.01");
                //}
                //else
                //{
                //    //_values.Add("size");
                //    //_values.Add("0");
                //    _values.Add("notional");
                //    _values.Add(_amount.ToString());
                //}
            }
            _values.Add("side");
            _values.Add(_side == MarketSide.Ask ? "sell" : "buy");
            _values.Add("instrument_id");
            _values.Add(_pair);

            JToken _token = base.HttpCall(HttpCallMethod.Json, "POST", _url, true, _values.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }
            JObject _json = (JObject)_token;
            if (_json.ContainsKey("error_code") && _json.ContainsKey("error_message")) { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["order_id"].Value<string>();
            return _item;
        }

        public override OrderItem OrderDetail(string _orderId, params string[] _values)
        {
            string _url = $"/api/spot/v3/orders/{_orderId}?instrument_id={_values[0]}";

            JToken _token = base.HttpCall(HttpCallMethod.Json, "GET", _url, true);
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }
            JObject _json = (JObject)_token;
            if (_json.ContainsKey("code") && _json.ContainsKey("message")) { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["order_id"].Value<string>();
            return _item;
        }

        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            throw new NotImplementedException();
        }

        public override void SubscribeDepth(JToken _token, params object[] _values)
        {
            foreach (string _pair in _token)
            {
                this.ListPair.Add(_pair);
            }
        }

        public override void SubscribeTicker(string _pair)
        {
            throw new NotImplementedException();
        }

        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            IList<object> _result = new List<object>();
            JObject _json = new JObject();
            Dictionary<string, string> _list = new Dictionary<string, string>();

            for (int i = 0; i < _keyValues.Length; i += 2)
            {
                _json[_keyValues[i].ToString()] = _keyValues[i + 1].ToString();

                _result.Add(_keyValues[i]);
                _result.Add(_keyValues[i + 1]);
            }
            string _sign = "";
            string _time = DateTime.UtcNow.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            _sign = $"{_time}{_method.ToUpper()}{_url}";
            if (_keyValues.Length > 0) { _sign += _json.ToString(Newtonsoft.Json.Formatting.None); }
            _list.Add("timestamp", _time);
            _list.Add("method", _method.ToUpper());
            _list.Add("requestPath", _url);

            _sign = SHA.EncodeHMACSHA256ToBase64(_sign, this.Secret);

            _http.Headers.Add("OK-ACCESS-KEY", this.Key);
            _http.Headers.Add("OK-ACCESS-SIGN", _sign);
            _http.Headers.Add("OK-ACCESS-TIMESTAMP", _time);
            _http.Headers.Add("OK-ACCESS-PASSPHRASE", this.PassPhrase);

            return _result.ToArray();
        }

        protected override JToken HttpCallResult(JToken _token)
        {
            return _token;
        }

        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            throw new NotImplementedException();
        }

        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            throw new NotImplementedException();
        }

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
            string _url = $"/api/spot/v3/instruments/{_pair}/book?size=10";

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

                    foreach (var _item in _json["asks"])
                    {
                        decimal _price = decimal.Parse(_item[0].Value<string>(), System.Globalization.NumberStyles.Float);
                        decimal _amount = decimal.Parse(_item[1].Value<string>(), System.Globalization.NumberStyles.Float);
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["bids"])
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

        #region ByteToString
        private string ByteToString(byte[] rgbyBuff)
        {
            string sHexStr = "";
            for (int nCnt = 0; nCnt < rgbyBuff.Length; nCnt++)
            {
                sHexStr += rgbyBuff[nCnt].ToString("x2");
            }
            return (sHexStr);
        }
        #endregion

        public static string EncodeBase64(string code)
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(code);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = code;
            }
            return encode;
        }

        public static string HmacSHA256(string infoStr, string secret)
        {
            byte[] sha256Data = Encoding.UTF8.GetBytes(infoStr);
            byte[] secretData = Encoding.UTF8.GetBytes(secret);
            using (var hmacsha256 = new HMACSHA256(secretData))
            {
                byte[] buffer = hmacsha256.ComputeHash(sha256Data);
                return Convert.ToBase64String(buffer);
            }
        }


    }
}
