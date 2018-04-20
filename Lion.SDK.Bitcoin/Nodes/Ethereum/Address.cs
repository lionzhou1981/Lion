using System;
using System.Collections.Generic;
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
    }
}
