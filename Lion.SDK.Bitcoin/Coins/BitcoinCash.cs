using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using Lion;
using Lion.Encrypt;

namespace Lion.SDK.Bitcoin.Coins
{
    public class BitcoinCash
    {
        #region Var
        internal const string CHARSET_BASE58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        internal const string CHARSET_CASHADDR = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

        internal static readonly sbyte[] DICT_CASHADDR = new sbyte[128]{
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            15, -1, 10, 17, 21, 20, 26, 30,  7,  5, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, 29, -1, 24, 13, 25,  9,  8, 23, -1, 18, 22, 31, 27, 19, -1,
            1,  0,  3, 16, 11, 28, 12, 14,  6,  4,  2, -1, -1, -1, -1, -1
        };

        internal static readonly sbyte[] DICT_BASE58 = new sbyte[128]{
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1,  0,  1,  2,  3,  4,  5,  6,  7,  8, -1, -1, -1, -1, -1, -1,
            -1,  9, 10, 11, 12, 13, 14, 15, 16, -1, 17, 18, 19, 20, 21, -1,
            22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, -1, -1, -1, -1, -1,
            -1, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, -1, 44, 45, 46,
            47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, -1, -1, -1, -1, -1
        };
        #endregion

        #region PolyMod
        internal static ulong PolyMod(byte[] input, ulong startValue = 1)
        {
            for (uint i = 0; i < 42; i++)
            {
                ulong c0 = startValue >> 35;
                startValue = ((startValue & 0x07ffffffff) << 5) ^ ((ulong)input[i]);
                if ((c0 & 0x01) != 0) { startValue ^= 0x98f2bc8e61; }
                if ((c0 & 0x02) != 0) { startValue ^= 0x79b76d99e2; }
                if ((c0 & 0x04) != 0) { startValue ^= 0xf33e5fb3c4; }
                if ((c0 & 0x08) != 0) { startValue ^= 0xae2eabe2a8; }
                if ((c0 & 0x10) != 0) { startValue ^= 0x1e4f43e470; }
            }
            return startValue ^ 1;
        }
        #endregion

        #region Bits8to5
        internal static byte[] Bits8to5(byte[] _bytes)
        {
            byte[] _converted = new byte[34 + 8];
            int _a1 = 0, _a2 = 0;
            for (; _a1 < 32; _a1 += 8, _a2 += 5)
            {
                _converted[_a1] = (byte)(_bytes[_a2] >> 3);
                _converted[_a1 + 1] = (byte)(_bytes[_a2] % 8 << 2 | _bytes[_a2 + 1] >> 6);
                _converted[_a1 + 2] = (byte)(_bytes[_a2 + 1] % 64 >> 1);
                _converted[_a1 + 3] = (byte)(_bytes[_a2 + 1] % 2 << 4 | _bytes[_a2 + 2] >> 4);
                _converted[_a1 + 4] = (byte)(_bytes[_a2 + 2] % 16 << 1 | _bytes[_a2 + 3] >> 7);
                _converted[_a1 + 5] = (byte)(_bytes[_a2 + 3] % 128 >> 2);
                _converted[_a1 + 6] = (byte)(_bytes[_a2 + 3] % 4 << 3 | _bytes[_a2 + 4] >> 5);
                _converted[_a1 + 7] = (byte)(_bytes[_a2 + 4] % 32);
            }
            _converted[_a1] = (byte)(_bytes[_a2] >> 3);
            _converted[_a1 + 1] = (byte)(_bytes[_a2] % 8 << 2);
            return _converted;
        }
        #endregion

        #region Bits5to8
        internal static byte[] Bits5to8(byte[] bytes)
        {
            byte[] converted = new byte[(1 + 20) + 4];
            int a1 = 0, a2 = 0;
            for (; a2 < 32; a1 += 5, a2 += 8)
            {
                converted[a1] = (byte)(bytes[a2] << 3 | bytes[a2 + 1] >> 2);
                converted[a1 + 1] = (byte)(bytes[a2 + 1] % 4 << 6 | bytes[a2 + 2] << 1 | bytes[a2 + 3] >> 4);
                converted[a1 + 2] = (byte)(bytes[a2 + 3] % 16 << 4 | bytes[a2 + 4] >> 1);
                converted[a1 + 3] = (byte)(bytes[a2 + 4] % 2 << 7 | bytes[a2 + 5] << 2 | bytes[a2 + 6] >> 3);
                converted[a1 + 4] = (byte)(bytes[a2 + 6] % 8 << 5 | bytes[a2 + 7]);
            }
            converted[a1] = (byte)(bytes[a2] << 3 | bytes[a2 + 1] >> 2);
            if (bytes[a2 + 1] % 4 != 0) { throw new Exception("Invalid CashAddr."); }

            return converted;
        }
        #endregion

        #region Bitcoin2Cash
        public static string Bitcoin2Cash(string oldAddress, out bool isP2PKH, out bool mainnet)
        {
            BigInteger _address = new BigInteger(0);
            BigInteger _base58 = new BigInteger(58);
            for (int x = 0; x < oldAddress.Length; x++)
            {
                int value = DICT_BASE58[oldAddress[x]];
                if (value != -1)
                {
                    _address = BigInteger.Multiply(_address, _base58);
                    _address = BigInteger.Add(_address, new BigInteger(value));
                }
                else
                {
                    throw new Exception("Address contains unexpected character.");
                }
            }
            int numZeros = 0;
            for (; (numZeros < oldAddress.Length) && (oldAddress[numZeros] == Convert.ToChar("1")); numZeros++) { }
            byte[] addrBytes = _address.ToByteArray();
            Array.Reverse(addrBytes);
            if (addrBytes[0] == 0)
            {
                var temp = new List<byte>(addrBytes);
                temp.RemoveAt(0);
                addrBytes = temp.ToArray();
            }
            if (numZeros > 0)
            {
                var temp = new List<byte>(addrBytes);
                for (; numZeros != 0; numZeros--)
                    temp.Insert(0, 0);
                addrBytes = temp.ToArray();
            }
            if (addrBytes.Length != 25)
            {
                throw new Exception("Address to be decoded is shorter or longer than expected!");
            }
            switch (addrBytes[0])
            {
                case 0x00:
                    isP2PKH = true;
                    mainnet = true;
                    break;
                case 0x05:
                    isP2PKH = false;
                    mainnet = true;
                    break;
                case 0x6f:
                    isP2PKH = true;
                    mainnet = false;
                    break;
                case 0xc4:
                    isP2PKH = false;
                    mainnet = false;
                    break;
                case 0x1c:
                // BitPay P2PKH, obsolete!
                case 0x28:
                // BitPay P2SH, obsolete!
                default:
                    throw new Exception("Unexpected address byte.");
            }
            if (addrBytes.Length != 25)
            {
                throw new Exception("Old address is longer or shorter than expected.");
            }

            SHA256 hasher = SHA256.Create();
            byte[] checksum = hasher.ComputeHash(hasher.ComputeHash(addrBytes, 0, 21));
            if (addrBytes[21] != checksum[0] || addrBytes[22] != checksum[1] || addrBytes[23] != checksum[2] || addrBytes[24] != checksum[3])
            {
                throw new Exception("Address checksum doesn't match. Have you made a mistake while typing it?");
            }

            addrBytes[0] = (byte)(isP2PKH ? 0x00 : 0x08);

            byte[] cashAddr = Bits8to5(addrBytes);
            var ret = new System.Text.StringBuilder(mainnet ? "bitcoincash:" : "bchtest:");
            ulong mod = PolyMod(cashAddr, (ulong)(mainnet ? 1058337025301 : 584719417569));
            for (int i = 0; i < 8; ++i)
            {
                cashAddr[i + 34] = (byte)((mod >> (5 * (7 - i))) & 0x1f);
            }
            for (int i = 0; i < cashAddr.Length; i++)
            {
                ret.Append(CHARSET_CASHADDR[cashAddr[i]]);
            }
            return ret.ToString();
        }
        #endregion

        #region Cash2Bitcoin
        public static string Cash2Bitcoin(string cashAddr, out bool isP2PKH, out bool mainnet)
        {
            cashAddr = cashAddr.ToLower();
            if (cashAddr.Length != 54 && cashAddr.Length != 42 && cashAddr.Length != 50)
            {
                if (cashAddr.StartsWith("bchreg:")) { throw new Exception("Decoding RegTest addresses is not implemented."); }
                throw new Exception("Address to be decoded is longer or shorter than expected.");
            }

            int afterPrefix;
            if (cashAddr.StartsWith("bitcoincash:"))
            {
                mainnet = true;
                afterPrefix = 12;
            }
            else if (cashAddr.StartsWith("bchtest:"))
            {
                mainnet = false;
                afterPrefix = 8;
            }
            else if (cashAddr.StartsWith("bchreg:"))
            {
                throw new Exception("Decoding RegTest addresses is not implemented.");
            }
            else
            {
                if (cashAddr.IndexOf(":") == -1)
                {
                    mainnet = true;
                    afterPrefix = 0;
                }
                else
                {
                    throw new Exception("Unexpected colon character.");
                }
            }

            int max = afterPrefix + 42;
            if (max != cashAddr.Length) { throw new Exception("Address to be decoded is longer or shorter than expected."); }

            byte[] decodedBytes = new byte[42];
            for (int i = afterPrefix; i < max; i++)
            {
                int value = DICT_CASHADDR[cashAddr[i]];
                if (value != -1)
                {
                    decodedBytes[i - afterPrefix] = (byte)value;
                }
                else
                {
                    throw new Exception("Address contains unexpected character.");
                }
            }
            if (PolyMod(decodedBytes, (ulong)(mainnet ? 1058337025301 : 584719417569)) != 0) { throw new Exception("Address checksum doesn't match. Have you made a mistake while typing it?"); }

            decodedBytes = Bits5to8(decodedBytes);
            switch (decodedBytes[0])
            {
                case 0x00:
                    isP2PKH = true;
                    break;
                case 0x08:
                    isP2PKH = false;
                    break;
                default:
                    throw new Exception("Unexpected address byte.");
            }

            if (mainnet && isP2PKH)
                decodedBytes[0] = 0x00;
            else if (mainnet && !isP2PKH)
                decodedBytes[0] = 0x05;
            else if (!mainnet && isP2PKH)
                decodedBytes[0] = 0x6f;
            else
                decodedBytes[0] = 0xc4;

            SHA256 hasher = SHA256.Create();
            byte[] checksum = hasher.ComputeHash(hasher.ComputeHash(decodedBytes, 0, 21));
            decodedBytes[21] = checksum[0];
            decodedBytes[22] = checksum[1];
            decodedBytes[23] = checksum[2];
            decodedBytes[24] = checksum[3];
            System.Text.StringBuilder ret = new System.Text.StringBuilder(40);
            for (int numZeros = 0; numZeros < 25 && decodedBytes[numZeros] == 0; numZeros++)
                ret.Append("1");
            {
                var temp = new List<byte>(decodedBytes);
                temp.Insert(0, 0);
                temp.Reverse();
                decodedBytes = temp.ToArray();
            }

            byte[] retArr = new byte[40];
            int retIdx = 0;
            BigInteger baseChanger = BigInteger.Abs(new BigInteger(decodedBytes));
            BigInteger baseFiftyEight = new BigInteger(58);
            BigInteger modulo = new BigInteger();
            while (!baseChanger.IsZero)
            {
                baseChanger = BigInteger.DivRem(baseChanger, baseFiftyEight, out modulo);
                retArr[retIdx++] = (byte)modulo;
            }

            for (retIdx--; retIdx >= 0; retIdx--)
            {
                ret.Append(CHARSET_BASE58[retArr[retIdx]]);
            }

            return ret.ToString();
        }
        #endregion

        #region IsAddress
        public static bool IsAddress(string _address, out byte? _version)
        {
            string _prefix = "";
            if (_address.IndexOf(':') > 0)
            {
                _prefix = _address.Split(':')[0].ToLower();
                _address = _address.Split(':')[1].ToLower();
            }

            if (_address.ToLower().StartsWith("bc1") || _address.ToLower().StartsWith("tb1")) { _version = null; return false; }
            if (Bitcoin.IsAddress(_address, out _version)) { return true; }

            bool _mainnet = false;
            if (_prefix == "bitcoincash:" || _prefix == "") { _mainnet = true; } else if (_prefix == "bchtest:") { _mainnet = false; } else { throw new Exception("Unexpected colon character."); }

            byte[] _decoded = new byte[42];
            for (int i = 0; i < _decoded.Length; i++)
            {
                int value = DICT_CASHADDR[_address[i]];
                if (value != -1) { _decoded[i] = (byte)value; } else { throw new Exception("Address contains unexpected character."); }
            }
            _decoded = Bits5to8(_decoded);

            bool _isP2PKH = false;
            switch (_decoded[0])
            {
                case 0x00:
                    _isP2PKH = true;
                    break;
                case 0x08:
                    _isP2PKH = false;
                    break;
                default:
                    throw new Exception("Unexpected address byte.");
            }

            if (_mainnet && _isP2PKH) { _decoded[0] = 0x00; } else if (_mainnet && !_isP2PKH) { _decoded[0] = 0x05; } else if (!_mainnet && _isP2PKH) { _decoded[0] = 0x6f; } else { _decoded[0] = 0xc4; }

            _version = _decoded[0];
            return true;
        }
        #endregion
    }
}