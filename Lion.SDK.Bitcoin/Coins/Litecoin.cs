﻿using System;
using Lion;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
    //LTC
    public class Litecoin
    {
        public static bool IsAddress(string _address, out byte? _version)
        {
            if (_address.ToLower().StartsWith("bc") || _address.ToLower().StartsWith("tb")) { _version = null; return false; }

            return Bitcoin.IsAddress(_address, out _version);
        }

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://api.blockcypher.com/v1/ltc/main";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                return _json["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}