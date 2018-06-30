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
        private long commandId = 0;

        #region CoinEx
        public CoinEx()
        {
            base.Name = "CEX";
            base.WebSocket = "wss://socket.coinex.com/";

            base.OnConnectedEvent += CoinEx_OnConnectedEvent;
            base.OnDisconnectedEvent += CoinEx_OnDisconnectedEvent;
            base.OnReceivedEvent += CoinEx_OnReceivedEvent;
        }
        #endregion

        private void CoinEx_OnConnectedEvent()
        {
            this.Log("Connected");
        }

        private void CoinEx_OnDisconnectedEvent()
        {
            this.Log("Disconnect");
        }

        private void CoinEx_OnReceivedEvent(JToken _token)
        {
            this.Log(_token.ToString(Newtonsoft.Json.Formatting.None));
        }

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, int _limit = 10, string _interval = "0")
        {
            this.Send("depth.subscribe", new JArray(_symbol, _limit, _interval));
        }
        #endregion

        #region SignIn
        public void SignIn(string _id,string _key)
        {
            long _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow);
            string _sign = $"access_id={_id}&tonce={_time}&secret_key={_key}";
            _sign = MD5.Encode(_sign).ToUpper();

            JObject _json = new JObject();
            _json["access_id"] = _id;
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

        #region Receive
        private void Receive(JObject _json)
        {
            //if (_json.Property("type") != null && _json["type"].Value<string>() == "hello")
            //{
            //    this.Books.Clear();

            //    JObject _sub = new JObject();
            //    _json.Add("cmd", "sub");
            //    _json.Add("args", new JArray(this.listens));
            //    this.Send(_json);
            //    return;
            //}

            //string _type = _json.Property("type") == null ? "" : _json["type"].Value<string>();
            //if (_type == "") { return; }

            //string[] _command = _type.Split('.');
            //if (_command[0] == "depth")
            //{
            //    //this.Receive_Depth(_command[2], _json);
            //    return;
            //}

            this.Log("RX - " + _json.ToString(Newtonsoft.Json.Formatting.None));
        }

        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
