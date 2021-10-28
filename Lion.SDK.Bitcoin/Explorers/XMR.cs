using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Explorers
{
    public class XMR
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                //string _url = "https://xmr.tokenview.com/api/blocks/xmr/1/1";
                string _url = "https://monerohash.com/api/stats?_=" + DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString() + DateTime.UtcNow.Millisecond.ToString();
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                //return _json["data"][0]["block_no"].Value<string>();
                return _json["network"]["height"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
