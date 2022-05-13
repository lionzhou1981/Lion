﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Ethereum
    {
        public const string ERC20_METHOD_TOTALSUPPLY = "0x18160ddd";
        public const string ERC20_METHOD_BALANCEOF = "0x70a08231";
        public const string ERC20_METHOD_TRANSFER = "0xa9059cbb";
        public const string ERC20_METHOD_TRANSFERFROM = "0x23b872dd";
        public const string ERC20_METHOD_APPROVE = "0x095ea7b3";
        public const string ERC20_METHOD_ALLOWANCE = "0xdd62ed3e";

        public const uint CHAIN_ID_MAINNET = 1;
        public const uint CHAIN_ID_ROPSTEN = 3;
        public const uint CHAIN_ID_RINKEBY = 4;
        public const uint CHAIN_ID_GOERLI = 5;
        public const uint CHAIN_ID_KOVAN = 42;
        public const uint CHAIN_ID_PRIVATE = 1337;

        #region HexToBigInteger
        public static BigInteger HexToBigInteger(string _hex)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            return BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier);
        }
        #endregion

        #region HexToDecimal
        public static string HexToDecimal(string _hex, int _decimal = 18)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            string _value = BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier).ToString();
            if (_value.Length < _decimal) { _value = _value.PadLeft(_decimal + 1, '0'); }
            return _value.Substring(0, _value.Length - _decimal) + "." + _value.Substring(_value.Length - _decimal);
        }
        #endregion

    }
}