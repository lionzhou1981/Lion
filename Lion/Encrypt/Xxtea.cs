using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  Lion.Encrypt
{
    public class Xxtea
    {
        #region Encrypt
        public static string Encrypt(string _source, string _key)
        {
            byte[] _binaryData = System.Text.Encoding.UTF8.GetBytes(Base64Encode(_source));
            byte[] _binaryKey = System.Text.Encoding.UTF8.GetBytes(_key);
            if (_binaryData.Length == 0) { return ""; }

            return Base64.Encode(ToByteArray(Encrypt(ToUInt32Array(_binaryData, true), ToUInt32Array(_binaryKey, false)), false));
        }
        #endregion

        #region Decrypt
        public static string Decrypt(string _source, string _key)
        {
            if (_source.Length == 0) { return ""; }

            byte[] _binaryData = System.Convert.FromBase64String(_source);
            byte[] _binaryKey = System.Text.Encoding.UTF8.GetBytes(_key);

            return Base64Decode(System.Text.Encoding.UTF8.GetString(ToByteArray(Decrypt(ToUInt32Array(_binaryData, false), ToUInt32Array(_binaryKey, false)), true)));
        }
        #endregion

        #region Encrypt
        private static UInt32[] Encrypt(UInt32[] v, UInt32[] k)
        {
            Int32 n = v.Length - 1;
            if (n < 1)
            {
                return v;
            }
            if (k.Length < 4)
            {
                UInt32[] Key = new UInt32[4];
                k.CopyTo(Key, 0);
                k = Key;
            }
            UInt32 z = v[n], y = v[0], delta = 0x9E3779B9, sum = 0, e;
            Int32 p, q = 6 + 52 / (n + 1);
            while (q-- > 0)
            {
                sum = unchecked(sum + delta);
                e = sum >> 2 & 3;
                for (p = 0; p < n; p++)
                {
                    y = v[p + 1];
                    //z = unchecked(v[p] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                    z = unchecked(v[p] += (z >> 4 ^ y << 2) + (y >> 3 ^ z << 5) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                }
                y = v[0];
                //z = unchecked(v[n] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                z = unchecked(v[n] += (z >> 4 ^ y << 2) + (y >> 3 ^ z << 5) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
            }
            return v;
        }
        #endregion

        #region Decrypt
        private static UInt32[] Decrypt(UInt32[] v, UInt32[] k)
        {
            Int32 n = v.Length - 1;
            if (n < 1)
            {
                return v;
            }
            if (k.Length < 4)
            {
                UInt32[] Key = new UInt32[4];
                k.CopyTo(Key, 0);
                k = Key;
            }
            UInt32 z = v[n], y = v[0], delta = 0x9E3779B9, sum, e;
            Int32 p, q = 6 + 52 / (n + 1);
            sum = unchecked((UInt32)(q * delta));
            while (sum != 0)
            {
                e = sum >> 2 & 3;
                for (p = n; p > 0; p--)
                {
                    z = v[p - 1];
                    //y = unchecked(v[p] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                    y = unchecked(v[p] -= (z >> 4 ^ y << 2) + (y >> 3 ^ z << 5) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                }
                z = v[n];
                //y = unchecked(v[0] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                y = unchecked(v[0] -= (z >> 4 ^ y << 2) + (y >> 3 ^ z << 5) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                sum = unchecked(sum - delta);
            }
            return v;
        }
        #endregion

        #region ToUInt32Array
        private static UInt32[] ToUInt32Array(Byte[] Data, Boolean IncludeLength)
        {
            Int32 n = (((Data.Length & 3) == 0) ? (Data.Length >> 2) : ((Data.Length >> 2) + 1));
            UInt32[] Result;
            if (IncludeLength)
            {
                Result = new UInt32[n + 1];
                Result[n] = (UInt32)Data.Length;
            }
            else
            {
                Result = new UInt32[n];
            }
            n = Data.Length;
            for (Int32 i = 0; i < n; i++)
            {
                Result[i >> 2] |= (UInt32)Data[i] << ((i & 3) << 3);
            }
            return Result;
        }
        #endregion

        #region ToByteArray
        private static Byte[] ToByteArray(UInt32[] Data, Boolean IncludeLength)
        {
            Int32 n;
            if (IncludeLength)
            {
                n = (Int32)Data[Data.Length - 1];
            }
            else
            {
                n = Data.Length << 2;
            }
            Byte[] Result = new Byte[n];
            for (Int32 i = 0; i < n; i++)
            {
                Result[i] = (Byte)(Data[i >> 2] >> ((i & 3) << 3));
            }
            return Result;
        }
        #endregion

        #region Base64Decode
        public static string Base64Decode(string _source)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetString(Base64.Decode(_source));
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e.Message);
            }
        }
        #endregion

        #region Base64Encode
        public static string Base64Encode(string _source)
        {
            try
            {
                return Base64.Encode(System.Text.Encoding.UTF8.GetBytes(_source));
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Encode" + e.Message);
            }
        }
        #endregion
    }
}
