using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
    public static class BitcoinHelper
    {
        public static string AddressToPublicKey(string _address)
        {
            var _base58Decode = Lion.Encrypt.Base58.Decode(_address);
            var _pubKeyBytes = new byte[_base58Decode.Length - 5];
            Buffer.BlockCopy(_base58Decode, 1, _pubKeyBytes, 0, 20);
            return Lion.HexPlus.ByteArrayToHexString(_pubKeyBytes);
        }

        public static string AddressToPKSH(string _address)
        {
            return PublicKeyToPKSH(AddressToPublicKey(_address));
        }

        public static string PublicKeyToPKSH(string _publicKey)
        {
            var _hashed = new Lion.Encrypt.RIPEMD160Managed().ComputeHash(new SHA256Managed().ComputeHash(Lion.HexPlus.HexStringToByteArray(_publicKey))).ToList();
            _hashed.Insert(0, 0x14);//PKSH A9 -> do a RipeMD160 on the top stack item 14->push hex 14(decimal 20) bytes on stack
            _hashed.Insert(0, 0xa9);
            _hashed.Insert(0, 0x76);
            _hashed.Add(0x88);
            _hashed.Add(0xac);
            _hashed.InsertRange(0, ((BigInteger)_hashed.Count).ToByteArray());
            return  Lion.HexPlus.ByteArrayToHexString(_hashed.ToArray());
        }

        public static void AddBytes(this List<byte> _bytes, params byte[] _addBytes)
        {
            _bytes.AddRange(_addBytes);
        }

        public static void AddBytesPadRightZero(this List<byte> _bytes, int _length, params byte[] _addBytes)
        {
            _bytes.AddRange(_addBytes);
            if (_length <= _addBytes.Length)
                return;
            for (var i = 0; i < _length - _addBytes.Length; i++)
            {
                _bytes.Add(0x00);
            }
        }

        const decimal SatoshiBase = 100000000M;

        public static BigInteger DecimalToSatoshi(decimal _value)
        {
            int _valuePay = decimal.ToInt32(SatoshiBase * _value);
            return _valuePay;
        }

        public static void SendValueToPubKey(this List<byte> _scripts, string _pubKey, BigInteger _value)
        {
            AddBytesPadRightZero(_scripts, 8, _value.ToByteArray());
            AddPKSH(_scripts, _pubKey);
        }

        public static void AddPKSH(this List <byte> _scripts, string _pubKey)
        {
            var _outputPubKeyBytes = Lion.HexPlus.HexStringToByteArray(_pubKey);
            AddBytes(_scripts, _outputPubKeyBytes);
        }
    }
}
