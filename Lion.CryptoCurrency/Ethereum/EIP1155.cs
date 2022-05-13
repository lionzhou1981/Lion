using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Ethereum
{
    public static class EIP1155
    {
        public static string Mint(uint _transactionNonce, uint _chainId, Address _addrFrom, string _contractAddress, uint _nftId, uint _amount, string _hexData)
        {
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_MINT);
            _abi.Add(_addrFrom);
            _abi.Add(_nftId);
            _abi.Add(_amount);
            _abi.Add(new Number(_hexData));
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }

        public static string MintBatch(uint _transactionNonce, uint _chainId, Address _addrFrom, string _contractAddress, Array _nftIds, Array _amounts, string _hexData)
        {
            if (_nftIds.Length != _amounts.Length)
                throw new ArgumentException("NFT ids length not equal amounts length");
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_MINTBATCH);
            _abi.Add(_addrFrom);
            _abi.Add(_nftIds);
            _abi.Add(_amounts);
            _abi.Add(new Number(_hexData));
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }


        public static string Burn(uint _transactionNonce, uint _chainId, Address _addrFrom, string _contractAddress, uint _nftId, uint _amount)
        {
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_BURN);
            _abi.Add(_addrFrom);
            _abi.Add(_nftId);
            _abi.Add(_amount);
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }

        public static string BurnBatch(uint _transactionNonce, uint _chainId, Address _addrFrom, string _contractAddress, Array _nftIds, Array _amounts)
        {
            if (_nftIds.Length != _amounts.Length)
                throw new ArgumentException("NFT ids length not equal amounts length");
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_BURNBATCH);
            _abi.Add(_addrFrom);
            _abi.Add(_nftIds);
            _abi.Add(_amounts);
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }


        public static string SafeTransferFrom(uint _transactionNonce,uint _chainId,Address _addrFrom,Address _addrTo,string _contractAddress,uint _id,uint _amount,string _hexData)
        {
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_SAFETRANSFERFROM);
            _abi.Add(_addrFrom);
            _abi.Add(_addrTo);
            _abi.Add(_id);
            _abi.Add(_amount);
            _abi.Add(new Number(_hexData)) ;
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }


        public static string SafeBatchTransferFrom(uint _transactionNonce, uint _chainId,Address _addrFrom, Address _addrTo, string _contractAddress, Array _ids, Array _amounts, string _hexData)
        {
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_SAFEBATCHTRANSFERFROM);
            _abi.Add(_addrFrom);
            _abi.Add(_addrTo);
            _abi.Add(_ids);
            _abi.Add(_amounts);
            _abi.Add(new Number(_hexData));
            return BuildSendDataTransaction(_transactionNonce, _chainId, _addrFrom.Private, _contractAddress, _abi.ToData());
        }

        /// <summary>
        /// return balance of NFT in batch address
        /// execute the return value with eth_call
        /// </summary>
        /// <param name="_transactionNonce"></param>
        /// <param name="_chainId"></param>
        /// <param name="_addr"></param>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static string BalanceOf(Address _addr,uint _id)
        {
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_BALANCEOF);
            _abi.Add(_addr);
            _abi.Add(_id);
            return _abi.ToString();
        }

        /// <summary>
        /// return balance of NFT in address
        /// addrs.length=_ids.length
        /// execute the return value with eth_call
        /// </summary>
        /// <param name="_transactionNonce"></param>
        /// <param name="_chainId"></param>
        /// <param name="_addr"></param>
        /// <param name="_id"></param>
        /// <returns></returns>
        public static string BalanceOfBatch(Array _addrs, Array _ids)
        {
            if (_addrs.Length != _ids.Length)
                throw new ArgumentException("Address length not equal ids length");
            ContractABI _abi = new ContractABI(Ethereum.EIP1155_METHOD_BALANCEOFBATCH);
            _abi.Add(_addrs);
            _abi.Add(_ids);
            return _abi.ToString();
        }

        private static string BuildSendDataTransaction(uint _transactionNonce, uint _chainId, string _senderPrivate, string _contractAddress, string _signedData)
        {
            Transaction _transaction = new Transaction();
            _transaction.Nonce = _transactionNonce;
            _transaction.Address = _contractAddress;
            _transaction.ChainId = _chainId;
            _transaction.Value = new Number(0M);
            _transaction.DataHex = _signedData;
            return _transaction.ToSignedHex(_senderPrivate);
        }
    }
}
