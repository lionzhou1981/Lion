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

        public static Address GenerateAddress(string _existsPrivateKey = "")
        {
            Address _address = new Address();
            _address.PrivateKey = string.IsNullOrWhiteSpace(_existsPrivateKey) ? RandomPlus.GenerateHexKey(64) : _existsPrivateKey;
            
            _address.PublicKey =  HexPlus.ByteArrayToHexString(Secp256k1.PrivateKeyToPublicKey(_address.PrivateKey));
            _address.PublicKey = _address.PublicKey.Substring(2);//remove 04 start;
            var _keccakHasher = new Keccak256();
            var _hexAddress = _keccakHasher.ComputeHashByHex(_address.PublicKey);
            _address.Text = "0x" + _hexAddress.Substring(_hexAddress.Length - 40);

            return _address;
        }
    }
}
