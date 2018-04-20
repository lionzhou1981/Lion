using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Bitcoin.Nodes.Ethereum
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
        public JObject Personal_NewAccount(string _random = "")
        {
            JObject _json = this.Build("personal_newAccount", _random);

            return this.Request(_json);
        }
        #endregion

        #region Personal_UnlockAccount
        public JObject Personal_UnlockAccount(string _address, string _password = "")
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

        #region Web3_ClientVersion
        public JObject Web3_ClientVersion()
        {
            JObject _json = this.Build("web3_clientVersion");
            return this.Request(_json);
        }
        #endregion

        #region Web3_Sha3
        public JObject Web3_Sha3(string _data)
        {
            JObject _json = this.Build("web3_sha3", _data);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Net_Version
        public JObject Net_Version()
        {
            JObject _json = this.Build("net_version");
            return this.Request(_json);
        }
        #endregion

        #region Net_Listening
        public JObject Net_Listening()
        {
            JObject _json = this.Build("net_listening");
            return this.Request(_json);
        }
        #endregion

        #region Net_PeerCount
        public JObject Net_PeerCount()
        {
            JObject _json = this.Build("net_peerCount");
            return this.Request(_json);
        }
        #endregion

        #region Eth_ProtocolVersion
        public JObject Eth_ProtocolVersion()
        {
            JObject _json = this.Build("eth_protocolVersion");
            return this.Request(_json);
        }
        #endregion

        #region Eth_Syncing
        public JObject Eth_Syncing()
        {
            JObject _json = this.Build("eth_syncing");
            return this.Request(_json);
        }
        #endregion

        #region Eth_Coinbase
        public JObject Eth_Coinbase()
        {
            JObject _json = this.Build("eth_coinbase");
            return this.Request(_json);
        }
        #endregion

        #region Eth_Mining
        public JObject Eth_Mining()
        {
            JObject _json = this.Build("eth_mining");
            return this.Request(_json);
        }
        #endregion

        #region Eth_Hashrate
        public JObject Eth_Hashrate()
        {
            JObject _json = this.Build("eth_hashrate");
            return this.Request(_json);
        }
        #endregion

        #region Eth_Accounts
        public JObject Eth_Accounts()
        {
            JObject _json = this.Build("eth_accounts");
            return this.Request(_json);
        }
        #endregion

        #region Eth_BlockNumber
        public JObject Eth_BlockNumber()
        {
            JObject _json = this.Build("eth_blockNumber");
            return this.Request(_json);
        }
        #endregion

        #region Eth_GetStorageAt
        public JObject Eth_GetStorageAt(string _address, string _position, string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getStorageAt", _address, _position, _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetTransactionCount
        public JObject Eth_GetTransactionCount(string _address, string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getTransactionCount", _address, _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetBlockTransactionCountByHash
        public JObject Eth_GetBlockTransactionCountByHash(string _hash)
        {
            JObject _json = this.Build("eth_getBlockTransactionCountByHash", _hash);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetBlockTransactionCountByNumber
        public JObject Eth_GetBlockTransactionCountByNumber(string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getBlockTransactionCountByNumber", _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region eth_GetUncleCountByBlockHash
        public JObject eth_GetUncleCountByBlockHash(string _hash)
        {
            JObject _json = this.Build("eth_getUncleCountByBlockHash", _hash);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetUncleCountByBlockNumber
        public JObject Eth_GetUncleCountByBlockNumber(string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getUncleCountByBlockNumber", _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetCode
        public JObject Eth_GetCode(string _address, string _quantity = "latest")
        {
            JObject _json = this.Build("eth_getCode", _address, _quantity);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_Sign
        public JObject Eth_Sign(string _address, string message)
        {
            JObject _json = this.Build("eth_sign", _address, message);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_SendTransaction
        public JObject Eth_SendTransaction(string _from, string _to, ContractABI _abi, string _gas = "", string _gasPrice = "", string _nonce = "")
        {
            JObject _value = new JObject();
            if (_from != "") { _value["from"] = _from; }
            if (_to != "") { _value["to"] = _to; }
            _value["data"] = _abi.ToData();
            if (_gas != "") { _value["gas"] = _gas; }
            if (_gasPrice != "") { _value["gasPrice"] = _gasPrice; }
            if (_nonce != "") { _value["nonce"] = _nonce; }

            JObject _json = this.Build("eth_sendTransaction", _value);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_SendRawTransaction
        public JObject Eth_SendRawTransaction(string _data)
        {
            JObject _json = this.Build("eth_sendRawTransaction", _data);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetBlockByHash
        public JObject Eth_GetBlockByHash(string _hash, bool _returnFullTransaction = true)
        {
            JObject _json = this.Build("eth_getBlockByHash", _hash, _returnFullTransaction);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetBlockByNumber
        public JObject Eth_GetBlockByNumber(string _quantity = "latest", bool _returnFullTransaction = true)
        {
            JObject _json = this.Build("eth_getBlockByNumber", _quantity, _returnFullTransaction);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetTransactionByHash
        public JObject Eth_GetTransactionByHash(string _hash)
        {
            JObject _json = this.Build("eth_getTransactionByHash", _hash);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetTransactionByBlockHashAndIndex
        public JObject Eth_GetTransactionByBlockHashAndIndex(string _hash, string _position = "0x0")
        {
            JObject _json = this.Build("eth_getTransactionByBlockHashAndIndex", _hash, _position);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetTransactionByBlockNumberAndIndex
        public JObject Eth_GetTransactionByBlockNumberAndIndex(string _blockNumber, string _position = "0x0")
        {
            JObject _json = this.Build("eth_getTransactionByBlockNumberAndIndex", _blockNumber, _position);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetTransactionReceipt
        public JObject Eth_GetTransactionReceipt(string _hash)
        {
            JObject _json = this.Build("eth_getTransactionReceipt", _hash);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetUncleByBlockHashAndIndex
        public JObject Eth_GetUncleByBlockHashAndIndex(string _hash, string _position = "0x0")
        {
            JObject _json = this.Build("eth_getUncleByBlockHashAndIndex", _hash, _position);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetUncleByBlockNumberAndIndex
        public JObject Eth_GetUncleByBlockNumberAndIndex(string _quantity = "latest", string _position = "0x0")
        {
            JObject _json = this.Build("eth_getUncleByBlockNumberAndIndex", _quantity, _position);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetCompilers
        public JObject Eth_GetCompilers()
        {
            JObject _json = this.Build("eth_getCompilers");
            return this.Request(_json);
        }
        #endregion

        #region Eth_CompileSolidity
        public JObject Eth_CompileSolidity(string _sourceCode)
        {
            JObject _json = this.Build("eth_compileSolidity", _sourceCode);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_CompileLLL
        public JObject Eth_CompileLLL(string _sourceCode)
        {
            JObject _json = this.Build("eth_compileLLL", _sourceCode);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_CompileSerpent
        public JObject Eth_CompileSerpent(string _sourceCode)
        {
            JObject _json = this.Build("eth_compileSerpent", _sourceCode);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_NewFilter
        public JObject Eth_NewFilter(string _address, string[] _topics, string _fromBlock = "latest", string _toBlock = "latest")
        {
			JObject _value = new JObject();
			_value["fromBlock"] = _fromBlock;
			_value["toBlock"] = _toBlock;    
			if (_address != "") { _value["address"] = _address; }
            _value["topics"] = new JArray(_topics);

            JObject _json = this.Build("eth_newFilter", _value);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_NewBlockFilter
        public JObject Eth_NewBlockFilter()
        {
            JObject _json = this.Build("eth_newBlockFilter");
            return this.Request(_json);
        }
        #endregion

        #region Eth_NewPendingTransactionFilter
        public JObject Eth_NewPendingTransactionFilter()
        {
            JObject _json = this.Build("eth_newPendingTransactionFilter");
            return this.Request(_json);
        }
        #endregion

        #region Eth_UninstallFilter
        public JObject Eth_UninstallFilter(string _filterId)
        {
            JObject _json = this.Build("eth_uninstallFilter", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetFilterChanges
        public JObject Eth_GetFilterChanges(string _filterId)
        {
            JObject _json = this.Build("eth_getFilterChanges", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetFilterLogs
        public JObject Eth_GetFilterLogs(string _filterId)
        {
            JObject _json = this.Build("eth_getFilterLogs", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetLogs
        public JObject Eth_GetLogs(string _address, object[] _topics, string fromBlock = "latest", string toBlock = "latest")
        {
            JObject _json = this.Build("eth_getLogs", _address, _topics, fromBlock, toBlock);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_GetWork
        public JObject Eth_GetWork()
        {
            JObject _json = this.Build("eth_getWork");
            return this.Request(_json);
        }
        #endregion

        #region Eth_SubmitWork
        public JObject Eth_SubmitWork(string _nonceFound, string _powHash, string _mixDigest)
        {
            JObject _json = this.Build("eth_submitWork", _nonceFound, _powHash, _mixDigest);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Eth_SubmitHashrate
        public JObject Eth_SubmitHashrate(string _hashrate, string _id)
        {
            JObject _json = this.Build("eth_submitHashrate", _hashrate, _id);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region DB_PutString
        public JObject DB_PutString(string _databaseName, string _keyName, string _stringToStore)
        {
            JObject _json = this.Build("db_putString", _databaseName, _keyName, _stringToStore);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region DB_GetString
        public JObject DB_GetString(string _databaseName, string _keyName)
        {
            JObject _json = this.Build("db_getString", _databaseName, _keyName);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region DB_PutHex
        public JObject DB_PutHex(string _databaseName, string _keyName, string _dataToStore)
        {
            JObject _json = this.Build("db_putHex", _databaseName, _keyName, _dataToStore);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region DB_GetHex
        public JObject DB_GetHex(string _databaseName, string _keyName)
        {
            JObject _json = this.Build("db_getHex", _databaseName, _keyName);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_Version
        public JObject Shh_Version()
        {
            JObject _json = this.Build("shh_version");
            return this.Request(_json);
        }
        #endregion

        #region Shh_Post
        public JObject Shh_Post(object[] _topics, string _payload, string _priority, string _ttl, string _from = "", string _to = "")
        {
            JObject _value = new JObject();
            if (_from != "") { _value["from"] = _from; }
            if (_to != "") { _value["to"] = _to; }
            _value["topics"] = _topics.ToString();
            _value["payload"] = _payload;
            _value["priority"] = _priority;
            _value["ttl"] = _ttl;

            JObject _json = this.Build("shh_post", _value);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_NewIdentity
        public JObject Shh_NewIdentity()
        {
            JObject _json = this.Build("shh_newIdentity");
            return this.Request(_json);
        }
        #endregion

        #region Shh_HasIdentity
        public JObject Shh_HasIdentity(string _data)
        {
            JObject _json = this.Build("shh_hasIdentity", _data);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_NewGroup
        public JObject Shh_NewGroup()
        {
            JObject _json = this.Build("shh_newGroup");
            return this.Request(_json);
        }
        #endregion

        #region Shh_AddToGroup
        public JObject Shh_AddToGroup(string _data)
        {
            JObject _json = this.Build("shh_addToGroup", _data);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_NewFilter
        public JObject Shh_NewFilter(object[] _topics, string _to = "")
        {
            JObject _value = new JObject();
            _value["topics"] = _topics.ToString();
            if (_to != "") { _value["to"] = _to; }

            JObject _json = this.Build("shh_newFilter", _value);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_UninstallFilter
        public JObject Shh_UninstallFilter(string _filterId)
        {
            JObject _json = this.Build("shh_uninstallFilter", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_GetFilterChanges
        public JObject Shh_GetFilterChanges(string _filterId)
        {
            JObject _json = this.Build("shh_getFilterChanges", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion

        #region Shh_GetMessages
        public JObject Shh_GetMessages(string _filterId)
        {
            JObject _json = this.Build("shh_getMessages", _filterId);
            Console.WriteLine(_json.ToString());

            return this.Request(_json);
        }
        #endregion





    }
}
