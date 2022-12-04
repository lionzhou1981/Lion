using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using Lion.Encrypt;
using ECPoint = Lion.Encrypt.ECPoint;
using System.Security.Cryptography.X509Certificates;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Ethereum
    {
        public const string ERC20_METHOD_TOTALSUPPLY = "0x18160ddd";
        public const string ERC20_METHOD_BALANCEOF = "0x70a08231";
        public const string ERC20_METHOD_TRANSFER = "0xa9059cbb";
        public const string ERC20_METHOD_TRANSFERFROM = "0x23b872dd";
        public const string ERC20_METHOD_APPROVE = "0x095ea7b3";
        public const string ERC20_METHOD_ALLOWANCE = "0xdd62ed3e";

        public const uint CHAIN_ID_MAINNET = 1;
        public const uint CHAIN_ID_ROPSTEN = 3;
        public const uint CHAIN_ID_RINKEBY = 4;
        public const uint CHAIN_ID_GOERLI = 5;
        public const uint CHAIN_ID_KOVAN = 42;
        public const uint CHAIN_ID_PRIVATE = 1337;

        public const string EIP1155_METHOD_BALANCEOF = "0x00fdd58e";
        public const string EIP1155_METHOD_BALANCEOFBATCH = "0x4e1273f4";
        public const string EIP1155_METHOD_BURN = "0xf5298aca";
        public const string EIP1155_METHOD_BURNBATCH = "0x6b20c454";
        public const string EIP1155_METHOD_MINT = "0x731133e9";
        public const string EIP1155_METHOD_MINTBATCH = "0x1f7fdffa";
        public const string EIP1155_METHOD_SAFEBATCHTRANSFERFROM = "0x2eb2c2d6";
        public const string EIP1155_METHOD_SAFETRANSFERFROM = "0xf242432a";
        public const string EIP1155_METHOD_TRANSFER = "0x12514bba";

        #region HexToBigInteger
        public static BigInteger HexToBigInteger(string _hex)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            return BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier);
        }
        #endregion

        #region HexToDecimal
        public static string HexToDecimal(string _hex, int _decimal = 18)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            string _value = BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier).ToString();
            if (_value.Length < _decimal) { _value = _value.PadLeft(_decimal + 1, '0'); }
            return _value.Substring(0, _value.Length - _decimal) + "." + _value.Substring(_value.Length - _decimal);
        }
        #endregion

        #region DecimalToHex
        public static string DecimalToHex(decimal _value, int _decimal = 18)
        {
            string _text = _value.ToString($"0.".PadRight(_decimal + 2, '0'));
            BigInteger _number = BigInteger.Parse(_text.Replace(".", ""));
            return $"0x{_number.ToString("X").TrimStart('0')}";
        }
        #endregion

        #region DecimalToBigInteger
        public static BigInteger DecimalToBigInteger(decimal _value, int _decimal = 18)
        {
            string _text = _value.ToString($"0.".PadRight(_decimal + 2, '0'));
            return BigInteger.Parse(_text.Replace(".", ""));
        }
        #endregion

        #region BigIntegerToDecimal
        public static decimal BigIntegerToDecimal(BigInteger _big, int _decimal = 18)
        {
            string _text = _big.ToString().PadLeft(_decimal + 1, '0');
            decimal _result = decimal.Parse($"{_text.Substring(0,_text.Length-_decimal)}.{_text.Substring(_text.Length - _decimal)}");
            return _result;
        }
        #endregion

        #region Sign
        public static string Sign(byte[] _basicRaw,string _private,int _chainId = 1)
        {
            byte[] _basicHashedRaw = new Keccak256().Compute(_basicRaw);
            byte[][] rlpItems = RLP.DecodeList(_basicRaw);

            BigInteger _limit = BigInteger.Pow(BigInteger.Parse("2"), 256),
                       _r = BigInteger.Zero,
                       _e = BigInteger.Zero,
                       _s = BigInteger.Zero,
                       _k = BigInteger.Zero,
                       _recid = BigInteger.Zero;

            RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

            while (true)
            {
                _k = BigInteger.Zero;
                if (_k == BigInteger.Zero)
                {
                    byte[] _kBytes = new byte[33];
                    _rng.GetBytes(_kBytes);
                    _kBytes[32] = 0;
                    _k = new BigInteger(_kBytes);
                }
                if (_k.IsZero || _k >= Secp256k1.N) { continue; }

                var _gk = Secp256k1.G.Multiply(_k);
                _r = _gk.X % Secp256k1.N;
                _recid = _gk.Y & 1;

                if (_r == BigInteger.Zero) { throw new Exception("Sign failed because R is Zero."); }
                if (_r >= _limit || _r.Sign == 0) { Thread.Sleep(100); continue; }

                _e = BigNumberPlus.HexToBigInt(BitConverter.ToString(_basicHashedRaw).Replace("-", ""));
                _s = ((_e + (_r * BigNumberPlus.HexToBigInt(_private))) * BigInteger.ModPow(_k, Secp256k1.N - 2, Secp256k1.N)) % Secp256k1.N;

                if (_s == BigInteger.Zero) { throw new Exception("Sign failed because S is Zero."); }
                if (_s > Secp256k1.HalfN) { _recid ^= 1; }
                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }
                if (_s >= _limit || _s.Sign == 0 || _r.ToString("X").StartsWith("0") || _s.ToString("X").StartsWith("0")) { Thread.Sleep(100); continue; }
                break;
            }

            BigInteger _v = BigInteger.Parse(_chainId.ToString()) * 2 + _recid + 35;

            byte[] _signed = RLP.EncodeList(
                RLP.EncodeBytes(rlpItems[0]),
                RLP.EncodeBytes(rlpItems[1]),
                RLP.EncodeBytes(rlpItems[2]),
                RLP.EncodeBytes(rlpItems[3]),
                RLP.EncodeBytes(rlpItems[4]),
                RLP.EncodeBytes(rlpItems[5]),
                RLP.EncodeBigInteger(_v),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_r.ToString("X"))),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_s.ToString("X")))
            );

            return HexPlus.ByteArrayToHexString(_signed).ToLower();
        }
        #endregion

        #region VerifySignedText
        public static bool VerifySignedText(string _address, string _orgText, string _signed)
        {
            var _points = PossiblePubFromSignedText(_orgText, _signed);
            var _pubKey1 = Lion.BigNumberPlus.BigIntToHex(_points.Item1.X, true) + Lion.BigNumberPlus.BigIntToHex(_points.Item1.Y, true);
            var _pubKey2 = Lion.BigNumberPlus.BigIntToHex(_points.Item2.X, true) + Lion.BigNumberPlus.BigIntToHex(_points.Item2.Y, true);
            var _address1 = Address.PubKeyToAddress(_pubKey1).ToLower();
            var _address2 = Address.PubKeyToAddress(_pubKey2).ToLower();
            _address = _address.StartsWith("0x") ? _address.ToLower() : "0x" + _address.ToLower();
            return _address == _address1 || _address == _address2;
        }

        public static bool VerifySignedTextByPub(string _pubKey, string _orgText, string _signed)
        {
            var _points = PossiblePubFromSignedText(_orgText, _signed);
            var _pubKeyX = BigNumberPlus.HexToBigInt(_pubKey.Substring(0, 64));
            var _pubKeyY = BigNumberPlus.HexToBigInt(_pubKey.Substring(64, 64));
            if ((_points.Item1.X == _pubKeyX && _points.Item1.Y == _pubKeyY) || (_points.Item2.X == _pubKeyX && _points.Item2.Y == _pubKeyY))
                return true;
            return false;
        }

        static (ECPoint, ECPoint) PossiblePubFromSignedText(string _orgText, string _signed)
        {
            Keccak256 _k = new Keccak256();
            var _orgBytes = Encoding.UTF8.GetBytes(_orgText);
            _orgText = $"{"\x19"}Ethereum Signed Message:\n{_orgBytes.Length}{_orgText}";
            var _hash = _k.Compute(Encoding.UTF8.GetBytes(_orgText));
            BigInteger _value = BigNumberPlus.HexToBigInt(Lion.HexPlus.ByteArrayToHexString(_hash));
            BigInteger _r = BigNumberPlus.HexToBigInt(_signed.Substring(0, 64));
            BigInteger _s = BigNumberPlus.HexToBigInt(_signed.Substring(64, 64));
            var _ps = R2Points(_r);
            var _invR = _r.ModInverse(Secp256k1.N);
            var _sOverR = _s * _invR;
            var _valueOverInvR = Secp256k1.G.Multiply(_invR * _value);
            var _minus_E_over_r = new ECPoint(_valueOverInvR.X, Secp256k1.P - _valueOverInvR.Y);
            var _p0 = _ps.Item1.Multiply(_sOverR).Add(_minus_E_over_r);
            var _p1 = _ps.Item2.Multiply(_sOverR).Add(_minus_E_over_r);
            return (_p0, _p1);
        }


        static (ECPoint, ECPoint) R2Points(BigInteger _r)
        {
            //alpha = (pow(x, 3, p) + self._a * x + self._b) % p
            var _alpha = (BigInteger.ModPow(_r, 3, Lion.Encrypt.Secp256k1.P) + Secp256k1.A * _r + Secp256k1.B) % Secp256k1.P;
            var _y0 = BigInteger.ModPow(_alpha, Lion.Encrypt.Secp256k1.ModSqrtPower, Lion.Encrypt.Secp256k1.P);
            var _p0 = new ECPoint(_r, _y0);
            var _p1 = new ECPoint(_r, Lion.Encrypt.Secp256k1.P - _y0);
            return (_p0, _p1);
        }
        #endregion
    }
}
