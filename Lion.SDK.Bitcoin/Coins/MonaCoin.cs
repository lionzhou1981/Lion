using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //MONA
    public class MonaCoin
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://mona.chainsight.info/api/blocks";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["blocks"].ToString());
                return _jArray[0]["height"].ToString();
            }
            catch (Exception _ex)
            {
                return "";
            }
        }
        #endregion
    }
}
