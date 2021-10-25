using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Lion.Encrypt;
using Lion.Net;

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
            catch (Exception)
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
                //string _url = $"https://api.blockcypher.com/v1/eth/main/addrs/{_address}/balance";
                string _url = $"https://api.etherscan.io/api?module=account&action=balance&address={_address}&tag=latest&apikey=YourApiKeyToken";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);

                //balance
                _error = "balance";
                string _value = _json["result"] + "";
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
            catch (Exception)
            {
                return _error;
            }
        }
        #endregion

        public static Address GenerateAddress(string _existsPrivateKey = "")
        {
            Address _address = new Address();
            _address.PrivateKey = string.IsNullOrWhiteSpace(_existsPrivateKey) ? RandomPlus.GenerateHexKey(64) : _existsPrivateKey;
            
            _address.PublicKey =  HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_address.PrivateKey));
            _address.PublicKey = _address.PublicKey.Substring(2);//remove 04 start;
            var _keccakHasher = new Keccak256();
            var _hexAddress = _keccakHasher.ComputeHashByHex(_address.PublicKey);
            _address.Text = "0x" + _hexAddress.Substring(_hexAddress.Length - 40);

            return _address;
        }
    }
}
