using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Encrypt;
using Lion.Net;
using System.Threading;
using System.Linq;


namespace Lion.SDK.Bitcoin.Coins
{
    public class Ethereum
    {
        #region GenerateAddress
        public static Address GenerateAddress(string _privateKey = "")
        {
            Address _address = new Address();
            _address.PrivateKey = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;
            _address.PublicKey = HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_address.PrivateKey));
            _address.PublicKey = _address.PublicKey.Substring(2); //remove 04 start;

            Keccak256 _keccakHasher = new Keccak256();
            string _hexAddress = _keccakHasher.ComputeHashByHex(_address.PublicKey);

            _address.Text = "0x" + _hexAddress.Substring(_hexAddress.Length - 40);
            return _address;
        }
        #endregion

        #region IsAddress
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

        #region BigDecimalMultiply
        internal static string BigDecimalMultiply(string _a, string _b)
        {
            var _decimalCounA = _a.Contains(".") ? _a.Trim().Split('.')[1].TrimEnd('0').Length : 0;
            var _decimalCounB = _b.Contains(".") ? _b.Trim().Split('.')[1].TrimEnd('0').Length : 0;
            _a = _a.Contains(".") ? _a.TrimEnd('0').Replace(".", "") : _a;
            _b = _b.Contains(".") ? _b.TrimEnd('0').Replace(".", "") : _b;
            var _factValueA = BigInteger.Parse(_a);
            var _factValueB = BigInteger.Parse(_b);
            var _value = (_factValueA * _factValueB).ToString();
            var _decimals = _decimalCounA + _decimalCounB;
            if (_decimals >= _value.Length)
                _value = "0." + _value.PadLeft(_decimals, '0');
            else if (_decimals > 0)
                _value = _value.Substring(0, _value.Length - _decimals) + "." + _value.Substring(_value.Length - _decimals);
            return _value;
        }
        #endregion

        #region ToETHValue
        internal static string ToETHValue(string _value, bool _addPrefix = false, int _fractionPoint = 18)
        {
            var _orgValue = _value;
            var _isdecimal = _value.ToString().Contains(".");
            BigInteger _fractionValue = System.Numerics.BigInteger.Parse(Convert.ToInt64(Math.Pow(10, _fractionPoint)).ToString());
            if (_isdecimal)
            {
                var _fraction = _value.TrimEnd('0').Split('.')[1];
                if (string.IsNullOrWhiteSpace(_fraction))
                    _orgValue = _value.Split('.')[0].Trim();
                else if (_fractionPoint < _fraction.Length)
                {
                    _orgValue = _value.Split('.')[0].Trim() + _fraction.Substring(0, _fractionPoint);
                    _fractionValue = 1;
                }
                else
                {
                    _orgValue = _value.Split('.')[0].Trim() + _value.Split('.')[1].Substring(0, _fraction.Length).Trim();
                    _fractionValue = System.Numerics.BigInteger.Parse(Convert.ToInt64(Math.Pow(10, _fractionPoint - _fraction.Length)).ToString());
                }
            }
            var _converted = _fractionValue * System.Numerics.BigInteger.Parse(_orgValue);
            return _addPrefix ? $"0x{_converted.ToString("X").TrimStart('0')}" : _converted.ToString("X").TrimStart('0');
        }
        #endregion

        internal static BigInteger keyLimit = BigInteger.Zero;
        internal static BigInteger KeyLimit
        {
            get
            {
                if (keyLimit != BigInteger.Zero)
                    return keyLimit;
                keyLimit = BigInteger.Pow(BigInteger.Parse("2"), 256);
                return keyLimit;
            }
        }
        internal static string BigIntToDecimal(BigInteger _bigInt, int _fractionPoint = 18)
        {
            var _orgValue = _bigInt.ToString();
            if (_orgValue.Length < _fractionPoint)
                _orgValue = _orgValue.PadLeft(_fractionPoint + 1, '0');
            return _orgValue.Substring(0, _orgValue.Length - _fractionPoint) + "." + _orgValue.Substring(_orgValue.Length - _fractionPoint);
        }


        internal static byte[] ToBytesFromNumber(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return TrimZeroBytes(bytes);
        }

        internal static byte[] ToBytesForRLPEncoding(BigInteger bigInteger)
        {
            return RLP.EncodeElement(ToBytesFromNumber(bigInteger.ToByteArray()));
        }

        internal static byte[] TrimZeroBytes(byte[] bytes)
        {
            var trimmed = new List<byte>();
            var previousByteWasZero = true;

            for (var i = 0; i < bytes.Length; i++)
            {
                if (previousByteWasZero && bytes[i] == 0)
                    continue;

                previousByteWasZero = false;
                trimmed.Add(bytes[i]);
            }

            return trimmed.ToArray();
        }
        private static readonly byte[] Empty = new byte[0];
        internal static byte[] HexToByteArrayInternal(string value)
        {
            byte[] bytes = null;
            if (string.IsNullOrEmpty(value))
            {
                bytes = Empty;
            }
            else
            {
                var string_length = value.Length;
                var character_index = value.StartsWith("0x", StringComparison.Ordinal) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                var number_of_characters = string_length - character_index;

                var add_leading_zero = false;
                if (0 != number_of_characters % 2)
                {
                    add_leading_zero = true;

                    number_of_characters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[number_of_characters / 2]; // Initialize our byte array to hold the converted string.

                var write_index = 0;
                if (add_leading_zero)
                {
                    bytes[write_index++] = FromCharacterToByte(value[character_index], character_index);
                    character_index += 1;
                }

                for (var read_index = character_index; read_index < value.Length; read_index += 2)
                {
                    var upper = FromCharacterToByte(value[read_index], read_index, 4);
                    var lower = FromCharacterToByte(value[read_index + 1], read_index + 1);

                    bytes[write_index++] = (byte)(upper | lower);
                }
            }

            return bytes;
        }

        private static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            var value = (byte)character;
            if (0x40 < value && 0x47 > value || 0x60 < value && 0x67 > value)
            {
                if (0x40 == (0x40 & value))
                    if (0x20 == (0x20 & value))
                        value = (byte)((value + 0xA - 0x61) << shift);
                    else
                        value = (byte)((value + 0xA - 0x41) << shift);
            }
            else if (0x29 < value && 0x40 > value)
            {
                value = (byte)((value - 0x30) << shift);
            }
            else
            {
                throw new FormatException(string.Format(
                    "Character '{0}' at index '{1}' is not valid alphanumeric character.", character, index));
            }

            return value;
        }

        static byte[] HexToByteArray(string value)
        {
            try
            {
                return RLP.EncodeElement(HexToByteArrayInternal(value));
            }
            catch (FormatException ex)
            {
                throw new FormatException(string.Format(
                    "String '{0}' could not be converted to byte array (not hex?).", value), ex);
            }
        }

        public static readonly BigInteger DEFAULT_GAS_PRICE = BigInteger.Parse("20000000000");
        public static readonly BigInteger DEFAULT_GAS_LIMIT = BigInteger.Parse("21000");


        //curl -H "Content-Type:application/json" -X POST --data '{"jsonrpc":"2.0","method":"eth_sendRawTransaction","params":[""],"id":1}' http://127.0.0.1:48883

        public static void Test()
        {
            Console.WriteLine($"0x{BuildRawTransaction(4, 10, DEFAULT_GAS_PRICE, DEFAULT_GAS_LIMIT, "01d66b61545935979813a3e3a450e67641109ff9c90b7909b49bcead65ec6fa5", "7f51e67c16af89b2abdc4906b788d8dd443ffbc1", BigInteger.Pow(BigInteger.Parse("10"),16)).ToLower()}");
        }

        public static string BuildRawTransaction(BigInteger _chainId, BigInteger _nonce, BigInteger _gasPrice, BigInteger _gasLimit, string _fromPrivateKey, string _addressTo, BigInteger _amount, string _data = "")
        {
            var _amountDex = _amount.ToString("X");//ToETHValue(_amount.ToString());
            var _basicRaw = Lion.Encrypt.RLP.EncodeList(new byte[][] {
                ToBytesForRLPEncoding(_nonce),
                ToBytesForRLPEncoding(_gasPrice),
                ToBytesForRLPEncoding(_gasLimit),
                HexToByteArray(_addressTo),
                ToBytesForRLPEncoding(BigInteger.Parse(_amountDex,System.Globalization.NumberStyles.HexNumber)),
                HexToByteArray(_data),
                RLP.EncodeElement(_chainId.ToByteArray()),
                HexToByteArray(""),
                HexToByteArray("")
            });
            var _basicRawSHA = new Keccak256().Compute(_basicRaw);
            var _r = BigInteger.Zero;
            var _e = BigInteger.Zero;
            var _s = BigInteger.Zero;
            var _k = BigInteger.Zero;
            var _recid = BigInteger.Zero;
            while (true)
            {
                _k = new Random(Lion.RandomPlus.RandomSeed).Next(1, int.MaxValue);
                var _Gk = Secp256k1.G.Multiply(_k);
                var _y = _Gk.Y & 1;
                _r = _Gk.X;
                _r = _r % Secp256k1.N;
                if (_r >= KeyLimit || _r.Sign == 0)
                {
                    Thread.Sleep(500);
                    continue;
                }
                _e = BigInteger.Parse($"0{BitConverter.ToString(_basicRawSHA).Replace("-", "")}", System.Globalization.NumberStyles.HexNumber);
                var _d = BigInteger.Parse($"0{_fromPrivateKey}", System.Globalization.NumberStyles.HexNumber);
                _s = _r * _d;
                _s = _s + _e;
                _s = _s * _k.ModInverse(Secp256k1.N);
                _s = _s % Secp256k1.N;
                if (_s > Secp256k1.HalfN)
                    _recid = _y;
                if (_s.CompareTo(Secp256k1.HalfN) > 0)
                    _s = Secp256k1.N - _s;

                if (_s >= KeyLimit || _s.Sign == 0)
                {
                    Thread.Sleep(500);
                    continue;
                }
                break;
            }

            if (_k == BigInteger.Zero || _r == BigInteger.Zero || _e == BigInteger.Zero || _s == BigInteger.Zero)
                throw new Exception("Transaction sign error");
            BigInteger _v = _chainId * 2 + _recid + 35; 
            return BitConverter.ToString(Lion.Encrypt.RLP.EncodeList(new byte[][] {
                ToBytesForRLPEncoding(_nonce),
                ToBytesForRLPEncoding(_gasPrice),
                ToBytesForRLPEncoding(_gasLimit),
                HexToByteArray(_addressTo),
                ToBytesForRLPEncoding(BigInteger.Parse(_amountDex,System.Globalization.NumberStyles.HexNumber)),
                HexToByteArray(_data),
                ToBytesForRLPEncoding(_v),
                RLP.EncodeElement(Lion.HexPlus.HexStringToByteArray(_r.ToString("X").TrimStart('0'))),
                RLP.EncodeElement(Lion.HexPlus.HexStringToByteArray(_s.ToString("X").TrimStart('0')))
            })).Replace("-", "");
        }
    }
}
;