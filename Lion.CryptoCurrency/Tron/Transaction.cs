using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Lion.Encrypt;
using Lion.CryptoCurrency.Tron.TransactionInfo;
using System.Linq;

namespace Lion.CryptoCurrency.Tron
{
    public static class Transaction
    {
        public static void Test()
        {
            var _raw = BuildTRC20Raw(Address.AddressToHex("TRmq5nLAHc3L9ijdRnYrVrbHr9rThTkYPZ"), Address.AddressToHex("TSQ3VWJsc99Jj9nZeUX8Jqq91HVgYaKw2A"), 1000M, "e370", "f9894b6189980c5a",  Address.AddressToHex("TXLAQ63Xg1NAzckPwKHvzw7CSEmLMEqcdj"), 60);
            Console.WriteLine("RAW:"+_raw);
            Console.WriteLine("SIGNED:"+ Sign("250f698c0ae74a98a9f1d0ae54c2770dddda53a9f62e5fa065ab9772a192c658", _raw));
        }

        public static string BuildTransferContractRaw(string _from, string _to, decimal _amount, string _refBlockBytes, string _refBlockHash,  int _expSecond = 60)
        {
            DateTime _now = DateTime.UtcNow;
            ulong _amountValue = decimal.ToUInt64(_amount * 1000000M);

            string _contract = $"0a15{_from}1215{_to}18{UInt64ToRaw(_amountValue)}";
            _contract = $"12{(_contract.Length / 2).ToString("x2")}{_contract}";
            _contract = $"0a2d{HexPlus.ByteArrayToHexString(Encoding.UTF8.GetBytes("type.googleapis.com/protocol.TransferContract"))}{_contract}";
            _contract = $"080112{(_contract.Length / 2).ToString("x2")}{_contract}";
            _contract = $"5a{(_contract.Length / 2).ToString("x2")}{_contract}";

            string _raw = $"0a02{_refBlockBytes}2208{_refBlockHash}40{DateTime2Raw(_now.AddSeconds(_expSecond))}";
            _raw += $"{_contract}70{DateTime2Raw(_now)}";

            Console.WriteLine($"RAW1: " + _raw);

            using MemoryStream _msFrom = new MemoryStream(HexPlus.HexStringToByteArray(_from));
            using MemoryStream _msTo = new MemoryStream(HexPlus.HexStringToByteArray(_to));
            using MemoryStream _refBB = new MemoryStream(HexPlus.HexStringToByteArray(_refBlockBytes));
            using MemoryStream _refBH = new MemoryStream(HexPlus.HexStringToByteArray(_refBlockHash));

            TransactionInfo.Transaction _tr = new TransactionInfo.Transaction();
            _tr.RawData = new TransactionInfo.Transaction.Types.raw();
            _tr.RawData.Contract.Add(
                new TransactionInfo.Transaction.Types.Contract()
                {
                    Type = TransactionInfo.Transaction.Types.Contract.Types.ContractType.TransferContract,
                    Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract()
                    {
                        Amount = decimal.ToInt64(_amount * 1000000M),
                        OwnerAddress = ByteString.FromStream(_msFrom),
                        ToAddress = ByteString.FromStream(_msTo)
                    }),
                });
            _tr.RawData.RefBlockHash = ByteString.FromStream(_refBH);
            _tr.RawData.RefBlockBytes = ByteString.FromStream(_refBB);
            _tr.RawData.Expiration = DateTimePlus.DateTime2UnixTime(_now.AddSeconds(_expSecond));
            _tr.RawData.Timestamp = DateTimePlus.DateTime2UnixTime(_now);
            byte[] _re = _tr.RawData.ToByteArray();

            Console.WriteLine($"RAW2: " + HexPlus.ByteArrayToHexString(_re));

            return _raw;

        }

        public static string BuildTRC20Raw(string _from, string _to, decimal _amount, string _refBlockBytes, string _refBlockHash, string _contractAddress,int _contractDecimal,long _fee = 6000000, int _expSecond = 60)
        {
            DateTime _now = DateTime.UtcNow;
            long _amountValue = decimal.ToInt64(_amount *  (decimal)Math.Pow(10, _contractDecimal));



            BigInteger _bigAmount = _amountValue;
            using MemoryStream _msFrom = new MemoryStream(HexPlus.HexStringToByteArray(_from));
            using MemoryStream _msTo = new MemoryStream(HexPlus.HexStringToByteArray(_to));
            using MemoryStream _msContractAddress = new MemoryStream(HexPlus.HexStringToByteArray(_contractAddress));
            using MemoryStream _refBB = new MemoryStream(HexPlus.HexStringToByteArray(_refBlockBytes));
            using MemoryStream _refBH = new MemoryStream(HexPlus.HexStringToByteArray(_refBlockHash));
            var _data = $"{Tron.TRC20_METHOD_TRANSFER}{(_to.StartsWith("41")?_to.Substring(2).PadLeft(64,'0'):_to.PadLeft(64,'0')) }{HexPlus.ByteArrayToHexString(_bigAmount.ToByteArrayUnsigned(true)).PadLeft(64, '0')}";
            using MemoryStream _msData = new MemoryStream(HexPlus.HexStringToByteArray(_data));

            TransactionInfo.Transaction _tr = new TransactionInfo.Transaction();
            _tr.RawData = new TransactionInfo.Transaction.Types.raw();
            _tr.RawData.Contract.Add(
                new TransactionInfo.Transaction.Types.Contract()
                {
                    Type = TransactionInfo.Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
                    Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(new TriggerSmartContract()
                    {
                        CallValue = 0,
                        Data = ByteString.FromStream(_msData),
                        OwnerAddress = ByteString.FromStream(_msFrom),
                        ContractAddress = ByteString.FromStream(_msContractAddress),
                    }),
                });
            _tr.RawData.FeeLimit = _fee;
            _tr.RawData.RefBlockHash = ByteString.FromStream(_refBH);
            _tr.RawData.RefBlockBytes = ByteString.FromStream(_refBB);
            _tr.RawData.Expiration = DateTimePlus.DateTime2UnixTime(_now.AddSeconds(_expSecond));
            _tr.RawData.Timestamp = DateTimePlus.DateTime2UnixTime(_now);
            byte[] _re = _tr.RawData.ToByteArray();
            return HexPlus.ByteArrayToHexString(_re);
        }

        public static string Sign(string _private, string _raw)
        {
            TransactionInfo.Transaction _tr = new TransactionInfo.Transaction();
            byte[] _rawBytes = HexPlus.HexStringToByteArray(_raw);
            try
            {
                _tr.RawData = TransactionInfo.Transaction.Types.raw.Parser.ParseFrom(_rawBytes);
            }
            catch
            {
                throw new Exception("Transaction raw decoded failed");
            }
            var _sign = GetRawSign(_private, _rawBytes);
            using MemoryStream _signStream = new MemoryStream(HexPlus.HexStringToByteArray(_sign));
            _tr.Signature.Add(ByteString.FromStream(_signStream));
            byte[] _re = _tr.ToByteArray();
            return HexPlus.ByteArrayToHexString(_re);
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

        #region DateTime2RawHex
        private static string DateTime2Raw(DateTime _time)
        {
            long _value = DateTimePlus.DateTime2UnixTime(_time);

            return UInt64ToRaw(ulong.Parse(_value.ToString()));
        }
        #endregion

        #region static string Int64ToRaw
        private static string UInt64ToRaw(ulong _value)
        {
            IList<byte> _bytes = new List<byte>();
            while (_value > 127)
            {
                _bytes.Add((byte)((_value & 0x7F) | 0x80));
                _value >>= 7;
            }
            _bytes.Add((byte)_value);

            return HexPlus.ByteArrayToHexString(_bytes.ToArray());
        }
        #endregion
    }
}
