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
    public static class Transaction
    {
        public static void Test()
        {
            var _raw = BuildRaw("4108e16726d0ea16d2ed187c8c20feb86e66ecf832", "416cd8a0295c1e750c4cb20d2e0f0f3f47f0a0b549", "f1a6", "0b01386f2ce3d5a7", 1668068517000, 1668068461406, 0.001M);
            Console.WriteLine("RAW:"+_raw);
            Console.WriteLine("SIGNED:"+ Sign("b6dbf97c509619f122bfe8af187192b03842105b7aa940a52b5059d2cea62623", _raw));
        }

        public static string BuildRaw(string _from,string _to,string _refBlockBytes,string _refBlockHash,long _expTime,long _timeStamp,decimal _amount)
        {
            long _amountValue = decimal.ToInt64(_amount * 1000000M);
            using MemoryStream _msFrom = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_from));
            using MemoryStream _msTo = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_to));
            using MemoryStream _refBB = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockBytes));
            using MemoryStream _refBH = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockHash));

            Lion.CryptoCurrency.Tron.TransactionInfo.Transaction _tr = new Lion.CryptoCurrency.Tron.TransactionInfo.Transaction();
            _tr.RawData = new TransactionInfo.Transaction.Types.raw();
            _tr.RawData.Contract.Add(
                new TransactionInfo.Transaction.Types.Contract()
            {
                Type = TransactionInfo.Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract()
                {
                    Amount = _amountValue,
                    OwnerAddress = ByteString.FromStream(_msFrom),
                    ToAddress = ByteString.FromStream(_msTo)
                }),
            });
            _tr.RawData.RefBlockHash = ByteString.FromStream(_refBH);
            _tr.RawData.RefBlockBytes = ByteString.FromStream(_refBB);
            _tr.RawData.Expiration = _expTime;
            _tr.RawData.Timestamp = _timeStamp;
            byte[] _re = _tr.RawData.ToByteArray();
            return Lion.HexPlus.ByteArrayToHexString(_re);
        }

        public static string Sign(string _private, string _raw)
        {
            Lion.CryptoCurrency.Tron.TransactionInfo.Transaction _tr = new Lion.CryptoCurrency.Tron.TransactionInfo.Transaction();
            var _rawBytes = Lion.HexPlus.HexStringToByteArray(_raw);
            try
            {
                _tr.RawData = TransactionInfo.Transaction.Types.raw.Parser.ParseFrom(_rawBytes);
            }
            catch
            {
                throw new Exception("Transaction raw decoded failed");
            }
            var _sign = GetRawSign(_private, _rawBytes);
            using MemoryStream _signStream = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_sign));
            _tr.Signature.Add(ByteString.FromStream(_signStream));
            byte[] _re = _tr.ToByteArray();
            return Lion.HexPlus.ByteArrayToHexString(_re);
        }

        private static string GetRawSign(string _private, byte[] _raw)
        {
            var _rawHash = SHA256.Create().ComputeHash(_raw);//txid;
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
                    Console.WriteLine(_k.ToString());
                }
                if (_k.IsZero || _k >= Secp256k1.N) { continue; }

                var _gk = Secp256k1.G.Multiply(_k);
                _r = _gk.X % Secp256k1.N;
                _recid = _gk.Y & 1;
                if (_r == BigInteger.Zero) { throw new Exception("Sign failed because R is Zero."); }
                if (_r >= _limit || _r.Sign == 0) { Thread.Sleep(100); continue; }

                _e = BigNumberPlus.HexToBigInt(Lion.HexPlus.ByteArrayToHexString(_rawHash));
                _s = ((_e + (_r * BigNumberPlus.HexToBigInt(_private))) * BigInteger.ModPow(_k, Secp256k1.N - 2, Secp256k1.N)) % Secp256k1.N;

                if (_s == BigInteger.Zero) { throw new Exception("Sign failed because S is Zero."); }
                if (_s > Secp256k1.HalfN) { _recid ^= 1; }
                if (_s.CompareTo(Secp256k1.HalfN) > 0) { _s = Secp256k1.N - _s; }
                if (_s >= _limit || _s.Sign == 0 || _r.ToString("X").StartsWith("0") || _s.ToString("X").StartsWith("0")) { Thread.Sleep(100); continue; }
                break;
            }
            return string.Join("", _r.ToString("X"), _s.ToString("X"), _recid.ToString("X").PadLeft(2,'0'));

        }
    }
}
