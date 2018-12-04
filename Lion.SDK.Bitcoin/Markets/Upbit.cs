using System;
using System.Text;
using Lion.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;

namespace Lion.SDK.Bitcoin.Markets
{
    public class Upbit : MarketBase
    {
        JArray SocketCommand = new JArray();
        #region Upbit
        public Upbit(string _key, string _secret) : base(_key, _secret)
        {
            base.Name = "UPB";
            base.WebSocket = "wss://api.upbit.com/websocket/v1";
            base.HttpUrl = "https://api.upbit.com/v1";
            base.OnReceivedEvent += Upbit_OnReceivedEvent;

            JObject _json = new JObject();
            _json["ticket"] = "UNIQUE_TICKET";
            this.SocketCommand.Add(_json);
        }
        #endregion

        #region Upbit_OnReceivedEvent
        private void Upbit_OnReceivedEvent(JToken _token)
        {
            JObject _json = (JObject)_token;
            string _type = _json.Property("type") == null ? "" : _json["type"].Value<string>();

            if (_type != "")
            {
                switch (_type)
                {
                    case "ticker":
                        this.ReceivedTicker(_json["code"].Value<string>(), _token);
                        break;
                    case "orderbook":
                        this.ReceivedDepth(_json["code"].Value<string>(), _type, _token);
                        break;
                    default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
        }
        #endregion

        #region GetBalances
        public override Balances GetBalances(params object[] _pairs)
        {
            string _url = "/accounts";
            JToken _token = base.HttpCall(HttpCallMethod.Get, "GET", _url, true);
            if (_token == null) { return null; }

            Balances _balances = new Balances();
            foreach (JToken _item in _token)
            {
                _balances[_item["currency"].ToString()] = new BalanceItem()
                {
                    Symbol = _item["currency"].ToString(),
                    Free = _item["balance"].Value<decimal>(),
                    Lock = _item["locked"].Value<decimal>()
                };
            }
            return _balances;
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
            this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask);
            this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid);

            JObject _json = new JObject();
            _json["type"] = "orderbook";
            _json["codes"] = new JArray() { _pair };
            this.SocketCommand.Add(_json);
        }
        public override void SubscribeDepth(JToken _token, params object[] _values)
        {
            JArray _arr = new JArray();
            foreach (string _pair in _token)
            {
                if (this.Books[_pair, MarketSide.Bid] == null) { this.Books[_pair, MarketSide.Bid] = new BookItems(MarketSide.Bid); }
                if (this.Books[_pair, MarketSide.Ask] == null) { this.Books[_pair, MarketSide.Ask] = new BookItems(MarketSide.Ask); }
                _arr.Add(_pair);
            }

            JObject _json = new JObject();
            _json["type"] = "orderbook";
            _json["codes"] = _token;
            this.SocketCommand.Add(_json);

            this.Send(this.SocketCommand.ToString());

        }
        #endregion

        #region SubscribeTicker
        public override void SubscribeTicker(string _pair)
        {
            JObject _json = new JObject();
            _json["type"] = "ticker";
            _json["codes"] = new JArray() { _pair };
            this.SocketCommand.Add(_json);
        }
        #endregion

        #region SendSocketCommand
        public void SendSocketCommand()
        {
            this.Send(this.SocketCommand.ToString());
        }
        #endregion

        #region HttpCallAuth
        protected override object[] HttpCallAuth(HttpClient _http, string _method, ref string _url, object[] _keyValues)
        {
            string _query = "";
            JObject _json = new JObject();
            for (int i = 0; i < _keyValues.Length - 1; i += 2)
            {
                _query += _query == "" ? "" : "&";
                _query += _keyValues[i] + "=" + System.Web.HttpUtility.UrlEncode(_keyValues[i + 1].ToString());

                Type _valueType = _keyValues[i + 1].GetType();
                if (_valueType == typeof(int)) { _json[_keyValues[i]] = (int)_keyValues[i + 1]; }
                else if (_valueType == typeof(bool)) { _json[_keyValues[i]] = (bool)_keyValues[i + 1]; }
                else if (_valueType == typeof(decimal)) { _json[_keyValues[i]] = (decimal)_keyValues[i + 1]; }
                else if (_valueType == typeof(long)) { _json[_keyValues[i]] = (long)_keyValues[i + 1]; }
                else if (_valueType == typeof(JArray)) { _json[_keyValues[i]] = (JArray)_keyValues[i + 1]; }
                else { _json[_keyValues[i]] = _keyValues[i + 1].ToString(); }
            }

            JwtPayload _payload = new JwtPayload { { "access_key", base.Key }, { "nonce", DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString() + DateTime.UtcNow.Millisecond.ToString() } };

            if (_method.ToUpper() == "POST")
            {
                _payload.Add("query", _query);
            }
            byte[] _keyBytes = Encoding.Default.GetBytes(base.Secret);
            var _securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(_keyBytes);
            var _credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(_securityKey, "HS256");
            var _header = new JwtHeader(_credentials);
            var _secToken = new JwtSecurityToken(_header, _payload);
            var _jwtToken = new JwtSecurityTokenHandler().WriteToken(_secToken);
            var _authorizationToken = _jwtToken;
            _http.Headers.Add("Authorization", $"Bearer {_authorizationToken}");

            return _keyValues;
        }
        #endregion

        #region HttpCallResult
        protected override JToken HttpCallResult(JToken _token)
        {
            if (_token == null) { return null; }

            JObject _json = (JObject)_token;
            if (_json.Property("error") != null)
            {
                this.Log(_json.ToString(Newtonsoft.Json.Formatting.None));
                return null;
            }
            return _token;
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _pair, string _type, JToken _token)
        {
            this.Books[_pair, MarketSide.Ask].Clear();
            this.Books[_pair, MarketSide.Bid].Clear();

            BookItem _bookItem;
            this.Books.Timestamp = _token["timestamp"].Value<long>();
            foreach (var _item in _token["orderbook_units"])
            {
                decimal _askPrice = _item["ask_price"].Value<decimal>();
                decimal _bidPrice = _item["bid_price"].Value<decimal>();
                decimal _askSize = _item["ask_size"].Value<decimal>();
                decimal _bidSize = _item["bid_size"].Value<decimal>();

                _bookItem = this.Books[_pair, MarketSide.Ask].Insert(_askPrice.ToString(), Math.Abs(_askPrice), _askSize);
                this.OnBookInsert(_bookItem);
                _bookItem = this.Books[_pair, MarketSide.Bid].Insert(_bidPrice.ToString(), Math.Abs(_bidPrice), _bidSize);
                this.OnBookInsert(_bookItem);
            }
        }
        #endregion

        #region ReceivedTicker
        protected override void ReceivedTicker(string _pair, JToken _token)
        {
            Ticker _ticker = new Ticker();
            _ticker.Pair = _pair;
            _ticker.High24H = _token["high_price"].Value<decimal>();
            _ticker.Low24H = _token["low_price"].Value<decimal>();
            _ticker.LastPrice = _token["trade_price"].Value<decimal>();
            this.Tickers[_pair] = _ticker;
        }
        #endregion

        #region OrderCreate
        public override OrderItem OrderCreate(string _pair, MarketSide _side, OrderType _type, decimal _amount, decimal _price = 0M)
        {
            string _url = "/orders";

            IList<object> _values = new List<object>();
            _values.Add("market");
            _values.Add(_pair);
            _values.Add("side");
            _values.Add(_side == MarketSide.Bid ? "bid" : "ask");
            _values.Add("volume");
            _values.Add(_amount);
            _values.Add("price");
            _values.Add(_price);
            _values.Add("ord_type");
            switch (_type)
            {
                case OrderType.Limit:
                    _values.Add("limit");
                    break;
                case OrderType.Market:
                    _values.Add("market");
                    break;
            }

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
        #endregion

        #region OrderDetail
        public override OrderItem OrderDetail( string _id, params string[] _values)
        {
            return null;
        }
        #endregion
    }
}
