using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using Lion;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
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

        public static string PrivKey2PubKey(string _privateKey, bool _mainNet = true)
        {
            int _zeros = 0;
            return "";
            //return new Secp256k1().PrivateKeyToPublicKey(_privateKey, out _zeros);
        }

        /// <summary>
        /// compress  private key 
        /// https://sourceforge.net/p/bitcoin/mailman/bitcoin-development/thread/CAPg+sBhDFCjAn1tRRQhaudtqwsh4vcVbxzm+AA2OuFxN71fwUA@mail.gmail.com/
        /// </summary>
        /// <param name="_uncompressKey"></param>
        /// <returns></returns>
        public static string CompressPrivateKey(string _uncompressKey, bool _mainnet)
        {
            string _orgKey = string.Join("", (!_mainnet ? "ef" : "80"), _uncompressKey);
            string _addmin = HexPlus.ByteArrayToHexString(Lion.Encrypt.SHA.EncodeSHA256(Lion.Encrypt.SHA.EncodeSHA256(Lion.HexPlus.HexStringToByteArray(_orgKey))).Take(4).ToArray());
            return Base58.Encode(_orgKey + _addmin);
        }

        internal static void AddBytes(List<byte> _bytes, params byte[] _addBytes)
        {
            _bytes.AddRange(_addBytes);
        }

        internal static void AddBytesPadRightZero(List<byte> _bytes, int _length, params byte[] _addBytes)
        {
            _bytes.AddRange(_addBytes);
            if (_length <= _addBytes.Length)
                return;
            for (var i = 0; i < _length - _addBytes.Length; i++)
            {
                _bytes.Add(0x00);
            }
        }

        const decimal SatoshiBase = 100000000M;

        internal static BigInteger DecimalToSatoshi(decimal _value)
        {
            int _valuePay = decimal.ToInt32(SatoshiBase * _value);
            return _valuePay;
        }

        internal static void SendValueToPubKey(List<byte> _scripts, string _pubKey, BigInteger _value)
        {
            AddBytesPadRightZero(_scripts, 8, _value.ToByteArray());
            AddPubKey(_scripts, _pubKey);
        }

        internal static void AddPubKey(List<byte> _scripts, string _pubKey)
        {
            var _outputPubKeyBytes = Lion.HexPlus.HexStringToByteArray(_pubKey);
            AddBytes(_scripts, ((BigInteger)_outputPubKeyBytes.Length).ToByteArray());
            AddBytes(_scripts, _outputPubKeyBytes);
        }

        public static void SendToAddress()
        {

            //Console.WriteLine(Lion.HexPlus.ByteArrayToHexString("134,190,215,244,208,241,28,107,96,148,77,109,254,207,143,246,185,47,53,143,150,234,203,196,10,71,78,18,72,77,149,41".Split(',').Select(t => byte.Parse(t)).ToArray()));
            //            
            //var _privateKey = "e218a0fcc2d8c42988439808f7c95cb0d6ee091383f35b6c6282c97ece3ce240";
            //

            //structure
            //no sig template:"01000000015a9a9769092219a975afc9d2ca49b80ba7fdc901dcf445e6e7cebe3f994952ed010000001976a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88acffffffff0210270000000000001976a91465ce9f49184862b59fc8f051c5ebdee0044f387d88ac645e0100000000001976a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88ac0000000001000000"
            //01000000 version
            //01 inputcount
            //5a9a9769092219a975afc9d2ca49b80ba7fdc901dcf445e6e7cebe3f994952ed input txid
            //01000000 input tx vout number
            //1976a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88ac input scriptpubkey
            //ffffffff seq number
            //02 payment count
            //027000000000000 payment value1 (satoshi)
            //1976a91465ce9f49184862b59fc8f051c5ebdee0044f387d88ac receive address 1 scriptpubkey
            //645e010000000000 payment value2 (satoshi)
            //1976a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88ac receive address 2 scriptpubkey
            //00000000 lock time
            //01000000 hash version
            //scriptsig template:
            //"01000000015a9a9769092219a975afc9d2ca49b80ba7fdc901dcf445e6e7cebe3f994952ed01000000{ECDSA(SHA256(SHA256(no sig template)).reverse()).RS}}0141{input scriptpubkey}ffffffff0210270000000000001976a91465ce9f49184862b59fc8f051c5ebdee0044f387d88ac645e0100000000001976a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88ac0000000001000000"
            string _lastTransactionId = "b956f26f85f00c5c79bae6276432aa5d977a2629bf665eebd3a940a4fa659905";
            //string _outputPubKey = "76a914767bf5194b5fc2f3922505a926f5484a9d52aa4b88ac";
            //string _outputPrivateKey = "e218a0fcc2d8c42988439808f7c95cb0d6ee091383f35b6c6282c97ece3ce240";

            string _lastTransactionId2 = "2b7e11d90af2ce4995ae98c2ccbedd2f4df9d81fd8eafdb4505cc935c41b2dc6";
            string _outputPubKey = "76a91465ce9f49184862b59fc8f051c5ebdee0044f387d88ac";
            string _outputPrivateKey = "2f3acf9eca4066b71763d67903f8c2f37dbfc94acd72c374bd3fef3ca6d78477";

            var _addr = Lion.SDK.Bitcoin.Coins.Bitcoin.GenerateAddress(out string _data, _outputPrivateKey, false);
            var _outputScriptPubKey = _addr.PublicKey;
            BigInteger _outputCount = 1;
            //var _pubKeyHash = new SHA256Managed().ComputeHash(Lion.HexPlus.HexStringToByteArray(_pubKey));
            //var _ripemd = new RIPEMD160Managed();
            //var _ripemdHashedInput = _ripemd.ComputeHash(_pubKeyHash);
            //Console.WriteLine(Lion.HexPlus.ByteArrayToHexString(_ripemdHashedInput));

            BigInteger _outputBalance = DecimalToSatoshi(0.000697M);
            BigInteger _payValue = DecimalToSatoshi(0.0001M);
            BigInteger _fee = DecimalToSatoshi(0.0001M);
            BigInteger _balance = _outputBalance - _payValue - _fee;
            var _outputPubKeyBytes = Lion.HexPlus.HexStringToByteArray(_outputPubKey);

            string _paytoPubKey = "76a914fc0fe6f869f4075d07dfae8146a3be074ef8ce1088ac";

            BigInteger _lastTransactionOutPutIndex = 1;
            BigInteger _lastTransactionOutPutIndex2 = 0;
            var _templateStartNoSign = new List<byte>();
            AddBytesPadRightZero(_templateStartNoSign, 4, 0x01);//version;
            AddBytes(_templateStartNoSign, 0x02);//input count;
            AddBytes(_templateStartNoSign, Lion.HexPlus.HexStringToByteArray(_lastTransactionId).Reverse().ToArray());
            AddBytesPadRightZero(_templateStartNoSign, 4, _lastTransactionOutPutIndex.ToByteArray().Reverse().ToArray());
            AddBytes(_templateStartNoSign, Lion.HexPlus.HexStringToByteArray(_lastTransactionId2).Reverse().ToArray());
            AddBytesPadRightZero(_templateStartNoSign, 4, _lastTransactionOutPutIndex2.ToByteArray().Reverse().ToArray());
            //-------------end with no pubkey script--------
            //start from output,not contains sign,not contains hash type
            var _templateFromOutPutToHashType = new List<byte>();
            AddBytes(_templateFromOutPutToHashType, 0xff, 0xff, 0xff, 0xff);//transaction seq
            AddBytes(_templateFromOutPutToHashType, (_outputCount + 1).ToByteArray().Reverse().ToArray());//transaction seq
            SendValueToPubKey(_templateFromOutPutToHashType, _paytoPubKey, _payValue);
            SendValueToPubKey(_templateFromOutPutToHashType, _outputPubKey, _balance);
            AddBytesPadRightZero(_templateFromOutPutToHashType, 4, 0x00);
            //--------------end output------------------------
            var _arrayToSign = new List<byte>();//sign bytes;
            _arrayToSign.AddRange(_templateStartNoSign);
            AddPubKey(_arrayToSign, _outputPubKey);
            _arrayToSign.AddRange(_templateFromOutPutToHashType);
            AddBytesPadRightZero(_arrayToSign, 4, 0x01);//hash type;
            //generate sig
            Console.WriteLine("Script:" + Lion.HexPlus.ByteArrayToHexString(_arrayToSign.ToArray()));
            var _sciptSig = BitConverter.ToString(new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_arrayToSign.ToArray()))).Replace("-", "").ToLower();
            Console.WriteLine("ScriptSig:" + _sciptSig);
            //ECDSA
            BigInteger _k = BigInteger.Parse($"0{Lion.RandomPlus.GenerateHexKey()}", NumberStyles.HexNumber);
            var _Gk = Secp256k1.G.Multiply(_k);
            var _r = _Gk.X;
            var _e = BigInteger.Parse($"0{_sciptSig}", NumberStyles.HexNumber);
            var _d = BigInteger.Parse($"0{_outputPrivateKey}", System.Globalization.NumberStyles.HexNumber);
            var _s = _r * _d;
            _s = _s + _e;
            _s = _s * _k.ModInverse(Secp256k1.N);
            _s = _s % Secp256k1.N;
            if (_s.CompareTo(Secp256k1.HalfN) > 0)
                _s = Secp256k1.N - _s;
            Console.WriteLine($"K:{_k.ToString()}\r\nR:{_r.ToString()}\r\nS:{_s.ToString()}");
            var _rbytes = _r.ToByteArray().Reverse().ToList();
            var _sbytes = _s.ToByteArray().Reverse().ToList();
            var _allBytes = new List<byte>();
            BigInteger _rsLength = _rbytes.Count() + _sbytes.Count() + 4;
            _allBytes.Add(0x30);
            _allBytes.AddRange(_rsLength.ToByteArray());
            _allBytes.Add(0x02);
            _allBytes.AddRange(((BigInteger)_rbytes.Count()).ToByteArray());
            _allBytes.AddRange(_rbytes.ToArray());
            _allBytes.Add(0x02);
            _allBytes.AddRange(((BigInteger)_sbytes.Count()).ToByteArray());
            _allBytes.AddRange(_sbytes.ToArray());
            _allBytes.Add(0x01);
            _allBytes.Add(0x41);
            _allBytes.InsertRange(0, ((BigInteger)_allBytes.Count - 1).ToByteArray());
            BigInteger _sigLength = BigInteger.Parse(_outputScriptPubKey, NumberStyles.HexNumber).ToByteArray().Length + _allBytes.Count;
            var _arrayToPay = new List<byte>();//sign bytes;
            _arrayToPay.AddRange(_templateStartNoSign);
            AddBytes(_arrayToPay, _sigLength.ToByteArray().Reverse().Where(t => t != 0x00).ToArray());
            AddBytes(_arrayToPay, _allBytes.ToArray());
            AddBytes(_arrayToPay, Lion.HexPlus.HexStringToByteArray(_outputScriptPubKey));
            _arrayToPay.AddRange(_templateFromOutPutToHashType);
            Console.WriteLine("Pay:" + Lion.HexPlus.ByteArrayToHexString(_arrayToPay.ToArray()));
        }

        public static Address GenerateAddress(out string _uncompressKey, string _existsPrivateKey = "", bool _mainNet = true)
        {
            string _netVersion = _mainNet ? "00" : "6f";
            string _privateKey = string.IsNullOrWhiteSpace(_existsPrivateKey) ? Lion.RandomPlus.GenerateHexKey(64) : _existsPrivateKey;
            _uncompressKey = _privateKey;
            BigInteger _bigPrivateKey = BigInteger.Parse("0" + _privateKey, System.Globalization.NumberStyles.HexNumber);
            var _publicKey = Secp256k1.PrivateKeyToPublicKey(_bigPrivateKey);
            SHA256Managed sha256 = new SHA256Managed();
            var _ripemd = new RIPEMD160Managed();
            var _ripemdHashed = _ripemd.ComputeHash(sha256.ComputeHash(_publicKey));
            var _addedVersion = new byte[_ripemdHashed.Length + 1];
            if (!_mainNet)
                _addedVersion[0] = 0x6f;
            Buffer.BlockCopy(_ripemdHashed, 0, _addedVersion, 1, _ripemdHashed.Length);
            var _doubleSha = sha256.ComputeHash(sha256.ComputeHash(_addedVersion));
            Array.Resize(ref _doubleSha, 4);

            byte[] _result = new byte[_addedVersion.Length + _doubleSha.Length];
            Buffer.BlockCopy(_addedVersion, 0, _result, 0, _addedVersion.Length);
            Buffer.BlockCopy(_doubleSha, 0, _result, _addedVersion.Length, _doubleSha.Length);

            Address _address = new Address();
            _address.Text = Base58.Encode(_result);
            _address.PublicKey = Lion.HexPlus.ByteArrayToHexString(_publicKey);
            _address.PrivateKey = CompressPrivateKey(_privateKey, _mainNet);
            _address.Text = (_mainNet ? (_address.Text.StartsWith("1") ? "" : "1") : "") + _address.Text;
            return _address;
        }
    }
}
