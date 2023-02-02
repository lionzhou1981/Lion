using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Lion.Encrypt;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Lion.CryptoCurrency.Tron
{
    public static class Transaction
    {
        //public static void Test()
        //{
        //    //Console.WriteLine(UInt64ToRaw(6000000));
        //    //Console.WriteLine(Address.AddressToHex("TRmq5nLAHc3L9ijdRnYrVrbHr9rThTkYPZ").ToLower());
        //    //Console.WriteLine(Address.AddressToHex("TSQ3VWJsc99Jj9nZeUX8Jqq91HVgYaKw2A").ToLower());
        //    //Console.WriteLine(Address.AddressToHex("TXLAQ63Xg1NAzckPwKHvzw7CSEmLMEqcdj").ToLower());
        //    //var _raw1 = BuildTransferContractRaw(Address.AddressToHex("TRmq5nLAHc3L9ijdRnYrVrbHr9rThTkYPZ"), Address.AddressToHex("TSQ3VWJsc99Jj9nZeUX8Jqq91HVgYaKw2A"), 10M, "9eff", "2c9c25ab94d54a32", 120);
        //    //var _raw = BuildTRC20Raw(Address.AddressToHex("TRmq5nLAHc3L9ijdRnYrVrbHr9rThTkYPZ"), Address.AddressToHex("TSQ3VWJsc99Jj9nZeUX8Jqq91HVgYaKw2A"), 10M, "9eff", "2c9c25ab94d54a32",  Address.AddressToHex("TXLAQ63Xg1NAzckPwKHvzw7CSEmLMEqcdj"),6);
        //    var _raw = BuildTriggerSmartContractRaw(Address.AddressToHex("TRmq5nLAHc3L9ijdRnYrVrbHr9rThTkYPZ"), Address.AddressToHex("TSQ3VWJsc99Jj9nZeUX8Jqq91HVgYaKw2A"), 10M, "9eff", "2c9c25ab94d54a32", Address.AddressToHex("TXLAQ63Xg1NAzckPwKHvzw7CSEmLMEqcdj"), 6);
        //    //Console.WriteLine("RAW1:" + _raw1);
        //    Console.WriteLine("RAW:"+_raw);
        //    Console.WriteLine("SIGNED:"+ Sign("250f698c0ae74a98a9f1d0ae54c2770dddda53a9f62e5fa065ab9772a192c658", _raw));
        //}

        #region BuildTriggerSmartContractRaw
        public static string BuildTriggerSmartContractRaw(string _from, string _to, decimal _amount, string _refBlockBytes, string _refBlockHash, string _contract, int _contractDecimal, ulong _fee = 6000000, int _expSecond = 60)
        {
            DateTime _now = DateTime.UtcNow;
            string _fromHex = Address.AddressToHex(_from);
            string _toHex = Address.AddressToHex(_to);
            string _contractHex = Address.AddressToHex(_contract);
            long _amountValue = decimal.ToInt64(_amount * (decimal)Math.Pow(10, _contractDecimal));
            BigInteger _bigAmount = _amountValue;
            
            var _data = $"{Tron.TRC20_METHOD_TRANSFER}{(_toHex.StartsWith("41") ? _toHex.Substring(2).PadLeft(64, '0') : _toHex.PadLeft(64, '0'))}{HexPlus.ByteArrayToHexString(_bigAmount.ToByteArrayUnsigned(true)).PadLeft(64, '0')}";

            var _raw = $"22{(_data.Length / 2).ToString("x2")}{_data}";//data tag=22
            _raw = $"12{(_contractHex.Length / 2).ToString("x2")}{_contractHex}{_raw}";//contract address tag=12
            _raw = $"0a{(_fromHex.Length / 2).ToString("x2")}{_fromHex}{_raw}";//owner address tag=0a
            _raw = $"12{(_raw.Length / 2).ToString("x2")}{_raw}";
            _raw = $"0a31{HexPlus.ByteArrayToHexString(Encoding.UTF8.GetBytes("type.googleapis.com/protocol.TriggerSmartContract"))}{_raw}";
            _raw = $"12{(_raw.Length / 2).ToString("x2")}01{_raw}";
            _raw = $"081f{_contract}";
            _raw = $"5a{(_raw.Length / 2).ToString("x2")}01{_raw}";
            _raw = $"0a02{_refBlockBytes}2208{_refBlockHash}40{DateTime2Raw(_now.AddSeconds(_expSecond))}{_raw}";
            _raw += $"{_raw}70{DateTime2Raw(_now)}9001{UInt64ToRaw(_fee)}";
            return _raw;
        }
        #endregion

        #region BuildTransferContractRaw
        public static string BuildTransferContractRaw(string _from, string _to, decimal _amount, string _refBlockBytes, string _refBlockHash,  int _expSecond = 120)
        {
            DateTime _now = DateTime.UtcNow;
            string _fromHex = Address.AddressToHex(_from);
            string _toHex = Address.AddressToHex(_to);
            ulong _amountValue = decimal.ToUInt64(_amount * 1000000M);

            string _raw = $"0a15{_fromHex}1215{_toHex}18{UInt64ToRaw(_amountValue)}";
            _raw = $"12{(_raw.Length / 2).ToString("x2")}{_raw}";
            _raw = $"0a2d{HexPlus.ByteArrayToHexString(Encoding.UTF8.GetBytes("type.googleapis.com/protocol.TransferContract"))}{_raw}";
            _raw = $"080112{(_raw.Length / 2).ToString("x2")}{_raw}";
            _raw = $"5a{(_raw.Length / 2).ToString("x2")}{_raw}";

            _raw = $"0a02{_refBlockBytes}2208{_refBlockHash}40{DateTime2Raw(_now.AddSeconds(_expSecond))}{_raw}";
            _raw = $"{_raw}70{DateTime2Raw(_now)}";
            return _raw;
        }
        #endregion

        #region Sign
        public static string Sign(string _private, string _raw)
        {
            byte[] _rawBytes = HexPlus.HexStringToByteArray(_raw);
            string _sign = SignRaw(_private, _rawBytes);
            Console.WriteLine("Sign:" + _sign.ToLower());
            var _re = $"0a{(_raw.Length / 2).ToString("x2")}01{_raw}12{(_sign.Length / 2).ToString("x2")}{_sign.ToLower()}";

            return _re;
        }
        #endregion

        #region SignRaw
        private static string SignRaw(string _private, byte[] _raw)
        {
            var _rawHash = SHA256.Create().ComputeHash(_raw);//txid;
            BigInteger _limit = BigInteger.Pow(BigInteger.Parse("2"), 256),
                     _r = BigInteger.Zero,
                     _e = BigInteger.Zero,
                     _s = BigInteger.Zero,
                     _k = BigInteger.Zero,
                     _recid = BigInteger.Zero;

            RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
            int _count = 5;
            while (true)
            {
                _count -= 1;
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
                if (_s >= _limit || _s.Sign == 0 || _r.ToString("X").StartsWith("0") || _s.ToString("X").StartsWith("0")) 
                {
                    if (_count <= 0)
                    {
                        Thread.Sleep(1000);
                        return SignRaw(_private, _raw);
                    }
                    Thread.Sleep(100); 
                    continue; 
                }
                break;
            }
            return string.Join("", _r.ToString("X"), _s.ToString("X"), _recid.ToString("X").PadLeft(2,'0'));
        }
        #endregion

        #region DateTime2RawHex
        private static string DateTime2Raw(DateTime _time)
        {
            long _value = DateTimePlus.DateTime2UnixTime(_time);

            return UInt64ToRaw(ulong.Parse(_value.ToString()));
        }
        #endregion

        #region Int64ToRaw
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
