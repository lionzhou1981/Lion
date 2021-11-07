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
        #region GenerateAddress
        public static Address GenerateAddress(string _privateKey = "", bool _mainNet = true)
        {
            _privateKey = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;

            BigInteger _privateInt = BigInteger.Parse("0" + _privateKey, System.Globalization.NumberStyles.HexNumber);
            byte[] _publicKey = Secp256k1.PrivateKeyToPublicKey(_privateInt);

            SHA256Managed _sha256 = new SHA256Managed();
            RIPEMD160Managed _ripemd = new RIPEMD160Managed();
            byte[] _ripemdHashed = _ripemd.ComputeHash(_sha256.ComputeHash(_publicKey));
            byte[] _addedVersion = new byte[_ripemdHashed.Length + 1];
            _addedVersion[0] = (byte)(_mainNet ? 0x00 : 0x6f);
            Array.Copy(_ripemdHashed, 0, _addedVersion, 1, _ripemdHashed.Length);

            byte[] _shaHashed = _sha256.ComputeHash(_sha256.ComputeHash(_addedVersion));
            Array.Resize(ref _shaHashed, 4);

            byte[] _result = new byte[_addedVersion.Length + _shaHashed.Length];
            Array.Copy(_addedVersion, 0, _result, 0, _addedVersion.Length);
            Array.Copy(_shaHashed, 0, _result, _addedVersion.Length, _shaHashed.Length);

            string _key1 = string.Join("", (_mainNet ? "80" : "ef"), _privateKey);
            string _key2 = HexPlus.ByteArrayToHexString(SHA.EncodeSHA256(SHA.EncodeSHA256(HexPlus.HexStringToByteArray(_key1))).Take(4).ToArray());

            Address _address = new Address();
            _address.Text = Base58.Encode(_result);
            _address.PublicKey = HexPlus.ByteArrayToHexString(_publicKey);
            _address.PrivateKey = Base58.Encode(_key1 + _key2);
            _address.Text = (_mainNet ? (_address.Text.StartsWith("1") ? "" : "1") : "") + _address.Text;
            return _address;
        }
        #endregion

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


        public static string PrivKey2PubKey(string _privateKey, bool _mainNet = true)
        {
            return Lion.HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_privateKey));
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

        public static List<byte> ECDSASign(string _scriptSig, string _outputPrivateKey)
        {
            BigInteger _k = BigInteger.Parse($"0{Lion.RandomPlus.RandomHex()}", NumberStyles.HexNumber);
            var _Gk = Secp256k1.G.Multiply(_k);
            var _r = _Gk.X;
            var _e = BigInteger.Parse($"0{_scriptSig}", NumberStyles.HexNumber);
            var _d = BigInteger.Parse($"0{_outputPrivateKey}", System.Globalization.NumberStyles.HexNumber);
            var _s = _r * _d;
            _s = _s + _e;
            _s = _s * _k.ModInverse(Secp256k1.N);
            _s = _s % Secp256k1.N;
            if (_s.CompareTo(Secp256k1.HalfN) > 0)
                _s = Secp256k1.N - _s;

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
            return _allBytes;
        }

        public static string SendToAddress(List<BitcoinInput> _inputs, Dictionary<string, decimal> _outputs, decimal _fee = 0.0001M, bool _testNet = true)
        {
            //pay to one structure
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


            //testdata
            //_outputs = new Dictionary<string, decimal>();
            //_outputs.Add("miy64GVdz6ZQaUKCVmhpjVLqKsUqMgyP6y", 0.0001M);
            //_outputs.Add("mpoG4nsQyV1DjhKDqGjvF9Mam22P67JiSk", 0.0001M);


            //_inputs = new List<BitcoinInput>();
            //_inputs.Add(new BitcoinInput("a6ce685dec45397d768c5465e17991f8e1b5ddc84f87900ff6b0d4ea88be669f", 1, 0.00149M, "e218a0fcc2d8c42988439808f7c95cb0d6ee091383f35b6c6282c97ece3ce240", _testNet));
            //_inputs.Add(new BitcoinInput("a6ce685dec45397d768c5465e17991f8e1b5ddc84f87900ff6b0d4ea88be669f", 0, 0.0001M, "071708c0a5a37a9c3c3ffa238fdc9c7928c2b5137a86e8f2db9407b3daa78646", _testNet));
            //end test

            BigInteger _outputCount = _outputs.Count;

            decimal _outputBalance = _inputs.Sum(t => t.Balance) - _outputs.Sum(t => t.Value) - _fee;
            if (_outputBalance < 0)
                throw new Exception("Not enough balance");

            var _firstInput = _inputs.First();
            var _address = GenerateAddress(out string _, _firstInput.PrivateKey, !_testNet).Text;
            if (_outputs.ContainsKey(_address))
                _outputs[_address] += _outputBalance;
            else
                _outputs.Add(_address, _outputBalance);//balance return 

            //base script: version/input count
            var _templateStartNoSign = new List<byte>();
            _templateStartNoSign.AddBytesPadRightZero(4, 0x01);//version;
            _templateStartNoSign.AddBytes(((BigInteger)_inputs.Count).ToByteArray().Reverse().ToArray());

            //start from output,not contains sign,not contains hash type
            var _templateFromOutPutToHashType = new List<byte>();
            _templateFromOutPutToHashType.AddBytes((_outputCount + 1).ToByteArray().Reverse().ToArray());//transaction seq
            _outputs.ToList().ForEach(t =>
            {
                var _outPutPKSH = BitcoinHelper.AddressToPKSH(t.Key);
                _templateFromOutPutToHashType.SendValueToPubKey(_outPutPKSH, BitcoinHelper.DecimalToSatoshi(t.Value));
            });
            _templateFromOutPutToHashType.AddBytesPadRightZero(4, 0x00);
            //base script sig = base+input+output+hashtype
            //pay script sig = ecdsa(base script sig)
            foreach (var _input in _inputs)
            {
                //sign bytes per input
                var _arrayToSign = new List<byte>();
                _arrayToSign.AddRange(_templateStartNoSign);
                _inputs.ForEach(t =>
                {
                    //script per input
                    _arrayToSign.AddRange(t.BaseInputScript);
                    if (t.TxId == _input.TxId && t.TxIndex == _input.TxIndex)
                        _arrayToSign.AddRange(Lion.HexPlus.HexStringToByteArray(t.SciptPubKey));
                    else
                        _arrayToSign.AddBytes(0x00);
                    _arrayToSign.AddBytes(0xff, 0xff, 0xff, 0xff);
                });
                _arrayToSign.AddRange(_templateFromOutPutToHashType);
                _arrayToSign.AddBytesPadRightZero(4, 0x01);//hash type;
                //ECDSA
                var _sciptSig = BitConverter.ToString(new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_arrayToSign.ToArray()))).Replace("-", "").ToLower();
                _input.ScriptSig = ECDSASign(_sciptSig, _input.PrivateKey);
            }
            //pay bytes
            var _arrayToPay = new List<byte>();
            _arrayToPay.AddRange(_templateStartNoSign);
            _inputs.ForEach(t =>
            {
                _arrayToPay.AddRange(t.BaseInputScript);//script per input
                BigInteger _sigLength = BigInteger.Parse(t.PublicKey, NumberStyles.HexNumber).ToByteArray().Length + t.ScriptSig.Count;
                _arrayToPay.AddBytes(_sigLength.ToByteArray().Reverse().Where(f => f != 0x00).ToArray());
                _arrayToPay.AddBytes(t.ScriptSig.ToArray());
                _arrayToPay.AddBytes(Lion.HexPlus.HexStringToByteArray(t.PublicKey));
                _arrayToPay.AddBytes(0xff, 0xff, 0xff, 0xff);//transaction seq
            });
            _arrayToPay.AddRange(_templateFromOutPutToHashType);
            Console.WriteLine("Pay:" + Lion.HexPlus.ByteArrayToHexString(_arrayToPay.ToArray()));
            return Lion.HexPlus.ByteArrayToHexString(_arrayToPay.ToArray());
        }

        public static Address GenerateAddress(out string _uncompressKey, string _existsPrivateKey = "", bool _mainNet = true)
        {
            string _netVersion = _mainNet ? "00" : "6f";
            string _privateKey = string.IsNullOrWhiteSpace(_existsPrivateKey) ? Lion.RandomPlus.RandomHex() : _existsPrivateKey;
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
