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

        private static Tuple<int, BigInteger> calculateParameters(BigInteger range)
        {
            int bitsNeeded = 0;
            int bytesNeeded = 0;
            BigInteger mask = new BigInteger(1);

            while (range > 0)
            {
                if (bitsNeeded % 8 == 0)
                {
                    bytesNeeded += 1;
                }

                bitsNeeded++;

                mask = (mask << 1) | 1;

                range >>= 1;
            }

            return Tuple.Create(bytesNeeded, mask);

        }

        private static BigInteger randomBetween(BigInteger minimum, BigInteger maximum)
        {
            if (maximum < minimum)
            {
                throw new ArgumentException("maximum must be greater than minimum");
            }

            BigInteger range = maximum - minimum;

            Tuple<int, BigInteger> response = calculateParameters(range);
            int bytesNeeded = response.Item1;
            BigInteger mask = response.Item2;

            byte[] randomBytes = new byte[bytesNeeded];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(randomBytes);
            }

            BigInteger randomValue = new BigInteger(randomBytes);

            /* We apply the mask to reduce the amount of attempts we might need
                * to make to get a number that is in range. This is somewhat like
                * the commonly used 'modulo trick', but without the bias:
                *
                *   "Let's say you invoke secure_rand(0, 60). When the other code
                *    generates a random integer, you might get 243. If you take
                *    (243 & 63)-- noting that the mask is 63-- you get 51. Since
                *    51 is less than 60, we can return this without bias. If we
                *    got 255, then 255 & 63 is 63. 63 > 60, so we try again.
                *
                *    The purpose of the mask is to reduce the number of random
                *    numbers discarded for the sake of ensuring an unbiased
                *    distribution. In the example above, 243 would discard, but
                *    (243 & 63) is in the range of 0 and 60."
                *
                *   (Source: Scott Arciszewski)
                */

            randomValue &= mask;

            if (randomValue <= range)
            {
                /* We've been working with 0 as a starting point, so we need to
                    * add the `minimum` here. */
                return minimum + randomValue;
            }

            /* Outside of the acceptable range, throw it away and try again.
                * We don't try any modulo tricks, as this would introduce bias. */
            return randomBetween(minimum, maximum);

        }

        #region ToBasicRaw
        public byte[] ToBasicRaw()
        {
            Console.WriteLine("Raw:" + ToRaw());
            var _noce = RLP.EncodeUInt(this.Nonce);
            return RLP.EncodeList(
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeBigInteger(this.GasPrice.ToGWei()),
                RLP.EncodeUInt(this.GasLimit),
                RLP.EncodeHex(this.Address.StartsWith("0x")?this.Address[2..]:this.Address),
                RLP.EncodeBigInteger(this.Value.Integer),
                RLP.EncodeHex(this.DataHex.StartsWith("0x")?this.DataHex[2..]:this.DataHex),
                RLP.EncodeInt((int)this.ChainId),
                RLP.EncodeHex(""),
                RLP.EncodeHex("")
            );
        }
        #endregion

        #region ToRaw
        private string BigIntToLenPrefixHex(BigInteger _value, bool _unsigned = true, bool _bigEndian = true)
        {
            if (_value == 0)
                return "80";
            var _valueArray = _value.ToByteArray(_unsigned, _bigEndian);
            var _re = Lion.HexPlus.ByteArrayToHexString(_valueArray);
            if ((_valueArray[0] == 0 && _valueArray.Length == 2) || _valueArray.Length == 1) return _re;
            if (_value < 128)
                return $"80{_re}";
            else
                return $"{80 + _re.Length / 2}{_re}";
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
            if (_length<56)
            {
                return $"{(192 + _length).ToString("x2")}{_hex}";
            }
            while (_length != 0) { ++_byteCount; _length = _length >> 8; }
            return $"{(247 + _byteCount).ToString("x2")}{(_hex.Length / 2).ToString("x2")}{_hex}";
        }

        public string ToRaw()
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
                _rawData = $"{_rawData}80";
            _rawData = $"{_rawData}{BigIntToLenPrefixHex(this.ChainId)}";            
            _rawData = $"{_rawData}80";//empty
            _rawData = $"{_rawData}80";//empty
            _rawData = ListToLenPrefix(_rawData);
            return _rawData;
        }
        #endregion

            #region ToSignedHex
        public string ToSignedHex(string _private)
        {
            byte[] _basicRaw = ToBasicRaw();
            return Ethereum.Sign(_basicRaw, _private, (int)this.ChainId);
        }
        #endregion
    }
}
