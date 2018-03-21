using System;

namespace Lion.Encrypt
{
    public class Base64
    {
        public static string Encode(byte[] _byteArray) => Convert.ToBase64String(_byteArray);

        public static byte[] Decode(string _base64) => Convert.FromBase64String(_base64);
    }
}