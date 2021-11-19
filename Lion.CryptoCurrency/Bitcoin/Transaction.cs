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
                    _voutUnsigned.AddRange(_out.TxId == _vout.TxId && _out.TxIndex == _vout.TxIndex ? HexPlus.HexStringToByteArray(_out.ScriptPKSH) : new byte[] { 0x00 });
                    _voutUnsigned.AddRange(new byte[] { 0xff, 0xff, 0xff, 0xff });
                }
                _voutUnsigned.AddRange(_vinUnsigned);
                _voutUnsigned.AddAndPadRight(4, 0x0, 0x01); //hash type;
                string _scriptSig = BitConverter.ToString(new SHA256Managed().ComputeHash(new SHA256Managed().ComputeHash(_voutUnsigned.ToArray()))).Replace("-", "").ToLower();

                //ECDSA
                BigInteger _k = BigInteger.Parse($"0{RandomPlus.RandomHex()}", NumberStyles.HexNumber);
                Encrypt.ECPoint _gk = Secp256k1.G.Multiply(_k);
                BigInteger _r = _gk.X;
                BigInteger _e = BigInteger.Parse($"0{_scriptSig}", NumberStyles.HexNumber);
                BigInteger _d = BigInteger.Parse($"0{_vout.PrivateHex}", NumberStyles.HexNumber);
                BigInteger _s = ((_r * _d + _e) * _k.ModInverse(Secp256k1.N)) % Secp256k1.N;

                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }

                List<byte> _rbytes = _r.ToByteArray().Reverse().ToList();
                List<byte> _sbytes = _s.ToByteArray().Reverse().ToList();

                List<byte> _allBytes = new List<byte>();
                BigInteger _rsLength = _rbytes.Count() + _sbytes.Count() + 4;
                _allBytes.Add(0x30);
                _allBytes.AddRange(_rsLength.ToByteArray());
                _allBytes.Add(0x02);
                _allBytes.AddRange(((BigInteger)_rbytes.Count()).ToByteArray());
                _allBytes.AddRange(_rbytes.ToArray());
                _allBytes.Add(0x02);
                _allBytes.AddRange(((BigInteger)_sbytes.Count()).ToByteArray());
                _allBytes.AddRange(_sbytes.ToArray());
                _allBytes.Add(0x01);
                _allBytes.Add(0x41);
                _allBytes.InsertRange(0, ((BigInteger)_allBytes.Count - 1).ToByteArray());

                _vout.ScriptSign = _allBytes;
            }

            //pay bytes
            List<byte> _signedRaw = new List<byte>();
            _signedRaw.AddRange(_voutHead);
            foreach (TransactionVout _vout in this.Vouts)
            {
                _signedRaw.AddRange(_vout.Scripts);//script per input
                BigInteger _sigLength = BigInteger.Parse(_vout.Public, NumberStyles.HexNumber).ToByteArray().Length + _vout.ScriptSign.Count;
                _signedRaw.AddRange(_sigLength.ToByteArray().Reverse().Where(f => f != 0x00).ToArray());
                _signedRaw.AddRange(_vout.ScriptSign.ToArray());
                _signedRaw.AddRange(HexPlus.HexStringToByteArray(_vout.Public));
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
        public string Private;
        public string PrivateHex
        {
            get
            {
                var _decoded = Base58.Decode(this.Private);
                return HexPlus.ByteArrayToHexString(_decoded.Skip(1).Take(_decoded.Length - 5).ToArray());
            }
        }
        public string Public => Address.Private2Public(this.Private, true);

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

        public string ScriptPKSH => Address.Public2PKSH(Lion.HexPlus.ByteArrayToHexString(new RIPEMD160Managed().ComputeHash(new SHA256Managed().ComputeHash(HexPlus.HexStringToByteArray(this.Public)))));

        public TransactionVout(string _txid, int _txIndex, decimal _amount, string _private)
        {
            this.TxId = _txid;
            this.TxIndex = _txIndex;
            this.Amount = _amount;
            this.Private = _private;
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
