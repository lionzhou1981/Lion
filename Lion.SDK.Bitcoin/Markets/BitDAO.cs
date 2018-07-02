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
    public class BitDAO : MarketBase
    {
        private string key;
        private string secret;
        private long commandId = 1;

        #region CoinEx
        public BitDAO(string _key, string _secret)
        {
            this.key = _key;
            this.secret = _secret;

            base.Name = "BTD";
            base.WebSocket = "wss://api.hibtc.com/wsjoint";
            base.OnReceivedEvent += BitDAO_OnReceivedEvent;
        }
        #endregion

        #region BitDAO_OnReceivedEvent
        private void BitDAO_OnReceivedEvent(JToken _token)
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

                    default: this.OnLog("RECV", _json.ToString(Newtonsoft.Json.Formatting.None)); break;
                }
            }
        }
        #endregion

        #region SubscribeDepth
        public void SubscribeDepth(string _symbol, int _limit = 10, int _prec = "0")
        {
            if (this.Books[_symbol, "BID"] == null) { this.Books[_symbol, "BID"] = new BookItems("BID"); }
            if (this.Books[_symbol, "ASK"] == null) { this.Books[_symbol, "ASK"] = new BookItems("ASK"); }

            JObject _json = new JObject();
            _json["event"] = "subscribe";
            _json["channel"] = "depth";
            _json["pair"] = _symbol;
            _json["depth"] = _limit;
            _json["prec"] = _prec;
            this.Send(_json);
        }
        #endregion

        #region ReceivedDepth
        protected override void ReceivedDepth(string _symbol, string _type, JToken _token)
        {
            JObject _json = (JObject)_token;

            if (_type == "START")
            {
                #region START
                JArray _askArray = _json["asks"].Value<JArray>();
                JArray _bidArray = _json["bids"].Value<JArray>();

                BookItems _asks = new BookItems("ASK");
                BookItems _bids = new BookItems("BID");

                foreach (JArray _item in _askArray)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_symbol, "ASK", _price, _size, _id);
                    _asks.TryAdd(_id, _bookItem);
                }

                foreach (JArray _item in _bidArray)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = _price.ToString();

                    BookItem _bookItem = new BookItem(_symbol, "BID", _price, _size, _id);
                    _bids.TryAdd(_id, _bookItem);
                }

                this.Books[_symbol, "ASK"] = _asks;
                this.Books[_symbol, "BID"] = _bids;

                this.OnBookStarted(_symbol);
                #endregion
            }
            else if (_type == "UPDATE")
            {
                #region UPDATE
                JArray _array = _json["change"].Value<JArray>();
                foreach (JArray _item in _array)
                {
                    decimal _price = _item[0].Value<decimal>();
                    decimal _size = _item[1].Value<decimal>();
                    string _id = _price.ToString();

                    if (_size > 0)
                    {
                        BookItem _bookItem = this.Books[_symbol, "BID"].Update(_id, _size);
                        if (_bookItem == null)
                        {
                            _bookItem = this.Books[_symbol, "BID"].Insert(_id, _price, _size);
                            this.OnBookInsert(_bookItem);
                        }
                        else
                        {
                            this.OnBookUpdate(_bookItem);
                        }
                    }
                    else if (_size < 0)
                    {
                        BookItem _bookItem = this.Books[_symbol, "ASK"].Update(_id, _size);
                        if (_bookItem == null)
                        {
                            _bookItem = this.Books[_symbol, "ASK"].Insert(_id, _price, _size);
                            this.OnBookInsert(_bookItem);
                        }
                        else
                        {
                            this.OnBookUpdate(_bookItem);
                        }
                    }
                    else
                    {
                        BookItem _bookItem = this.Books[_symbol, "ASK"].Delete(_id);
                        if (_bookItem == null)
                        {
                            _bookItem = this.Books[_symbol, "BID"].Delete(_id);
                            if (_bookItem != null)
                            {
                                this.OnBookDelete(_bookItem);
                            }
                        }
                        else
                        {
                            this.OnBookDelete(_bookItem);
                        }
                    }
                }
                #endregion
            }
        }
        #endregion
    }
}
