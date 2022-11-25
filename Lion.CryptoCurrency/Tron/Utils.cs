using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Tron
{
    public class Utils
    {
        public static string DecodeIP(string _ip)
        {
            var _len = _ip.Length / 2;
            var _hex = "42" + _len.ToString("X").ToString().ToLower().PadLeft(2, '0') + _ip;
            var _info = Lion.CryptoCurrency.Tron.TransactionInfo.NodeInfo.Types.PeerInfo.Parser.ParseFrom(Lion.HexPlus.HexStringToByteArray(_hex));
            return JObject.Parse(_info.ToString())["host"].ToString();
        }
    }
}
