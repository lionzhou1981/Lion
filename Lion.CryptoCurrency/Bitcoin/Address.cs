using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Address : CryptoCurrency.Address
    {
        #region GenerateMultiSignAddress
        public static string Privats2ScriptPubKey(int _requireKeyCount, List<string> _privateKeys)
        {
            return Publics2ScriptPubKey(_requireKeyCount, _privateKeys.Select(t => Address.Private2Public(t, false, true)).ToList());
        }

        public static string Publics2ScriptPubKey(int _requireKeyCount, List<string> _publicKeys)
        {
            try
            {
                using (MemoryStream _keyStream = new MemoryStream())
                {
                    _keyStream.WriteByte((byte)(80 + _requireKeyCount));//RequireCount
                    _publicKeys.ForEach(t =>
                    {
                        var _keyBytes = Lion.HexPlus.HexStringToByteArray(t);
                        var _lengthBytes = ((BigInteger)_keyBytes.Length).ToByteArray();
                        _keyStream.Write(_lengthBytes, 0, _lengthBytes.Length);
                        _keyStream.Write(_keyBytes, 0, _keyBytes.Length);
                    });
                    _keyStream.WriteByte((byte)(80 + _publicKeys.Count));//KEYCOUNT
                    _keyStream.WriteByte(0xae);//OP_CHECKMULTISIG=0xae
                    var _signArray = _keyStream.ToArray();
                    return BitConverter.ToString(_signArray).ToLower().Replace("-", "");
                }
            }
            catch
            {
                throw new Exception("Public keys convert to scriptpubkey error");
            }
        }
        public static Address GenerateMultiSignAddress(int _requireKeyCount, List<string> _privateKeys, bool _mainNet)
        {
            if (_privateKeys.Count < _requireKeyCount)
                throw new ArgumentException("Key count lower then require count");
            var _scriptPubHash = Publics2ScriptPubKey(_requireKeyCount, _privateKeys.Select(t=>Address.Private2Public(t,false,true)).ToList());
            var _signArray = Lion.HexPlus.HexStringToByteArray(_scriptPubHash);
            SHA256Managed _sha256 = new SHA256Managed();
            RIPEMD160Managed _ripemd = new RIPEMD160Managed();
            var _notVersioned = _ripemd.ComputeHash(_sha256.ComputeHash(_signArray));
            var _versioned = new List<byte>();
            _versioned.Add(_mainNet ? (byte)5 : (byte)196);
            _versioned.AddRange(_notVersioned);
            var _doubleSHA = _sha256.ComputeHash(_sha256.ComputeHash(_versioned.ToArray())); //double sha take 4  for verify code 
            _versioned.AddRange(_doubleSHA.Take(4));
            var _address = Base58.Encode(_versioned.ToArray());
            return new Address()
            {
                Public = _scriptPubHash,
                Text = _address
            };
        }
        #endregion

        #region Generate
        public static Address Generate(string _privateKey = "", bool _mainNet = true, bool _compressed = false)
        {
            _privateKey = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;

            BigInteger _privateInt = BigInteger.Parse("0" + _privateKey, System.Globalization.NumberStyles.HexNumber);
            byte[] _publicKey = Secp256k1.PrivateKeyToPublicKey(_privateInt, _compressed);

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
        public static string Private2Public(string _private, bool _base58 = false,bool _compressedPublicKey = false)
        {
            if (_base58)
            {
                byte[] _base58s = Base58.Decode(_private);
                _private = HexPlus.ByteArrayToHexString(_base58s.Skip(1).Take(_base58s.Length - 5).ToArray());
            }
            return HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_private, _compressedPublicKey));
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


        #region PrivateDecompress
        public static string PrivateDecompress(string _compressed)
        {
            var _decoded = Base58.Decode(_compressed);
            return HexPlus.ByteArrayToHexString(_decoded.Skip(1).Take(_decoded.Length - 5).ToArray());
        }
        #endregion

        #region Private2Wif
        public static string Private2Wif(string _privateKeyHex,bool _mainNet,bool _compressed)
        {
            List<byte> _resultArray = new List<byte>();
            _resultArray.Add((byte)(_mainNet ? 0x80 : 0xef));
            _resultArray.AddRange(Lion.HexPlus.HexStringToByteArray(_privateKeyHex));
            if (_compressed)
                _resultArray.Add(0x01);
            SHA256Managed _sha256 = new SHA256Managed();
            _resultArray.AddRange(_sha256.ComputeHash(_sha256.ComputeHash(_resultArray.ToArray())).Take(4).ToArray());
            return Base58.Encode(_resultArray.ToArray());
        }
        #endregion

        #region Wif2Private
        public static string Wif2Private(string _wifKey,out bool _mainNet,out bool _compressed)
        {
            var _allDecoded = Base58.Decode(_wifKey);
            _mainNet = _allDecoded[0] == 0x80;
            var _key = _allDecoded.Skip(1).ToList();
            var _keyCheckSum = _key.Skip(_key.Count - 4).Take(4).ToArray();
            _key = _key.Take(_key.Count - 4).ToList();
            _compressed = false;
            if (_key.Last() == 0x01)
            {
                _key = _key.Take(_key.Count - 1).ToList();
                _compressed = true;
            }
            var _result =  BitConverter.ToString(_key.ToArray()).ToLower().Replace("-","");
            List<byte> _resultArray = new List<byte>();
            _resultArray.Add((byte)(_mainNet ? 0x80 : 0xef));
            _resultArray.AddRange(Lion.HexPlus.HexStringToByteArray(_result));
            if (_compressed)
                _resultArray.Add(0x01);
            SHA256Managed _sha256 = new SHA256Managed();
            var _checksum = Lion.HexPlus.ByteArrayToHexString(_sha256.ComputeHash(_sha256.ComputeHash(_resultArray.ToArray())).Take(4).ToArray());
            if (_checksum != Lion.HexPlus.ByteArrayToHexString(_keyCheckSum))
                throw new Exception("Check sum error");
            return _result;
        }
        #endregion

        #region Address2PKSH
        public static string Address2PKSH(string _address) => Public2PKSH(Address2Public(_address),_address.StartsWith("2") || _address.StartsWith("3"));
        #endregion

        #region Public2PKSH
        public static string Public2PKSH(string _public,bool _multiSig = false)
        {
            List<byte> _hashed = HexPlus.HexStringToByteArray(_public).ToList();
            _hashed.Insert(0, 0x14);//PKSH A9 -> do a RipeMD160 on the top stack item 14->push hex 14(decimal 20) bytes on stack
            _hashed.Insert(0, 0xa9);
            if (_multiSig)
            {               
                _hashed.Add(0x87);
            }
            else
            {
                _hashed.Insert(0, 0x76);
                _hashed.Add(0x88);
                _hashed.Add(0xac);
            }
            _hashed.InsertRange(0, BigInteger.Parse(_hashed.Count.ToString()).ToByteArray());
            return HexPlus.ByteArrayToHexString(_hashed.ToArray());
        }
        #endregion

        #region Public2P2SH
        public static string Public2P2SH(string _public)
        {
            var _hash160 = new RIPEMD160Managed().ComputeHash(new SHA256Managed().ComputeHash(Lion.HexPlus.HexStringToByteArray(_public))).ToList();
            _hash160.InsertRange(0,((BigInteger)_hash160.Count).ToByteArray());
            _hash160.Insert(0, 0x00);
            _hash160.Insert(0, 0x16);
            _hash160.Insert(0, 0x17);
            return BitConverter.ToString(_hash160.ToArray()).ToLower().Replace("-", "");
        }
        #endregion
    }
}
