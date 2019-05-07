using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //MERCI
    public class MERCI
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://apiinfo.mercibq.com/merciinfo-api/info";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
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
