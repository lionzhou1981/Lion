using System;
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
    public class HiBTC : MarketBase, IDisposable
    {
        private static string url = "https://api.hibtc.com";
        private static string ws = "wss://api.hibtc.com/wsjoint";

        public string symbol;
        private string key;
        private string secret;
        private bool running = false;
        private ClientWebSocket socket = null;
        private Thread thread;
        private ConcurrentDictionary<string, string[]> SubscribedChannels;

        public HiBTC(string _symbol, string _key, string _secret)
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
            string _buffered = "";
            while (this.running)
            {
                if (this.socket == null || this.socket.State != WebSocketState.Open)
                {
                    #region 建立连接
                    _buffered = "";
                    this.socket = new ClientWebSocket();
                    this.socket.Options.KeepAliveInterval = new TimeSpan(0, 0, 0, 1, 0);

                    Task _task = this.socket.ConnectAsync(new Uri(HiBTC.ws), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(1000); }

                    if (_task.Status != TaskStatus.RanToCompletion) { this.socket = null; }

                    base.OnWebSocketConnected();
                    #endregion
                }
                else
                {
                    #region 处理数据
                    byte[] _buffer = new byte[8192];
                    Task<WebSocketReceiveResult> _task = this.socket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None);
                    while (_task.Status != TaskStatus.Canceled
                        && _task.Status != TaskStatus.Faulted
                        && _task.Status != TaskStatus.RanToCompletion) { Thread.Sleep(10); }

                    if (_task.Result == null || _task.Status == TaskStatus.Faulted || _task.Result.MessageType == WebSocketMessageType.Close)
                    {
                        base.OnWebSocketDisconnected();
                        this.socket = null;
                        continue;
                    }

                    _buffered += Encoding.UTF8.GetString(_buffer, 0, _task.Result.Count);

                    int _start = 0;
                    while (true)
                    {
                        if (_buffered.Length == 0) { break; }
                        int _s1 = _buffered.IndexOf("}", _start);
                        int _s2 = _buffered.IndexOf("]", _start);
                        _s1 = _s1 == -1 ? int.MaxValue : _s1;
                        _s2 = _s2 == -1 ? int.MaxValue : _s2;
                        if (_s1 == _s2) { break; }

                        int _index = _s1 > _s2 ? _s2 : _s1;
                        string _testJson = _buffered.Substring(0, _index + 1);

                        JToken _json = null;
                        try { _json = JObject.Parse(_testJson); } catch { _json = null; }
                        if (_json == null) { try { _json = JArray.Parse(_testJson); } catch { _json = null; } }
                        if (_json == null) { _start = _index + 1; continue; }
                        _buffered = _buffered.Substring(_index + 1);

                        while (_buffered.Length > 0 && _buffered[0] != '[' && _buffered[0] != '{')
                        {
                            Console.WriteLine("Bitfinex" + _buffered);
                            _buffered = _buffered.Substring(1);
                        }
                        this.Receive(_json);
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.running = false;
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Send
        public void Send(JObject _json)
        {
            if (this.socket == null || this.socket.State != WebSocketState.Open) { return; }

            Console.WriteLine(_json.ToString(Newtonsoft.Json.Formatting.None));

            this.socket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None))),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).Wait();
        }
        #endregion

        #region Receive
        public void Receive(JToken _json)
        {
            base.OnWebSocketReceived(_json);
            return;
            //JObject _item = (JObject)_json;
            //switch (_item["event"].Value<string>())
            //{
            //    case "info": break;
            //    case "error": this.OnError(_item["code"].Value<string>(), _item["msg"].Value<string>()); break;
            //case "subscribed":
            //    this.SubscribedChannels.AddOrUpdate(
            //        _item["chanId"].Value<string>(),
            //        new string[] { _item["channel"].Value<string>(), this.symbol },
            //        (k, v) => new string[] { _item["channel"].Value<string>(), this.symbol }
            //        );
            //    break;
            //case "cancel_subscribe":
            //    string[] _values;
            //    if (!this.SubscribedChannels.TryRemove(_item["chanId"].Value<string>(), out _values))
            //    {
            //        this.OnError(_item["code"].Value<string>(), _item["msg"].Value<string>()); break;
            //    }
            //    break;
            //    default: base.OnWebSocketReceived(_json); break;
            //}
        }
        #endregion

        #region ReceiveSubscribe
        public void ReceiveSubscribe(string _channelId, JArray _item)
        {
            string[] _channel = this.SubscribedChannels[_channelId];
            if (_channel[0] == "book")
            {
                decimal _price = _item.Count == 3 ? _item[0].Value<decimal>() : _item[1].Value<decimal>();
                int _count = _item.Count == 3 ? _item[1].Value<int>() : _item[2].Value<int>();
                decimal _amount = _item.Count == 3 ? _item[2].Value<decimal>() : _item[3].Value<decimal>();

                this.OnBookChanged(_amount > 0 ? "bid" : "ask", _price, _count == 0 ? 0 : Math.Abs(_amount));
            }
        }
        #endregion

        #region Subscribe
        //{"event":"subscribe", "channel":"depth","pair":"ETH_BTC","depth":20, "prec":0}
        public void Subscribe(string _channel, string _symbol, params object[] _keyValues)
        {//
            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = _channel;
            _json["pair"] = _symbol;
            if (_keyValues.Length == 0)
            {
                _json["depth"] = 100;
                _json["prec"] = 0;
            }

            for (int i = 0; i < _keyValues.Length - 1; i += 2)
            {
                if (_keyValues[i + 1].GetType() == typeof(int)) { _json[_keyValues[i]] = (int)_keyValues[i + 1]; }
                else { _json[_keyValues[i]] = _keyValues[i + 1].ToString(); }
            }

            this.Send(_json);
        }
        #endregion

        #region Unsubscribe
        public void Unsubscribe(string _channelId, string _pair)
        {
            JObject _json = new JObject();
            _json["event"] = "cancel_subscribe";
            _json["chanId"] = _channelId;
            _json["pair"] = _pair;

            this.Send(_json);
        }
        #endregion
    }
}
