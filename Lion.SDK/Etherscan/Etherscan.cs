using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Etherscan
{
    public class Etherscan
    {
        internal static string API_KEY = "";
        internal static string API_HOST = "";

        public static void Init(string _key, string _host = "https://api.etherscan.io/api")
        {
            API_KEY = _key;
            API_HOST = _host;
        }

        public static long Eth_BlockNumber()
        {
            WebClientPlus _webClient = new WebClientPlus(5000);
            string _result = _webClient.DownloadString($"{API_HOST}?module=proxy&action=eth_blockNumber&apikey={API_KEY}");
            _webClient.Dispose();

            JObject _json = JObject.Parse(_result);
            return HexPlus.HexToInt64(_json["result"].Value<string>());
        }
    }
}
