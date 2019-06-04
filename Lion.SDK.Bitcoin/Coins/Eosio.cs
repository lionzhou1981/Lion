using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //EOS
    public class Eosio
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                //string _url = "https://api.eospark.com/api?module=block&action=get_latest_block&apikey=a9564ebc3289b7a14551baf8ad5ec60a";
                //string _url = "https://eospark.com/api/v2/chain/baseinfo";
                string _url = "https://eospark.com/api/v2/overview/high_refresh";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                return _json["data"]["head_block_num"].Value<string>();
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
                //string _url = $"https://eospark.com/api/v2/account/{_address}/info";
                string _url = $"https://api.eospark.com/api?module=account&action=get_account_balance&apikey=b16c6fa55f862b8f8b0f98aed2062b59&account={_address}";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);

                //balance
                _error = "balance";
                //string _value = _json["data"]["resource_info"]["core_liquid_balance"] + "";
                string _value = _json["data"]["balance"] + "";
                //_value = _value.ToLower().Replace("eos", "").Trim();
                //_outBalance = Common.Change2Decimal(_value);
                _outBalance = decimal.Parse(_value);
                //if (!_outBalance.ToString().Contains("."))
                //{
                //    _outBalance = _outBalance / 100000000M;
                //}
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
