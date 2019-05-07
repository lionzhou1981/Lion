using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //BAT
    public class BasicAttentionToken
    {
        #region CheckTxidBalance
        public static string CheckTxidBalance(string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                string _url = $"http://api.ethplorer.io/getAddressInfo/{_address}?apiKey=freekey";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["tokens"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    string _name = _item["tokenInfo"]["symbol"].Value<string>().Trim();
                    if (_name.ToLower() != "bat") { continue; }
                    _jToken = _item;
                    break;
                }
                if (_jToken == null)
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken["balance"] + "";
                _outBalance = Common.Change2Decimal(_value);
                if (!_outBalance.ToString().Contains("."))
                {
                    _outBalance = _outBalance / 1000000000000000000M;
                }
                if (_outBalance < _balance)
                {
                    return _error;
                }

                return "";
            }
            catch (Exception _ex)
            {
                return _error;
            }
        }
        #endregion

    }
}
