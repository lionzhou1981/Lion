using Lion.Encrypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Lion.CryptoCurrency.Tron
{
    public class Address : CryptoCurrency.Address
    {
        public Address(string _text) : base(_text) { }

        public byte[] ToBytes() => HexPlus.HexStringToByteArray(base.Text.Substring(2));

        #region Generate
        public static Address Generate(string _privateKey = "")
        {
            Address _address = new Address("");
            _address.Private = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;
            _address.Public = HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_address.Private));
            _address.Public = _address.Public.Substring(2);
            Keccak256 _keccakHasher = new Keccak256();
            string _hexAddress = _keccakHasher.ComputeHashByHex(_address.Public);
            _hexAddress = "41" + _hexAddress.Substring(_hexAddress.Length - 40); //address hex
            _address.Text = HexToAddress(_hexAddress);
            return _address;
        }
        #endregion

        #region HexToAddress
        public static string HexToAddress(string _hexAddress)
        {
            var _sha = SHA256.Create();
            var _doubleShaed = _sha.ComputeHash(_sha.ComputeHash(BigInteger.Parse(_hexAddress, System.Globalization.NumberStyles.HexNumber).ToByteArrayUnsigned(true)));
            var _checkSum = Lion.HexPlus.ByteArrayToHexString(_doubleShaed).Substring(0, 8);
            _hexAddress = _hexAddress + _checkSum;
            return Base58.Encode(_hexAddress);
        }
        #endregion


        #region AddressToHex
        public static string AddressToHex(string _address)
        {
            var _decoded = Base58.Decode(_address);
            var _hex = Lion.HexPlus.ByteArrayToHexString(_decoded);
            return _hex.Substring(0,_hex.Length - 8);            
        }
        #endregion
    }
}
