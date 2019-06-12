using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lion
{
    public class RandomPlus
    {
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

        public static string GenerateHexKey(int _length = 64)
        {
            List<string> _privateKeys = new List<string>();
            while (_privateKeys.Count < _length)
            {
                var _random = new Random(RandomSeed);
                _privateKeys.Add(_random.Next(0, 16).ToString("X"));
            }
            return string.Join("", _privateKeys).ToLower();
        }
    }
}
