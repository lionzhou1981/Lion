using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Explorers
{
    public class BCHN
    {


        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://api.blockchair.com/bitcoin-cash/stats";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                return _json["data"]["blocks"].Value<string>();
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
                string _url = $"https://bch-chain.api.btc.com/v3/tx/{_txid}?verbose=3";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JToken _jToken = _json["data"]["outputs"][_index];

                //address
                _error = "address";
                string _cashAddr = _jToken["addresses"][0].Value<string>().Trim();
                _cashAddr = Change2NewAddress(_cashAddr);
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

        #region Change2NewAddress
        public static string Change2NewAddress(string _address)
        {
            try
            {
                WebClientPlus _client = new WebClientPlus(10000);
                string _return = _client.DownloadString($"https://cashaddr.bitcoincash.org/convert?address={_address}");
                _client.Dispose();
                JObject _json = JObject.Parse(_return);
                return _json["cashaddr"].Value<string>().Trim();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
