using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Bitcoin.Nodes
{
    public class OmniCoreClient : BitcoinCoreClient
    {
        public OmniCoreClient(string _url, string _user = "", string _pass = "") : base(_url, _user, _pass) { }

        #region OmniListBlockTransactions
        public JArray OmniListBlockTransactions()
        {
            return null;
        }
        #endregion

        #region OmniGetTransaction
        public JObject OmniGetTransaction()
        {
            return null;
        }
        #endregion

        #region OmniCreatePayloadSimpleSend
        public string OmniCreatePayloadSimpleSend(int _contract, decimal _amount)
        {
            return null;
        }
        #endregion

        #region OmniCreateRawTxOpreturn
        public string OmniCreateRawTxOpreturn(string _hex, string _payload)
        {
            return null;
        }
        #endregion

        #region OmniCreateRawTxReference
        public string OmniCreateRawTxReference(string _hex, string _address)
        {
            return null;
        }
        #endregion

        #region OmniCreateRawTxChange
        public string OmniCreateRawTxChange(string _hex, string _address)
        {
            return null;
        }
        #endregion

        #region SignRawTransaction
        public string SignRawTransaction(string _hex, JArray _txList, JArray _keys)
        {
            return "";
        }
        #endregion
    }
}