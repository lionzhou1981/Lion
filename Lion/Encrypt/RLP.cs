using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Lion.Encrypt
{
    public class RLP
    {
        private const int SIZE_THRESHOLD = 56;
        private const byte OFFSET_SHORT_ITEM = 0x80;
        private const byte OFFSET_LONG_ITEM = 0xb7;
        private const byte OFFSET_SHORT_LIST = 0xc0;
        private const byte OFFSET_LONG_LIST = 0xf7;

        #region EncodeList
        public static byte[] EncodeList(params byte[][] items)
        {
            if (items == null || (items.Length == 1 && items[0] == null)) { return new[] { OFFSET_SHORT_LIST }; }

            var totalLength = 0;
            for (var i = 0; i < items.Length; i++) { totalLength += items[i].Length; }


            byte[] data;

            int copyPos;

            if (totalLength < SIZE_THRESHOLD)
            {
                var dataLength = 1 + totalLength;
                data = new byte[dataLength];

                //single byte length
                data[0] = (byte)(OFFSET_SHORT_LIST + totalLength);
                copyPos = 1;
            }
            else
            {
                // length of length = BX
                // prefix = [BX, [length]]
                var tmpLength = totalLength;
                byte byteNum = 0;

                while (tmpLength != 0)
                {
                    ++byteNum;
                    tmpLength >>= 8;
                }

                tmpLength = totalLength;

                var lenBytes = new byte[byteNum];
                for (var i = 0; i < byteNum; ++i)
                    lenBytes[byteNum - 1 - i] = (byte)(tmpLength >> (8 * i));
                // first byte = F7 + bytes.length
                data = new byte[1 + lenBytes.Length + totalLength];

                data[0] = (byte)(OFFSET_LONG_LIST + byteNum);
                Array.Copy(lenBytes, 0, data, 1, lenBytes.Length);

                copyPos = lenBytes.Length + 1;
            }

            //Combine all elements
            foreach (var item in items)
            {
                Array.Copy(item, 0, data, copyPos, item.Length);
                copyPos += item.Length;
            }
            return data;
        }
        #endregion

        public static byte[] EncodeInt(int _int) => EncodeBigInteger(BigInteger.Parse(_int.ToString()));
        public static byte[] EncodeUInt(uint _uint) => EncodeBigInteger(BigInteger.Parse(_uint.ToString()));
        public static byte[] EncodeHex(string _hex) => EncodeBytes(HexPlus.HexStringToByteArray(_hex));
        public static byte[] EncodeString(string _string, bool _hex = true) => _hex ? EncodeHex(_string) : Encoding.UTF8.GetBytes(_string);

        #region EncodeBigInteger
        public static byte[] EncodeBigInteger(BigInteger _bi)
        {
            byte[] _bytes = _bi.ToByteArray();
            if (BitConverter.IsLittleEndian) { _bytes = _bytes.Reverse().ToArray(); }

            IList<byte> _trimed = new List<byte>();
            bool _previousZero = true;

            for (var i = 0; i < _bytes.Length; i++)
            {
                if (_previousZero && _bytes[i] == 0) { continue; }

                _previousZero = false;
                _trimed.Add(_bytes[i]);
            }
            _bytes = _trimed.ToArray();

            return RLP.EncodeBytes(_bytes);
        }
        #endregion

        #region EncodeBytes
        public static byte[] EncodeBytes(byte[] _source)
        {
            if (IsNullOrZero(_source)) { return new[] { OFFSET_SHORT_ITEM }; }
            if (IsSingleZero(_source)) { return _source; }
            if (_source.Length == 1 && _source[0] < 0x80) { return _source; }

            if (_source.Length < SIZE_THRESHOLD)
            {
                // length = 8X
                var length = (byte)(OFFSET_SHORT_ITEM + _source.Length);
                var data = new byte[_source.Length + 1];
                Array.Copy(_source, 0, data, 1, _source.Length);
                data[0] = length;

                return data;
            }
            else
            {
                // length of length = BX
                // prefix = [BX, [length]]
                var tmpLength = _source.Length;
                byte byteNum = 0;
                while (tmpLength != 0)
                {
                    ++byteNum;
                    tmpLength = tmpLength >> 8;
                }
                var lenBytes = new byte[byteNum];
                for (var i = 0; i < byteNum; ++i)
                    lenBytes[byteNum - 1 - i] = (byte)(_source.Length >> (8 * i));
                // first byte = F7 + bytes.length
                var data = new byte[_source.Length + 1 + byteNum];
                Array.Copy(_source, 0, data, 1 + byteNum, _source.Length);
                data[0] = (byte)(OFFSET_LONG_ITEM + byteNum);
                Array.Copy(lenBytes, 0, data, 1, lenBytes.Length);

                return data;
            }
        }
        #endregion

        public static bool IsNullOrZero(byte[] _array) { return _array == null || _array.Length == 0; }
        public static bool IsSingleZero(byte[] _array) { return _array.Length == 1 && _array[0] == 0; }

        #region DecodeList
        public static byte[][] DecodeList(byte[] _source)
        {
            return null;
        }
        #endregion
    }
}
