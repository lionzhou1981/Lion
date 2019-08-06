using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Linq;

namespace Lion.Encrypt
{
    public class Secp256k1
    {
        #region ECP椭圆曲线
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
        #endregion

        #region PrivateKeyToPublicKey
        public string PrivateKeyToPublicKey(string _privateKey, out int _zeros, bool _compressKey = false)
        {
            _zeros = 0;

            if (_privateKey.Length != 64) { throw new Exception("Private key length must be 64."); }

            ECPoint _pubKey = Multiplication(BigInteger.Parse(_privateKey, NumberStyles.HexNumber));
            var _x = _pubKey.x.ToByteArray().ToList();
            _x.Reverse();
            var _y = _pubKey.y.ToByteArray().ToList();
            _y.Reverse();
            string _xPos = HexPlus.ByteArrayToHexString(_x.ToArray());
            string _yPos = HexPlus.ByteArrayToHexString(_y.ToArray());
            _zeros = (_xPos.StartsWith("00") ? 1 : 0) + (_yPos.StartsWith("00") ? 1 : 0);
            return string.Join("", "04", _xPos.StartsWith("00") ? _xPos.TrimStart('0') : _xPos, _yPos.StartsWith("00") ? _yPos.TrimStart('0') : _yPos);

        }
        #endregion
    }
}
