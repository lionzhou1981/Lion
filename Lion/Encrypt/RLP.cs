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

        public static readonly byte[] EMPTY_BYTE_ARRAY = new byte[0];
        private static int CalculateLength(int _length, byte[] _msgData, int _pos)
        {
            var pow = (byte)(_length - 1);
            var _currentLength = 0;
            for (var i = 1; i <= _length; ++i)
            {
                _currentLength += _msgData[_pos + i] << (8 * pow);
                pow--;
            }
            return _currentLength;
        }
        private static int SingleByteItem(byte[] msgData, int _currentPos, RLPItemList _list)
        {
            _list.Add(new RLPItem(new byte[] { msgData[_currentPos] }));
            _currentPos += 1;
            return _currentPos;
        }

        private static int NullItem(int _currentPos, RLPItemList _list)
        {
            _list.Add(new RLPItem(EMPTY_BYTE_ARRAY));
            _currentPos += 1;
            return _currentPos;
        }

        private static int ItemLessThan55Bytes(byte[] _msgData, int _currentPos, RLPItemList _list)
        {
            var _length = (byte)(_msgData[_currentPos] - OFFSET_SHORT_ITEM);
            var _item = new byte[_length];
            Array.Copy(_msgData, _currentPos + 1, _item, 0, _length);
            var _prefix = new byte[2];
            Array.Copy(_msgData, _currentPos, _prefix, 0, 2);
            _list.Add(new RLPItem(_item));
            _currentPos += 1 + _length;
            return _currentPos;
        }

        private static int ItemBiggerThan55Bytes(byte[] _msgData, int _currentPos, RLPItemList _list)
        {
            var _currentLength = (byte)(_msgData[_currentPos] - OFFSET_LONG_ITEM);
            var _length = CalculateLength(_currentLength, _msgData, _currentPos);
            var _data = new byte[_length];
            Array.Copy(_msgData, _currentPos + _currentLength + 1, _data, 0, _length);
            var _prefix = new byte[_currentLength + 1];
            Array.Copy(_msgData, _currentPos, _prefix, 0, _currentLength + 1);
            _list.Add(new RLPItem(_data));
            _currentPos += _currentLength + _length + 1;
            return _currentPos;
        }

        private static int ListLessThan55Bytes(byte[] _msgData, int _level, int _levelToIndex, int currentPos, RLPItemList _list)
        {
            var _length = _msgData[currentPos] - OFFSET_SHORT_LIST;
            var _dataLength = _length + 1;
            var _data = new byte[_length + 1];

            Array.Copy(_msgData, currentPos, _data, 0, _dataLength);

            var _childs = new RLPItemList { Data = _data };

            if (_length > 0)
                Decode(_msgData, _level + 1, currentPos + 1, currentPos + _dataLength,
                    _levelToIndex,
                    _childs);

            _list.Add(_childs);

            currentPos += _dataLength;
            return currentPos;
        }

        private static int ListBiggerThan55Bytes(byte[] _msgData, int _level, int _levelToIndex, int _currentPos, RLPItemList _list)
        {
            var _listlen = (byte)(_msgData[_currentPos] - OFFSET_LONG_LIST);
            var _lengthConvert = CalculateLength(_listlen, _msgData, _currentPos);

            var _dataLength = _listlen + _lengthConvert + 1;
            var _data = new byte[_dataLength];

            Array.Copy(_msgData, _currentPos, _data, 0, _dataLength);
            var _childList = new RLPItemList { Data = _data };

            Decode(_msgData, _level + 1, _currentPos + _listlen + 1,
                _currentPos + _dataLength, _levelToIndex,
                _childList);
            _list.Add(_childList);

            _currentPos += _dataLength;
            return _currentPos;
        }

        public static byte[][] DecodeList(byte[] _msgData, int _level, int _startPos, int _endPos, int _levelToIndex)
        {
            var _items = new RLPItemList();
            Decode(_msgData, _level, _startPos, _endPos, _levelToIndex, _items);
            return _items.Select(t=>t.Data).ToArray();
        }
            
        public static void Decode(byte[] _msgData, int _level, int _startPos,
          int _endPos, int _levelToIndex, RLPItemList _items)
        {
            if (_msgData == null || _msgData.Length == 0)
                return ;

            var currentData = new byte[_endPos - _startPos];
            Array.Copy(_msgData, _startPos, currentData, 0, currentData.Length);

            try
            {
                var _currentPos = _startPos;

                while (_currentPos < _endPos)
                {
                    if (_msgData[_currentPos] > OFFSET_LONG_LIST) //biglist
                    {
                        _currentPos = ListBiggerThan55Bytes(_msgData, _level, _levelToIndex, _currentPos, _items);
                        continue;
                    }
                    else if(_msgData[_currentPos] >= OFFSET_SHORT_LIST && _msgData[_currentPos] <= OFFSET_LONG_LIST) //small list
                    {
                        _currentPos = ListLessThan55Bytes(_msgData, _level, _levelToIndex, _currentPos, _items);
                        continue;
                    }
                    else if (_msgData[_currentPos] > OFFSET_LONG_ITEM && _msgData[_currentPos] < OFFSET_SHORT_LIST) //big item
                    {
                        _currentPos = ItemBiggerThan55Bytes(_msgData, _currentPos, _items);
                        continue;
                    }
                    else if (_msgData[_currentPos] > OFFSET_SHORT_ITEM && _msgData[_currentPos] <= OFFSET_LONG_ITEM) //small item
                    {
                        _currentPos = ItemLessThan55Bytes(_msgData, _currentPos, _items);
                        continue;
                    }

                    if (_msgData[_currentPos] == OFFSET_SHORT_ITEM) //null
                    {
                        _currentPos = NullItem(_currentPos,_items);
                        continue;
                    }
                    else if (_msgData[_currentPos] < OFFSET_SHORT_ITEM) //single
                        _currentPos = SingleByteItem(_msgData, _currentPos, _items);
                }
            }
            catch
            {
                throw new Exception("None rpl encode");
            }
        }
    }
}
