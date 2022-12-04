using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Ethereum
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

            _address.Text = "0x" + _hexAddress.Substring(_hexAddress.Length - 40);
            return _address;
        }
        #endregion

        public static string PubKeyToAddress(string _pubKey)
        {
            Keccak256 _keccakHasher = new Keccak256();
            string _hexAddress = _keccakHasher.ComputeHashByHex(_pubKey);
            return "0x" + _hexAddress.Substring(_hexAddress.Length - 40);
        }
        
        #region Check
        public static bool Check(string _address)
        {
            if (!_address.StartsWith("0x")) { return false; }
            if (_address.Length != 42) { return false; }

            _address = _address.Substring(2);
            if (!BigInteger.TryParse(_address, NumberStyles.AllowHexSpecifier, null, out BigInteger _value)) { return false; }
            if (_value <= BigInteger.Zero) { return false; }

            return true;
        }
        #endregion
    }
}
