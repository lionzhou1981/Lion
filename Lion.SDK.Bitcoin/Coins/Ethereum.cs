using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
    }
}
