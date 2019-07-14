using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Bitcoin.Nodes
{
    public class BitcoinCoreClient
    {
        private string url;
        private string user;
        private string pass;

        public BitcoinCoreClient(string _url, string _user = "", string _pass = "")
        {
            this.url = _url;
            this.user = _user;
            this.pass = _pass;
        }

        #region GetBlockCount
        public int GetBlockCount()
        {
            return 0;
        }
        #endregion

        #region GetBlockHash
        public string GetBlockHash()
        {
            return "";
        }
        #endregion

        #region GetBlock
        public JObject GetBlock()
        {
            return null;
        }
        #endregion

        #region GetRawTransaction
        public JObject GetRawTransaction()
        {
            return null;
        }
        #endregion

        #region EstimateSmartFee
        public decimal EstimateSmartFee(int _block = 10)
        {
            return 0M;
        }
        #endregion

        #region CreateRawTransaction
        public string CreateRawTransaction(JArray _in, JArray _out)
        {
            return "";
        }
        #endregion

        #region SignRawTransactionWithKey
        public string SignRawTransactionWithKey(string _hex)
        {
            return "";
        }
        #endregion

        #region SendRawTransaction
        public string SendRawTransaction(string _hexSigned)
        {
            return "";
        }
        #endregion

        #region Request
        private JObject Request(JObject _json)
        {
            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", this.url, "");
            _http.Request.Credentials = new NetworkCredential(this.user, this.pass);
            _http.Request.ContentType = "application/json";
            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None)));
            string _result = _http.GetResponseString(Encoding.UTF8);

            return JObject.Parse(_result);
        }
        #endregion
    }
}