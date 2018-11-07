﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Bithumb : MarketBase
    {
        private string BooksLimit;
        private List<string> ListPair;

        #region Bithumb
        public Bithumb(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "BIT";
            base.WebSocket = "";
            base.HttpUrl = "https://api.bithumb.com";
            base.OnReceivedEvent += Bithumb_OnReceivedEvent;

            ListPair = new List<string>();
        }

        private void Bithumb_OnReceivedEvent(JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override Balances GetBalances()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0)
        {
            try
            {
                string _url = "/trade";
                IList<object> _values = new List<object>();
                _pair = _pair.Contains("_") ? _pair.Split('_')[0] : _pair;

                if (_type == OrderType.Limit)
                {
                    _url += "/place";
                    _values.Add("order_currency");
                    _values.Add(_pair);
                    _values.Add("units");
                    _values.Add(_amount);
                    _values.Add("price");
                    _values.Add(_price);
                    _values.Add("type");
                    _values.Add(_side == MarketSide.Ask ? "Sell" : "Buy");
                }
                else if (_type == OrderType.Market)
                {
                    if (_side == MarketSide.Ask)
                    {
                        _url += "/market_sell";
                    }
                    else
                    {
                        _url += "/market_buy";
                    }
                    _values.Add("currency");
                    _values.Add(_pair);
                    _values.Add("units");
                    _values.Add(_amount);
                }

                JToken _token = base.HttpCall(HttpCallMethod.Form, "POST", _url, true, _values.ToArray());
                if (_token == null) { return null; }

                OrderItem _item = new OrderItem();
                _item.Id = _token["order_id"].Value<string>();
                return _item;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public OrderItem OrderCreate(MarketSide _side, string _pair, decimal _units)
        {
            string _url = _side == MarketSide.Ask ? "/trade/market_sell" : "/trade/market_buy";
            _pair = _pair.Contains("_") ? _pair.Split('_')[0] : _pair;

            IList<object> _values = new List<object>();
            _values.Add("currency");
            _values.Add(_pair);
            _values.Add("units");
            _values.Add(_units);

            JToken _token = base.HttpCall(HttpCallMethod.Form, "POST", _url, true, _values.ToArray());
            if (_token == null) { return null; }

            OrderItem _item = new OrderItem();
            _item.Id = _token["uuid"].Value<string>();
            _item.Pair = _token["market"].Value<string>();
            _item.Side = _token["side"].Value<string>().ToLower() == "bid" ? MarketSide.Bid : MarketSide.Ask;
            _item.Price = _token["price"].Value<decimal>();
            _item.Amount = _token["volume"].Value<decimal>();
            _item.CreateTime = _token["created_at"].Value<DateTime>();
            return _item;
        }

        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            throw new NotImplementedException();
        }

        #region SubscribeDepth
        public override void SubscribeDepth(JToken _token)
        {
            foreach (string _pair in _token)
            {
                this.ListPair.Add(_pair);
            }
        }
        #endregion

        public override void SubscribeTicker(string _pair)
        {
            throw new NotImplementedException();
        }

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            string _query = "";
            for (int i = 0; i < _keyValues.Length - 1; i += 2)
            {
                _query += _query == "" ? "" : "&";
                _query += _keyValues[i] + "=" + _keyValues[i + 1].ToString();
            }
            _query += "&endpoint=" + Uri.EscapeDataString(_url);
            string _timestamp = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString() + DateTime.UtcNow.Millisecond.ToString();
            string _sign = _url + (char)0 + _query + (char)0 + _timestamp;

            byte[] _rgbyKey = Encoding.UTF8.GetBytes(this.Secret);
            using (var _hmacsha512 = new HMACSHA512(_rgbyKey))
            {
                _hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(_sign));
                _sign = ByteToString(_hmacsha512.Hash);
            }

            _sign = Convert.ToBase64String(StringToByte(_sign));
            _http.Headers.Add("Api-Key", base.Key);
            _http.Headers.Add("Api-Sign", _sign);
            _http.Headers.Add("Api-Nonce", _timestamp);
            _http.ContentType = "application/x-www-form-urlencoded";

            return new object[0];
        }
        #endregion

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
            string _pairA = _pair.Contains("_") ? _pair.Split('_')[0] : _pair;
            string _url = $"/public/orderbook/{_pairA}";

            if (this.BooksLimit != "") { _url += $"?count={this.BooksLimit}"; }

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

                    this.Books.Timestamp = _json["data"]["timestamp"].Value<long>();

                    foreach (var _item in _json["data"]["asks"])
                    {
                        decimal _price = _item["price"].Value<decimal>();
                        decimal _amount = _item["quantity"].Value<decimal>();
                        _asks.Insert(_price.ToString(), Math.Abs(_price), _amount);
                    }
                    foreach (var _item in _json["data"]["bids"])
                    {
                        decimal _price = _item["price"].Value<decimal>();
                        decimal _amount = _item["quantity"].Value<decimal>();
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

        private string ByteToString(byte[] rgbyBuff)
        {
            string sHexStr = "";
            for (int nCnt = 0; nCnt < rgbyBuff.Length; nCnt++)
            {
                sHexStr += rgbyBuff[nCnt].ToString("x2");
            }
            return (sHexStr);
        }
        private byte[] StringToByte(string sStr)
        {
            byte[] rgbyBuff = Encoding.UTF8.GetBytes(sStr);

            return (rgbyBuff);
        }
    }
}