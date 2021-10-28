using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lion
{
    public class RandomPlus
    {
        #region RandomSeed
        public static int RandomSeed
        {
            get
            {
                byte[] _byteArray = new byte[4];

                RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
                _rng.GetBytes(_byteArray);

                int _value = BitConverter.ToInt32(_byteArray, 0);
                return _value <= 0 ? -_value : _value;
            }
        }
        #endregion

        #region RandomHex
        public static string RandomHex(int _length = 64)
        {
            Random _random = new Random(RandomSeed);
            List<string> _hexs = new List<string>();
            while (_hexs.Count < _length) { _hexs.Add(_random.Next(0, 16).ToString("X")); }
            return string.Join("", _hexs).ToLower();
        }
        #endregion
    }
}
