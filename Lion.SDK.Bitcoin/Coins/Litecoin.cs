using System;
using Lion;
using Lion.Encrypt;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Litecoin
    {
        public static bool IsAddress(string _address, out byte? _version)
        {
            _address = _address.ToLower();

            if (_address.StartsWith("bc") || _address.StartsWith("tb")) { _version = null; return false; }

            return Bitcoin.IsAddress(_address, out _version);
        }
    }
}