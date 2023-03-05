using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Lion.Net;

namespace Lion.SDK.Tronscan
{
    public class Tronscan
    {
        internal static string API_HOST = "";

        public static void Init(string _host = "https://apilist.tronscan.org/api")
        {
            API_HOST = _host;
        }

        public static long BlockNumber()
        {
            WebClientPlus _webClient = new WebClientPlus(5000);
            string _result = _webClient.DownloadString($"{API_HOST}block/latest");
            _webClient.Dispose();

            JObject _json = JObject.Parse(_result);
            return long.Parse(_json["number"].Value<string>());
        }
    }
}
