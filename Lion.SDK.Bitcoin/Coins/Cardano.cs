using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //ADA
    public class Cardano
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://cardanoexplorer.com/api/blocks/pages";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                string _cbeEpoch = _json["Right"][1][0]["cbeEpoch"].Value<string>();
                string _cbeSlot = _json["Right"][1][0]["cbeSlot"].Value<string>();

                return $"{_cbeEpoch}{_cbeSlot.PadLeft(6, '0')}";
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
                string _url = $"https://cardanoexplorer.com/api/txs/summary/{_txid}";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JToken _jToken = _json["Right"]["ctsOutputs"][_index];

                //address
                _error = "address";
                if (_jToken[0].Value<string>().Trim() != _address.Trim())
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken[1]["getCoin"].Value<string>();
                _outBalance = decimal.Parse(_value);
                if (!_value.Contains("."))
                {
                    _outBalance = _outBalance / 1000000M;
                }
                if (_outBalance != _balance)
                {
                    return _error;
                }

                ////spent
                //_error = "spent";
                //if (_jToken["spent_by_tx"].HasValues || _jToken["spent_by_tx_position"].Value<string>().Trim() != "-1")
                //{
                //    return _error;
                //}

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
