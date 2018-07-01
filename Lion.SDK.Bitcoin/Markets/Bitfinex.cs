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
    public class Bitfinex :  MarketBase 
    {
        private string key;
        private string secret;
        private ConcurrentDictionary<string, string> Channels;

        #region Bitfinex
        public Bitfinex(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;

            base.Name = "BFX";
            base.WebSocket = "wss://api.bitfinex.com/ws/2";
            base.OnReceivedEvent += Bitfinex_OnReceivedEvent;

            this.Channels = new ConcurrentDictionary<string, string>();
        }
        #endregion

        #region Bitfinex_OnReceivedEvent
        private void Bitfinex_OnReceivedEvent(JToken _token)
        {
            if (_token is JObject)
            {
                JObject _item = (JObject)_token;
                switch (_item["event"].Value<string>())
                {
                    case "ping": this.Send("{\"event\":\"pong\"}"); break;
                    case "subscribed":
                        string _value = $"{_item["channel"].Value<string>()}.{_item["pair"].Value<string>()}.{_item["prec"].Value<string>()}.{_item["freq"].Value<string>()}.{_item["len"].Value<string>()}";
                        this.Channels.AddOrUpdate(_item["chanId"].Value<string>(), _value, (k, v) => _value);
                        break;
                    case "unsubscribed": this.Channels.TryRemove(_item["chanId"].Value<string>(), out string _out); break;
                    default: this.OnLog("RECV", _item.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
            else if (_token is JArray)
            {
                JArray _array = (JArray)_token;
                string _channelId = _array[0].Value<string>();

                if (_array.Count == 2 && _array[1].Type == JTokenType.Array)
                {
                    this.ReceiveSubscribe(_channelId, _array[1].Value<JArray>());
                }
                else if (_array.Count != 2)
                {
                    this.ReceiveSubscribe(_channelId, _array);
                }
            }
        }
        #endregion

        #region ReceiveSubscribe
        public void ReceiveSubscribe(string _channelId, JArray _item)
        {
            if (!this.Channels.TryGetValue(_channelId, out string _value))
            {
                this.OnLog("RECV", $"Channel not found {_channelId}");
                return;
            }

            string[] _channel = _value.Split('.');
            if (_channel[0] == "book")
            {
                this.ReceivedDepth(_channel[1], _item[0].Type == JTokenType.Array ? "START" : "UPDATE", _item);
            }
        }
        #endregion

        #region Clear
        protected override void Clear()
        {
            base.Clear();

            this.Channels?.Clear();
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, string _prec = "P0", string _freq = "F0", int _len = 100)
        {
            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = "book";
            _json["pair"] = _symbol;
            _json["prec"] = _prec;
            _json["freq"] = _freq;
            _json["len"] = _len;

            if (this.Books[_symbol, "BID"] == null) { this.Books[_symbol, "BID"] = new BookItems("BID"); }
            if (this.Books[_symbol, "ASK"] == null) { this.Books[_symbol, "ASK"] = new BookItems("ASK"); }

            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JArray _list = (JArray)_token;

            if (_type == "START")
            {
                BookItems _asks = new BookItems("ASK");
                BookItems _bids = new BookItems("BID");

                foreach (JArray _item in _list)
                {
                    decimal _price = _item[0].Value<decimal>();
                    int _count =  _item[1].Value<int>();
                    decimal _amount =_item[2].Value<decimal>();
                    string _side = _amount > 0 ? "BID" : "ASK";
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_symbol, _side, _price, Math.Abs(_amount), _id);
                    if (_side == "BID") { _bids.TryAdd(_id, _bookItem); }
                    if (_side == "ASK") { _asks.TryAdd(_id, _bookItem); }
                }

                this.Books[_symbol, "ASK"] = _asks;
                this.Books[_symbol, "BID"] = _bids;
                this.OnBookStarted(_symbol);
            }
            else
            {
                decimal _price = _list[0].Value<decimal>();
                int _count = _list[1].Value<int>();
                decimal _amount = _list[2].Value<decimal>();
                string _side = _amount > 0 ? "BID" : "ASK";

                BookItems _items = this.Books[_symbol, _side];
                if (_count == 0)
                {
                    BookItem _item = _items.Delete(_price.ToString());
                    if (_item != null)
                    {
                        this.OnBookDelete(_item);
                    }
                }
                else
                {
                    BookItem _item = _items.Update(_price.ToString(),Math.Abs(_amount));
                    if (_item == null)
                    {
                        _item = _items.Insert(_price.ToString(), _price, _amount);
                        this.OnBookInsert(_item);
                    }
                    else
                    {
                        this.OnBookUpdate(_item);
                    }
                }
            }
        }
        #endregion



        private static string url = "https://api.bitfinex.com";

        #region Account
        public JArray AccountInfo() => JArray.Parse(this.Post("/v1/account_infos"));

        public JObject AccountFees()=> JObject.Parse(this.Post("/v1/account_fees"));

        public JObject Summary() => JObject.Parse(this.Post("/v1/summary"));

        public JObject Deposit(
            string _method,
            string _wallet_name,
            int _renew
            ) => JObject.Parse(this.Post("/v1/deposit/new",
                "method" ,_method,
                "wallet_name",_wallet_name,
                "renew",_renew
                ));

        public JObject KeyPermissions() => JObject.Parse(this.Post("/v1/key_info"));

        public JArray MarginInformation() => JArray.Parse(this.Post("/v1/margin_infos"));

        public JArray WalletBalances() => JArray.Parse(this.Post("/v1/balances"));

        public JArray TransferBetweenWallets(
            string _currency, 
            decimal _amount, 
            string _from, 
            string _to
            ) => JArray.Parse(this.Post("/v1/transfer",
                "amount", _amount,
                "currency", _currency,
                "walletfrom", _from,
                "walletto", _to
                ));

        public JArray WithdrawalCoin(
            string _withdraw_type,
            string _walletselected,
            decimal _amount,
            string _address,
            string _payment_id = null
            ) => JArray.Parse(this.Post("/v1/withdraw",
                "withdraw_type", _withdraw_type,
                "walletselected", _walletselected,
                "amount", _amount,
                "address", _address,
                "payment_id", _payment_id
                ));


        public JArray WithdrawalBank(
            string _withdraw_type,
            string _walletselected,
            decimal _amount,
            string _account_name,
            string _account_number,
            string _swift,
            string _bank_name,
            string _bank_address,
            string _bank_city,
            string _bank_country,
            int _expressWire = 0,
            string _detail_payment = null,
            string _intermediary_bank_name = null,
            string _intermediary_bank_address = null,
            string _intermediary_bank_city = null,
            string _intermediary_bank_country = null,
            string _intermediary_bank_account = null,
            string _intermediary_bank_swift = null
            ) => JArray.Parse(this.Post("/v1/withdraw",
                "withdraw_type", _withdraw_type,
                "walletselected", _walletselected,
                "amount", _amount,
                "account_name", _account_name,
                "account_number", _account_number,
                "swift", _swift,
                "bank_name", _bank_name,
                "bank_address", _bank_address,
                "bank_city", _bank_city,
                "bank_country", _bank_country,
                "expressWire", _expressWire,
                "detail_payment", _detail_payment,
                "intermediary_bank_name", _intermediary_bank_name,
                "intermediary_bank_address", _intermediary_bank_address,
                "intermediary_bank_city", _intermediary_bank_city,
                "intermediary_bank_country", _intermediary_bank_country,
                "intermediary_bank_account", _intermediary_bank_account,
                "intermediary_bank_swift", _intermediary_bank_swift
                ));
        #endregion

        #region Orders
        public JObject NewOrder(
            string _type,
            string _symbol,
            string _side,
            decimal _amount,
            decimal _price,
            bool _is_hidden = false,
            bool _is_postonly = false,
            int _use_all_available = 0,
            bool _ocoorder = false,
            decimal _price_oco = 0M
            ) => JObject.Parse(this.Post("/v1/order/new",
                "type", _type,
                "symbol", _symbol,
                "side", _side,
                "amount", _amount,
                "price", _price,
                "exchange", "bitfinex",
                "is_hidden", _is_hidden,
                "is_postonly", _is_postonly,
                "use_all_available", _use_all_available,
                "ocoorder", _ocoorder,
                _side + "_price_oco", _price_oco
                ));

        public JObject MultipleNewOrders(JArray _orders) => JObject.Parse(this.Post("/v1/order/new/multi", "orders", _orders));
        public JObject CancelOrder(long _order_id) => JObject.Parse(this.Post("/v1/order/cancel", "order_id", _order_id));
        public JObject CancelMultipleOrders(JArray _orders) => JObject.Parse(this.Post("/v1/order/cancel/multi", "orders", _orders));
        public JObject CancelAllOrders() => JObject.Parse(this.Post("/v1/order/cancel/all"));

        public JObject ReplaceOrder(
         long _order_id,
         string _type,
         string _symbol,
         string _side,
         decimal _amount,
         decimal _price,
         bool _is_hidden = false,
         bool _is_postonly = false,
         bool _use_remaining = false
         ) => JObject.Parse(this.Post("/v1/order/cancel/replace",
             "order_id", _order_id,
             "type", _type,
             "symbol", _symbol,
             "side", _side,
             "amount", _amount,
             "price", _price,
             "exchange", "bitfinex",
             "is_hidden", _is_hidden,
             "is_postonly", _is_postonly,
             "use_remaining", _use_remaining
             ));

        public JObject OrderStatus(long _order_id) => JObject.Parse(this.Post("/v1/order/status", "order_id", _order_id));
        public JArray ActiveOrders() => JArray.Parse(this.Post("/v1/orders"));
        public JArray OrderStatus(int _limit) => JArray.Parse(this.Post("/v1/orders/hist", "limit", _limit));
        #endregion

        #region Positions
        public JArray ActivePositions() => JArray.Parse(this.Post("/v1/positions"));
        public JObject ClaimPosition(int _position_id,int _amount) => JObject.Parse(this.Post("/v1/positions/claim", "position_id", _position_id, "amount", _amount));
        #endregion

        #region Historical Data
        public JArray BalanceHistory(
            string _currency,
            string _since = null,
            string _until = null,
            int _limit = 500,
            string _wallet = null
            ) => JArray.Parse(this.Post("/v1/history",
                "currency", _currency,
                "since", _since == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_since)).ToString(),
                "until", _until == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_until)).ToString(),
                "limit", _limit,
                "wallet", _wallet
                ));

        public JArray DepositWithdrawalHistory(
            string _currency,
            string _method = null,
            string _since = null,
            string _until = null,
            int _limit = 500
            ) => JArray.Parse(this.Post("/v1/history/movements",
                "currency", _currency,
                "method", _method,
                "since", _since == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_since)).ToString(),
                "until", _until == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_until)).ToString(),
                "limit", _limit
                ));

        public JArray PastTrades(
            string _symbol,
            string _timestamp = null,
            string _until = null,
            int _limit = 500
            ) => JArray.Parse(this.Post("/v1/mytrades",
                "symbol", _symbol,
                "timestamp", _timestamp == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_timestamp)).ToString(),
                "until", _until == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_until)).ToString(),
                "limit", _limit
                ));

        #endregion

        #region Margin Funding
        public JObject NewOffer(
            string _currency,
            decimal _amount,
            decimal _rate,
            int _period,
            string _direction
            ) => JObject.Parse(this.Post("/v1/offer/new",
                "currency", _currency,
                "amount", _amount,
                "rate", _rate,
                "period", _period,
                "direction", _direction
                ));

        public JObject CancelOffer(long _offer_id) => JObject.Parse(this.Post("/v1/offer/cancel", "offer_id", _offer_id));
        public JObject OfferStatus(long _offer_id) => JObject.Parse(this.Post("/v1/offer/status", "offer_id", _offer_id));
        public JArray ActiveCredits() => JArray.Parse(this.Post("/v1/credits"));
        public JArray Offers() => JArray.Parse(this.Post("/v1/offers"));
        public JObject OffersHistory(int _limit) => JObject.Parse(this.Post("/v1/offer/hist", "limit", _limit));

        public JArray PastFundingTrades(
            string _symbol,
            string _until = null,
            int _limit_trades = 500
            ) => JArray.Parse(this.Post("/v1/mytrades_funding",
                "symbol", _symbol,
                "until", _until == null ? null : DateTimePlus.DateTime2JSTime(DateTime.Parse(_until)).ToString(),
                "limit_trades", _limit_trades
                ));

        public JArray ActiveFundingUsedInAMarginPosition() => JArray.Parse(this.Post("/v1/taken_funds"));
        public JArray ActiveFundingNotUsedInAMarginPosition() => JArray.Parse(this.Post("/v1/unused_taken_funds"));
        public JArray TotalTakenFunds(string _position_pair,decimal _total_swaps) => JArray.Parse(this.Post("/v1/total_taken_funds", "position_pair", _position_pair, "total_swaps", _total_swaps));
        public JObject CloseMarginFunding(int _swap_id) => JObject.Parse(this.Post("/v1/funding/close", "swap_id", _swap_id));
        public JObject ClosePosition(int _position_id) => JObject.Parse(this.Post("/v1/positions/close", "position_id", _position_id));
        #endregion

        #region MarketTicker
        public static JArray MarketTicker(string _symbol)
        {
            try
            {
                WebClientPlus _webClient = new WebClientPlus(5000);
                string _result = _webClient.DownloadString($"{Bitfinex.url}/v2/ticker/{_symbol}");
                _webClient.Dispose();

                return JArray.Parse(_result);
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Post
        private string Post(string _path,params object[] _keyValue)
        {
            string _url = Bitfinex.url + _path;
            JObject _json = new JObject();
            _json["request"] = _path;
            _json["nonce"] = DateTime.Now.Ticks.ToString();
            for(int i = 0; i < (_keyValue.Length - 1); i+=2)
            {
                if(_keyValue[i + 1] == null) { continue; }

                Type _valueType = _keyValue[i + 1].GetType();
                if (_valueType == typeof(int)) { _json[_keyValue[i]] = (int)_keyValue[i + 1]; }
                else if (_valueType == typeof(bool)) { _json[_keyValue[i]] = (bool)_keyValue[i + 1]; }
                else if (_valueType == typeof(long)) { _json[_keyValue[i]] = (long)_keyValue[i + 1]; }
                else if (_valueType == typeof(JArray)) { _json[_keyValue[i]] = (JArray)_keyValue[i + 1]; }
                else { _json[_keyValue[i]] = _keyValue[i + 1].ToString(); }
            }

            string _payload = _json.ToString(Newtonsoft.Json.Formatting.None);
            string _payloadBase64 = Base64.Encode(Encoding.UTF8.GetBytes(_payload));
            string _sign = SHA.EncodeHMACSHA384(_payloadBase64, this.secret);

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", _url, "");
            _http.Request.Headers.Add("Content-Type", "application/json");
            _http.Request.Headers.Add("Accept", "application/json");
            _http.Request.Headers.Add("X-BFX-APIKEY", this.key);
            _http.Request.Headers.Add("X-BFX-PAYLOAD", _payloadBase64);
            _http.Request.Headers.Add("X-BFX-SIGNATURE", _sign);
            _http.EndResponse();

            string _result = _http.GetResponseString(Encoding.UTF8);

            return _result;
        }
        #endregion
    }
}
