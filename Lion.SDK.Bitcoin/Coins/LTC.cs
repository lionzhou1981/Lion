using System;
using Lion;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
    public class LTC
    {
        public static bool IsAddress(string _address, out byte? _version)
        {
            if (_address.ToLower().StartsWith("bc") || _address.ToLower().StartsWith("tb")) { _version = null; return false; }

            return Bitcoin.IsAddress(_address, out _version);
        }
    }
}