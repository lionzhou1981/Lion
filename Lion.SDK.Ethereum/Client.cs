using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Ethereum
{
    public class Client
    {
        public string RpcHost;

        public Client(string _rpcHost)
        {
            this.RpcHost = _rpcHost;
        }

        public JObject Eth_Call(string _from, string _to, ContractABI _abi,string _quantity = "latest")
        {
            JObject _value = new JObject();
            if (_from != "") { _value["from"] = _from; }
            _value["to"] = _to;
            _value["data"] = _abi.ToData();

            JObject _json = this.BuildJsonRPC("eth_call", _value, _quantity);

            return _json;
        }

        #region BuildJsonRPC
        private JObject BuildJsonRPC(string _method, params object[] _params)
        {
            JObject _json = new JObject();
            _json["jsonrpc"] = "2.0";
            _json["method"] = _method;

            JArray _jsonParams = new JArray();
            foreach (object _item in _params)
            {
                _jsonParams.Add(_item);
            }

            _json["params"] = _json;
            _json["id"] = DateTime.UtcNow.Ticks.ToString();
            return _json;
        }
        #endregion
    }
}
