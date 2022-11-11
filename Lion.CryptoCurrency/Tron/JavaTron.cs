﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.CryptoCurrency.Tron
{
    public class JavaTron
    {
        public static bool Debug;
        public static string Host;

        #region Init
        public static void Init(string _host, bool _debug = false)
        {
            Host = _host;
            Debug = _debug;
        }
        #endregion

        #region Wallet_GetNowBlock
        public static JObject Wallet_GetNowBlock()
        {
            var (Success, Result) = Call("wallet/getnowblock");
            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetBlockByNumber
        public static JObject Wallet_GetBlockByNumber(BigInteger _block)
        {
            JObject _params = new JObject();
            _params["num"] = Int64.Parse(_block.ToString());
            var (Success, Result) = Call("wallet/getblockbynum", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetTransactionInfoById
        public static JObject Wallet_GetTransactionInfoById(string _txid)
        {
            JObject _params = new JObject();
            _params["value"] = _txid;
            var (Success, Result) = Call("wallet/gettransactioninfobyid", _params);
        
            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetTransactionById
        public static JObject Wallet_GetTransactionById(string _txid)
        {
            JObject _params = new JObject();
            _params["value"] = _txid;
            var (Success, Result) = Call("wallet/gettransactionbyid", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetAccount
        public static JObject Wallet_GetAccount(string _address)
        {
            JObject _params = new JObject();
            _params["address"] = _address;
            var (Success, Result) = Call("wallet/getaccount", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Call
        public static (bool Success, JObject Result) Call(string _method, JObject _params = null)
        {
            try
            {
                HttpClient _http = new HttpClient(60000);
                _http.BeginResponse("POST", Host + _method, "");
                if (_params != null)
                {
                    _http.EndResponse(Encoding.UTF8.GetBytes(_params.ToString(Formatting.None)));
                }
                else
                {
                    _http.EndResponse();
                }
                string _result = _http.GetResponseString(Encoding.UTF8);
                _http.Dispose();

                return (true, JObject.Parse(_result));
            }
            catch(Exception _ex)
            {
                Console.WriteLine(_ex);
                return (false, new JObject() { ["error"] = _ex.ToString() });
            }
        }
        #endregion
    }
}
