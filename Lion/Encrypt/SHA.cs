﻿using System;
using System.Text;
using System.Security.Cryptography;

namespace  Lion.Encrypt
{
    public class SHA
    {
        #region EncodeSHA1
        public static byte[] EncodeSHA1(string _source)
        {
            SHA1 _sha1 = SHA1.Create();
            return _sha1.ComputeHash(Encoding.UTF8.GetBytes(_source));
        }
        #endregion

        #region EncodeSHA1ToHex
        public static string EncodeSHA1ToHex(string _source)
        {
            byte[] _binary = EncodeSHA1(_source);
            StringBuilder _sb = new StringBuilder();
            foreach (byte _byte in _binary)
            {
                _sb.AppendFormat("{0:x2}", _byte);
            }
            return _sb.ToString();
        }
        #endregion

        #region EncodeSHA1ToBase64
        public static string EncodeSHA1ToBase64(string _source)
        {
            byte[] _binary = EncodeSHA1(_source);

            return Convert.ToBase64String(_binary);
        }
        #endregion

        #region EncodeSHA256
        public static string EncodeSHA256(string _source, Encoding _encoding)
        {
            byte[] _binary = EncodeSHA256(_encoding.GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_binary);
        }
        public static byte[] EncodeSHA256(byte[] _source)
        {
            SHA256 _sha256 = SHA256Managed.Create();
            byte[] _binary = _sha256.ComputeHash(_source);

            return _binary;
        }
        #endregion

        #region EncodeHMACSHA1ToBase64
        public static string EncodeHMACSHA1ToBase64(string _source, string _password, Encoding _encoder = null)
        {
            HMACSHA1 _provider = new HMACSHA1((_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source));

            return Base64.Encode(_hashed);
        }
        #endregion

        #region EncodeHMACSHA256
        public static byte[] EncodeHMACSHA256(string _source, string _password, Encoding _encoder = null)
        {
            return EncodeHMACSHA256((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source), (_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
        }
        public static byte[] EncodeHMACSHA256(byte[] _source, byte[] _password)
        {
            HMACSHA256 _provider = new HMACSHA256(_password);
            byte[] _hashed = _provider.ComputeHash(_source);

            return _hashed;
        }

        #endregion

        #region EncodeHMACSHA256ToHex
        public static string EncodeHMACSHA256ToHex(string _source, string _password, Encoding _encoder = null)
        {
            HMACSHA256 _provider = new HMACSHA256((_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_hashed);
        }
        #endregion

        #region EncodeHMACSHA256ToBase64
        public static string EncodeHMACSHA256ToBase64(string _source, string _password, Encoding _encoder = null)
        {
            HMACSHA256 _provider = new HMACSHA256((_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source));

            return Base64.Encode(_hashed); 
        }
        #endregion

        #region EncodeHMACSHA384
        public static string EncodeHMACSHA384(string _source, string _password, Encoding _encoder = null)
        {
            HMACSHA384 _provider = new HMACSHA384((_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_hashed);
        }
        #endregion


        #region EncodeHMACSHA512ToHex
        public static string EncodeHMACSHA512ToHex(string _source, string _password, Encoding _encoder = null)
        {
            HMACSHA512 _provider = new HMACSHA512((_encoder == null ? Encoding.Default : _encoder).GetBytes(_password));
            byte[] _hashed = _provider.ComputeHash((_encoder == null ? Encoding.Default : _encoder).GetBytes(_source));

            return HexPlus.ByteArrayToHexString(_hashed);
        }
        #endregion

    }
}
