using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //ETH
    public class Ethereum
    {
        public static bool IsAddress(string _address)
        {
            if (!_address.StartsWith("0x")) { return false; }

            string _num64 = _address.Substring(2);
            BigInteger _valueOf = BigInteger.Zero;
            if (BigInteger.TryParse(_num64, System.Globalization.NumberStyles.AllowHexSpecifier, null, out _valueOf))
            {
                return _valueOf != BigInteger.Zero;
            }
            else
            {
                return false;
            }
        }

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=YourApiKeyToken";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                _result = _json["result"].Value<string>();
                int _height = Convert.ToInt32(_result, 16);
                return _height.ToString();
            }
            catch (Exception _ex)
            {
                return "";
            }
        }
        #endregion

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
                //string _url = $"http://api.ethplorer.io/getAddressInfo/0x32Be343B94f860124dC4fEe278FDCBD38C102D88?apiKey=freekey";
                string _url = $"https://api.blockcypher.com/v1/eth/main/addrs/{_address}/balance";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);

                //balance
                _error = "balance";
                string _value = _json["balance"] + "";
                _outBalance = decimal.Parse(_value);
                if (!_value.Contains("."))
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
