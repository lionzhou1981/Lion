using Lion.Net;
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
                string _url = "https://explorer.dash.org/chain/Dash/q/getblockcount";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                return _result;
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
