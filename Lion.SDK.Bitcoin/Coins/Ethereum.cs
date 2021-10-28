using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Encrypt;
using Lion.Net;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Ethereum
    {
        #region GenerateAddress
        public static Address GenerateAddress(string _privateKey = "")
        {
            Address _address = new Address();
            _address.PrivateKey = _privateKey == "" ? RandomPlus.RandomHex(64) : _privateKey;
            _address.PublicKey = HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_address.PrivateKey));
            _address.PublicKey = _address.PublicKey.Substring(2); //remove 04 start;

            Keccak256 _keccakHasher = new Keccak256();
            string _hexAddress = _keccakHasher.ComputeHashByHex(_address.PublicKey);

            _address.Text = "0x" + _hexAddress.Substring(_hexAddress.Length - 40);
            return _address;
        }
        #endregion

        #region IsAddress
        public static bool IsAddress(string _address)
        {
            if (!_address.StartsWith("0x")) { return false; }

            string _num64 = _address.Substring(2);
            BigInteger _valueOf = BigInteger.Zero;
            if (BigInteger.TryParse(_num64, System.Globalization.NumberStyles.AllowHexSpecifier, null, out _valueOf))
            {
                return _valueOf != BigInteger.Zero;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
