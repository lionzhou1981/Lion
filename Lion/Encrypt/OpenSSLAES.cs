using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace  Lion.Encrypt
{
    public class OpenSSLAes
    {
        #region Encode
        public static string Encode(string _input, string _password)
        {
            byte[] _key, _iv;
            byte[] _salt = new byte[8];
            new RNGCryptoServiceProvider().GetNonZeroBytes(_salt);

            EvpBytesToKey(_password, _salt, out _key, out _iv);

            byte[] encryptedBytes = Encrypt(_input, _key, _iv);
            var encryptedBytesWithSalt = CombineSaltAndEncryptedData(encryptedBytes, _salt);
            return Convert.ToBase64String(encryptedBytesWithSalt);
        }
        private static byte[] CombineSaltAndEncryptedData(byte[] _data, byte[] _salt)
        {
            byte[] _withSalt = new byte[_salt.Length + _data.Length + 8];
            Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("Salted__"), 0, _withSalt, 0, 8);
            Buffer.BlockCopy(_salt, 0, _withSalt, 8, _salt.Length);
            Buffer.BlockCopy(_data, 0, _withSalt, _salt.Length + 8, _data.Length);
            return _withSalt;
        }
        private static byte[] ExtractEncryptedData(byte[] _salt, byte[] _withSalt)
        {
            byte[] _inputBytes = new byte[_withSalt.Length - _salt.Length - 8];
            Buffer.BlockCopy(_withSalt, _salt.Length + 8, _inputBytes, 0, _inputBytes.Length);
            return _inputBytes;
        }
        private static byte[] ExtractSalt(byte[] _withSalt)
        {
            byte[] _salt = new byte[8];
            Buffer.BlockCopy(_withSalt, 8, _salt, 0, _salt.Length);
            return _salt;
        }
        private static void EvpBytesToKey(string _password, byte[] _salt, out byte[] _key, out byte[] _iv)
        {
            List<byte> _hashes = new List<byte>(48);

            byte[] _passwordBytes = System.Text.Encoding.UTF8.GetBytes(_password);
            byte[] _currentHash = new byte[0];
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            bool _enoughBytesForKey = false;

            while (!_enoughBytesForKey)
            {
                int _preHashLength = _currentHash.Length + _passwordBytes.Length + _salt.Length;
                byte[] _preHash = new byte[_preHashLength];

                Buffer.BlockCopy(_currentHash, 0, _preHash, 0, _currentHash.Length);
                Buffer.BlockCopy(_passwordBytes, 0, _preHash, _currentHash.Length, _passwordBytes.Length);
                Buffer.BlockCopy(_salt, 0, _preHash, _currentHash.Length + _passwordBytes.Length, _salt.Length);

                _currentHash = md5.ComputeHash(_preHash);
                _hashes.AddRange(_currentHash);

                if (_hashes.Count >= 48) _enoughBytesForKey = true;
            }

            _key = new byte[32];
            _iv = new byte[16];
            _hashes.CopyTo(0, _key, 0, 32);
            _hashes.CopyTo(32, _iv, 0, 16);

            md5.Clear();
            md5 = null;
        }
        private static byte[] Encrypt(string _input, byte[] _key, byte[] _iv)
        {
            MemoryStream _memoryStream;
            RijndaelManaged _aesAlgorithm = null;

            try
            {
                _aesAlgorithm = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    KeySize = 256,
                    BlockSize = 128,
                    Key = _key,
                    IV = _iv
                };

                var _cryptoTransform = _aesAlgorithm.CreateEncryptor(_aesAlgorithm.Key, _aesAlgorithm.IV);
                _memoryStream = new MemoryStream();

                using (CryptoStream _cryptoStream = new CryptoStream(_memoryStream, _cryptoTransform, CryptoStreamMode.Write))
                {
                    using (StreamWriter _streamWriter = new StreamWriter(_cryptoStream))
                    {
                        _streamWriter.Write(_input);
                        _streamWriter.Flush();
                        _streamWriter.Close();
                    }
                }
            }
            finally
            {
                _aesAlgorithm?.Clear();
            }

            return _memoryStream.ToArray();
        }
        #endregion

        #region Decode
        public static string Decode(string _input, string _password)
        {
            byte[] _withSalt = Convert.FromBase64String(_input);

            byte[] _salt = ExtractSalt(_withSalt);
            byte[] _inputBytes = ExtractEncryptedData(_salt, _withSalt);

            byte[] _key, _iv;
            EvpBytesToKey(_password, _salt, out _key, out _iv);
            return Decrypt(_inputBytes, _key, _iv);
        }
        private static string Decrypt(byte[] _input, byte[] _key, byte[] _iv)
        {
            RijndaelManaged _aesAlgorithm = null;
            string _result = "";

            try
            {
                _aesAlgorithm = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    KeySize = 256,
                    BlockSize = 128,
                    Key = _key,
                    IV = _iv
                };

                ICryptoTransform _decryptor = _aesAlgorithm.CreateDecryptor(_aesAlgorithm.Key, _aesAlgorithm.IV);

                using (MemoryStream _memoryStream = new MemoryStream(_input))
                {
                    using (CryptoStream _cryptoStream = new CryptoStream(_memoryStream, _decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader _streamReader = new StreamReader(_cryptoStream))
                        {
                            _result = _streamReader.ReadToEnd();
                            _streamReader.Close();
                        }
                    }
                }
            }
            finally
            {
                _aesAlgorithm?.Clear();
            }

            return _result;
        }
        #endregion
    }
}
