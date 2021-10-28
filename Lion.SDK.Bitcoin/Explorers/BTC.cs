using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Explorers
{
    public class BTC
    {

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://api.blockcypher.com/v1/btc/main";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                return _json["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion

        #region GetTxidInfo
        public static JObject GetTxidInfo(string _txid)
        {
            try
            {
                string _url = $"https://chain.api.btc.com/v3/tx/{_txid}?verbose=3";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                return _json;
            }
            catch (Exception)
            {
                return null;
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
                string _url = $"https://chain.api.btc.com/v3/tx/{_txid}?verbose=3";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JToken _jToken = _json["data"]["outputs"][_index];

                //address
                _error = "address";
                if (_jToken["addresses"][0].Value<string>().Trim() != _address.Trim())
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
                if (_jToken["spent_by_tx"].HasValues || _jToken["spent_by_tx_position"].Value<string>().Trim() != "-1")
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

        #region CheckTxidBalance
        public static string CheckTxidBalance(WebClientPlus _webClient, string _txid, int _index, string _address, decimal _balance)
        {
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                string _url = $"https://chain.api.btc.com/v3/tx/{_txid}?verbose=3";
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JToken _jToken = _json["data"]["outputs"][_index];

                //address
                _error = "address";
                if (_jToken["addresses"][0].Value<string>().Trim() != _address.Trim())
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken["value"].Value<string>();
                decimal _infoBalance = decimal.Parse(_value);
                if (!_value.Contains("."))
                {
                    _infoBalance = _infoBalance / 100000000M;
                }
                if (_infoBalance != _balance)
                {
                    return _error;
                }

                //spent
                _error = "spent";
                if (_jToken["spent_by_tx"].HasValues || _jToken["spent_by_tx_position"].Value<string>().Trim() != "-1")
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
