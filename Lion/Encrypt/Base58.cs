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
        public static string Encode(byte[] _byteArray)
        {
            SHA256 _sha256 = new SHA256Managed();
            byte[] _hash1 = _sha256.ComputeHash(_byteArray);
            byte[] _hash2 = _sha256.ComputeHash(_hash1);

            byte[] _checksum = new byte[4];
            Buffer.BlockCopy(_hash2, 0, _checksum, 0, _checksum.Length);

            byte[] _dataWithChecksum = new byte[_byteArray.Length + _checksum.Length];
            Buffer.BlockCopy(_byteArray, 0, _dataWithChecksum, 0, _byteArray.Length);
            Buffer.BlockCopy(_checksum, 0, _dataWithChecksum, _byteArray.Length, _checksum.Length);

            BigInteger _dataInt = _dataWithChecksum.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

            string _result = string.Empty;
            while (_dataInt > 0)
            {
                int _remainder = (int)(_dataInt % 58);
                _dataInt /= 58;
                _result = Base58.Base58characters[_remainder] + _result;
            }

            for (var i = 0; i < _dataWithChecksum.Length && _dataWithChecksum[i] == 0; i++)
            {
                _result = '1' + _result;
            }
            return _result;
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