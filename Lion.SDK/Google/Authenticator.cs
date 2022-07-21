using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lion;
using Lion.Encrypt;

namespace Lion.SDK.Google
{
    public class Authenticator
    {
        private const int PIN_LENGTH = 6;
        private const int INTERVAL_LENGTH = 30;
        private const string UNRESERVED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        private const int IN_BYTE_SIZE = 8;
        private const int OUT_BYTE_SIZE = 5;
        private static char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

        public static string GenerateKey(int _length = 10)
        {
            byte[] _byteArray = new byte[_length];
            RNGCryptoServiceProvider _rnd = new RNGCryptoServiceProvider();
            _rnd.GetBytes(_byteArray);

            return Base32.Encode(_byteArray);
        }

        public static string CalculateCode(string _key, long _tick = -1)
        {
            if (_tick == -1)
            {
                TimeSpan _timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                long _currentTimeSeconds = (long)Math.Floor(_timeSpan.TotalSeconds);
                _tick = _currentTimeSeconds / Authenticator.INTERVAL_LENGTH;
            }

            byte[] _keyByteArray = Base32.Decode(_key);

            HMACSHA1 _myHmac = new HMACSHA1(_keyByteArray);
            _myHmac.Initialize();

            byte[] _value = BitConverter.GetBytes(_tick);
            Array.Reverse(_value);
            _myHmac.ComputeHash(_value);
            byte[] _hash = _myHmac.Hash;
            int _offset = _hash[_hash.Length - 1] & 0xF;
            byte[] _fourBytes = new byte[4];
            _fourBytes[0] = _hash[_offset];
            _fourBytes[1] = _hash[_offset + 1];
            _fourBytes[2] = _hash[_offset + 2];
            _fourBytes[3] = _hash[_offset + 3];
            Array.Reverse(_fourBytes);
            int _finalInt = BitConverter.ToInt32(_fourBytes, 0);
            int _truncatedHash = _finalInt & 0x7FFFFFFF;
            int _pinValue = _truncatedHash % (int)Math.Pow(10, Authenticator.PIN_LENGTH);
            return _pinValue.ToString().PadLeft(Authenticator.PIN_LENGTH, '0');
        }

        public static string MakeQRCode(string _key, string _title, string _issuer)
        {
            // http://chart.apis.google.com/chart?cht=qr&chs=200x200&chl=xxxx
            //https://github.com/google/google-authenticator/wiki/Key-Uri-Format
            return $"otpauth://totp/{FormatParam(_title)}?secret={FormatParam(_key)}&issuer={FormatParam(_issuer)}";
        }

        private static string FormatParam(string _param)
        {
            StringBuilder _result = new StringBuilder();
            foreach (char _symbol in _param)
            {
                if (Authenticator.UNRESERVED_CHARS.IndexOf(_symbol) != -1)
                {
                    _result.Append(_symbol);
                }
                else
                {
                    _result.Append('%' + String.Format("{0:X2}", (int)_symbol));
                }
            }
            return _result.ToString();
        }
    }
}