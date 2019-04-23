using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Monero
    {
        const int AddressLength = 95;
        const int PaymentIdLength = 64;
        public static bool IsAddress(string _address)
        {
            _address = _address.Trim();
            var _paymentid = _address.Contains(":") ? _address.Split(':')[1] : "";
            _address = _address.Contains(":") ? _address.Split(':')[0] : _address;
            if (string.IsNullOrEmpty(_address) || _address.Length != AddressLength ||
                _address[0] != '4' ||
                _address[1] < '0' || _address[1] > 'B'
            )
            {
                return false;
            }
            for (var i = 2; i < _address.Length; i++)
            {
                var _currentChar = _address[i];
                if (_currentChar < '0' || _currentChar > 'z')
                {
                    return false;
                }
            }
            if(!string.IsNullOrWhiteSpace(_paymentid))
            {
                if (_paymentid.Length != PaymentIdLength)
                    return false;

                for (var i = _paymentid.Length - 1; i >= 0; i--)
                {
                    var currentChar = _paymentid[i];
                    if (currentChar < '0' || char.ToUpper(currentChar) > 'F')
                    {
                        return false;
                    }
                }

                return true;
            }
            return true;
        }

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                //string _url = "https://xmr.tokenview.com/api/blocks/xmr/1/1";
                string _url = "https://monerohash.com/api/stats?_=" + DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString() + DateTime.UtcNow.Millisecond.ToString();
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                //return _json["data"][0]["block_no"].Value<string>();
                return _json["network"]["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
