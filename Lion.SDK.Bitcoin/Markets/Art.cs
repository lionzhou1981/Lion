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
    public class Art : MarketBase
    {
        #region Art
        public Art(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "Art";
            base.WebSocket = "wss://api.collectionpark.com/wsv1";
            base.HttpUrl = "http://api.collectionpark.com";
            base.OnReceivedEvent += Art_OnReceivedEvent;
        }
        #endregion

        #region Art_OnReceivedEvent
        private void Art_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _channel = _json.Property("channel") == null ? "" : _json["channel"].Value<string>();

            if (_channel != "")
            {
                switch (_channel)
                {
                    case "depth":
                        this.ReceivedDepth(
                            _json["pair"].Value<string>(),
                            ((JObject)_json["data"]).Property("change") == null ? "START" : "UPDATE",
                            _json["data"]); break;
                    case "ticker":
                        this.ReceivedTicker(_json["pair"].Value<string>(), _json["data"]);
                        break;
                    default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _pairs)
        {
            string _url = "/GET/rest/art/auth/wallet";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token["free"])
            {
                JProperty _property = (JProperty)_item;
                BalanceItem _balance = new BalanceItem();
                _balance.Symbol = _property.Name;
                _balance.Free = decimal.Parse(_property.Value.ToString());
                _balances.TryAdd(_balance.Symbol, _balance);
            }
            foreach (JToken _item in _token["freezed"])
            {
                JProperty _property = (JProperty)_item;
                _balances[_property.Name].Lock = decimal.Parse(_property.Value.ToString());
            }
            this.Balances = _balances;
            return _balances;
        }
        #endregion

        public override Books GetDepths(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }

        public override KLine[] GetKLines(string _pair, KLineType _type, params string[] _values)
        {
            throw new NotImplementedException();
        }

        #region GetTicker
        public override Ticker GetTicker(string _pair)
        {
            string _url = $"/GET/rest/art/ticker?pair={_pair}";

            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url);
            if (_token == null) { return null; }

            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.LastPrice = _token["last"].Value<decimal>();
            _ticker.BidPrice = _token["buy"].Value<decimal>();
            _ticker.AskPrice = _token["sell"].Value<decimal>();
            _ticker.High24H = _token["high"].Value<decimal>();
            _ticker.Low24H = _token["low"].Value<decimal>();
            _ticker.Volume24H = _token["vol"].Value<decimal>();
            _ticker.Change24H = _token["dchange"].Value<decimal>();
            _ticker.ChangeRate24H = _token["dchange_pec"].Value<decimal>();

            return _ticker;
        }
        #endregion

        public override Trade[] GetTrades(string _pair, params string[] _values)
        {
            throw new NotImplementedException();
        }

        #region OrderCreate
        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0M)
        {
            string _url = "/POST/rest/art/placeOrder";

            IList<string> _values = new List<string>();
            _values.Add("pair");
            _values.Add(_pair);
            _values.Add("isbid");
            _values.Add(_side == MarketSide.Bid ? "true" : "false");
            //_values.Add("orderQty");
            //_values.Add(_amount.ToString().Split('.')[0]);
            _values.Add("order_type");
            switch (_type)
            {
                case OrderType.Limit:
                    _values.Add("LIMIT");
                    _values.Add("price");
                    _values.Add(_price.ToString());
                    break;
                case OrderType.Market:
                    _values.Add("MARKET");
                    _values.Add("price");
                    _values.Add("0");
                    break;
            }
            _values.Add("amount");
            _values.Add(_amount.ToString());
            _values.Add("stop_price");
            _values.Add("0");

            JToken _token = base.HttpCall(HttpCallMethod.Form, "POST", _url, true, _values.ToArray());
            if (_token == null) { return null; }

            OrderItem _order = new OrderItem();
            _order.Id = _token["orderId"].Value<string>();
            _order.Pair = _pair;
            _order.Side = _side;
            _order.Amount = _amount;
            _order.Price = _price;
            _order.Status = OrderStatus.New;

            return _order;
        }
        #endregion

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

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            Dictionary<string, string> _list = new Dictionary<string, string>();
            if (_method.ToUpper() == "POST")
            {
                for (int i = 0; i < _keyValues.Length; i += 2)
                {
                    _list.Add(_keyValues[i].ToString(), _keyValues[i + 1].ToString());
                }
            }
            string _time = DateTimePlus.DateTime2JSTime(DateTime.UtcNow.AddSeconds(-1)).ToString();
            _list.Add("api_key", base.Key);
            _list.Add("auth_nonce", _time);
            KeyValuePair<string, string>[] _sorted = _list.OrderBy(c => c.Key).ToArray();

            string _sign = "";
            foreach (KeyValuePair<string, string> _item in _sorted) { _sign += _item.Value; }
            _list.Add("auth_sign", MD5.Encode(_sign + base.Secret).ToLower());

            IList<string> _keyValueList = new List<string>();
            if (_method.ToUpper() == "GET")
            {
                foreach (var _item in _keyValues)
                {
                    _keyValueList.Add(_item.ToString());
                }
            }
            foreach (KeyValuePair<string, string> _item in _list)
            {
                _keyValueList.Add(_item.Key);
                _keyValueList.Add(_item.Value);
            }
            return _keyValueList.ToArray();
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;
            if (_json.Property("code") == null || _json["code"].Value<int>() != 0)
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _json["data"];
        }
        #endregion

        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            throw new NotImplementedException();
        }

        protected override void ReceivedTicker(string _symbol, JToken _token)
        {
            throw new NotImplementedException();
        }

        #region OrderCancel
        public OrderItem OrderCancel(string _pair, string _orderId)
        {
            string _url = "/POST/rest/art/cancelOrder";

            //JToken _token = base.HttpCall(HttpCallMethod.Json, "DELETE", _url, true, "orderID", _orderId);
            JToken _token = base.HttpCall(HttpCallMethod.Form, "POST", _url, true, "order_id", _orderId, "pair", _pair);
            if (_token == null) { return null; }

            OrderItem _order = new OrderItem();
            _order.Id = _token["orderid"].Value<string>();

            return _order;
        }
        #endregion
    }
}
