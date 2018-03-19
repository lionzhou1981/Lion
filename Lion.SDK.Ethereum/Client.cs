using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Ethereum
{
    public class Client
    {
        public string RpcHost;

        public Client(string _rpcHost)
        {
            this.RpcHost = _rpcHost;
        }

        #region Eth_Call
        public JObject Eth_Call(string _from, string _to, ContractABI _abi, string _gas = "", string _gasPrice = "", string _quantity = "latest")
        {
            JObject _value = new JObject();
            if (_from != "") { _value["from"] = _from; }
            _value["to"] = _to;
            _value["data"] = _abi.ToData();
            if (_gas != "") { _value["gas"] = _gas; }
            if (_gasPrice != "") { _value["gasPrice"] = _gasPrice; }

            JObject _json = this.Build("eth_call", _value, _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_EstimateGas
        public JObject Eth_EstimateGas(ContractABI _abi)
        {
            JObject _value = new JObject();
            _value["data"] = _abi.ToData();

            JObject _json = this.Build("eth_estimateGas", _value);
            return this.Request(_json);
        }
        #endregion

        #region Eth_GasPrice
        public JObject Eth_GasPrice()
        {
            JObject _json = this.Build("eth_gasPrice");
            return this.Request(_json);
        }
        #endregion

        #region Eth_GetBalance
        public JObject Eth_GetBalance(string _address, string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getBalance", _address, _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Personal_NewAccount
        public JObject Personal_NewAccount(string _random ="")
        {
            JObject _json = this.Build("personal_newAccount", _random);

            return this.Request(_json);
        }
        #endregion

        #region Personal_UnlockAccount
        public JObject Personal_UnlockAccount(string _address,string _password = "")
        {
            JObject _json = this.Build("personal_unlockAccount", _address, _password);
            return this.Request(_json);
        }
        #endregion

        #region Build
        private JObject Build(string _method, params object[] _params)
        {
            JObject _json = new JObject();
            _json["jsonrpc"] = "2.0";
            _json["method"] = _method;

            JArray _jsonParams = new JArray();
            foreach (object _item in _params)
            {
                _jsonParams.Add(_item);
            }

            _json["params"] = _jsonParams;
            _json["id"] = DateTime.UtcNow.Ticks.ToString();
            return _json;
        }
        #endregion

        #region Request
        private JObject Request(JObject _json)
        {
            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", this.RpcHost, "");
            _http.Request.ContentType = "application/json";
            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Newtonsoft.Json.Formatting.None)));
            string _result = _http.GetResponseString(Encoding.UTF8);

            return JObject.Parse(_result);
        }
        #endregion
    }
}
