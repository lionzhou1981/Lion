using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace  Lion.Encrypt
{
    public class DES
    {
        #region Decode
        public static byte[] Decode2ByteArray(byte[] _buffer, string _key, CipherMode _mode = CipherMode.CBC, PaddingMode _padding = PaddingMode.PKCS7)
        {
            DESCryptoServiceProvider _des = new DESCryptoServiceProvider();
            _des.Mode = _mode;
            _des.Padding = _padding;
            _des.Key = ASCIIEncoding.ASCII.GetBytes(_key);
            _des.IV = ASCIIEncoding.ASCII.GetBytes(_key);
            MemoryStream _memoryStream = new MemoryStream();
            CryptoStream _cryptoStream = new CryptoStream(_memoryStream, _des.CreateDecryptor(), CryptoStreamMode.Write);
            _cryptoStream.Write(_buffer, 0, _buffer.Length);
            _cryptoStream.FlushFinalBlock();
            return _memoryStream.ToArray();
        }
        public static string Decode(string _source, string _key, CipherMode _mode = CipherMode.CBC)
        {
            byte[] _buffer = System.Text.Encoding.UTF8.GetBytes(_source);
            return System.Text.Encoding.UTF8.GetString(DES.Decode2ByteArray(_buffer, _key));
        }
        #endregion

        #region Encode
        public static byte[] Encode2ByteArray(byte[] _buffer, string _key, CipherMode _mode = CipherMode.CBC, PaddingMode _padding = PaddingMode.PKCS7)
        {
            DESCryptoServiceProvider _des = new DESCryptoServiceProvider();
            _des.Mode = _mode;
            _des.Padding = _padding;
            _des.Key = ASCIIEncoding.ASCII.GetBytes(_key);
            _des.IV = ASCIIEncoding.ASCII.GetBytes(_key);
            MemoryStream _memoryStream = new MemoryStream();
            CryptoStream _cryptoStream = new CryptoStream(_memoryStream, _des.CreateEncryptor(), CryptoStreamMode.Write);
            _cryptoStream.Write(_buffer, 0, _buffer.Length);
            _cryptoStream.FlushFinalBlock();
            return _memoryStream.ToArray();
        }

        public static string Encode(string _source, string _key, CipherMode _mode = CipherMode.CBC)
        {
            byte[] _buffer = System.Text.Encoding.UTF8.GetBytes(_source);
            return System.Text.Encoding.UTF8.GetString(_buffer);
        }
        #endregion
    }
}
