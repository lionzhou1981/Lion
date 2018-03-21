using System.Security.Cryptography;
using System.Text;


namespace  Lion.Encrypt
{
    public class MD5
    {
        public static string Encode(string _source)
        {
            return Encode(System.Text.Encoding.Default.GetBytes(_source));
        }

        public static string Encode(byte[] _source)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] _buffer = md5Hasher.ComputeHash(_source);
            StringBuilder _sb = new StringBuilder();
            for (int i = 0; i < _buffer.Length; i++)
            {
                _sb.Append(_buffer[i].ToString("x2"));
            }
            return _sb.ToString();
        }

        public static byte[] Encode2ByteArray(byte[] _source)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            return md5Hasher.ComputeHash(_source);
        }
    }
}
