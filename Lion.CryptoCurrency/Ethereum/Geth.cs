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
        public static bool Debug;
        public static string Host;

        #region Init
        public static void Init(string _host, bool _debug = false)
        {
            Host = _host;
            Debug = _debug;
        }
        #endregion

        #region Eth_BlockNumber
        public static BigInteger Eth_BlockNumber()
        {
            var (Success, Result) = Call("eth_blockNumber");
            return Success ? Ethereum.HexToBigInteger(Result["result"].Value<string>()) - BigInteger.One : -1;
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

        #region Eth_EstimateGas 
        public static BigInteger Eth_EstimateGas(string _from = "", string _to = "", uint _gas = 0, Number _gasPrice = null, Number _value = null, string _data = "", uint _nonce = uint.MaxValue)
        {
            Dictionary<string, string> _values = new Dictionary<string, string>();
            if (_from != "") { _values.Add("from", _from); }
            if (_to != "") { _values.Add("to", _to); }
            if (_gas != 0) { _values.Add("gas", "0x" + BigInteger.Parse(_gas.ToString()).ToString("X").TrimStart('0')); }
            if (_gasPrice != null) { _values.Add("gasPrice", "0x" + _gasPrice.ToGWei().ToString("X").TrimStart('0')); }
            if (_value != null) { _values.Add("value", "0x" + _value.ToGWei().ToString("X").TrimStart('0')); }
            if (_data != "") { _values.Add("data", _data); }
            if (_nonce != uint.MaxValue) { _values.Add("nonce", "0x"+(new Number(_nonce).ToHex().TrimStart('0'))); }

            var (Success, Result) = Call("eth_estimateGas", "1", _values);
            return Success ? Ethereum.HexToBigInteger(Result["result"].Value<string>()) : BigInteger.MinusOne;
        }
        #endregion

        #region Eth_GasPrice
        public static BigInteger Eth_GasPrice()
        {
            var (Success, Result) = Call("eth_gasPrice");
            return Success ? Ethereum.HexToBigInteger(Result["result"].Value<string>()) : BigInteger.MinusOne;
        }
        #endregion

        #region Eth_GetBalance
        public static string Eth_GetBalance(string _address, string _flag = "latest")
        {
            var (Success, Result) = Call("eth_getBalance", "1", _address, _flag);
            return Success ? Ethereum.HexToDecimal(Result["result"].Value<string>()) : "";
        }
        #endregion

        #region Eth_GetBlockByNumber
        public static JObject Eth_GetBlockByNumber(BigInteger _block)
        {
            var (Success, Result) = Call("eth_getBlockByNumber", "1", "0x" + _block.ToString("X").TrimStart('0'), true);
            return Success ? Result["result"].Value<JObject>() : null;
        }
        #endregion

        #region Eth_GetTransactionCount
        public static BigInteger Eth_GetTransactionCount(string _address,string _tag= "latest")
        {
            var (Success, Result) = Call("eth_getTransactionCount", "1", _address, _tag);
            return Success ?  Ethereum.HexToBigInteger(Result["result"].Value<string>()) : BigInteger.MinusOne;
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

        #region Eth_SendRawTransaction
        public static string Eth_SendRawTransaction(string _hex)
        {
            var (Success, Result) = Call("eth_sendRawTransaction", "1", _hex);
            return Success ? Result["result"].Value<string>() : "";
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

                if (Debug) { Console.WriteLine(_jsonRpc.ToString(Formatting.None)); }

                HttpClient _http = new HttpClient(60000);
                _http.BeginResponse("POST", Host, "");
                _http.Request.ContentType = "application/json";
                _http.EndResponse(Encoding.UTF8.GetBytes(_jsonRpc.ToString(Formatting.None)));
                string _result = _http.GetResponseString(Encoding.UTF8);
                _http.Dispose();

                if (Debug) { Console.WriteLine(JObject.Parse(_result).ToString(Formatting.None)); }

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
