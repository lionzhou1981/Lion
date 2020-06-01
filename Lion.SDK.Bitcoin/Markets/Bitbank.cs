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
    public class Bitbank : MarketBase
    {
        private List<string> ListPair;
        private string UrlPublic = "https://public.bitbank.cc";
        //private string UrlPrivate = "https://api.bitbank.cc";

        #region Bitbank
        public Bitbank(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "Bitbank";
            base.WebSocket = "";
            base.HttpUrl = "https://api.bitbank.cc";
            base.OnReceivedEvent += Bitbank_OnReceivedEvent;

            this.ListPair = new List<string>();
        }

        private void Bitbank_OnReceivedEvent(JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _pairs)
        {
            string _url = "/v1/user/assets";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            //OrderItem _item = new OrderItem();
            //_item.Id = _token["order_id"].Value<string>();
            //_item.Pair = _token["pair"].Value<string>();
            //_item.Side = _token["side"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
            //_item.Price = _token["price"].Value<decimal>();
            //_item.Amount = _token["start_amount"].Value<decimal>();
            //_item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["ordered_at"].Value<string>().Remove(10)));
            //string _status = _token["status"].Value<string>();
            //switch (_status)
            //{
            //    case "UNFILLED": _item.Status = OrderStatus.New; break;
            //    case "PARTIALLY_FILLED": _item.Status = OrderStatus.Filling; break;
            //    case "FULLY_FILLED": _item.Status = OrderStatus.Filled; break;
            //    case "CANCELED_UNFILLED": case "CANCELED_PARTIALLY_FILLED": _item.Status = OrderStatus.Canceled; break;
            //}
            //return _item;
            return null;
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
            throw new NotImplementedException();
        }
        #endregion

        #region GetTrades
        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region OrderCreate
        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0)
        {
            string _url = "/v1/user/spot/order";

            IList<object> _values = new List<object>();
            _values.Add("pair");
            _values.Add(_pair.ToLower());
            _values.Add("amount");
            _values.Add(_amount.ToString());
            _values.Add("price");
            _values.Add(int.Parse(_price.ToString()));
            _values.Add("side");
            _values.Add(_side == MarketSide.Bid ? "buy" : "sell");
            _values.Add("type");
            switch (_type)
            {
                case OrderType.Limit:
                    _values.Add("limit");
                    break;
                case OrderType.Market:
                    _values.Add("market");
                    break;
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
        public override OrderItem OrderDetail(string _orderId, params string[] _values)
        {
            string _url = "/v1/user/spot/order";
            string _pair = _values[0].ToString();

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, "pair", _pair, "order_id", _orderId);
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["order_id"].Value<string>();
            _item.Pair = _token["pair"].Value<string>();
            _item.Side = _token["side"].Value<string>() == "sell" ? MarketSide.Ask : MarketSide.Bid;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["start_amount"].Value<decimal>();
            _item.CreateTime = DateTimePlus.JSTime2DateTime(long.Parse(_token["ordered_at"].Value<string>().Remove(10)));
            string _status = _token["status"].Value<string>();
            switch (_status)
            {
                case "UNFILLED": _item.Status = OrderStatus.New; break;
                case "PARTIALLY_FILLED": _item.Status = OrderStatus.Filling; break;
                case "FULLY_FILLED": _item.Status = OrderStatus.Filled; break;
                case "CANCELED_UNFILLED": case "CANCELED_PARTIALLY_FILLED": _item.Status = OrderStatus.Canceled; break;
            }
            return _item;
        }
        #endregion

        #region SubscribeDepth
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region SubscribeDepth
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
            throw new NotImplementedException();
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            string _query = "";
            for (int i = 0; i < _keyValues.Length; i += 2)
            {
                _query += _query == "" ? "?" : "&";
                _query += $"{_keyValues[i].ToString()}={_keyValues[i + 1].ToString()}";
            }

            _http.Headers.Add("ACCESS-KEY", base.Key);
            long _nonce = DateTimePlus.DateTime2JSTime(DateTime.UtcNow);
            string _time = _nonce.ToString() + DateTime.UtcNow.Millisecond.ToString();
            _http.Headers.Add("ACCESS-NONCE", _time);
            string _signData = _time + _url + _query;
            string _sign = SHA.EncodeHMACSHA256ToHex(_signData, base.Secret);
            _http.Headers.Add("ACCESS-SIGNATURE", _sign);

            return _keyValues;
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            try
            {
                JObject _json = JObject.Parse(_token["data"].ToString());
                if (_json.Property("code") != null) { return null; }

                return _json;
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            throw new NotImplementedException();
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
            string _url = $"/{_pair}/depth";

            this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask);
            this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid);

            string _result = "";
            while (this.Running)
            {
                Thread.Sleep(1000);

                try
                {
                    WebClientPlus _client = new WebClientPlus(5000);
                    _result = _client.DownloadString($"{this.UrlPublic}{_url}");
                    _client.Dispose();

                    JObject _json = JObject.Parse(_result);

                    BookItems _asks = new BookItems(MarketSide.Ask);
                    BookItems _bids = new BookItems(MarketSide.Bid);

                    this.Books.Timestamp = _json["data"]["timestamp"].Value<long>();

                    foreach (var _item in _json["data"]["asks"])
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["data"]["bids"])
                    {
                        decimal _price = _item[0].Value<decimal>();
                        decimal _amount = _item[1].Value<decimal>();
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
    }
}
