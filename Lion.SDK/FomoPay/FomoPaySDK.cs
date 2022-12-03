using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Encrypt;
using Lion.Net;

namespace Lion.SDK.FomoPay
{
    public class FomoPaySDK
    {
        public static string Mid = "";
        public static string Sid = "";
        public static string Psk = "";
        public static string Auth = "";
        public static string NotifyUrl = "";
        public static string FomoPayUrl = "https://ipg.fomopay.net/api/orders";
        public static Dictionary<string, string> Currencies = new Dictionary<string,string>();

        public static void Init(JObject _settings)
        {
            Mid = _settings["Mid"].Value<string>();
            Sid = _settings["Sid"].Value<string>();
            Psk = _settings["Psk"].Value<string>();
            Auth = _settings["Auth"].Value<string>();
            NotifyUrl = _settings["NotifyUrl"].Value<string>();

            Currencies = new Dictionary<string, string>();
            foreach (JProperty _item in _settings["Currencies"]) { Currencies.Add(_item.Name, _item.Value.Value<string>()); }
        }

        #region PayDirectCard
        public static JObject PayDirectCard(
            string _orderNo,
            string _subject,
            string _description,
            decimal _amount,
            string _currencyCode,
            string _expiryYear,
            string _expiryMonth,
            string _nameOnCard,
            string _number,
            string _securityCode,
            string _ip,
            bool _retry = false
            )
        {
            if (!Currencies.ContainsKey(_currencyCode)) { throw new Exception($"WRONG_CURRENCY:{_currencyCode}"); }

            JObject _json = new JObject();
            _json["mode"] = "DIRECT";
            _json["orderNo"] = _orderNo;
            _json["subMid"] = Sid;
            _json["subject"] = _subject;
            _json["description"] = _description;
            _json["amount"] = _amount.ToString(Currencies[_currencyCode]);
            _json["currencyCode"] = _currencyCode;
            _json["notifyUrl"] = NotifyUrl;
            _json["sourceOfFund"] = "CARD";
            _json["transactionOptions"] = new JObject();
            _json["transactionOptions"]["timeout"] = 300;
            _json["transactionOptions"]["expiryYear"] = _expiryYear;
            _json["transactionOptions"]["expiryMonth"] = _expiryMonth;
            _json["transactionOptions"]["nameOnCard"] = _nameOnCard;
            _json["transactionOptions"]["number"] = _number;
            _json["transactionOptions"]["securityCode"] = _securityCode;
            _json["transactionOptions"]["ip"] = _ip;
            _json["transactionOptions"]["threeDSecure"] = "auto";

            Console.WriteLine(_json);

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse(_retry ? "PUT" : "POST", FomoPayUrl, "");
            _http.Request.ContentType = "application/json";
            _http.Request.Headers.Add("Authorization", $"Basic {Auth}");
            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Formatting.None)));

            Console.WriteLine((int)_http.Response.StatusCode);
            if ((int)_http.Response.StatusCode != 200 && (int)_http.Response.StatusCode != 201) { throw new Exception(((int)_http.Response.StatusCode).ToString()); }

            string _result = _http.GetResponseString(Encoding.UTF8);
            Console.WriteLine(_result);

            JObject _resultJson = JObject.Parse(_result);

            string _status = _resultJson["status"].Value<string>();
            if (_status != "SUCCESS") { throw new Exception(_status); }

            return _resultJson;
        }
        #endregion

        #region CheckNotify
        public static void CheckNotify(string _auth, string _body)
        {
            if (!_auth.StartsWith("FOMOPAY1-HMAC-SHA256 ")) { throw new Exception($"WRONG_AUTH_HEAD:{_auth}"); }
            _auth = _auth["FOMOPAY1-HMAC-SHA256 ".Length..];

            string[] _auths = _auth.Split(',');
            Dictionary<string, string> _authList = new Dictionary<string, string>();
            foreach (string _item in _auths)
            {
                string[] _items = _item.Split('=');
                if (_items.Length != 2) { continue; }
                _authList.Add(_items[0].ToLower(), _items[1]);
            }

            if (!_authList.ContainsKey("version")){ throw new Exception($"MISSING_VERSION"); }
            if(_authList["version"] != "1.1") { throw new Exception($"WRONG_VERSION:{_authList["version"]}"); }

            if (!_authList.ContainsKey("credential")) { throw new Exception($"MISSING_CREDENTIAL"); }
            if(_authList["credential"] != Mid) { throw new Exception($"WRONG_CREDENTIAL:{_authList["credential"]}"); }

            if (!_authList.ContainsKey("nonce")) { throw new Exception($"MISSING_NONCE"); }
            if (_authList["nonce"].Length < 16 || _authList["nonce"].Length > 64) { throw new Exception($"WRONG_NONCE:{_authList["nonce"]}");  }

            if (!_authList.ContainsKey("timestamp")) { throw new Exception($"MISSING_TIMESTAMP"); }
            if (!_authList.ContainsKey("signature")) { throw new Exception($"MISSING_SIGNATURE"); }

            DateTime _time = DateTimePlus.UnixTime2DateTime(long.Parse(_authList["timestamp"]));
            DateTime _now = DateTime.UtcNow;
            if (_time < _now.AddSeconds(-300) || _time > _now.AddSeconds(300)) { throw new Exception($"WRONG_TIMESTAMP:{_authList["timestamp"]}:{_now.ToString("yyyy-MM-dd HH:mm:ss")}"); }

            string _message = $"{_body}{_authList["timestamp"]}{_authList["nonce"]}";
            string _sign = HexPlus.ByteArrayToHexString(SHA.EncodeHMACSHA256(_message, Psk));

            if (_sign != _authList["signature"]) { throw new Exception($"WRONG_SIGNATURE"); }
        }
        #endregion
    }
}
