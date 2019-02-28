using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //DASH
    public class DigitalCash
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://insight.dash.org/insight-api/blocks";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                return _json["blocks"][0]["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
