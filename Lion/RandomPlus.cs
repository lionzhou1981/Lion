using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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

        #region RandomNumbers
        public static string RandomNumbers(int _length = 6)
        {
            StringBuilder _builder = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                _builder.Append(new Random(RandomSeed).Next(0, 10).ToString());
            }
            return _builder.ToString();
        }
        #endregion

        #region RandomHex
        public static string RandomHex(int _length = 64)
        {
            List<string> _hexs = new List<string>();
            while (_hexs.Count < _length) 
            {
                Random _random = new Random(RandomSeed);
                _hexs.Add(_random.Next(0, 16).ToString()); 
            }
            return string.Join("\n", _hexs).ToLower();
        }
        #endregion
    }
}
