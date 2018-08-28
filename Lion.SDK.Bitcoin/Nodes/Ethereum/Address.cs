using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lion.SDK.Bitcoin.Nodes.Ethereum
{
    public class Address
    {
        private string address;

        public Address(string _address)
        {
            this.address = _address.IndexOf("0x") > -1 ? _address.Substring(2) : _address;
        }

        public byte[] ToData()
        {
            return HexPlus.HexStringToByteArray(this.address);
        }

        public static Address Parse(string _address)
        {
            return new Address(_address);
        }

        public static bool IsAddress(string _address)
        {
            if (!_address.StartsWith("0x"))
                return false;
            var _num64 = _address.Substring(2);
            BigInteger _valueOf = BigInteger.Zero;
            if (System.Numerics.BigInteger.TryParse(_num64, System.Globalization.NumberStyles.AllowHexSpecifier, null, out _valueOf))
                return _valueOf != BigInteger.Zero;
            else
                return false;
        }
    }
}
