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
        public static void Test()
        {
            Lion.CryptoCurrency.Bitcoin.TransactionVout _voutMulti1 = new Lion.CryptoCurrency.Bitcoin.TransactionVout(Lion.HexPlus.ByteArrayToHexString(Lion.HexPlus.HexStringToByteArray("f3a3b596f33c7db267f8e2cae3c091c1b5f9b5610acba4e7a50ec8bafd1f43b6").Reverse().ToArray()), 0, 0.0006M,
new WifPrivateKey("32rBPMC58TzvuPCFZShfxbUdh6QxZA9wgS", ""));
            Lion.CryptoCurrency.Bitcoin.Transaction _transactionMulti = new Lion.CryptoCurrency.Bitcoin.Transaction();
            _transactionMulti.Vouts.Add(_voutMulti1);
            var _v = _transactionMulti.Vouts.Sum(t => t.Amount) - 0.00001M;
            _transactionMulti.Vins.Add(new Lion.CryptoCurrency.Bitcoin.TransactionVin("3Lj5gR83W6vp1bJYkfDHrzyw49EyXYXCyL", _v));
            Console.WriteLine("Signed complete:" + _transactionMulti.ToSignedHex());
        }

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

            var _txOutCount = BigInteger.Parse(this.Vouts.Count.ToString()).ToByteArray();
            //base script: version/input count
            List<byte> _voutHead = new List<byte>();
            _voutHead.AddAndPadRight(5, 0x0, 0x02); //version;
            _voutHead.Add(0x01);
            _voutHead.AddRange(_txOutCount);
            //start from output,not contains sign,not contains hash type
            List<byte> _vinUnsigned = new List<byte>();
            foreach (TransactionVin _vin in this.Vins)
            {
                _vinUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vin.Amount).ToString("0")).ToByteArray());
                _vinUnsigned.AddRange(HexPlus.HexStringToByteArray(Address.Address2PKSH(_vin.Address)));
            }
            //base script sig = base+input+output+hashtype
            //pay script sig = ecdsa(base script sig)
            var _sha = new SHA256Managed();
            var _seq = new byte[] { 0xff, 0xff, 0xff, 0xff };
            var _seqHash = new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_seq));
            var _vinHash = _sha.ComputeHash(_sha.ComputeHash(_vinUnsigned.ToArray()));
            var _preVouts = new List<byte>();
            var _seqs = new List<byte>();
            var _txInCount = BigInteger.Parse(this.Vins.Count.ToString()).ToByteArray().Reverse().ToArray();
            foreach (TransactionVout _vout in this.Vouts)
            {
                _preVouts.AddRange(_vout.Scripts);
                _seqs.AddRange(_seq);
            }
            var _preVoutHash = _sha.ComputeHash(_sha.ComputeHash(_preVouts.ToArray()));
            var _seqHashs = _sha.ComputeHash(_sha.ComputeHash(_seqs.ToArray()));

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
                    _voutUnsigned.AddRange(_txOutCount);
                    foreach (TransactionVout _childVout in this.Vouts)
                    {
                        _voutUnsigned.AddRange(_childVout.Scripts);
                        if (_childVout.TxId != _vout.TxId || _childVout.TxIndex != _vout.TxIndex) //each inputs script in scripts,not current input skip PKSH,replace with 0x00
                            _voutUnsigned.Add(0x00);
                        else
                        {
                            _voutUnsigned.AddRange(HexPlus.HexStringToByteArray(_childVout.ScriptPKSH));
                        }
                        _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                    }
                    _voutUnsigned.AddRange(_txInCount);
                    _voutUnsigned.AddRange(_vinUnsigned);
                }
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x00);
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x01); //hash type=HASH_ALL;
                string _scriptSig = BitConverter.ToString(_sha.ComputeHash(_sha.ComputeHash(_voutUnsigned.ToArray()))).Replace("-", "").ToLower();
                //ECDSA
                BigInteger _k = Lion.BigNumberPlus.HexToBigInt(RandomPlus.RandomHex());
                Encrypt.ECPoint _gk = Secp256k1.G.Multiply(_k);
                BigInteger _r = _gk.X;
                BigInteger _e = Lion.BigNumberPlus.HexToBigInt(_scriptSig);
                BigInteger _d = Lion.BigNumberPlus.HexToBigInt(_vout.Private.PrivateKey);
                BigInteger _s = ((_r * _d + _e) * _k.ModInverse(Secp256k1.N)) % Secp256k1.N;

                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }

                List<byte> _rbytes = _r.ToByteArray().Reverse().ToList();
                List<byte> _sbytes = _s.ToByteArray().Reverse().ToList();
                List<byte> _subBytes = new List<byte>();
                BigInteger _rsLength = _rbytes.Count() + _sbytes.Count() + 4;
                _subBytes.Add(0x30);
                _subBytes.AddRange(_rsLength.ToByteArray());
                _subBytes.Add(0x02);
                _subBytes.AddRange(((BigInteger)_rbytes.Count()).ToByteArray());
                _subBytes.AddRange(_rbytes.ToArray());
                _subBytes.Add(0x02);
                _subBytes.AddRange(((BigInteger)_sbytes.Count()).ToByteArray());
                _subBytes.AddRange(_sbytes.ToArray());
                _subBytes.Add(0x01);
                BigInteger _publicBytesLength = Lion.BigNumberPlus.HexToBigInt(_vout.Private.PublicKey).ToByteArray().Length;
                _subBytes.AddRange(_publicBytesLength.ToByteArray());
                _subBytes.InsertRange(0, ((BigInteger)(_subBytes.Count - 1)).ToByteArray());
                _vout.ScriptSign = _subBytes;
            }

            _vinUnsigned.InsertRange(0, _txInCount); //transaction seq
            //pay bytes
            List<byte> _signedRaw = new List<byte>();
            _signedRaw.AddRange(_voutHead);
            foreach (TransactionVout _vout in this.Vouts)
            {
                _signedRaw.AddRange(_vout.Scripts);//script per input             
                if (!_vout.IsWitness)
                {
                    var _publicKeys = Lion.HexPlus.HexStringToByteArray(_vout.Private.PublicKey);
                    BigInteger _sigLength = _vout.ScriptSign.Count + (BigInteger)_publicKeys.Length;
                    _signedRaw.AddRange(_sigLength.ToByteArray().Where(t => t != 0x00).ToArray());
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(_publicKeys);
                }
                else
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.ScriptP2SH));
                _signedRaw.AddRange(_seq);
            }
            _signedRaw.AddRange(_vinUnsigned);
            foreach (TransactionVout _vout in this.Vouts)
            {
                if (!_vout.IsWitness)
                    _signedRaw.Add(0x00);
                else
                {
                    BigInteger _sigLength = BigInteger.Parse(_vout.Private.PublicKey, NumberStyles.HexNumber).ToByteArray().Length + _vout.ScriptSign.Count + 1;
                    _signedRaw.Add(0x02);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.Private.PublicKey));
                }
            }
            _signedRaw.AddAndPadRight(4, 0x0, 0x00);


            return HexPlus.ByteArrayToHexString(_signedRaw.ToArray());
        }
        #endregion

        #region ToMultiSignedHex
        public string ToMultiSignedHex(decimal _maxFee = 0.0001M)
        {
            throw new Exception("Not test end method,wait");
            //untest complete
            if (this.Vouts.Count <= 0) { throw new Exception("Vout is empty."); }
            if (this.Vins.Count <= 0) { throw new Exception("Vin is empty."); }

            decimal _voutAmount = this.Vouts.Sum(t => t.Amount);
            decimal _vinAmount = this.Vins.Sum(t => t.Amount);

            if (_vinAmount <= 0M) { throw new Exception("Vin amount is zero."); }
            if (_voutAmount <= 0M) { throw new Exception("Vout amount is zero."); }
            if (_vinAmount >= _voutAmount) { throw new Exception("Vout amount less than Vin amount."); }
            if (_voutAmount - _vinAmount > _maxFee) { throw new Exception("Fee is too much."); }

            var _txOutCount = BigInteger.Parse(this.Vouts.Count.ToString()).ToByteArray();
            //base script: version/input count
            List<byte> _voutHead = new List<byte>();
            _voutHead.AddAndPadRight(5, 0x0, 0x02); //version;
            _voutHead.Add(0x01);
            _voutHead.AddRange(_txOutCount);
            //start from output,not contains sign,not contains hash type
            List<byte> _vinUnsigned = new List<byte>();
            foreach (TransactionVin _vin in this.Vins)
            {
                _vinUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vin.Amount).ToString("0")).ToByteArray());
                _vinUnsigned.AddRange(HexPlus.HexStringToByteArray(Address.Address2PKSH(_vin.Address)));
            }
            //base script sig = base+input+output+hashtype
            //pay script sig = ecdsa(base script sig)
            var _sha = new SHA256Managed();
            var _seq = new byte[] { 0xff, 0xff, 0xff, 0xff };
            var _seqHash = new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_seq));
            var _vinHash = _sha.ComputeHash(_sha.ComputeHash(_vinUnsigned.ToArray()));
            var _preVouts = new List<byte>();
            var _seqs = new List<byte>();
            var _txInCount = BigInteger.Parse(this.Vins.Count.ToString()).ToByteArray().Reverse().ToArray();
            foreach (TransactionVout _vout in this.Vouts)
            {
                _preVouts.AddRange(_vout.Scripts);
                _seqs.AddRange(_seq);
            }
            var _preVoutHash = _sha.ComputeHash(_sha.ComputeHash(_preVouts.ToArray()));
            var _seqHashs = _sha.ComputeHash(_sha.ComputeHash(_seqs.ToArray()));

            foreach (TransactionVout _vout in this.Vouts)
            {
                List<byte> _voutUnsigned = new List<byte>();
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x02);
                var _pkshArray = HexPlus.HexStringToByteArray(_vout.ScriptPKSH);
                if (_vout.IsWitness && _vout.PublicKeys.Count <= 1)
                {
                    //witness transaction join HASH_ALL(hash_outputs/hash_seqs/hash_inputs) to transaction scripts
                    _voutUnsigned.AddRange(_preVoutHash);
                    _voutUnsigned.AddRange(_seqHashs);
                    _voutUnsigned.AddRange(_vout.Scripts);
                    _voutUnsigned.AddRange(_pkshArray);
                    _voutUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vout.Amount).ToString("0")).ToByteArray());
                    _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                    _voutUnsigned.AddRange(_vinHash);
                }
                else
                {
                    //legacy transaction HASH_ALL(transaction scripts)
                    _voutUnsigned.AddRange(_txOutCount);
                    foreach (TransactionVout _childVout in this.Vouts)
                    {
                        _voutUnsigned.AddRange(_childVout.Scripts);
                        if (_childVout.TxId != _vout.TxId || _childVout.TxIndex != _vout.TxIndex) //each inputs script in scripts,not current input skip PKSH,replace with 0x00
                            _voutUnsigned.Add(0x00);
                        else
                        {
                            if (_vout.PublicKeys.Count > 1)
                                _voutUnsigned.AddRange(HexPlus.HexStringToByteArray(Address.Public2PKSH(Address.Address2Public(_vout.Private.Address), true)));
                            else
                                _voutUnsigned.AddRange(HexPlus.HexStringToByteArray(_childVout.ScriptPKSH));
                        }
                        _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                    }
                    _voutUnsigned.AddRange(_txInCount);
                    _voutUnsigned.AddRange(_vinUnsigned);
                }
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x00);
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x01); //hash type=HASH_ALL;
                Console.WriteLine("Before sign:" + BitConverter.ToString(_voutUnsigned.ToArray()).Replace("-", "").ToLower());
                string _scriptSig = BitConverter.ToString(_sha.ComputeHash(_sha.ComputeHash(_voutUnsigned.ToArray()))).Replace("-", "").ToLower();
                Console.WriteLine("Sig:" + _scriptSig);
                BigInteger _e = Lion.BigNumberPlus.HexToBigInt(_scriptSig);

                List<byte> _allBytes = new List<byte>();
                foreach (var _privateKey in _vout.Privates)
                {
                    while (true)
                    {
                        //ECDSA
                        BigInteger _k = Lion.BigNumberPlus.HexToBigInt(RandomPlus.RandomHex());
                        Encrypt.ECPoint _gk = Secp256k1.G.Multiply(_k);
                        BigInteger _r = _gk.X;
                        BigInteger _d = Lion.BigNumberPlus.HexToBigInt(_privateKey.PrivateKey);
                        BigInteger _s = ((_r * _d + _e) * _k.ModInverse(Secp256k1.N)) % Secp256k1.N;

                        if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }

                        List<byte> _rbytes = _r.ToByteArray().Reverse().ToList();
                        List<byte> _sbytes = _s.ToByteArray().Reverse().ToList();

                        if (_rbytes.Count() == _sbytes.Count())
                            continue;

                        List<byte> _subBytes = new List<byte>();
                        BigInteger _rsLength = _rbytes.Count() + _sbytes.Count() + 4;
                        _subBytes.Add(0x30);
                        _subBytes.AddRange(_rsLength.ToByteArray());
                        _subBytes.Add(0x02);
                        _subBytes.AddRange(((BigInteger)_rbytes.Count()).ToByteArray());
                        _subBytes.AddRange(_rbytes.ToArray());
                        _subBytes.Add(0x02);
                        _subBytes.AddRange(((BigInteger)_sbytes.Count()).ToByteArray());
                        _subBytes.AddRange(_sbytes.ToArray());
                        Console.WriteLine("Signed:" + Lion.HexPlus.ByteArrayToHexString(_subBytes.ToArray()));
                        if (_vout.PublicKeys.Count <= 1)
                        {
                            _subBytes.Add(0x01);
                            BigInteger _publicBytesLength = Lion.BigNumberPlus.HexToBigInt(_privateKey.PublicKey).ToByteArray().Length;
                            _subBytes.AddRange(_publicBytesLength.ToByteArray());
                            _subBytes.InsertRange(0, ((BigInteger)(_subBytes.Count - 1)).ToByteArray());
                        }
                        else
                        {
                            _subBytes.Add(0x01);
                            BigInteger _publicBytesLength = Lion.BigNumberPlus.HexToBigInt(_privateKey.PublicKey).ToByteArray().Length;
                            //_subBytes.AddRange(_publicBytesLength.ToByteArray());
                            _subBytes.InsertRange(0, ((BigInteger)(_subBytes.Count)).ToByteArray());
                        }
                        _allBytes.AddRange(_subBytes);
                        break;
                    }
                }
                if (_vout.PublicKeys.Count > 1)
                    _allBytes.Add(0x4c);
                _vout.ScriptSign = _allBytes;
            }

            _vinUnsigned.InsertRange(0, _txInCount); //transaction seq
            //pay bytes
            List<byte> _signedRaw = new List<byte>();
            _signedRaw.AddRange(_voutHead);
            foreach (TransactionVout _vout in this.Vouts)
            {
                _signedRaw.AddRange(_vout.Scripts);//script per input             
                if (_vout.PublicKeys.Count > 1)
                {
                    _signedRaw.Add(0xfd);
                    var _scriptPubKeyArray = Lion.HexPlus.HexStringToByteArray(Address.Publics2PublicScript(_vout.PublicKeys.ToArray(), _vout.PrivateKeyRequired));
                    BigInteger _scriptPubKeyArrayLength = _scriptPubKeyArray.Length;
                    BigInteger _sigLength = _scriptPubKeyArrayLength + _vout.ScriptSign.Count + _scriptPubKeyArrayLength.ToByteArray().Length + 1;
                    _signedRaw.AddRange(_sigLength.ToByteArray().Where(t => t != 0x0).ToArray());
                    _signedRaw.Add(0x00);
                    _signedRaw.Add(0x00);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(_scriptPubKeyArrayLength.ToByteArray());
                    _signedRaw.AddRange(_scriptPubKeyArray);
                }
                else if (!_vout.IsWitness)
                {
                    var _publicKeys = Lion.HexPlus.HexStringToByteArray(_vout.Private.PublicKey);
                    BigInteger _sigLength = _vout.ScriptSign.Count + (BigInteger)_publicKeys.Length;
                    _signedRaw.AddRange(_sigLength.ToByteArray().Where(t => t != 0x00).ToArray());
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(_publicKeys);
                }
                else
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.ScriptP2SH));
                _signedRaw.AddRange(_seq);
            }
            _signedRaw.AddRange(_vinUnsigned);
            foreach (TransactionVout _vout in this.Vouts)
            {
                if (!_vout.IsWitness || (_vout.IsWitness && _vout.PublicKeys.Count > 1))
                    _signedRaw.Add(0x00);
                else if (_vout.IsWitness)
                {
                    BigInteger _sigLength = BigInteger.Parse(_vout.Private.PublicKey, NumberStyles.HexNumber).ToByteArray().Length + _vout.ScriptSign.Count + 1;
                    _signedRaw.Add(0x02);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.Private.PublicKey));
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
        public List<IPrivateKey> Privates = new List<IPrivateKey>();
        public List<string> PublicKeys = new List<string>();
        public int PrivateKeyRequired;
        public IPrivateKey Private
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
                _scripts.AddAndPadRight(4, 0x0, BigInteger.Parse(this.TxIndex.ToString()).ToByteArray().Reverse().ToArray());
                return _scripts;
            }
        }

        public List<byte> ScriptSign;

        public string ScriptPKSH
        {
            get
            {
                return Privates.Count > 1 ? Address.Publics2PublicScript(PublicKeys.ToArray(), PrivateKeyRequired) : Address.Public2PKSH(Lion.HexPlus.ByteArrayToHexString(new RIPEMD160Managed().ComputeHash(new SHA256Managed().ComputeHash(HexPlus.HexStringToByteArray(this.Private.PublicKey)))));
            }
        }

        public string ScriptP2SH
        {
            get
            {
                return Privates.Count > 1 ? Address.Publics2PublicScript(PublicKeys.ToArray(), PrivateKeyRequired) : Address.Public2P2SH(this.Private.PublicKey);
            }
        }

        public bool IsWitness
        {
            get
            {
                return this.Private.Address.StartsWith("3") || this.Private.Address.StartsWith("2");
            }
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, IPrivateKey _private)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Privates = new List<IPrivateKey>() { _private };
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, IPrivateKey[] _privates, string[] _publicKeys, int _privateRequireCount)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Privates = _privates.Distinct().ToList();
            this.PublicKeys = _publicKeys.Distinct().ToList();
            this.PrivateKeyRequired = _privateRequireCount;
            if (_privateRequireCount > this.PublicKeys.Count || _privateRequireCount > this.Privates.Count)
                throw new Exception("Keys required error");
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
