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
    public class CoinEx : MarketBase
    {
        private string key;
        private string secret;
        private long commandId = 1;
        private string http = "https://api.coinex.com";
        private Thread threadBalance;

        #region CoinEx
        public CoinEx(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;

            base.Name = "CEX";
            base.WebSocket = "wss://socket.coinex.com/";
            base.OnReceivedEvent += CoinEx_OnReceivedEvent;
        }
        #endregion

        #region CoinEx_OnReceivedEvent
        private void CoinEx_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _type = _json.Property("method") == null ? "" : _json["method"].Value<string>();

            switch (_type)
            {
                case "depth.update":
                    this.ReceivedDepth(
                        _json["params"][2].Value<string>(),
                        _json["params"][0].Value<bool>() ? "FULL" : "UPDATE",
                        _json["params"][1].Value<JObject>());
                    break;
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
        public void SubscribeDepth(string _symbol, int _limit = 10, string _interval = "0")
        {
            if (this.Books[_symbol, "BID"] == null) { this.Books[_symbol, "BID"] = new BookItems("BID"); }
            if (this.Books[_symbol, "ASK"] == null) { this.Books[_symbol, "ASK"] = new BookItems("ASK"); }

            this.Send("depth.subscribe", new JArray(_symbol, _limit, _interval));
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            if (_type == "FULL")
            {
                #region Bid
                IList<KeyValuePair<string, BookItem>> _bidItems = this.Books[_symbol, "BID"].ToList();
                BookItems _bidList = new BookItems("BID");
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
                BookItems _askList = new BookItems("ASK");
                IList<KeyValuePair<string, BookItem>> _askItems = this.Books[_symbol, "ASK"].ToList();
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

                this.Books[_symbol, "ASK"] = _askList;
                this.Books[_symbol, "BID"] = _bidList;
            }
            else if (_type == "UPDATE")
            {
                #region Bid
                if (_json.Property("bids") != null)
                {
                    JArray _bids = _json["bids"].Value<JArray>();
                    BookItems _bidList = this.Books[_symbol, "BID"];
                    for (int i = 0; i < _bids.Count; i++)
                    {
                        decimal _price = _bids[i][0].Value<decimal>();
                        decimal _amount = _bids[i][1].Value<decimal>();

                        if (_amount == 0)
                        {
                            BookItem _item = _bidList.Delete(_price.ToString());
                            if (_item != null)
                            {
                                this.OnBookDelete(_item);
                            }
                        }
                        else
                        {
                            BookItem _item = _bidList.Update(_price.ToString(), _amount);
                            if (_item != null)
                            {
                                this.OnBookUpdate(_item);
                            }
                            else
                            {
                                this.OnBookInsert(_bidList.Insert(_price.ToString(), _price, _amount));
                            }
                        }
                    }
                }
                #endregion

                #region Ask
                if (_json.Property("asks") != null)
                {
                    BookItems _askList = this.Books[_symbol, "ASK"];
                    JArray _asks = _json["asks"].Value<JArray>();
                    for (int i = 0; i < _asks.Count; i++)
                    {
                        decimal _price = _asks[i][0].Value<decimal>();
                        decimal _amount = _asks[i][1].Value<decimal>();

                        if (_amount == 0)
                        {
                            BookItem _item = _askList.Delete(_price.ToString());
                            if (_item != null)
                            {
                                this.OnBookDelete(_item);
                            }
                        }
                        else
                        {
                            BookItem _item = _askList.Update(_price.ToString(), _amount);
                            if (_item != null)
                            {
                                this.OnBookUpdate(_item);
                            }
                            else
                            {
                                this.OnBookInsert(_askList.Insert(_price.ToString(), _price, _amount));
                            }
                        }
                    }
                }
                #endregion
            }
        }
        #endregion

        #region SignIn
        public void SignIn()
        {
            long _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow) * 1000;
            string _sign = $"access_id={this.key}&tonce={_time}&secret_key={this.secret}";
            _sign = MD5.Encode(_sign).ToUpper();

            JObject _json = new JObject();
            _json["access_id"] = this.key;
            _json["authorization"] = _sign;
            _json["tonce"] = _time;

            this.Send("server.sign", _json);
        }
        #endregion

        #region Send
        private void Send(string _method, JToken _params)
        {
            JObject _json = new JObject();
            _json["method"] = _method;
            _json["params"] = _params;
            _json["id"] = this.commandId++;

            this.Send(_json);
        }
        #endregion

        #region StartBalance
        private void StartBalance()
        {
            int _loop = 100;
            while (this.Running)
            {
                Thread.Sleep(100);
                if (_loop > 0) { _loop--; continue; }
                _loop = 100;

                JObject _json = this.HttpCall("GET", "/v1/balance/");
                if (_json == null) { continue; }

                string _code = _json.Property("code") == null ? "" : _json["code"].Value<string>();
                if (_code != "0")
                {
                    this.Log("Balance failed - " + _json.ToString(Newtonsoft.Json.Formatting.None));
                    continue;
                }

                foreach (JProperty _item in _json["data"].Children())
                {
                    this.Balance[_item.Name] = _json["data"][_item.Name]["available"].Value<decimal>();
                }
            }
        }
        #endregion

        #region HttpCall
        public JObject HttpCall(string _method, string _url, params string[] _keyValue)
        {
            try
            {
                string _query = "";
                SortedDictionary<string, string> _list = new SortedDictionary<string, string>();
                for (int i = 0; i < _keyValue.Length; i += 2)
                {
                    _query += _query == "" ? "" : "&";
                    _query += _keyValue[i] + "=" + System.Web.HttpUtility.UrlEncode(_keyValue[i + 1]);
                    _list.Add(_keyValue[i], _keyValue[i + 1]);
                }

                string _sign = "";
                long _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow) * 1000;
                _list.Add("access_id", this.key);
                _list.Add("tonce", _time.ToString());

                _query += _query == "" ? "" : "&";
                _query += $"access_id={this.key}&tonce={_time}";

                JObject _json = new JObject();
                foreach (KeyValuePair<string, string> _item in _list)
                {
                    _sign += _sign == "" ? "" : "&";
                    _sign += $"{_item.Key}={_item.Value}";
                    _json[_item.Key] = _item.Value;
                }
                _sign += $"&secret_key={this.secret}";
                _sign = MD5.Encode(_sign).ToUpper();

                HttpClient _http = new HttpClient(10000);
                if (_method == "GET" || _method == "DELETE")
                {
                    _http.BeginResponse(_method, $"{this.http}{_url}?{_query}", "");
                    _http.Request.Headers.Add("authorization", _sign);
                    _http.EndResponse();
                }
                else
                {
                    _http.UserAgent = " User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36";
                    _http.BeginResponse(_method, $"{this.http}{_url}", "");
                    _http.Request.ContentType = "application/json";
                    _http.Request.Headers.Add("authorization", _sign);
                    _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString()));
                }

                string _result = _http.GetResponseString(Encoding.UTF8);
                JObject _resultJson = JObject.Parse(_result);

                this.OnLog(">>>", $"{_resultJson.ToString(Newtonsoft.Json.Formatting.None)}");

                return _resultJson;
            }
            catch (Exception _ex)
            {
                this.OnLog("HTTP", $"{_ex.Message} - {_url} {string.Join(",", _keyValue)}");
                return null;
            }
        }
        #endregion

        #region OrderLimit
        public string OrderLimit(string _symbol, string _side, decimal _amount, decimal _price)
        {
            string _url = "/v1/order/limit";
            JObject _json = this.HttpCall("POST", _url,
                "market", _symbol,
                "type", _side == "BID" ? "buy" : "sell",
                "amount", _amount.ToString(),
                "price", _price.ToString()
                );
            if (_json == null) { return ""; }
            if (_json.Property("code")== null) { return ""; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return "";
            }
            return _json["data"]["id"].Value<string>();
        }
        #endregion

        #region OrderMarket
        public string OrderMarket(string _symbol, string _side, decimal _amount)
        {
            string _url = "/v1/order/market";
            JObject _json = this.HttpCall("POST", _url,
                "market", _symbol,
                "type", _side == "BID" ? "buy" : "sell",
                "amount", _amount.ToString()
                );
            if (_json == null) { return ""; }
            if (_json.Property("code") == null) { return ""; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return "";
            }
            return _json["data"]["id"].Value<string>();
        }
        #endregion

        #region OrderStatus
        public JObject OrderStatus(string _symbol, string _id)
        {
            string _url = "/v1/order";
            JObject _json = this.HttpCall("GET", _url,
                "market", _symbol,
                "id", _id
                );
            if (_json == null) { return null; }
            if (_json.Property("code") == null) { return null; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"].Value<JObject>();
        }
        #endregion

        #region OrderCancel
        public JObject OrderCancel(string _symbol, string _id, out bool _done)
        {
            _done = false;
            string _url = "/v1/order/pending";
            JObject _json = this.HttpCall("DELETE", _url,
                "market", _symbol,
                "id", _id
                );
            if (_json == null) { return null; }
            if (_json.Property("code") == null) { return null; }

            if (_json["code"].Value<int>() == 600)
            {
                _done = true;
                return null;
            }
            else if (_json["code"].Value<int>() != 0)
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
            JObject _json = this.HttpCall("GET", _url);
            if (_json == null) { return null; }
            if (_json.Property("code") == null) { return null; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"].Value<JObject>();
        }
        #endregion

        #region MarketKLine
        public JArray MarketKLine(string _symbol, string _type = "1hour",int _limit=10)
        {
            string _url = "/v1/market/kline";
            JObject _json = this.HttpCall("GET", _url, "market", _symbol, "type", _type, "limit", _limit.ToString());

            if (_json == null) { return null; }
            if (_json.Property("code") == null) { return null; }
            if (_json["code"].Value<int>() != 0)
            {
                this.OnLog(_url, _json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"].Value<JArray>();
        }
        #endregion
    }
}
