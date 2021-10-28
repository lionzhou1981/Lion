using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class TRX
    {
        public static bool IsAddress(string _address)
        {
            if (_address.StartsWith("41") && _address.Length == 42)
                return true;
            if (!_address.StartsWith("41"))
            {
                try
                {
                    var _decoded = HexPlus.ByteArrayToHexString(Base58.Decode(_address));
                    if (_decoded.Length != 50)
                        return false;
                }
                catch { return false; }
                return true;
            }
            return false;
        }
    }
}
