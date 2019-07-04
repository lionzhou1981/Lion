using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

        public static byte[] GenerateRandomBytes()
        {
            byte[] _byteArray = new byte[10];
            RNGCryptoServiceProvider _rnd = new RNGCryptoServiceProvider();
            _rnd.GetBytes(_byteArray);
            return _byteArray;
        }

        public static string GenerateRandomString(byte[] _randomByteArray)
        {
            return Authenticator.Base32Encode(_randomByteArray);
        }

        public static long GetCurrentInterval()
        {
            TimeSpan _timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long _currentTimeSeconds = (long)Math.Floor(_timeSpan.TotalSeconds);
            long _currentInterval = _currentTimeSeconds / Authenticator.INTERVAL_LENGTH;
            return _currentInterval;
        }

        public static string GenerateResponseCode(long _challenge, byte[] _randomByteArray)
        {
            HMACSHA1 _myHmac = new HMACSHA1(_randomByteArray);
            _myHmac.Initialize();

            byte[] _value = BitConverter.GetBytes(_challenge);
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

        private static string UrlEncode(string _value)
        {
            StringBuilder _result = new StringBuilder();

            foreach (char _symbol in _value)
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

        public static string GenerateImageUrl(int _width, int _height, string _code, string _randomString, string _issuer)
        {
            string _chl = Authenticator.UrlEncode(String.Format("otpauth://totp/{0}?secret={1}&issuer={2}", _code, _randomString, _issuer));
            string _url = "http://chart.apis.google.com/chart?cht=qr&chs=" + _width + "x" + _height + "&chl=" + _chl;
            return _url;
        }

        public static string Base32Encode(byte[] data)
        {
            int i = 0, index = 0, digit = 0;
            int current_byte, next_byte;
            StringBuilder result = new StringBuilder((data.Length + 7) * IN_BYTE_SIZE / OUT_BYTE_SIZE);

            while (i < data.Length)
            {
                current_byte = (data[i] >= 0) ? data[i] : (data[i] + 256); // Unsign

                /* Is the current digit going to span a byte boundary? */
                if (index > (IN_BYTE_SIZE - OUT_BYTE_SIZE))
                {
                    if ((i + 1) < data.Length)
                        next_byte = (data[i + 1] >= 0) ? data[i + 1] : (data[i + 1] + 256);
                    else
                        next_byte = 0;

                    digit = current_byte & (0xFF >> index);
                    index = (index + OUT_BYTE_SIZE) % IN_BYTE_SIZE;
                    digit <<= index;
                    digit |= next_byte >> (IN_BYTE_SIZE - index);
                    i++;
                }
                else
                {
                    digit = (current_byte >> (IN_BYTE_SIZE - (index + OUT_BYTE_SIZE))) & 0x1F;
                    index = (index + OUT_BYTE_SIZE) % IN_BYTE_SIZE;
                    if (index == 0)
                        i++;
                }
                result.Append(alphabet[digit]);
            }

            return result.ToString();
        }
    }
}