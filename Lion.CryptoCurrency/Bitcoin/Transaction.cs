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
            Lion.CryptoCurrency.Bitcoin.TransactionVout _vout = new Lion.CryptoCurrency.Bitcoin.TransactionVout("30bd4ebeb721558adf5665708b080be225a35e874247b72d1eb3f556f9c5d4a9", 0, 0.0076774M, new WifPrivateKey("L1NQUnWSftP4NqCgVYQe1G5jPRvFdL5agkHWigHznk6XHJKeYvRV"));
            Lion.CryptoCurrency.Bitcoin.Transaction _transaction = new Lion.CryptoCurrency.Bitcoin.Transaction();
            _transaction.Vouts.Add(_vout);
            _transaction.Vins.Add(new Lion.CryptoCurrency.Bitcoin.TransactionVin("3Lj5gR83W6vp1bJYkfDHrzyw49EyXYXCyL", 0.0075774M));
            Console.WriteLine("Single:"+_transaction.ToSignedHex());
            /////Multi
            //Lion.CryptoCurrency.Bitcoin.TransactionVout _voutMulti = new Lion.CryptoCurrency.Bitcoin.TransactionVout("3d2c8bc178d33e8be1e2a968d71ea99cf78347b42d1320c9aba04058f9793e1e", 0, 0.00008M, 
            //    new string[] { "93JVVDgTPmfSdrA844ZycQyo88V9NzujdRkaHsbyBv19m9afGc6", "92cEYV4dqDVAsmHyugsXneVAx6zEmzw7H5XJk2y4zog667KkiC5" }, 
            //    new string[] {
            //    Lion.CryptoCurrency.Bitcoin.Address.Private2Public("93JVVDgTPmfSdrA844ZycQyo88V9NzujdRkaHsbyBv19m9afGc6",true,true),
            //    Lion.CryptoCurrency.Bitcoin.Address.Private2Public("91wianQnsbCCGxfQuc9GWc4WUYeJ5bVjeZAUj64V4ToWfsy8Q15",true,true),
            //    Lion.CryptoCurrency.Bitcoin.Address.Private2Public("92cEYV4dqDVAsmHyugsXneVAx6zEmzw7H5XJk2y4zog667KkiC5",true,true) }, 2);

            //Lion.CryptoCurrency.Bitcoin.Transaction _transactionMulti = new Lion.CryptoCurrency.Bitcoin.Transaction();
            //_transactionMulti.Vouts.Add(_voutMulti);
            //_transactionMulti.Vins.Add(new Lion.CryptoCurrency.Bitcoin.TransactionVin("miy64GVdz6ZQaUKCVmhpjVLqKsUqMgyP6y", 0.00007M));
            //Console.WriteLine("Multi:"+_transactionMulti.ToSignedHex());
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
            //if (_voutAmount - _vinAmount > _maxFee) { throw new Exception("Fee is too much."); }

            //base script: version/input count
            List<byte> _voutHead = new List<byte>();
            _voutHead.AddAndPadRight(4, 0x0, 0x01); //version;
            _voutHead.AddRange(BigInteger.Parse(this.Vouts.Count.ToString()).ToByteArray().Reverse().ToArray());

            //start from output,not contains sign,not contains hash type
            List<byte> _vinUnsigned = new List<byte>();
            _vinUnsigned.AddRange(BigInteger.Parse(this.Vins.Count.ToString()).ToByteArray().Reverse().ToArray()); //transaction seq
            foreach (TransactionVin _vin in this.Vins)
            {
                _vinUnsigned.AddAndPadRight(8, 0x0, BigInteger.Parse((100000000M * _vin.Amount).ToString("0")).ToByteArray());
                _vinUnsigned.AddRange(HexPlus.HexStringToByteArray(Address.Address2PKSH(_vin.Address)));
            }
            _vinUnsigned.AddAndPadRight(4, 0x0, 0x00);

            //base script sig = base+input+output+hashtype
            //pay script sig = ecdsa(base script sig)
            foreach (TransactionVout _vout in this.Vouts)
            {
                List<byte> _voutUnsigned = new List<byte>();
                _voutUnsigned.AddRange(_voutHead);

                foreach (TransactionVout _out in this.Vouts)
                {
                    //script per input
                    _voutUnsigned.AddRange(_out.Scripts);
                    var _pkshArray = HexPlus.HexStringToByteArray(_out.ScriptPKSH);
                    if (_out.PublicKeys.Count > 1)
                    {
                        BigInteger _pkshArrayLength = _pkshArray.Length;
                        _voutUnsigned.AddRange(_pkshArrayLength.ToByteArray());
                    }
                    _voutUnsigned.AddRange(_out.TxId == _vout.TxId && _out.TxIndex == _vout.TxIndex ? _pkshArray : new byte[] { 0x00 });
                    _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                }
                _voutUnsigned.AddRange(_vinUnsigned);
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x01); //hash type;
                string _scriptSig = BitConverter.ToString(new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_voutUnsigned.ToArray()))).Replace("-", "").ToLower();
                
                List<byte> _allBytes = new List<byte>();
                foreach (var _privateKey in _vout.Privates)
                {
                    //ECDSA
                    BigInteger _k = Lion.BigNumberPlus.HexToBigInt(RandomPlus.RandomHex());
                    Encrypt.ECPoint _gk = Secp256k1.G.Multiply(_k);
                    BigInteger _r = _gk.X;
                    BigInteger _e = Lion.BigNumberPlus.HexToBigInt(_scriptSig);
                    BigInteger _d = Lion.BigNumberPlus.HexToBigInt(_privateKey.PrivateKey);
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
                    if (_vout.Privates.Count == 1)
                    {
                        _subBytes.Add((byte)(_vout.Private.Compressed ? 0x21 : 0x41));
                        _subBytes.InsertRange(0, ((BigInteger)(_subBytes.Count - 1)).ToByteArray());
                    }
                    else
                    {
                        _subBytes.InsertRange(0, ((BigInteger)(_subBytes.Count)).ToByteArray());
                    }
                    _allBytes.AddRange(_subBytes);
                }

                if (_vout.Privates.Count > 1)
                    _allBytes.Add(0x4c);
                _vout.ScriptSign = _allBytes;
            }

            //pay bytes
            List<byte> _signedRaw = new List<byte>();
            _signedRaw.AddRange(_voutHead);
            foreach (TransactionVout _vout in this.Vouts)
            {
                _signedRaw.AddRange(_vout.Scripts);//script per input
                if (_vout.PublicKeys.Count <= 1)
                {
                    BigInteger _sigLength = BigInteger.Parse(_vout.Private.PublicKey, NumberStyles.HexNumber).ToByteArray().Length + _vout.ScriptSign.Count;//;+1;
                    _signedRaw.AddRange(_sigLength.ToByteArray().ToArray());
                    //_signedRaw.Add(0x00);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.Private.PublicKey));
                }
                else
                {
                    _signedRaw.Add(0xfd);
                    var _scriptPubKeyArray = Lion.HexPlus.HexStringToByteArray(Address.Publics2ScriptPubKey(_vout.PrivateKeyRequired, _vout.PublicKeys));
                    BigInteger _scriptPubKeyArrayLength = _scriptPubKeyArray.Length;
                    BigInteger _sigLength = _scriptPubKeyArrayLength + _vout.ScriptSign.Count + _scriptPubKeyArrayLength.ToByteArray().Length+1;
                    _signedRaw.AddRange(_sigLength.ToByteArray().ToArray());
                    _signedRaw.Add(0x00);
                    _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                    _signedRaw.AddRange(_scriptPubKeyArrayLength.ToByteArray());
                    _signedRaw.AddRange(_scriptPubKeyArray);
                }
                _signedRaw.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });//transaction seq
            }
            _signedRaw.AddRange(_vinUnsigned);

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
                return Privates.Count > 1 ? Address.Publics2ScriptPubKey(PrivateKeyRequired, PublicKeys) : Address.Public2PKSH(Lion.HexPlus.ByteArrayToHexString(new RIPEMD160Managed().ComputeHash(new SHA256Managed().ComputeHash(HexPlus.HexStringToByteArray(this.Private.PublicKey)))));
            }
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, IPrivateKey _private)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Privates = new List<IPrivateKey>() { _private };
        }

        public TransactionVout(string _txid, int _txIndex, decimal _amount, IPrivateKey[] _privates,string[] _publicKeys,int _privateRequireCount)
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
