using System;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Transaction
    {
        public uint ChainId;
        public uint GasPrice;
        public uint GasLimit;
        public string Address;
        public uint Nonce;
        public Number Value;
        public string DataHex;

        #region ToSignedHex
        public string ToSignedHex(string _private)
        {
            byte[] _basicRaw = RLP.EncodeList(new byte[][] {
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeUInt(this.GasPrice),
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

            Random _random = new Random(RandomPlus.RandomSeed);
            while (true)
            {
                _k = _random.Next(1, int.MaxValue);
                ECPoint _gk = Secp256k1.G.Multiply(_k);

                _r = _gk.X % Secp256k1.N;
                if (_r == BigInteger.Zero) { throw new Exception("Sign failed because R is Zero."); }
                if (_r >= _limit || _r.Sign == 0) { Thread.Sleep(100); continue; }

                _e = BigInteger.Parse($"0{BitConverter.ToString(_basicHashedRaw).Replace("-", "")}", NumberStyles.HexNumber);
                _s = ((_r * BigInteger.Parse($"0{_private}", NumberStyles.HexNumber) + _e) * _k.ModInverse(Secp256k1.N)) % Secp256k1.N;

                if (_s == BigInteger.Zero) { throw new Exception("Sign failed because S is Zero."); }
                if (_s > Secp256k1.HalfN) { _recid = _gk.Y & 1; }
                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }
                if (_s >= _limit || _s.Sign == 0) { Thread.Sleep(100); continue; }

                break;
            }

            BigInteger _v = BigInteger.Parse(((int)this.ChainId).ToString()) * 2 + _recid + 35;

            byte[] _signed = RLP.EncodeList(new byte[][] {
                RLP.EncodeUInt(this.Nonce),
                RLP.EncodeUInt(this.GasPrice),
                RLP.EncodeUInt(this.GasLimit),
                RLP.EncodeHex(this.Address),
                RLP.EncodeBigInteger(this.Value.Integer),
                RLP.EncodeString(this.DataHex),
                RLP.EncodeBigInteger(_v),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_r.ToString("X").TrimStart('0'))),
                RLP.EncodeBytes(HexPlus.HexStringToByteArray(_s.ToString("X").TrimStart('0')))
            });

            return HexPlus.ByteArrayToHexString(_signed).ToLower();
        }
        #endregion
    }
}
