using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Explorers
{
    public class XRP
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                //string _url = "https://data.ripple.com/v2/health/importer?verbose=true";
                string _url = "https://data.ripple.com/v2/ledgers/";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                //return _json["last_validated_ledger"].Value<string>();
                return _json["ledger"]["ledger_index"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion


        #region CheckTxidBalance
        public static string CheckTxidBalance(string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                string _url = $"https://data.ripple.com/v2/accounts/{_address}/balances";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["balances"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    string _currency = _item["currency"].Value<string>();
                    if (_currency.ToLower() != "xrp") { continue; }
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
                //_outBalance = Common.Change2Decimal(_value);
                _outBalance = decimal.Parse(_value);
                //if (!_outBalance.ToString().Contains("."))
                //{
                //    //_outBalance = _outBalance / 1000000000000000000M;
                //    _outBalance = _outBalance / 1000000M;
                //}
                if (_outBalance < _balance)
                {
                    return _error;
                }

                return "";
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex.Message);
                return _error;
            }
        }
        #endregion
    }
}
