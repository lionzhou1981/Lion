using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //USDT
    public class TetherUS
    {
        #region CheckTxidBalance
        internal static string Name = "TetherUS";
        public static string CheckTxidBalance(string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                string _url = $"https://api.omniwallet.org/v2/address/addr/";
                string _postData = $"addr={_address}";
                WebClientPlus _webClient = new WebClientPlus(10000);
                _webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                string _result = _webClient.UploadString(_url, _postData);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json[_address]["balance"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    string _name = _item["propertyinfo"]["name"].Value<string>().Trim();
                    if (_name.ToLower() != Name.ToLower()) { continue; }
                    _jToken = _item;
                    break;
                }
                if (_jToken == null)
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken["value"].Value<string>();
                _outBalance = decimal.Parse(_value);
                if (!_value.Contains("."))
                {
                    _outBalance = _outBalance / 100000000M;
                }
                if (_outBalance < _balance)
                {
                    return _error;
                }

                return "";
            }
            catch (Exception)
            {
                return _error;
            }
        }
        #endregion

    }
}
