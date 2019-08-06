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

        #region Encode
        public static string Encode(string _hexString)
        {
            return Encode(HexPlus.HexStringToByteArray(_hexString));
        }
        public static string Encode(byte[] data)
        {

            // Decode byte[] to BigInteger
            BigInteger intData = 0;
            for (int i = 0; i < data.Length; i++)
            {
                intData = intData * 256 + data[i];
            }

            // Encode BigInteger to Base58 string
            string result = "";
            while (intData > 0)
            {
                int remainder = (int)(intData % 58);
                intData /= 58;
                result = Base58characters[remainder] + result;
            }

            // Append `1` for each leading 0 byte
            for (int i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }
            return result;
        }
        #endregion

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