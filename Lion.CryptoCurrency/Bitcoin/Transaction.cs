using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Transaction
    {
        public IList<TransactionVout> Vouts = new List<TransactionVout>();
        public IList<TransactionVin> Vins = new List<TransactionVin>();

        #region EstimateFee
        public decimal EstimateFee(decimal _estimatedFee = 0.00001M, bool _leftBack = false)
        {
            decimal _totalBytesLength = this.Vouts.Count * 180 + 34 * (this.Vins.Count + (_leftBack ? 1 : 0)) + 10;
            return _totalBytesLength * _estimatedFee / 1000M;
        }
        #endregion

        #region ToSignedHex
        public string ToSignedHex(decimal _maxFee = 0.0001M)
        {
            if (this.Vouts.Count <= 0) { throw new Exception("Vout is empty."); }
            if (this.Vins.Count <= 0) { throw new Exception("Vin is empty."); }

            decimal _voutAmount = this.Vouts.Sum(t => t.Amount);
            decimal _vinAmount = this.Vins.Sum(t => t.Amount);

            if (_vinAmount <= 0M) { throw new Exception("Vin amount is zero."); }
            if (_voutAmount <= 0M) { throw new Exception("Vout amount is zero."); }
            if (_vinAmount >= _voutAmount) { throw new Exception("Vout amount less than Vin amount."); }
            if (_voutAmount - _vinAmount > _maxFee) { throw new Exception("Fee is too much."); }

            byte[] _voutCount = BigInteger.Parse(this.Vouts.Count.ToString()).ToByteArray(true,false);

            //base script: version/input count
            List<byte> _voutHead = new List<byte>();
            _voutHead.AddAndPadRight(5, 0x0, 0x02); //version;
            _voutHead.Add(0x01);
            _voutHead.AddRange(_voutCount);

            //start from output,not contains sign,not contains hash type
            List<byte> _vinUnsigned = new List<byte>();
            foreach (TransactionVin _vin in this.Vins)
            {
                _vinUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vin.Amount).ToString("0")).ToByteArray());
                _vinUnsigned.AddRange(HexPlus.HexStringToByteArray(Address.Address2PKSH(_vin.Address)));
            }

            //base script sig = base+input+output+hashtype
            //pay script sig = ecdsa(base script sig)
            byte[] _seq = new byte[] { 0xff, 0xff, 0xff, 0xff };
            byte[] _seqHash = SHA.EncodeSHA256(SHA.EncodeSHA256(_seq));
            byte[] _vinHash = SHA.EncodeSHA256(SHA.EncodeSHA256(_vinUnsigned.ToArray()));
            List<byte> _preVouts = new List<byte>();
            List<byte> _seqs = new List<byte>();
            byte[] vinCount = BigInteger.Parse(this.Vins.Count.ToString()).ToByteArray(true,false);

            foreach (TransactionVout _vout in this.Vouts)
            {
                _preVouts.AddRange(_vout.Scripts);
                _seqs.AddRange(_seq);
            }
            byte[] _preVoutHash = SHA.EncodeSHA256(SHA.EncodeSHA256(_preVouts.ToArray()));
            byte[] _seqHashs = SHA.EncodeSHA256(SHA.EncodeSHA256(_seqs.ToArray()));

            foreach (TransactionVout _vout in this.Vouts)
            {
                List<byte> _voutUnsigned = new List<byte>();
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x02);

                if (_vout.IsWitness)
                {
                    //witness transaction join HASH_ALL(hash_outputs/hash_seqs/hash_inputs) to transaction scripts
                    _voutUnsigned.AddRange(_preVoutHash);
                    _voutUnsigned.AddRange(_seqHashs);
                    _voutUnsigned.AddRange(_vout.Scripts);
                    _voutUnsigned.AddRange(HexPlus.HexStringToByteArray(_vout.ScriptPKSH));
                    _voutUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vout.Amount).ToString("0")).ToByteArray());
                    _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                    _voutUnsigned.AddRange(_vinHash);
                }
                else
                {
                    //legacy transaction HASH_ALL(transaction scripts)
                    _voutUnsigned.AddRange(_voutCount);
                    foreach (TransactionVout _childVout in this.Vouts)
                    {
                        _voutUnsigned.AddRange(_childVout.Scripts);

                        //each inputs script in scripts,not current input skip PKSH,replace with 0x00
                        if (_childVout.TxId != _vout.TxId || _childVout.TxIndex != _vout.TxIndex) 
                        {
                            _voutUnsigned.Add(0x00);
                        }
                        
                        else
                        {
                            _voutUnsigned.AddRange(HexPlus.HexStringToByteArray(_childVout.ScriptPKSH));
                        }

                        _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                    }

                    _voutUnsigned.AddRange(vinCount);
                    _voutUnsigned.AddRange(_vinUnsigned);
                }
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x00);
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x01); //hash type=HASH_ALL;
                
                string _scriptSig = BitConverter.ToString(SHA.EncodeSHA256(SHA.EncodeSHA256(_voutUnsigned.ToArray()))).Replace("-", "").ToLower();
                _vout.ScriptSign = HexPlus.HexStringToByteArray(Signature.SignHex(_scriptSig, _vout.Private.Wif));
            }

            _vinUnsigned.InsertRange(0, vinCount); //transaction seq
            
            //pay bytes
            List<byte> _signedRaw = new List<byte>();
            _signedRaw.AddRange(_voutHead);
            foreach (TransactionVout _vout in this.Vouts)
            {
                _signedRaw.AddRange(_vout.Scripts); //script per input             
                if (!_vout.IsWitness)
                {
                    byte[] _publicKeys = HexPlus.HexStringToByteArray(_vout.Private.Public);
                    BigInteger _sigLength = _vout.ScriptSign.Length + (BigInteger)_publicKeys.Length;
                    _signedRaw.AddRange(_sigLength.ToByteArray(true,false));
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(_publicKeys);
                }
                else
                {
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.ScriptP2SH));
                }
                _signedRaw.AddRange(_seq);
            }
            _signedRaw.AddRange(_vinUnsigned);

            foreach (TransactionVout _vout in this.Vouts)
            {
                if (!_vout.IsWitness)
                {
                    _signedRaw.Add(0x00);
                }
                else
                {
                    BigInteger _sigLength = BigInteger.Parse(_vout.Private.Public, NumberStyles.HexNumber).ToByteArray().Length + _vout.ScriptSign.Length + 1;
                    _signedRaw.Add(0x02);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.Private.Public));
                }
            }
            _signedRaw.AddAndPadRight(4, 0x0, 0x00);

            return HexPlus.ByteArrayToHexString(_signedRaw.ToArray());
        }
        #endregion
    }

    #region TransactionVout
    public class TransactionVout
    {
        public string TxId;
        public int TxIndex;
        public decimal Amount;
        public List<Private> Privates = new List<Private>();
        public List<string> PublicKeys = new List<string>();
        public int PrivateKeyRequired;

        public Private Private
        {
            get
            {
                return Privates[0];
            }
        }

        public List<byte> Scripts
        {
            get
            {
                List<byte> _scripts = new List<byte>();
                _scripts.AddRange(HexPlus.HexStringToByteArray(TxId).Reverse().ToArray());
                _scripts.AddAndPadRight(4, 0x0, ((BigInteger)this.TxIndex).ToByteArray(true, false));
                return _scripts;
            }
        }

        public byte[] ScriptSign;

        public string ScriptPKSH
        {
            get
            {
                return Privates.Count > 1 ? Address.Publics2PublicScript(PublicKeys.ToArray(), PrivateKeyRequired) : Address.Public2PKSH(HexPlus.ByteArrayToHexString(new RIPEMD160Managed().ComputeHash(SHA.EncodeSHA256(HexPlus.HexStringToByteArray(this.Private.Public)))));
            }
        }

        public string ScriptP2SH
        {
            get
            {
                return Privates.Count > 1 ? Address.Publics2PublicScript(PublicKeys.ToArray(), PrivateKeyRequired) : Address.Public2P2SH(this.Private.Public);
            }
        }

        public bool IsWitness
        {
            get
            {
                return this.Private.Address.StartsWith("3") || this.Private.Address.StartsWith("2");
            }
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, Private _private)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Privates = new List<Private>() { _private };
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, Private[] _privates, string[] _publicKeys, int _privateRequireCount)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Privates = _privates.Distinct().ToList();
            this.PublicKeys = _publicKeys.Distinct().ToList();
            this.PrivateKeyRequired = _privateRequireCount;
            if (_privateRequireCount > this.PublicKeys.Count || _privateRequireCount > this.Privates.Count) { throw new Exception("Keys required error"); }
        }
    }
    #endregion

    #region TransactionVin
    public class TransactionVin
    {
        public string Address;
        public decimal Amount;

        public TransactionVin(string _address, decimal _amount)
        {
            this.Address = _address;
            this.Amount = _amount;
        }
    }
    #endregion
}
