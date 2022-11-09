using Google.Protobuf;
using Lion.CryptoCurrency.Tron.TransactionInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using Lion.Encrypt;
using System.Threading;

namespace Lion.CryptoCurrency.Tron
{
    internal class Transaction
    {
        public static string BuildRaw(string _from,string _to,string _refBlockBytes,string _refBlockHash,long _expTime,long _timeStamp)
        {
            using MemoryStream _msFrom = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_from));
            using MemoryStream _msTo = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_to));
            using MemoryStream _refBB = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockBytes));
            using MemoryStream _refBH = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockHash));

            Lion.CryptoCurrency.Tron.TransactionInfo.Transaction tr = new Lion.CryptoCurrency.Tron.TransactionInfo.Transaction();
            tr.RawData = new TransactionInfo.Transaction.Types.raw();
            tr.RawData.Contract.Add(
                new TransactionInfo.Transaction.Types.Contract()
            {
                Type = TransactionInfo.Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract()
                {
                    Amount = 1000,
                    OwnerAddress = Google.Protobuf.ByteString.FromStream(_msFrom),
                    ToAddress = Google.Protobuf.ByteString.FromStream(_msTo)
                }),
            });
            tr.RawData.RefBlockHash = ByteString.FromStream(_refBH);
            tr.RawData.RefBlockBytes = ByteString.FromStream(_refBB);
            tr.RawData.Expiration = 1667899389000;
            tr.RawData.Timestamp = 1667899330766;
            byte[] _re = tr.ToByteArray();
            return Lion.HexPlus.ByteArrayToHexString(_re).Substring(6);
        }


        public static string Sign(string _private,string _raw)
        {
            BigInteger _limit = BigInteger.Pow(BigInteger.Parse("2"), 256),
                     _r = BigInteger.Zero,
                     _e = BigInteger.Zero,
                     _s = BigInteger.Zero,
                     _k = BigInteger.Zero,
                     _recid = BigInteger.Zero;

            RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

            while (true)
            {
                _k = BigInteger.Zero;
                if (_k == BigInteger.Zero)
                {
                    byte[] _kBytes = new byte[33];
                    _rng.GetBytes(_kBytes);
                    _kBytes[32] = 0;
                    _k = new BigInteger(_kBytes);
                }
                if (_k.IsZero || _k >= Secp256k1.N) { continue; }

                var _gk = Secp256k1.G.Multiply(_k);
                _r = _gk.X % Secp256k1.N;
                _recid = _gk.Y & 1;

                if (_r == BigInteger.Zero) { throw new Exception("Sign failed because R is Zero."); }
                if (_r >= _limit || _r.Sign == 0) { Thread.Sleep(100); continue; }

                _e = BigNumberPlus.HexToBigInt(_raw);
                _s = ((_e + (_r * BigNumberPlus.HexToBigInt(_private))) * BigInteger.ModPow(_k, Secp256k1.N - 2, Secp256k1.N)) % Secp256k1.N;

                if (_s == BigInteger.Zero) { throw new Exception("Sign failed because S is Zero."); }
                if (_s > Secp256k1.HalfN) { _recid ^= 1; }
                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }
                if (_s >= _limit || _s.Sign == 0 || _r.ToString("X").StartsWith("0") || _s.ToString("X").StartsWith("0")) { Thread.Sleep(100); continue; }
                break;
            }
            return string.Join("", _r.ToString("X"), _s.ToString("X"), _recid.ToString("X"));

        }
    }
}
