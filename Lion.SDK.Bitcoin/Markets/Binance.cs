using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Binance : MarketBase
    {
        #region Binance
        public Binance(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "BIN";
            base.WebSocket = "wss://stream.binance.com:9443/ws/<Symbol>@miniTicker";
            base.HttpUrl = "https://api.binance.com";
            base.OnReceivedEvent += Binance_OnReceivedEvent;
        }

        private void Binance_OnReceivedEvent(JToken _token)
        {
            this.OnLog($"Binance_OnReceivedEvent--{_token.ToString()}");
            JObject _json = (JObject)_token["data"];
            this.OnLog($"Binance_OnReceivedEvent111---");
            if (_token["stream"].Value<string>().Contains("Ticker"))
            {
                this.OnLog($"Binance_OnReceivedEvent222");
                this.ReceivedTicker(_json["s"].Value<string>(), _json);
                this.OnLog($"Binance_OnReceivedEvent333");
            }
            if (_token["stream"].Value<string>().Contains("depth"))
            {
                this.OnLog($"Binance_OnReceivedEvent444---{_json["s"].Value<string>()}+++{_json.ToString()}");
                this.ReceivedDepth(_json["s"].Value<string>(), "", _json);
                this.OnLog($"Binance_OnReceivedEvent555");
            }
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances()
        {
            throw new NotImplementedException();
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

        #region SubscribeDepth
        public override void SubscribeDepth(string _pair, params object[] _values)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region SubscribeTicker
        public override void SubscribeTicker(string _pair)
        {
            JObject _json = new JObject();
            _json["stream"] = $"{_pair}@miniTicker";
            _json["data"] = "";
            this.Send(_json);
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            this.OnLog($"ReceivedDepth111");
            try
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

                this.Books[_symbol, MarketSide.Bid] = _bidList;
                this.Books[_symbol, MarketSide.Ask] = _askList;
            }
            catch (Exception _ex)
            {
            }
            this.OnLog($"ReceivedDepth222");
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            this.OnLog($"ReceivedTicker111");
            try
            {
                Ticker _ticker = new Ticker();
                _ticker.Pair = _symbol;
                _ticker.Open24H = _token["o"].Value<decimal>();
                _ticker.LastPrice = _token["c"].Value<decimal>();
                _ticker.High24H = _token["h"].Value<decimal>();
                _ticker.Low24H = _token["l"].Value<decimal>();
                _ticker.Volume24H = _token["v"].Value<decimal>();
                _ticker.Volume24H2 = _token["q"].Value<decimal>();
                _ticker.DateTime = DateTime.UtcNow;

                base.Tickers[_symbol] = _ticker;
                this.OnLog($"ReceivedTicker222");
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }
}
