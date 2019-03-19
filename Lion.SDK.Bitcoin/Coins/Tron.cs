using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Tron
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

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://apilist.tronscan.org/api/system/status";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                return _json["database"]["block"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
