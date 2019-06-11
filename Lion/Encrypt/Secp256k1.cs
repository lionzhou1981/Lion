using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Lion.Encrypt
{
    public class Secp256k1
    {
        class ECPoint
        {
            public BigInteger x;
            public BigInteger y;
        }

        readonly BigInteger P = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", NumberStyles.HexNumber);
        readonly BigInteger N = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141", NumberStyles.HexNumber);
        readonly ECPoint G = new ECPoint()
        {
            x = BigInteger.Parse("79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798", NumberStyles.HexNumber),
            y = BigInteger.Parse("483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8", NumberStyles.HexNumber)
        };

        /// <summary>
        /// N - 1 
        /// </summary>
        private static readonly BigInteger N_1 = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364140", NumberStyles.HexNumber);

        /// <summary>
        /// P - 2
        /// </summary>
        private static readonly BigInteger P_2 = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2D", NumberStyles.HexNumber);
        private static readonly int MaxBit = 256;
        private static readonly ECPoint[] Power_G = new ECPoint[MaxBit];

        /// <summary>
        /// 为了提高运算效率，将G与2的n次方的积进行缓存
        /// </summary>
        public Secp256k1()
        {
            for (int i = 0; i < MaxBit; i++)
            {
                if (i == 0)
                {
                    Power_G[0] = G;
                }
                else
                {
                    Power_G[i] = Addition(Power_G[i - 1], Power_G[i - 1]);
                }
            }
        }

        /// <summary>
        /// 判断某点是否在椭圆曲线 secp256k1 上(mod P)
        /// </summary>
        bool IsOnCurve(ECPoint point)
        {
            BigInteger leftSide = BigInteger.Pow(point.y, 2) % P;
            BigInteger rightSide = (BigInteger.Pow(point.x, 3) + 7) % P;
            return leftSide == rightSide;
        }

        /// <summary>
        /// 取得x的倒数(mod P)
        /// 根据费尔玛小定理
        /// </summary>
        BigInteger GetReciprocalModP(BigInteger x)
        {
            BigInteger[] array = new BigInteger[MaxBit];
            BigInteger ret = 1;
            BigInteger temp = P_2;
            for (int i = 0; i < MaxBit; i++)
            {
                if (i == 0)
                {
                    array[0] = x;
                }
                else
                {
                    array[i] = BigInteger.Pow(array[i - 1], 2) % P;
                }

                if (!temp.IsEven)
                {
                    ret *= array[i];
                    ret %= P;
                }

                temp >>= 1;

                if (temp.IsZero)
                    break;
            }
            return ret;
        }

        /// <summary>
        /// 两个点相加
        /// </summary>
        ECPoint Addition(ECPoint a, ECPoint b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;

            BigInteger k;
            if (a.x == b.x)
            {
                if ((a.y + b.y) % P == 0)
                {
                    return null;
                }
                k = 3 * BigInteger.Pow(a.x, 2);
                k *= GetReciprocalModP(2 * a.y);
                k %= P;
            }
            else
            {
                k = (b.y + P - a.y) % P;
                k *= GetReciprocalModP((b.x + P - a.x) % P);
                k %= P;
            }
            ECPoint ret = new ECPoint();
            ret.x = (k * k + P - a.x + P - b.x) % P;
            ret.y = (k * (P + a.x - ret.x) + P - a.y) % P;
            return ret;
        }

        /// <summary>
        /// 对点G的标量乘法
        /// </summary>
        ECPoint Multiplication(BigInteger pri)
        {
            ECPoint ret = null;
            for (int i = 0; i < MaxBit; i++)
            {
                if (!pri.IsEven)
                {
                    if (ret == null)
                    {
                        ret = Power_G[i];
                    }
                    else
                    {
                        ret = Addition(ret, Power_G[i]);
                    }
                }

                pri >>= 1;

                if (pri.IsZero)
                    break;
            }
            return ret;
        }

        public Tuple<bool, string, int> PrivateKeyToPublicKey(string _privatekey)
        {
            try
            {
                if (_privatekey.Length != 64)
                    return new Tuple<bool, string, int>(false, "PrivateKey Length not 64(32bit)", 0);
                var _pubKey = Multiplication(BigInteger.Parse(_privatekey, NumberStyles.HexNumber));
                var _xPos = BytesToHexString(_pubKey.x.ToByteArray()).ToLower();
                var _yPos = BytesToHexString(_pubKey.y.ToByteArray()).ToLower();
                var _zeros = (_xPos.StartsWith("00") ? 1 : 0) + (_yPos.StartsWith("00") ? 1 : 0);
                return new Tuple<bool, string, int>(true, string.Join("", "04", _xPos.TrimStart('0'), _yPos.TrimStart('0')), _zeros);
            }
            catch (Exception _ex)
            {
                return new Tuple<bool, string, int>(false, _ex.StackTrace + "|" + _ex.Message, 0);
            }
        }

        /// <summary>
        /// 将字节数组输出为字符串
        /// </summary>
        public static string BytesToHexString(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                builder.Append(bytes[i].ToString("X2"));
            }
            return builder.ToString();
        }
        public static byte[] StringToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    }
}
