using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    //ADA
    public class Cardano
    {
        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                string _url = "https://cardanoexplorer.com/api/blocks/pages";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                string _cbeEpoch = _json["Right"][1][0]["cbeEpoch"].Value<string>();
                string _cbeSlot = _json["Right"][1][0]["cbeSlot"].Value<string>();

                return $"{_cbeEpoch}{_cbeSlot.PadLeft(6, '0')}";
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
