using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Geth
    {
        public static string Host;

        #region Init
        public static void Init(string _host)
        {
            Host = _host;
        }
        #endregion

        #region Eth_BlockNumber
        public static BigInteger Eth_BlockNumber()
        {
            var _result = Call("eth_blockNumber");
            if (_result.Success)
            {
                return Ethereum.HexToBigInteger(_result.Result["result"].Value<string>()) - BigInteger.One;
            }
            else
            {
                return -1;
            }
        }
        #endregion

        #region Eth_GetBlockByNumber
        public static JObject Eth_GetBlockByNumber(BigInteger _block)
        {
            var _result = Call("eth_getBlockByNumber", "1", "0x" + _block.ToString("X").TrimStart('0'), true);
            if (_result.Success)
            {
                return _result.Result["result"].Value<JObject>();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Eth_GetTransactionByHash
        public static JObject Eth_GetTransactionByHash(string _txid)
        {
            var _result = Call("eth_getTransactionByHash", "1", _txid);
            if (_result.Success)
            {
                return _result.Result["result"].Value<JObject>();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Eth_GetTransactionReceipt
        public static JObject Eth_GetTransactionReceipt(string _txid)
        {
            var _result = Call("eth_getTransactionReceipt", "1", _txid);
            if (_result.Success)
            {
                return _result.Result["result"].Value<JObject>();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Call
        public static (bool Success,JObject Result) Call(string _method, string _id = "1", params object[] _params)
        {
            try
            {
                JObject _jsonRpc = new JObject();
                _jsonRpc["jsonrpc"] = "2.0";
                _jsonRpc["method"] = _method;
                _jsonRpc["id"] = _id;

                JArray _data = new JArray();
                foreach (object _e in _params)
                {
                    if (_e is KeyValuePair<string, string>)
                    {
                        KeyValuePair<string, string> _value = (KeyValuePair<string, string>)_e;
                        JObject _sub = new JObject();
                        _sub[_value.Key] = _value.Value;
                        _data.Add(_sub);
                    }
                    else if (_e is List<KeyValuePair<string, string>>)
                    {
                        List<KeyValuePair<string, string>> _childs = (List<KeyValuePair<string, string>>)_e;
                        JObject _sub = new JObject();
                        foreach (KeyValuePair<string, string> _child in _childs)
                        {
                            _sub[_child.Key] = _child.Value;
                        }
                        _data.Add(_sub);
                    }
                    else
                    {
                        _data.Add(_e);
                    }
                }
                _jsonRpc["params"] = _data;

                HttpClient _http = new HttpClient(60000);
                _http.BeginResponse("POST", Host, "");
                _http.Request.ContentType = "application/json";
                _http.EndResponse(Encoding.UTF8.GetBytes(_jsonRpc.ToString(Formatting.None)));
                string _result = _http.GetResponseString(Encoding.UTF8);
                _http.Dispose();

                return (true, JObject.Parse(_result));
            }
            catch(Exception _ex)
            {
                return (false, new JObject() { ["error"] = _ex.ToString() });
            }
        }
        #endregion


    }
}
