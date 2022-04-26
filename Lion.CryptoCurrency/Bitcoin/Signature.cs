using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using Lion.Encrypt;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Signature
    {
        public static string SignHex(string _hex, string _wif)
        {
            BigInteger _k = BigNumberPlus.HexToBigInt(RandomPlus.RandomHex());
            Encrypt.ECPoint _gk = Secp256k1.G.Multiply(_k);
            BigInteger _r = _gk.X;
            BigInteger _e = BigNumberPlus.HexToBigInt(_hex);
            string _private = Address.Wif2Private(_wif, out _, out bool _compressed);
            BigInteger _d = BigNumberPlus.HexToBigInt(_private);
            BigInteger _s = ((_r * _d + _e) * _k.ModInverse(Secp256k1.N)) % Secp256k1.N;

            if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }

            List<byte> _rbytes = _r.ToByteArray().Reverse().ToList();
            List<byte> _sbytes = _s.ToByteArray().Reverse().ToList();
            List<byte> _result = new List<byte>();
            BigInteger _rsLength = _rbytes.Count() + _sbytes.Count() + 4;
            _result.Add(0x30);
            _result.AddRange(_rsLength.ToByteArray());
            _result.Add(0x02);
            _result.AddRange(((BigInteger)_rbytes.Count()).ToByteArray());
            _result.AddRange(_rbytes.ToArray());
            _result.Add(0x02);
            _result.AddRange(((BigInteger)_sbytes.Count()).ToByteArray());
            _result.AddRange(_sbytes.ToArray());
            _result.Add(0x01);
            BigInteger _publicBytesLength = BigNumberPlus.HexToBigInt(Address.Private2Public(_private, false, _compressed)).ToByteArray().Length;

            _result.AddRange(_publicBytesLength.ToByteArray());
            _result.InsertRange(0, ((BigInteger)(_result.Count - 1)).ToByteArray());

            return HexPlus.ByteArrayToHexString(_result.ToArray());
        }
    }
}
