using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Address : CryptoCurrency.Address
    {
        public Address(string _text) : base(_text) { }

        #region GetLegacyAddress
        public static Address GetLegacyAddress(string _private = "", bool _mainnet = true)
        {
            _private = _private == "" ? RandomPlus.RandomHex(64) : _private;

            BigInteger _privateInt = BigInteger.Parse("0" + _private, NumberStyles.HexNumber);
            byte[] _public = Secp256k1.PrivateKeyToPublicKey(_privateInt, false);

            RIPEMD160Managed _ripemd = new RIPEMD160Managed();

            byte[] _ripemdHashed = _ripemd.ComputeHash(SHA.EncodeSHA256(_public));
            byte[] _addedVersion = new byte[_ripemdHashed.Length + 1];
            _addedVersion[0] = (byte)(_mainnet ? 0x00 : 0x6f);
            Array.Copy(_ripemdHashed, 0, _addedVersion, 1, _ripemdHashed.Length);

            byte[] _shaHashed = SHA.EncodeSHA256(SHA.EncodeSHA256(_addedVersion));
            Array.Resize(ref _shaHashed, 4);

            byte[] _result = new byte[_addedVersion.Length + _shaHashed.Length];
            Array.Copy(_addedVersion, 0, _result, 0, _addedVersion.Length);
            Array.Copy(_shaHashed, 0, _result, _addedVersion.Length, _shaHashed.Length);

            string _key1 = string.Join("", (_mainnet ? "80" : "ef"), _private);
            string _key2 = HexPlus.ByteArrayToHexString(SHA.EncodeSHA256(SHA.EncodeSHA256(HexPlus.HexStringToByteArray(_key1))).Take(4).ToArray());

            Address _address = new Address(Base58.Encode(_result))
            {
                Public = HexPlus.ByteArrayToHexString(_public),
                Private = Base58.Encode(_key1 + _key2)
            };

            return _address;
        }
        #endregion

        #region GetSegWitAddress
        public static Address GetSegWitAddress(string _private = "", bool _mainnet = true)
        {
            return null;
        }
        #endregion

        #region GetMultiSignAddress
        public static Address GetMultisignAddress(string[] _privates = null, int _required = 1, bool _mainnet = true)
        {
            if (_privates == null) { _privates = new string[1]; }
            if (_privates.Length < _required) { throw new ArgumentException("Key count lower then require count"); }

            for (int i = 0; i < _privates.Length; i++) { _privates[i] = string.IsNullOrWhiteSpace(_privates[i]) ? RandomPlus.RandomHex(64) : _privates[i]; }

            string _publicScript = Privates2PublicScript(_privates, _required);
            byte[] _signArray = HexPlus.HexStringToByteArray(_publicScript);

            RIPEMD160Managed _ripemd = new RIPEMD160Managed();
            byte[] _ripemdHashed = _ripemd.ComputeHash(SHA.EncodeSHA256(_signArray));

            byte[] _addedVersion = new byte[_ripemdHashed.Length + 1];
            _addedVersion[0] = (byte)(_mainnet ? (byte)5 : (byte)196);
            Array.Copy(_ripemdHashed, 0, _addedVersion, 1, _ripemdHashed.Length);

            byte[] _shaHashed = SHA.EncodeSHA256(SHA.EncodeSHA256(_addedVersion));
            Array.Resize(ref _shaHashed, 4);

            byte[] _result = new byte[_addedVersion.Length + _shaHashed.Length];
            Array.Copy(_addedVersion, 0, _result, 0, _addedVersion.Length);
            Array.Copy(_shaHashed, 0, _result, _addedVersion.Length, _shaHashed.Length);

            for (int i = 0; i < _privates.Length; i++) { _privates[i] = Private2Wif(_privates[i], _mainnet, true); }

            Address _address = new Address(Base58.Encode(_result))
            {
                Public = _publicScript,
                Privates = _privates
            };

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

        #region Privates2PublicScript
        public static string Privates2PublicScript(string[] _privates, int _required)
        {
            return Publics2PublicScript(_privates.Select(t => Address.Private2Public(t, false, true)).ToArray(), _required);
        }
        #endregion

        #region Publics2PublicScript
        public static string Publics2PublicScript(string[] _publics, int _required)
        {
            try
            {
                List<byte> _bytes = new List<byte>();
                _bytes.Add((byte)(80 + _required));

                foreach (string _public in _publics)
                {
                    byte[] _keyBytes = HexPlus.HexStringToByteArray(_public);

                    _bytes.AddRange(((BigInteger)_keyBytes.Length).ToByteArray());
                    _bytes.AddRange(_keyBytes);
                }

                _bytes.Add((byte)(80 + _publics.Length));
                _bytes.Add(0xae);

                return HexPlus.ByteArrayToHexString(_bytes.ToArray());
            }
            catch
            {
                throw new Exception("Public keys convert to  PublicScript error");
            }
        }
        #endregion

        #region Private2Public
        public static string Private2Public(string _private, bool _base58 = false, bool _compressedPublicKey = false)
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

        #region Private2Wif
        public static string Private2Wif(string _private, bool _mainnet, bool _compressed)
        {
            List<byte> _resultArray = new List<byte>();
            _resultArray.Add((byte)(_mainnet ? 0x80 : 0xef));
            _resultArray.AddRange(HexPlus.HexStringToByteArray(_private));
            if (_compressed) { _resultArray.Add(0x01); }

            _resultArray.AddRange(SHA.EncodeSHA256(SHA.EncodeSHA256(_resultArray.ToArray())).Take(4).ToArray());
            return Base58.Encode(_resultArray.ToArray());
        }
        #endregion

        #region Wif2Private
        public static string Wif2Private(string _wif, out bool _mainnet, out bool _compressed)
        {
            byte[] _decoded = Base58.Decode(_wif);
            _mainnet = _decoded[0] == 0x80;

            IList<byte> _key = _decoded.Skip(1).ToList();
            byte[] _keyCheckSum = _key.Skip(_key.Count - 4).Take(4).ToArray();

            _key = _key.Take(_key.Count - 4).ToList();
            _compressed = false;

            if (_key.Last() == 0x01)
            {
                _key = _key.Take(_key.Count - 1).ToList();
                _compressed = true;
            }

            string _result = HexPlus.ByteArrayToHexString(_key.ToArray());

            List<byte> _resultArray = new List<byte>();
            _resultArray.Add((byte)(_mainnet ? 0x80 : 0xef));
            _resultArray.AddRange(_key.ToArray());
            if (_compressed) { _resultArray.Add(0x01); }

            string _checksum = HexPlus.ByteArrayToHexString(SHA.EncodeSHA256(SHA.EncodeSHA256(_resultArray.ToArray())).Take(4).ToArray());
            if (_checksum != HexPlus.ByteArrayToHexString(_keyCheckSum)) { throw new Exception("Checksum failed."); }

            return _result;
        }
        #endregion

        #region Address2PKSH
        public static string Address2PKSH(string _address) => Public2PKSH(Address2Public(_address), _address.StartsWith("2") || _address.StartsWith("3"));
        #endregion

        #region Public2PKSH
        public static string Public2PKSH(string _public, bool _multiSig = false)
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
            List<byte> _hash160 = new RIPEMD160Managed().ComputeHash(SHA.EncodeSHA256(HexPlus.HexStringToByteArray(_public))).ToList();
            _hash160.InsertRange(0, ((BigInteger)_hash160.Count).ToByteArray());
            _hash160.Insert(0, 0x00);
            _hash160.Insert(0, 0x16);
            _hash160.Insert(0, 0x17);
            return BitConverter.ToString(_hash160.ToArray()).ToLower().Replace("-", "");
        }
        #endregion

        #region PrivateDecompress
        public static string PrivateDecompress(string _compressed)
        {
            byte[] _decoded = Base58.Decode(_compressed);
            return HexPlus.ByteArrayToHexString(_decoded.Skip(1).Take(_decoded.Length - 5).ToArray());
        }
        #endregion

        #region Privates
        public string[] Privates;
        public override string Private
        {
            get => Privates[0];
            set
            {
                if (Privates == null) { Privates = new string[1]; }
                Privates[0] = value;
            }
        }
        #endregion
    }
}
