using System;
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
        public static string HostSolidity;

        #region Init
        public static void Init(string _host, string _hostSolidity = "", bool _debug = false)
        {
            Host = _host;
            HostSolidity = _hostSolidity;
            Debug = _debug;
        }
        #endregion

        #region Wallet_BroadcastHex
        public static JObject Wallet_BroadcastHex(string _hex)
        {
            JObject _params = new JObject();
            _params["transaction"] = _hex;
            var (Success, Result) = Call("wallet/broadcasthex", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetNowBlock
        public static JObject Wallet_GetNowBlock(bool _solidity = false)
        {
            var (Success, Result) = _solidity ? Call("walletsolidity/getnowblock", null, true) : Call("wallet/getnowblock");
            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetBlockByNumber
        public static JObject Wallet_GetBlockByNumber(BigInteger _block, bool _solidity = false)
        {
            JObject _params = new JObject();
            _params["num"] = Int64.Parse(_block.ToString());

            var (Success, Result) = _solidity ? Call("walletsolidity/getblockbynum", _params,true) : Call("wallet/getblockbynum", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetTransactionInfoById
        public static JObject Wallet_GetTransactionInfoById(string _txid, bool _solidity = false)
        {
            JObject _params = new JObject();
            _params["value"] = _txid;
            var (Success, Result) = _solidity ? Call("walletsolidity/gettransactioninfobyid", _params, true) : Call("wallet/gettransactioninfobyid", _params);
        
            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetTransactionById
        public static JObject Wallet_GetTransactionById(string _txid, bool _solidity = false)
        {
            JObject _params = new JObject();
            _params["value"] = _txid;

            var (Success, Result) = _solidity ? Call("walletsolidity/gettransactionbyid", _params,true) : Call("wallet/gettransactionbyid", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_GetAccount
        public static JObject Wallet_GetAccount(string _address, bool _solidity = false)
        {
            JObject _params = new JObject();
            _params["address"] = _address;

            var (Success, Result) = _solidity ? Call("walletsolidity/getaccount", _params, true) : Call("wallet/getaccount", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_TriggerConstantContract 
        public static JObject Wallet_TriggerConstantContract(string _contract, string _function, string _parameter, string _address)
        {
            JObject _params = new JObject();
            _params["contract_address"] = _contract;
            _params["function_selector"] = _function;
            _params["parameter"] = _parameter;
            _params["owner_address"] = _address;
            var (Success, Result) = Call("wallet/triggerconstantcontract", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Wallet_TriggerSmartContract
        public static JObject Wallet_TriggerSmartContract(string _contract, string _function, string _parameter, string _address, int _fee = 100000000, int _value = 0)
        {
            JObject _params = new JObject();
            _params["contract_address"] = _contract;
            _params["function_selector"] = _function;
            _params["parameter"] = _parameter;
            _params["owner_address"] = _address;
            _params["fee_limit"] = _fee;
            _params["call_value"] = _value;
            var (Success, Result) = Call("wallet/triggersmartcontract ", _params);

            return Success ? Result : null;
        }
        #endregion

        #region Call
        public static (bool Success, JObject Result) Call(string _method, JObject _params = null, bool _solidity = false)
        {
            try
            {
                HttpClient _http = new HttpClient(60000);
                _http.BeginResponse("POST", $"{(_solidity ? HostSolidity : Host)}{_method}", "");
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
            catch (Exception _ex)
            {
                Console.WriteLine(_ex);
                return (false, new JObject() { ["error"] = _ex.ToString() });
            }
        }
        #endregion
    }
}
