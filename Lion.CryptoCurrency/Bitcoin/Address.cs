using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Address : CryptoCurrency.Address
    {
        #region Generate
        public static Address Generate(string _privateKey = "", bool _mainNet = true)
        {
            _privateKey = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;

            BigInteger _privateInt = BigInteger.Parse("0" + _privateKey, System.Globalization.NumberStyles.HexNumber);
            byte[] _publicKey = Secp256k1.PrivateKeyToPublicKey(_privateInt);

            SHA256Managed _sha256 = new SHA256Managed();
            RIPEMD160Managed _ripemd = new RIPEMD160Managed();
            byte[] _ripemdHashed = _ripemd.ComputeHash(_sha256.ComputeHash(_publicKey));
            byte[] _addedVersion = new byte[_ripemdHashed.Length + 1];
            _addedVersion[0] = (byte)(_mainNet ? 0x00 : 0x6f);
            Array.Copy(_ripemdHashed, 0, _addedVersion, 1, _ripemdHashed.Length);

            byte[] _shaHashed = _sha256.ComputeHash(_sha256.ComputeHash(_addedVersion));
            Array.Resize(ref _shaHashed, 4);

            byte[] _result = new byte[_addedVersion.Length + _shaHashed.Length];
            Array.Copy(_addedVersion, 0, _result, 0, _addedVersion.Length);
            Array.Copy(_shaHashed, 0, _result, _addedVersion.Length, _shaHashed.Length);

            string _key1 = string.Join("", (_mainNet ? "80" : "ef"), _privateKey);//
            string _key2 = HexPlus.ByteArrayToHexString(SHA.EncodeSHA256(SHA.EncodeSHA256(HexPlus.HexStringToByteArray(_key1))).Take(4).ToArray());

            Address _address = new Address();
            _address.Text = Base58.Encode(_result);
            _address.Public = HexPlus.ByteArrayToHexString(_publicKey);
            _address.Private = Base58.Encode(_key1 + _key2);
            _address.Text = (_mainNet ? (_address.Text.StartsWith("1") ? "" : "1") : "") + _address.Text;
            return _address;
        }
        #endregion

        #region Check
        public static bool Check(string _address, out byte? _version)
        {
            try
            {
                if (_address.StartsWith("bc1") || _address.StartsWith("tb1"))
                {
                    #region Bech32
                    if (_address.Length == 42)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x00 : 0x6F);
                    }
                    else if (_address.Length == 62)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x05 : 0xC4);
                    }
                    else
                    {
                        _version = null;
                        return false;
                    }

                    try
                    {
                        Bech32.Bech32Decode(_address, out byte[] _hrp);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                    #endregion
                }
                else
                {
                    #region Base58
                    byte[] _bytes = Base58.Decode(_address);
                    if (_bytes.Length != 25) { throw new Exception(); }
                    _version = _bytes[0];

                    byte[] _byteBody = new byte[21];
                    Array.Copy(_bytes, 0, _byteBody, 0, 21);
                    byte[] _byteCheck = new byte[4];
                    Array.Copy(_bytes, 21, _byteCheck, 0, 4);
                    string _checkSum = HexPlus.ByteArrayToHexString(_byteCheck);

                    byte[] _sha256A = SHA.EncodeSHA256(_byteBody);
                    byte[] _sha256B = SHA.EncodeSHA256(_sha256A);
                    Array.Copy(_sha256B, 0, _byteCheck, 0, 4);
                    string _caleSum = HexPlus.ByteArrayToHexString(_byteCheck);

                    return _checkSum == _caleSum;
                    #endregion
                }
            }
            catch
            {
                _version = null;
                return false;
            }
        }
        #endregion

        #region Private2Public
        public static string Private2Public(string _private,bool _base58 = false)
        {            
            if (_base58)
            {
                byte[] _base58s = Base58.Decode(_private);
                _private = HexPlus.ByteArrayToHexString(_base58s.Skip(1).Take(_base58s.Length - 5).ToArray());
            }
            return HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_private));
        }
        #endregion

        #region Address2Public
        public static string Address2Public(string _address)
        {
            byte[] _decoded = Base58.Decode(_address);
            byte[] _public = new byte[_decoded.Length - 5];
            Array.Copy(_decoded, 1, _public, 0, 20);
            return HexPlus.ByteArrayToHexString(_public);
        }
        #endregion

        #region Address2PKSH
        public static string Address2PKSH(string _address) => Public2PKSH(Address2Public(_address));
        #endregion

        #region Public2PKSH
        public static string Public2PKSH(string _public)
        {
            List<byte> _hashed = HexPlus.HexStringToByteArray(_public).ToList();
            _hashed.Insert(0, 0x14);//PKSH A9 -> do a RipeMD160 on the top stack item 14->push hex 14(decimal 20) bytes on stack
            _hashed.Insert(0, 0xa9);
            _hashed.Insert(0, 0x76);
            _hashed.Add(0x88);
            _hashed.Add(0xac);
            _hashed.InsertRange(0, BigInteger.Parse(_hashed.Count.ToString()).ToByteArray());
            return HexPlus.ByteArrayToHexString(_hashed.ToArray());
        }
        #endregion
    }
}
