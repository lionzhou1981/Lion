using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using Lion.Encrypt;
using ECPoint = Lion.Encrypt.ECPoint;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Transaction
    {
        public uint ChainId;
        public Number GasPrice;
        public uint GasLimit;
        public string Address;
        public uint Nonce;
        public Number Value = new Number(0M);
        public string DataHex = "";

        #region ToBasicRaw
        public byte[] ToBasicRaw()
        {
            byte[] _raw = RLP.EncodeList(
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeBigInteger(this.GasPrice.ToGWei()),
                RLP.EncodeUInt(this.GasLimit),
                RLP.EncodeHex(this.Address.StartsWith("0x") ? this.Address[2..] : this.Address),
                RLP.EncodeBigInteger(this.Value.Integer),
                RLP.EncodeHex(this.DataHex.StartsWith("0x") ? this.DataHex[2..] : this.DataHex),
                RLP.EncodeInt((int)this.ChainId),
                RLP.EncodeHex(""),
                RLP.EncodeHex(""));

            return _raw;
        }
        #endregion

        #region ToRaw
        private string BigIntToLenPrefixHex(BigInteger _value, bool _unsigned = true, bool _bigEndian = true)
        {
            if (_value == 0) { return "80"; }
                
            var _valueArray = _value.ToByteArray(_unsigned, _bigEndian);
            var _re = HexPlus.ByteArrayToHexString(_valueArray);
            if ((_valueArray[0] == 0 && _valueArray.Length == 2) || _valueArray.Length == 1) { return _re; }

            if (_value < 128)
            {
                return $"80{_re}";
            }
            else
            {
                return $"{80 + _re.Length / 2}{_re}";
            }
        }

        private string HexToLenPrefix(string _hex)
        {
            var _length = _hex.Length / 2;
            var _byteCount = 0;
            while (_length != 0) { ++_byteCount; _length = _length >> 8; }
            return $"{(183 + _byteCount).ToString("x2")}{_hex}";
        }

        private string ListToLenPrefix(string _hex)
        {
            var _length = _hex.Length / 2;
            var _byteCount = 0;
            if (_length < 56)
            {
                return $"{(192 + _length).ToString("x2")}{_hex}";
            }
            while (_length != 0) { ++_byteCount; _length = _length >> 8; }
            return $"{(247 + _byteCount).ToString("x2")}{(_hex.Length / 2).ToString("x2")}{_hex}";
        }

        public byte[] ToRaw()
        {
            var _rawData = BigIntToLenPrefixHex(this.Nonce);
            _rawData = $"{_rawData}{BigIntToLenPrefixHex(this.GasPrice.ToGWei())}";
            _rawData = $"{_rawData}{BigIntToLenPrefixHex(this.GasLimit)}";
            var _address = this.Address[2..].ToLower();
            _rawData = $"{_rawData}{(_address.Length / 2 + 128).ToString("x2")}{_address}";//address长度恒定            
            _rawData = $"{_rawData}{BigIntToLenPrefixHex(this.Value.Integer)}";
            if (!string.IsNullOrWhiteSpace(this.DataHex))
            {
                var _dataHex = this.DataHex.StartsWith("0x") ? this.DataHex[2..] : this.DataHex;
                _dataHex = $"{(_dataHex.Length / 2).ToString("x2")}{_dataHex}";
                _rawData = $"{_rawData}{HexToLenPrefix(_dataHex)}";//data
            }
            else
            {
                _rawData = $"{_rawData}80";
            }
            _rawData = $"{_rawData}{BigIntToLenPrefixHex(this.ChainId)}";            
            _rawData = $"{_rawData}80";//empty
            _rawData = $"{_rawData}80";//empty
            _rawData = ListToLenPrefix(_rawData);
            return HexPlus.HexStringToByteArray(_rawData);
        }
        #endregion

        #region ToSignedHex
        public string ToSignedHex(string _private)
        {
            byte[] _basicRaw = ToBasicRaw();
            byte[] _raw = ToRaw();

            Console.WriteLine("Raw 1:" + HexPlus.ByteArrayToHexString(_basicRaw));
            Console.WriteLine("Raw 2:" + HexPlus.ByteArrayToHexString(_raw));

            return Ethereum.Sign(_raw, _private, (int)this.ChainId);
        }
        #endregion
    }
}
