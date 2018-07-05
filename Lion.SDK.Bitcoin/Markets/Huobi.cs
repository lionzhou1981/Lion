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
        private static string url = "https://api.huobi.pro";

        private string key;
        private string secret;

        #region Huobi
        public Huobi(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;

            base.Name = "HUO";
            base.WebSocket = "wss://api.huobi.pro/ws";
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
                    case "depth": this.ReceivedDepth(_command[1], _command[3], _json["tick"].Value<JObject>()); break;
                }
                return;
            }
            #endregion

            this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None));
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, string _type)
        {
            string _id = $"market.{_symbol}.depth.{_type}";

            JObject _json = new JObject();
            _json["sub"] = _id;
            _json["id"] = DateTime.UtcNow.Ticks;

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

            this.Books[_symbol, "BID"] = _bidList;
            this.Books[_symbol, "ASK"] = _askList;
        }
        #endregion

        #region MarketTicker
        public static JObject MarketTicker(string _symbol)
        {
            try
            {
                WebClientPlus _webClient = new WebClientPlus(5000);
                string _result = _webClient.DownloadString($"{Huobi.url}/market/detail/merged?symbol={_symbol}");
                _webClient.Dispose();

                return JObject.Parse(_result);
            }
            catch
            {
                return null;
            }
        }
        #endregion



        public JObject Accounts() => JObject.Parse(this.Get("/v1/account/accounts"));

        public JObject NewOrder(
            string _account,
            string _symbol,
            decimal _amount,
            string _type
            ) => JObject.Parse(this.Post("/v1/order/orders/place",
                "account-id", _account,
                "symbol", _symbol,
                "amount", _amount.ToString(),
                "type", _type
                ));

        public JObject OrderStatus(long _order_id) => JObject.Parse(this.Get("/v1/order/orders", "order-id", _order_id.ToString()));

        #region Get
        private string Get(string _path, params string[] _keyValue)
        {
            string _url = Huobi.url + _path;
            string _query = "AccessKeyId=" + this.key;
            _query += "&SignatureMethod=HmacSHA256";
            _query += "&SignatureVersion=2";
            _query += "&Timestamp=" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T").Replace(":", "%3A");
            for (int i = 0; i < (_keyValue.Length - 1); i += 2)
            {
                if (_keyValue[i + 1] == null) { continue; }
                _query += "&" + _keyValue[i] + "=" + _keyValue[i + 1];
            }
            string _sign = "GET\napi.huobi.pro\n" + _path + "\n" + _query;
            string _signed = SHA.EncodeHMACSHA256ToBase64(_sign, this.secret).Replace("+", "%2B").Replace("=", "%3D");
            _query += "&Signature=" + _signed;

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("GET", _url + "?" + _query, "");
            _http.Request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            _http.EndResponse();

            return _http.GetResponseString(Encoding.UTF8);
        }
        #endregion

        #region Post
        private string Post(string _path, params string[] _keyValue)
        {
            string _url = Huobi.url + _path;

            string _query = "AccessKeyId=" + this.key;
            _query += "&SignatureMethod=HmacSHA256";
            _query += "&SignatureVersion=2";
            _query += "&Timestamp=" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T").Replace(":", "%3A");

            string _sign = "GET\napi.huobi.pro\n" + _path + "\n" + _query;
            string _signed = SHA.EncodeHMACSHA256ToBase64(_sign, this.secret).Replace("+", "%2B").Replace("=", "%3D");
            _query += "&Signature=" + _signed;

            JObject _json = new JObject();
            for(int i = 0; i < (_keyValue.Length - 1); i+=2)
            {
                if(_keyValue[i + 1] == null) { continue; }
                _json[_keyValue[i]] = _keyValue[i + 1].ToString();
            }

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", _url, "");
            _http.Request.Headers.Add("Content-Type", "application/json");
            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString()));

            string _result = _http.GetResponseString(Encoding.UTF8);

            return _result;
        }
        #endregion
    }
}
