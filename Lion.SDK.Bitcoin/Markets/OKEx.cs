using System;
using System.Collections.Generic;
using System.Text;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class OKEx : MarketBase
    {
        #region OKEx
        public OKEx(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "OKEx";
            base.WebSocket = $"";
            base.HttpUrl = "https://www.okex.com";
            base.OnReceivedEvent += OKEx_OnReceivedEvent;
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

        public override Balances GetBalances(params object[] _pairs)
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
            throw new NotImplementedException();
        }

        public override OrderItem OrderDetail(string _orderId, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            throw new NotImplementedException();
        }

        public override void SubscribeDepth(JToken _token, params object[] _values)
        {
            throw new NotImplementedException();
        }

        public override void SubscribeTicker(string _pair)
        {
            throw new NotImplementedException();
        }

        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            throw new NotImplementedException();
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
    }
}
