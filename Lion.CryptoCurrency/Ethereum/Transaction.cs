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
        public Number Value;
        public string DataHex;
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

        public static BigInteger randomBetween(BigInteger minimum, BigInteger maximum)
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

        #region ToSignedHex
        private RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        public string ToSignedHex(string _private)
        {
            byte[] _basicRaw = RLP.EncodeList(new byte[][] {
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeBigInteger(this.GasPrice.ToGWei()),
                RLP.EncodeUInt(this.GasLimit),
                RLP.EncodeHex(this.Address),
                RLP.EncodeBigInteger(this.Value.Integer),
                RLP.EncodeString(this.DataHex),
                RLP.EncodeInt((int)this.ChainId),
                RLP.EncodeString(""),
                RLP.EncodeString("")
            });

            byte[] _basicHashedRaw = new Keccak256().Compute(_basicRaw);

            BigInteger _limit = BigInteger.Pow(BigInteger.Parse("2"), 256), 
                       _r = BigInteger.Zero, 
                       _e = BigInteger.Zero, 
                       _s = BigInteger.Zero, 
                       _k = BigInteger.Zero,
                       _recid = BigInteger.Zero;

            while (true)
            {
                _k = BigInteger.Zero;
                if (_k == BigInteger.Zero)
                {
                    byte[] kBytes = new byte[33];
                    rngCsp.GetBytes(kBytes);
                    kBytes[32] = 0;
                    _k = new BigInteger(kBytes);
                }
                if (_k.IsZero || _k >= Secp256k1.N) continue;

                var _gk = Secp256k1.G.Multiply(_k);
                _r = _gk.X % Secp256k1.N;
                _recid = _gk.Y & 1;
                if (_r == BigInteger.Zero) { throw new Exception("Sign failed because R is Zero."); }
                if (_r >= _limit || _r.Sign == 0) { Thread.Sleep(100); continue; }                
                _e =  Lion.BigNumberPlus.HexToBigInt(BitConverter.ToString(_basicHashedRaw).Replace("-", ""));
                _s = ((_e + (_r * Lion.BigNumberPlus.HexToBigInt(_private))) * BigInteger.ModPow(_k, Secp256k1.N - 2, Secp256k1.N)) % Secp256k1.N;
                if (_s == BigInteger.Zero) { throw new Exception("Sign failed because S is Zero."); }
                if (_s > Secp256k1.HalfN) { _recid = _recid ^ 1; }
                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }
                if (_s >= _limit || _s.Sign == 0 || _r.ToString("X").StartsWith("0") || _s.ToString("X").StartsWith("0")) { Thread.Sleep(100); continue; }
                break;
            }
            BigInteger _v = BigInteger.Parse(((int)this.ChainId).ToString()) * 2 + _recid + 35;
            byte[] _signed = RLP.EncodeList(new byte[][] {
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeBigInteger(this.GasPrice.ToGWei()),
                RLP.EncodeUInt(this.GasLimit),
                RLP.EncodeHex(this.Address),
                RLP.EncodeBigInteger(this.Value.Integer),
                RLP.EncodeString(this.DataHex),
                RLP.EncodeBigInteger(_v),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_r.ToString("X"))),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_s.ToString("X")))
            });

            return HexPlus.ByteArrayToHexString(_signed).ToLower();
        }
        #endregion
    }
}
