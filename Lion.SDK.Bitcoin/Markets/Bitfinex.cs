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
    public class Bitfinex :  MarketBase, IDisposable
    {
        private static string url = "https://api.bitfinex.com";
        private static string ws = "wss://api.bitfinex.com/ws";

        public string symbol;
        private string key;
        private string secret;
        private bool running = false;
        private ClientWebSocket socket = null;
        private Thread thread;
        private ConcurrentDictionary<string, string[]> SubscribedChannels;

        public Bitfinex(string _symbol, string _key, string _secret)
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

                    Task _task = this.socket.ConnectAsync(new Uri(Bitfinex.ws), CancellationToken.None);
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

                    if (_task.Result == null || _task.Result.MessageType == WebSocketMessageType.Close)
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
            this.running = false;
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
        public void Receive(JToken _json)
        {
            if (_json is JObject)
            {
                JObject _item = (JObject)_json;
                switch (_item["event"].Value<string>())
                {
                    case "info": break;
                    case "error": this.OnError(_item["code"].Value<string>(), _item["msg"].Value<string>()); break;
                    case "subscribed":
                        this.SubscribedChannels.AddOrUpdate(
                            _item["chanId"].Value<string>(),
                            new string[] { _item["channel"].Value<string>(), this.symbol },
                            (k, v) => new string[] { _item["channel"].Value<string>(), this.symbol }
                            );
                        break;
                    case "unsubscribed":
                        string[] _values;
                        if (!this.SubscribedChannels.TryRemove(_item["chanId"].Value<string>(), out _values))
                        {
                            this.OnError(_item["code"].Value<string>(), _item["msg"].Value<string>()); break;
                        }
                        break;
                    default: base.OnWebSocketReceived(_json); break;
                }
            }
            else if (_json is JArray)
            {
                JArray _array = (JArray)_json;
                string _channelId = _array[0].Value<string>();

                if (_array.Count == 2)
                {
                    if (_array[1].GetType() == typeof(JArray))
                    {
                        foreach (JArray _item in _array[1])
                        {
                            this.ReceiveSubscribe(_channelId, _item);
                        }
                    }
                }
                else
                {
                    this.ReceiveSubscribe(_channelId, _array);
                }
            }
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
        public void Subscribe(string _channel, string _symbol, params object[] _keyValues)
        {
            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = _channel;
            _json["pair"] = _symbol;
            if (_keyValues.Length == 0)
            {
                _json["prec"] = "P0";
                _json["freq"] = "F0";
                _json["len"] = 100;
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
        public void Unsubscribe(string _channelId)
        {
            JObject _json = new JObject();
            _json["event"] = "unsubscribe";
            _json["chanId"] = _channelId;

            this.Send(_json);
        }
        #endregion

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
