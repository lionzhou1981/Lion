using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace Lion.Encrypt
{
    public class Base58
    {
        private static string Base58characters = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string Encode(string _hexString)
        {
            // WARNING: Beware of bignumber implementations that clip leading 0x00 bytes, or prepend extra 0x00 
            // bytes to indicate sign - your code must handle these cases properly or else you may generate valid-looking
            // addresses which can be sent to, but cannot be spent from - which would lead to the permanent loss of coins.)
            // Base58Check encoding is also used for encoding private keys in the Wallet Import Format. This is formed exactly
            // the same as a Bitcoin address, except that 0x80 is used for the version/application byte, and the payload is 32 bytes
            // instead of 20 (a private key in Bitcoin is a single 32-byte unsigned big-endian integer). Such encodings will always
            // yield a 51-character string that starts with '5', or more specifically, either '5H', '5J', or '5K'.   https://en.bitcoin.it/wiki/Base58Check_encoding
            try
            {
                var _numberToShorten = BigInteger.Parse(_hexString, System.Globalization.NumberStyles.HexNumber);
                char[] _result = new char[33];

                int i = 0;
                while (_numberToShorten >= 0 && _result.Length > i)
                {
                    var _lNumberRemainder = BigInteger.Remainder(_numberToShorten, (BigInteger)Base58characters.Length);
                    _numberToShorten = _numberToShorten / (BigInteger)Base58characters.Length;
                    _result[_result.Length - 1 - i] = Base58characters[(int)_lNumberRemainder];
                    i++;
                }
                return new string(_result);
            }
            catch
            {
                return null;
            }
        }


        #region Decode
        public static byte[] Decode(string _base58)
        {
            BigInteger _int = 0;
            for (int i = 0; i < _base58.Length; i++)
            {
                int _index = Base58characters.IndexOf(_base58[i]);
                if (_index < 0) { throw new FormatException($"Invalid Base58 character `{_base58[i]}` at position {i}"); }
                _int = _int * 58 + _index;
            }
 
            IEnumerable<byte> _zeros = Enumerable.Repeat((byte)0, _base58.TakeWhile(c => c == '1').Count());
            IEnumerable<byte> data = _int.ToByteArray().Reverse().SkipWhile(b => b == 0);
            return _zeros.Concat(data).ToArray();
        }
        #endregion
    }
}