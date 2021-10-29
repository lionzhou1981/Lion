using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Lion.Encrypt
{
    public class RLP
    {
        private const int SIZE_THRESHOLD = 56;
        private const byte OFFSET_SHORT_ITEM = 0x80;
        private const byte OFFSET_LONG_ITEM = 0xb7;
        private const byte OFFSET_SHORT_LIST = 0xc0;
        private const byte OFFSET_LONG_LIST = 0xf7;

        private static readonly byte[] EMPTY_BYTE_ARRAY = new byte[0];
        private static readonly byte[] ZERO_BYTE_ARRAY = { 0 };

        public static byte[] EncodeElement(byte[] srcData)
        {
            if (IsNullOrZeroArray(srcData))
                return new[] { OFFSET_SHORT_ITEM };
            if (IsSingleZero(srcData))
                return srcData;
            if (srcData.Length == 1 && srcData[0] < 0x80)
                return srcData;
            if (srcData.Length < SIZE_THRESHOLD)
            {
                // length = 8X
                var length = (byte)(OFFSET_SHORT_ITEM + srcData.Length);
                var data = new byte[srcData.Length + 1];
                Array.Copy(srcData, 0, data, 1, srcData.Length);
                data[0] = length;

                return data;
            }
            else
            {
                // length of length = BX
                // prefix = [BX, [length]]
                var tmpLength = srcData.Length;
                byte byteNum = 0;
                while (tmpLength != 0)
                {
                    ++byteNum;
                    tmpLength = tmpLength >> 8;
                }
                var lenBytes = new byte[byteNum];
                for (var i = 0; i < byteNum; ++i)
                    lenBytes[byteNum - 1 - i] = (byte)(srcData.Length >> (8 * i));
                // first byte = F7 + bytes.length
                var data = new byte[srcData.Length + 1 + byteNum];
                Array.Copy(srcData, 0, data, 1 + byteNum, srcData.Length);
                data[0] = (byte)(OFFSET_LONG_ITEM + byteNum);
                Array.Copy(lenBytes, 0, data, 1, lenBytes.Length);

                return data;
            }
        }

        public static bool IsNullOrZeroArray(byte[] array)
        {
            return array == null || array.Length == 0;
        }

        public static bool IsSingleZero(byte[] array)
        {
            return array.Length == 1 && array[0] == 0;
        }

        private static int CalculateLength(int lengthOfLength, byte[] msgData, int pos)
        {
            var pow = (byte)(lengthOfLength - 1);
            var length = 0;
            for (var i = 1; i <= lengthOfLength; ++i)
            {
                length += msgData[pos + i] << (8 * pow);
                pow--;
            }
            return length;
        }

        public static byte[] EncodeList(params byte[][] items)
        {
            if (items == null || (items.Length == 1 && items[0] == null))
                return new[] { OFFSET_SHORT_LIST };

            var totalLength = 0;
            for (var i = 0; i < items.Length; i++)
                totalLength += items[i].Length;

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
                    tmpLength = tmpLength >> 8;
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

        public static string EncodeList(params string[] _params)
        {
            return BitConverter.ToString(EncodeList(_params.Select(t => EncodeElement(Lion.HexPlus.HexStringToByteArray(t))).ToArray())).Replace("-", "").ToLower();
        }

        public static byte[] EncodeListToByte(params string[] _params)
        {
            return EncodeList(_params.Select(t => EncodeElement(Lion.HexPlus.HexStringToByteArray(t))).ToArray());
        }
    }
}
