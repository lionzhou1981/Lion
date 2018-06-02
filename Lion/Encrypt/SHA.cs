using System.Text;
using System.Security.Cryptography;

namespace  Lion.Encrypt
{
    public class SHA
    {
        #region EncodeSHA1
        public static string EncodeSHA1(string _source)
        {
            SHA1 _sha1 = SHA1.Create();
            byte[] _binary = _sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_source));

            StringBuilder _sb = new StringBuilder();
            foreach (byte _byte in _binary)
            {
                _sb.AppendFormat("{0:x2}", _byte);
            }
            return _sb.ToString();
        }
        #endregion

        #region EncodeSHA256
        public static string EncodeSHA256(string _source, System.Text.Encoding _encoding)
        {
            SHA256 _sha256 = SHA256Managed.Create();
            byte[] _binary = _sha256.ComputeHash(_encoding.GetBytes(_source));

            StringBuilder _sb = new StringBuilder();
            foreach (byte _byte in _binary)
            {
                _sb.AppendFormat("{0:x2}", _byte);
            }
            return _sb.ToString();
        }
        #endregion

        #region EncodeHMACSHA256
        public static string EncodeHMACSHA256(string _source, string _password, System.Text.Encoding _encoder = null)
        {
            HMACSHA256 _provider = new HMACSHA256((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_hashed);
        }
        #endregion

        #region EncodeHMACSHA256ToBase64
        public static string EncodeHMACSHA256ToBase64(string _source, string _password, System.Text.Encoding _encoder = null)
        {
            HMACSHA256 _provider = new HMACSHA256((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_source));

            return Base64.Encode(_hashed); 
        }
        #endregion

        #region EncodeHMACSHA384
        public static string EncodeHMACSHA384(string _source, string _password, System.Text.Encoding _encoder = null)
        {
            HMACSHA384 _provider = new HMACSHA384((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? System.Text.Encoding.Default : _encoder).GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_hashed);
        }
        #endregion
    }
}
