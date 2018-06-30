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
        private string key;
        private string secret;
        private Thread threadBalance;

        #region FCoin
        public FCoin(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;

            base.Name = "FCN";
            base.WebSocket = "wss://api.fcoin.com/v2/ws";
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

        #region Start
        public override void Start()
        {
            base.Start();

            this.threadBalance = new Thread(new ThreadStart(this.StartBalance));
            this.threadBalance.Start();
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol,string _type)
        {
            JObject _json = new JObject();
            _json.Add("type", "hello");
            _json.Add("ts", DateTimePlus.DateTime2JSTime(DateTime.UtcNow));
            _json.Add("cmd", "sub");
            _json.Add("args", new JArray("depth.L20.btcusdt"));

            if (this.Books[_symbol, "BID"] == null) { this.Books[_symbol, "BID"] = new BookItems("BID"); }
            if (this.Books[_symbol, "ASK"] == null) { this.Books[_symbol, "ASK"] = new BookItems("ASK"); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            #region Bid
            IList<KeyValuePair<string, BookItem>> _bidItems = this.Books[_symbol, "BID"].ToList();
            BookItems _bidList = new BookItems("BID");
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
            BookItems _askList = new BookItems("ASK");
            IList<KeyValuePair<string, BookItem>> _askItems = this.Books[_symbol, "ASK"].ToList();
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

            this.Books[_symbol, "ASK"] = _askList;
            this.Books[_symbol, "BID"] = _bidList;
        }
        #endregion

        private static string httpUrl = "https://api.fcoin.com/v2";
        private static string httpUrlMarket = "https://api.fcoin.com/v2/market";

        #region StartBalance
        private void StartBalance()
        {
            int _loop = 100;
            while (this.Running)
            {
                if (_loop > 0) { _loop--; Thread.Sleep(100); continue; }
                _loop = 100;

                JObject _json = this.Call("GET", "/accounts/balance");
                if (_json == null) { continue; }

                int _status = _json.Property("status") == null ? -1 : _json["status"].Value<int>();
                if (_status != 0)
                {
                    this.Log("Balance failed - " + _json.ToString(Newtonsoft.Json.Formatting.None));
                    continue;
                }

                foreach (JObject _item in _json["data"])
                {
                    this.Balance[_item["currency"].Value<string>().ToUpper()] = _item["available"].Value<decimal>();
                }
            }
        }
        #endregion

        #region Call
        public JObject Call(string _method, string _url, params string[] _values)
        {
            try
            {
                JObject _json = new JObject();
                Dictionary<string, string> _list = new Dictionary<string, string>();
                for (int i = 0; i < _values.Length - 1; i += 2)
                {
                    _list.Add(_values[i], _values[i + 1]);
                    _json.Add(_values[i], _values[i + 1]);
                }
                KeyValuePair<string, string>[] _sorted = _list.ToArray().OrderBy(c => c.Key).ToArray();

                string _ts = (DateTimePlus.DateTime2JSTime(DateTime.UtcNow) * 1000).ToString();
                string _sign = _method + FCoin.httpUrl + _url + _ts;
                for (int i = 0; i < _sorted.Length; i++)
                {
                    _sign += i == 0 ? "" : "&";
                    _sign += _sorted[i].Key + "=" + _sorted[i].Value;
                }
                _sign = Base64.Encode(Encoding.UTF8.GetBytes(_sign));
                _sign = SHA.EncodeHMACSHA1ToBase64(_sign, this.secret);

                HttpClient _http = new HttpClient(10000);
                _http.BeginResponse(_method, FCoin.httpUrl + _url, "");
                _http.Request.ContentType = "application/json";
                _http.Request.Headers.Add("FC-ACCESS-KEY", this.key);
                _http.Request.Headers.Add("FC-ACCESS-SIGNATURE", _sign);
                _http.Request.Headers.Add("FC-ACCESS-TIMESTAMP", _ts);
                _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None)));
                string _result = _http.GetResponseString(Encoding.UTF8);

                return JObject.Parse(_result);
            }
            catch (Exception _ex)
            {
                this.Log($"CALL - {_ex.Message} - {_method} {_url} {string.Join(",", _values)}");
                return null;
            }
        }
        #endregion

        #region OrderCreateMarket
        public string OrderCreateMarket(string _symbol, string _side, decimal _amount)
        {
            JObject _result = this.Call(
                "POST", "/orders",
                "symbol", _symbol,
                "side", _side == "BID" ? "buy" : "sell",
                "type", "market",
                "amount", _amount.ToString());

            if (_result == null) { return ""; }
            if (_result.Property("status") == null || _result["status"].Value<int>() != 0)
            {
                this.Log($"Order create failed - {_side} {_amount}");
                this.Log($"Order create failed - {_result.ToString(Newtonsoft.Json.Formatting.None)}");
                return "";
            }
            return _result["data"].Value<string>();
        }
        #endregion

        #region MarketTicker
        public static JObject MarketTicker(string _symbol)
        {
            try
            {
                WebClientPlus _webClient = new WebClientPlus(5000);
                string _result = _webClient.DownloadString($"{FCoin.httpUrlMarket}/ticker/{_symbol}");
                _webClient.Dispose();

                return JObject.Parse(_result);
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
