using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Bittrex : MarketBase
    {
        private List<string> ListPair;

        #region Bittrex
        public Bittrex(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "Bittrex";
            base.WebSocket = "";
            base.HttpUrl = "https://bittrex.com/api";
            base.OnReceivedEvent += Bittrex_OnReceivedEvent;

            this.ListPair = new List<string>();
        }
        private void Bittrex_OnReceivedEvent(JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _symbols)
        {
            try
            {
                string _url = "/v1.1/account/getbalances";

                IList<object> _values = new List<object>();
                _values.Add("apikey");
                _values.Add(this.Key);
                _values.Add("nonce");
                _values.Add(DateTimePlus.DateTime2JSTime(DateTime.UtcNow));

                JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, _values.ToArray());
                if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

                Balances _balances = new Balances();
                foreach (JToken _item in _token)
                {
                    _balances[_item["Currency"].ToString()] = new BalanceItem()
                    {
                        Symbol = _item["Currency"].ToString(),
                        Free = _item["Available"].ToString() == "" ? 0 : _item["Available"].Value<decimal>(),
                        Lock = _item["Pending"].ToString() == "" ? 0 : _item["Pending"].Value<decimal>()
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
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region
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
            string _urlSide = _side == MarketSide.Ask ? "/selllimit" : "/buylimit";
            string _url = "/v1.1/market" + _urlSide;

            IList<object> _values = new List<object>();
            _values.Add("apikey");
            _values.Add(this.Key);
            _values.Add("nonce");
            _values.Add(DateTimePlus.DateTime2JSTime(DateTime.UtcNow));
            _values.Add("market");
            _values.Add(_pair.ToUpper());
            _values.Add("quantity");
            _values.Add(_amount.ToString());
            _values.Add("rate");
            _values.Add(_price.ToString());

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, _values.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["uuid"].Value<string>();
            return _item;
        }
        #endregion

        #region OrderDetail
        public override OrderItem OrderDetail(string _orderId, params string[] _values)
        {
            string _url = "/v1.1/account/getorder";

            IList<object> _list = new List<object>();
            _list.Add("apikey");
            _list.Add(this.Key);
            _list.Add("nonce");
            _list.Add(DateTimePlus.DateTime2JSTime(DateTime.UtcNow));
            _list.Add("uuid");
            _list.Add(_orderId);

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true, _list.ToArray());
            if (_token == null || _token.ToString(Newtonsoft.Json.Formatting.None).Trim() == "{}") { return null; }

            OrderItem _order = new OrderItem();
            _order.Id = _token["OrderUuid"].Value<string>();
            _order.Pair = _token["Exchange"].Value<string>();
            _order.Side = _token["Type"].Value<string>().ToUpper().Split('_')[1] == "BUY" ? MarketSide.Bid : MarketSide.Ask;
            _order.Amount = _token["Quantity"].Value<decimal>();
            _order.Price = _token["Limit"].Value<decimal>();

            decimal _quantityRemaining = _token["QuantityRemaining"].Value<decimal>();
            if (_quantityRemaining >= _order.Amount)
            {
                _order.Status = OrderStatus.New;
            }
            else if (_quantityRemaining <= 0M)
            {
                _order.Status = OrderStatus.Filled;
            }
            else
            {
                _order.Status = OrderStatus.Filling;
            }
            _order.FilledAmount = _order.Amount - _quantityRemaining;
            _order.FilledPrice = _token["PricePerUnit"].Value<decimal>();
            _order.FilledVolume = _order.FilledAmount * _order.FilledPrice;

            return _order;
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

            string _sign = "";
            string _signData = this.HttpUrl + _url + _query;
            byte[] _key = Encoding.UTF8.GetBytes(this.Secret);
            using (var _hmacsha512 = new HMACSHA512(_key))
            {
                _hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(_signData));
                _sign = ByteToString(_hmacsha512.Hash);
            }
            _http.Headers.Add("apisign", _sign);

            return _keyValues;
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            try
            {
                if (_token["success"].Value<bool>()) { return _token["result"]; }

                return null;
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
            string _url = $"/v1.1/public/getorderbook?market={_pair}&type=both";

            this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask);
            this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid);

            string _result = "";
            while (this.Running)
            {
                Thread.Sleep(1000);

                try
                {
                    WebClientPlus _client = new WebClientPlus(5000);
                    _result = _client.DownloadString($"{this.HttpUrl}{_url}");
                    _client.Dispose();

                    JObject _json = JObject.Parse(_result);

                    BookItems _asks = new BookItems(MarketSide.Ask);
                    BookItems _bids = new BookItems(MarketSide.Bid);

                    this.Books.Timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow);

                    foreach (var _item in _json["result"]["sell"])
                    {
                        decimal _price = _item["Rate"].Value<decimal>();
                        decimal _amount = _item["Quantity"].Value<decimal>();
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["result"]["buy"])
                    {
                        decimal _price = _item["Rate"].Value<decimal>();
                        decimal _amount = _item["Quantity"].Value<decimal>();
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
    }
}
