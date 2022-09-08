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
        public static byte[] EncodeList(params byte[][] _items)
        {
            if (_items == null || (_items.Length == 1 && _items[0] == null)) { return new[] { OFFSET_SHORT_LIST }; }

            var _total = 0;
            for (var i = 0; i < _items.Length; i++) { _total += _items[i].Length; }

            byte[] _data;
            int _pos;
            if (_total < SIZE_THRESHOLD)
            {
                _data = new byte[1 + _total];
                _data[0] = (byte)(OFFSET_SHORT_LIST + _total);
                _pos = 1;
            }
            else
            {
                // length of length = BX
                // prefix = [BX, [length]]
                var _length = _total;
                byte _byteCount = 0;
                while (_length != 0) { ++_byteCount; _length >>= 8; }

                _length = _total;

                var _lengthBytes = new byte[_byteCount];
                for (var i = 0; i < _byteCount; ++i) { _lengthBytes[_byteCount - 1 - i] = (byte)(_length >> (8 * i)); }
                    
                // first byte = F7 + bytes.length
                _data = new byte[1 + _lengthBytes.Length + _total];

                _data[0] = (byte)(OFFSET_LONG_LIST + _byteCount);
                Array.Copy(_lengthBytes, 0, _data, 1, _lengthBytes.Length);

                _pos = _lengthBytes.Length + 1;
            }

            //Combine all elements
            foreach (var item in _items)
            {
                Array.Copy(item, 0, _data, _pos, item.Length);
                _pos += item.Length;
            }
            return _data;
        }
        #endregion

        public static byte[] EncodeInt(int _int) => EncodeBigInteger(BigInteger.Parse(_int.ToString()));
        public static byte[] EncodeUInt(uint _uint) => EncodeBigInteger(BigInteger.Parse(_uint.ToString()));
        public static byte[] EncodeHex(string _hex) => EncodeBytes(HexPlus.HexStringToByteArray(_hex));

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
            if (_source == null || _source.Length == 0) { return new[] { OFFSET_SHORT_ITEM }; }
            if (_source.Length == 1 && _source[0] < 0x80) { return _source; }

            if (_source.Length < SIZE_THRESHOLD)
            {
                // length = 8X
                var _length = (byte)(OFFSET_SHORT_ITEM + _source.Length);
                var _data = new byte[_source.Length + 1];
                Array.Copy(_source, 0, _data, 1, _source.Length);
                _data[0] = _length;
                return _data;
            }
            else
            {
                // length of length = BX
                // prefix = [BX, [length]]
                var _length = _source.Length;
                byte _byteCount = 0;
                while (_length != 0) { ++_byteCount; _length = _length >> 8; }

                var _lenghtBytes = new byte[_byteCount];
                for (var i = 0; i < _byteCount; ++i) { _lenghtBytes[_byteCount - 1 - i] = (byte)(_source.Length >> (8 * i)); }
                    
                // first byte = F7 + bytes.length
                var _data = new byte[_source.Length + 1 + _byteCount];
                Array.Copy(_source, 0, _data, 1 + _byteCount, _source.Length);
                _data[0] = (byte)(OFFSET_LONG_ITEM + _byteCount);
                Array.Copy(_lenghtBytes, 0, _data, 1, _lenghtBytes.Length);

                return _data;
            }
        }
        #endregion

        #region DecodeList
        public static byte[][] DecodeList(byte[] _source)
        {
            if (_source == null || _source.Length == 0) { return null; }

            var _data = new byte[0];
            if (_source[0] > OFFSET_LONG_LIST) //biglist
            {
                var _headLength = (byte)(_source[0] - OFFSET_LONG_LIST);
                var _dataLength = DecodeLength(_headLength, _source, 0);
                _data = new byte[_dataLength];
                Array.Copy(_source, _headLength + 1, _data, 0, _data.Length);
            }
            else if (_source[0] >= OFFSET_SHORT_LIST && _source[0] <= OFFSET_LONG_LIST) //small list
            {
                var _dataLength = _source[0] - OFFSET_SHORT_LIST;
                _data = new byte[_dataLength];
                Array.Copy(_source, 1, _data, 0, _dataLength);
            }
            else
            {
                return new byte[0][];
            }

            int _pos = 0;
            IList<byte[]> _decoded = new List<byte[]>();
            while (_pos < _data.Length)
            {
                if (_data[_pos] > OFFSET_LONG_LIST) //biglist
                {
                    var _headLength = (byte)(_data[_pos] - OFFSET_LONG_LIST);
                    var _itemLength = DecodeLength(_headLength, _data, _pos);
                    var _item = new byte[_itemLength + _headLength + 1];
                    Array.Copy(_data, _pos, _item, 0, _item.Length);
                    _pos += _headLength + _itemLength + 1;
                    _decoded.Add(_data);
                }
                else if (_data[_pos] >= OFFSET_SHORT_LIST && _data[_pos] <= OFFSET_LONG_LIST) //small list
                {
                    var _itemLength = _data[_pos] - OFFSET_SHORT_LIST;
                    var _item = new byte[_itemLength + 1];
                    Array.Copy(_data, _pos, _data, 0, _itemLength);
                    _pos += _itemLength + 1;
                    _decoded.Add(_data);
                }
                else if (_data[_pos] > OFFSET_LONG_ITEM && _data[_pos] < OFFSET_SHORT_LIST) //big item
                {
                    var _headLength = (byte)(_data[_pos] - OFFSET_LONG_ITEM);
                    var _itemlength = DecodeLength((int)_headLength, _data, (int)_pos);
                    var _item = new byte[_itemlength];
                    Array.Copy(_data, _pos + _headLength + 1, _item, 0, _itemlength);
                    _pos += _headLength + _itemlength + 1;
                    _decoded.Add(_item);
                }
                else if (_data[_pos] > OFFSET_SHORT_ITEM && _data[_pos] <= OFFSET_LONG_ITEM) //small item
                {
                    var _length = (byte)(_data[_pos] - OFFSET_SHORT_ITEM);
                    var _item = new byte[_length];
                    Array.Copy(_data, _pos + 1, _item, 0, _length);
                    _pos += (1 + _length);
                    _decoded.Add(_item);
                }
                else if (_data[_pos] == OFFSET_SHORT_ITEM) //null
                {
                    _decoded.Add(new byte[0]);
                    _pos += 1;
                }
                else if (_data[_pos] < OFFSET_SHORT_ITEM) //single
                {
                    _decoded.Add(new byte[] { _data[_pos] });
                    _pos += 1;
                }
            }

            return _decoded.ToArray();
        }
        #endregion

        #region DecodeLength
        private static int DecodeLength(int _length, byte[] _source, int _pos)
        {
            var pow = (byte)(_length - 1);
            var _currentLength = 0;
            for (var i = 1; i <= _length; ++i)
            {
                _currentLength += _source[_pos + i] << (8 * pow);
                pow--;
            }
            return _currentLength;
        }
        #endregion

        public static string DecodeHex(byte[] _source) => HexPlus.ByteArrayToHexString(_source);

        #region DecodeBytes
        public static byte[] DecodeBytes(byte[] _source)
        {
            if (_source == null || _source.Length == 0) { return new byte[0]; }
            if (_source.Length == 1 && _source[0] < 0x80) { return _source; }

            if (_source[0] > OFFSET_LONG_ITEM)
            {
                var _headLength = (byte)(_source[0] - OFFSET_LONG_ITEM);
                var _length = DecodeLength((int)_headLength, _source, 0);
                var _data = new byte[_length];
                Array.Copy(_source, 1 + _headLength, _data, 0, _data.Length);
                return _data;
            }
            else if (_source[0] > OFFSET_SHORT_ITEM && _source[0] <= OFFSET_LONG_ITEM)
            {
                var _length = _source[0] - OFFSET_SHORT_ITEM;
                var _data = new byte[_length];
                Array.Copy(_source, 1, _data, 0, _data.Length);
                return _data;
            }
            return new byte[0];
        }
        #endregion
    }
}
