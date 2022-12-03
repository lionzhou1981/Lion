using System;
using System.Text;

namespace Lion.Encrypt
{
    public class Base64
    {
        public static string Encode(string _text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(_text));

        public static string Encode(byte[] _byteArray) => Convert.ToBase64String(_byteArray);

        public static byte[] Decode(string _base64) => Convert.FromBase64String(_base64);
    }
}