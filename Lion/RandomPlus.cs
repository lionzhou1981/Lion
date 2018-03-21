using System;
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
    }
}
