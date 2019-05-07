using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //ZEC
    public class Zcash
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://api.zcha.in/v2/mainnet/blocks?sort=height&direction=descending&limit=1&offset=0";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JArray _jArray = JArray.Parse(_result);
                return _jArray[0]["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion

        #region CheckTxidBalance
        public static string CheckTxidBalance(string _txid, int _index, string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                //string _url = $"http://www.tokenview.com:8088/search/{_txid}";
                //string _url = $"https://chain.so/api/v2/get_tx_outputs/zec/{_txid}/{_index}";
                string _url = $"https://zcashnetwork.info/api/tx/{_txid}";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["vout"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    int _n = _item["n"].Value<int>();
                    if (_n != _index) { continue; }
                    _jToken = _item;
                    break;
                }
                if (_jToken == null)
                {
                    return _error;
                }

                //address
                _error = "address";
                string _cashAddr = _jToken["scriptPubKey"]["addresses"][0].Value<string>().Trim();
                if (_cashAddr != _address.Trim())
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
                if (_outBalance != _balance)
                {
                    return _error;
                }

                //spent
                _error = "spent";
                if (_jToken["spentTxId"].HasValues || _jToken["spentIndex"].HasValues || _jToken["spentHeight"].HasValues)
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
