using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Huobi :  MarketBase, IDisposable 
    {
        private static string url = "https://api.huobi.pro";
        private static string ws = "wss://api.huobi.pro/ws";

        public string symbol;
        private string key;
        private string secret;
        private bool running = false;
        private ClientWebSocket socket = null;
        private DateTime socketTime;
        private Thread thread;
        private ConcurrentDictionary<string, string[]> SubscribedChannels;

        public Huobi(string _symbol, string _key, string _secret)
        {
            this.symbol = _symbol;
            this.key = _key;
            this.secret = _secret;
            this.SubscribedChannels = new ConcurrentDictionary<string, string[]>();
        }

        #region Start
        public void Start()
        {
            if (this.running) { return; }

            this.running = true;
            this.thread = new Thread(new ThreadStart(this.StartThread));
            this.thread.Start();
        }
        #endregion

        #region StartThread
        private void StartThread()
        {
            while (this.running)
            {
                Thread.Sleep(10);
                if (this.socket == null || this.socket.State != WebSocketState.Open)
                {
                    #region 建立连接
                    this.socket = new ClientWebSocket();

                    Task _task = this.socket.ConnectAsync(new Uri(Huobi.ws), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(1000); }

                    if (_task.Status != TaskStatus.RanToCompletion || this.socket.State != WebSocketState.Open)
                    {
                        this.Stop();
                        continue;
                    }

                    this.socketTime = DateTime.UtcNow;
                    base.OnWebSocketConnected();
                    #endregion
                }
                else
                {
                    #region 处理数据
                    byte[] _buffer = new byte[16384];
                    Task<WebSocketReceiveResult> _task = this.socket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(10); }

                    try
                    {
                        if (_task.Status != TaskStatus.RanToCompletion
                            || _task.Result == null
                            || _task.Result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        base.OnWebSocketDisconnected();
                        this.Stop();
                        continue;
                    }

                    try
                    {
                        _buffer = GZip.Decompress(_buffer);
                    }
                    catch
                    {
                        continue;
                    }

                    JObject _json = null;
                    try { _json = JObject.Parse(Encoding.UTF8.GetString(_buffer)); } catch { _json = null; }

                    if (_json == null) { continue; }
                    this.Receive(_json);
                    #endregion
                }
            }

            this.SubscribedChannels.Clear();
            this.socket.Dispose();
            this.socket = null;
        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.running = false;

            this.SubscribedChannels?.Clear();
            this.socket?.Dispose();
            this.socket = null;
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            this.Stop();
        }
        #endregion

        #region Send
        public void Send(JObject _json)
        {
            if (this.socket == null || this.socket.State != WebSocketState.Open) { return; }

            this.socket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None))),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }
        #endregion

        #region Receive
        public void Receive(JObject _json)
        {
            if (_json.Property("ping") != null)
            {
                Console.WriteLine("RECV : A " + _json["ping"].Value<long>());
                this.Send(new JObject() { ["pong"] = _json["ping"].Value<long>() });
                this.socketTime = DateTime.Now;
                return;
            }
            if (_json.Property("subbed") != null && _json.Property("status") != null && _json["status"].Value<string>() == "ok")
            {
                Console.WriteLine("RECV : B " + _json["subbed"].Value<string>());
                this.SubscribedChannels.AddOrUpdate(
                    _json["subbed"].Value<string>(),
                    new string[] { "book", this.symbol },
                    (k, v) => new string[] { _json["subbed"].Value<string>(), this.symbol }
                    );
                return;
            }
            if (_json.Property("unsub") != null)
            {
                Console.WriteLine("RECV : C " + _json["unsub"].Value<string>());
                string[] _values;
                if (!this.SubscribedChannels.TryRemove(_json["unsub"].Value<string>(), out _values))
                {
                    this.OnError(_json["unsub"].Value<string>(), _json.ToString(Newtonsoft.Json.Formatting.None));
                }
                return;
            }
            if (_json.Property("err-code") != null)
            {
                Console.WriteLine("EX : " + _json.ToString(Newtonsoft.Json.Formatting.None));
                return;
            }
            if (_json.Property("ch") != null)
            {
                Console.WriteLine("RECV : D " + _json["ch"].Value<string>() + " " + _json.ToString(Newtonsoft.Json.Formatting.None).Length);
                this.ReceiveSubscribeDepth(_json["ch"].Value<string>(), _json);
                return;
            }

            Console.WriteLine("RECV : " + _json.ToString(Newtonsoft.Json.Formatting.None));
        }
        #endregion

        #region ReceiveSubscribeDepth
        public void ReceiveSubscribeDepth(string _channelId, JObject _json)
        {
            if (!this.SubscribedChannels.ContainsKey(_channelId))
            {
                Console.WriteLine("RECV : E " + _channelId + " " + _json.ToString(Newtonsoft.Json.Formatting.None).Length);
                return;
            }

            string[] _channel = this.SubscribedChannels[_channelId];
            if (_channel[0] == "book")
            {
                JArray _bids = (JArray)_json["tick"]["bids"];
                Dictionary<decimal, decimal> _bidList = new Dictionary<decimal, decimal>();
                for(int i = 0; i < _bids.Count; i++)
                {
                    _bidList.Add(_bids[i][0].Value<decimal>(), _bids[i][1].Value<decimal>());
                }

                JArray _asks = (JArray)_json["tick"]["asks"];
                Dictionary<decimal, decimal> _askList = new Dictionary<decimal, decimal>();
                for (int i = 0; i < _asks.Count; i++)
                {
                    _askList.Add(_asks[i][0].Value<decimal>(), _asks[i][1].Value<decimal>());
                }
                this.OnBookUpdated(_askList, _bidList);
            }
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, string _type)
        {
            string _id = $"market.{_symbol}.depth.{_type}";

            JObject _json = new JObject();
            _json["sub"] = _id;
            _json["id"] = DateTime.UtcNow.Ticks;

            this.Send(_json);
        }
        #endregion

        #region Unsubscribe
        public void Unsubscribe(string _channelId)
        {
            JObject _json = new JObject();
            _json["event"] = "unsub";
            _json["id"] = _channelId;

            this.Send(_json);
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
