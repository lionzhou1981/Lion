using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Lion;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
    //BTC
    public class Bitcoin
    {
        #region IsAddress
        public static bool IsAddress(string _address, out byte? _version)
        {
            try
            {
                if (_address.StartsWith("bc1") || _address.StartsWith("tb1"))
                {
                    #region Bech32
                    if (_address.Length == 42)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x00 : 0x6F);
                    }
                    else if (_address.Length == 62)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x05 : 0xC4);
                    }
                    else
                    {
                        _version = null;
                        return false;
                    }

                    try
                    {
                        Bech32.Bech32Decode(_address, out byte[] _hrp);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                    #endregion
                }
                else
                {
                    #region Base58
                    byte[] _bytes = Base58.Decode(_address);
                    if (_bytes.Length != 25) { throw new Exception(); }
                    _version = _bytes[0];

                    byte[] _byteBody = new byte[21];
                    Array.Copy(_bytes, 0, _byteBody, 0, 21);
                    byte[] _byteCheck = new byte[4];
                    Array.Copy(_bytes, 21, _byteCheck, 0, 4);
                    string _checkSum = HexPlus.ByteArrayToHexString(_byteCheck);

                    byte[] _sha256A = SHA.EncodeSHA256(_byteBody);
                    byte[] _sha256B = SHA.EncodeSHA256(_sha256A);
                    Array.Copy(_sha256B, 0, _byteCheck, 0, 4);
                    string _caleSum = HexPlus.ByteArrayToHexString(_byteCheck);

                    return _checkSum == _caleSum;
                    #endregion
                }
            }
            catch
            {
                _version = null;
                return false;
            }
        }
        #endregion

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
 

        public static Tuple<bool, string, string, string> GenerateAddress(string _existsPrivateKey = "", bool _mainNet = true)
        {
            var _netVersion = _mainNet ? "00" : "ef";
            var _privateKey = string.IsNullOrWhiteSpace(_existsPrivateKey) ? Lion.RandomPlus.GenerateHexKey(64) : _existsPrivateKey;
            var _secpHashed = new Lion.Encrypt.Secp256k1().PrivateKeyToPublicKey(_privateKey);
            if (!_secpHashed.Item1)
                return new Tuple<bool, string, string, string>(false, _secpHashed.Item2, "", "");
            string _publicKey = _secpHashed.Item2;

            HashAlgorithm _shahasher = HashAlgorithm.Create("SHA-256");
            var _sha1 = _shahasher.ComputeHash(Lion.HexPlus.HexStringToByteArray(_publicKey));
            var _ripemd = new Lion.Encrypt.RIPEMD160Managed();
            var _ripemdHashed = BitConverter.ToString(_ripemd.ComputeHash(_sha1)).Replace("-", "");
            var _versioned = _netVersion + _ripemdHashed;
            var _sha2 = _shahasher.ComputeHash(Lion.HexPlus.HexStringToByteArray(_versioned));
            var _sha3 = _shahasher.ComputeHash(_sha2);
            var _verifyCode = BitConverter.ToString(_sha3).Replace("-", "").Substring(0, 8);
            var _address = Lion.Encrypt.Base58.Encode(_versioned + _verifyCode);
            _address = (_secpHashed.Item3 > 1 ? "11" : "1") + _address;
            return new Tuple<bool, string, string, string>(true, _privateKey, _publicKey, _address);
        }
    }
}