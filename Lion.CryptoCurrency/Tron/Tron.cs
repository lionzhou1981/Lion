using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Lion.CryptoCurrency.Tron
{
    public class Tron
    {
        public const string TRC20_METHOD_TOTALSUPPLY = "18160ddd";
        public const string TRC20_METHOD_BALANCEOF = "70a08231";
        public const string TRC20_METHOD_TRANSFER = "a9059cbb";
        public const string TRC20_METHOD_TRANSFERFROM = "23b872dd";
        public const string TRC20_METHOD_APPROVE = "095ea7b3";
        public const string TRC20_METHOD_ALLOWANCE = "dd62ed3e";

        #region HexToDecimal
        public static string HexToDecimal(string _hex, int _decimal = 18)
        {
            _hex = "0" + _hex;
            string _value = BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier).ToString();
            if (_value.Length < _decimal) { _value = _value.PadLeft(_decimal + 1, '0'); }
            return _value.Substring(0, _value.Length - _decimal) + "." + _value.Substring(_value.Length - _decimal);
        }
        #endregion

        #region DecodeIP
        public static string DecodeIP(string _ip)
        {
            var _len = _ip.Length / 2;
            var _hex = "42" + _len.ToString("X").ToString().ToLower().PadLeft(2, '0') + _ip;
            var _info = TransactionInfo.NodeInfo.Types.PeerInfo.Parser.ParseFrom(HexPlus.HexStringToByteArray(_hex));
            return JObject.Parse(_info.ToString())["host"].ToString();
        }
        #endregion
    }
}
