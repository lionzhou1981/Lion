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
            var (Success, Result) = Call("eth_blockNumber");
            return Success ? Ethereum.HexToBigInteger(Result["result"].Value<string>()) - BigInteger.One : -1;
        }
        #endregion

        #region Eth_GetBlockByNumber
        public static JObject Eth_GetBlockByNumber(BigInteger _block)
        {
            var (Success, Result) = Call("eth_getBlockByNumber", "1", "0x" + _block.ToString("X").TrimStart('0'), true);
            return Success ? Result["result"].Value<JObject>() : null;
        }
        #endregion

        #region Eth_Call
        public static string Eth_Call(string _from = "", string _to = "", uint _gas = 0, Number _gasPrice = null, Number _value = null, string _data = "", string _tag = "latest")
        {
            Dictionary<string, string> _values = new Dictionary<string, string>();
            if (_from != "") { _values.Add("from", _from); }
            if (_to != "") { _values.Add("to", _to); }
            if (_gas != 0) { _values.Add("gas", "0x" + BigInteger.Parse(_gas.ToString()).ToString("X").TrimStart('0')); }
            if (_gasPrice != null) { _values.Add("gasPrice", "0x" + _gasPrice.ToGWei().ToString("X").TrimStart('0')); }
            if (_value != null) { _values.Add("value", "0x" + _value.ToGWei().ToString("X").TrimStart('0')); }
            if (_data != "") { _values.Add("data", _data); }

            var (Success, Result) = Call("eth_call", "1", _values, _tag);
            return Success ? Result["result"].Value<string>() : "";
        }
        #endregion

        #region Eth_GetTransactionByHash
        public static JObject Eth_GetTransactionByHash(string _txid)
        {
            var (Success, Result) = Call("eth_getTransactionByHash", "1", _txid);
            return Success ? Result["result"].Value<JObject>() : null;
        }
        #endregion

        #region Eth_GetTransactionReceipt
        public static JObject Eth_GetTransactionReceipt(string _txid)
        {
            var (Success, Result) = Call("eth_getTransactionReceipt", "1", _txid);
            return Success ? Result["result"].Value<JObject>() : null;
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
                    if (_e is KeyValuePair<string, string> _value)
                    {
                        JObject _sub = new JObject();
                        _sub[_value.Key] = _value.Value;
                        _data.Add(_sub);
                    }
                    else if (_e is List<KeyValuePair<string, string>> _childs)
                    {
                        JObject _sub = new JObject();
                        foreach (KeyValuePair<string, string> _child in _childs)
                        {
                            _sub[_child.Key] = _child.Value;
                        }
                        _data.Add(_sub);
                    }
                    else if (_e is Dictionary<string, string> _dicts)
                    {
                        JObject _sub = new JObject();
                        foreach (KeyValuePair<string, string> _child in _dicts)
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
                Console.WriteLine(_ex);
                return (false, new JObject() { ["error"] = _ex.ToString() });
            }
        }
        #endregion
    }
}
