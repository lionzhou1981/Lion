using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Lion.SDK.Bitcoin.Nodes.Ethereum
{
    public class ContractABI : List<object>
    {
        public string MethodId;

        public ContractABI(string _methodId) : base() => this.MethodId = _methodId;

        #region ToData()
        /// <summary>
        /// Convert to eth_call data field.
        /// </summary>
        /// <returns>eth_call data field.</returns>
        public string ToData()
        {
            string[] _head = new string[this.Count];
            string[] _body = new string[this.Count];

            int _position = this.Count*32;
            for (int i = 0; i < this.Count; i++)
            {
                int _length = 0;
                byte[] _data = this.ToData(this[i], ref _length);

                if (_length == -1)
                {
                    _head[i] = HexPlus.ByteArrayToHexString(_data).PadLeft(64, '0');
                }
                else
                {
                    byte[] _positionByte = BitConverter.GetBytes(_position);
                    if (BitConverter.IsLittleEndian) { Array.Reverse(_positionByte); }
                    _head[i] = HexPlus.ByteArrayToHexString(_positionByte).PadLeft(64, '0');
                    _body[i] = HexPlus.ByteArrayToHexString(_data);
                    _position += _length;
                }
            }

            return this.MethodId + String.Concat(_head) + String.Concat(_body);
        }
        #endregion

        #region ToData(object,ref string)
        private byte[] ToData(object _item, ref int _length)
        {
            if (_item is Array)
            {
                #region Array
                Array _array = (Array)_item;

                byte[] _arrayCount = BitConverter.GetBytes(_array.Length);
                if (BitConverter.IsLittleEndian) { Array.Reverse(_arrayCount); }

                IList<byte[]> _dataList = new List<byte[]>();
                _dataList.Add(HexPlus.PadLeft(_arrayCount, 32));

                for (int i = 0; i < _array.Length; i++)
                {
                    int _itemLength = 0;
                    byte[] _itemData = this.ToData(_array.GetValue(i), ref _itemLength);

                    switch (_array.GetValue(i).GetType().ToString())
                    {
                        case "System.Bool":
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                        case "System.UInt16":
                        case "System.UInt32":
                        case "System.UInt64":
                        case "Lion.SDK.Ethereum.Address":
                        case "Lion.SDK.Ethereum.Number":
                            _dataList.Add(HexPlus.PadLeft(_itemData, 32));
                            break;
                        case "System.String":
                            _dataList.Add(_itemData);
                            break;
                    }
                }
                _length = _dataList.Sum(c => c.Length);
                return HexPlus.Concat(_dataList.ToArray());
                #endregion
            }
            else
            {
                #region Single
                _length = -1;
                byte[] _data = new byte[0];
                switch (_item.GetType().ToString())
                {
                    case "System.Boolean": _data = BitConverter.GetBytes((bool)_item); break;
                    case "System.Int16": _data = BitConverter.GetBytes((Int16)_item); break;
                    case "System.Int32": _data = BitConverter.GetBytes((Int32)_item); break;
                    case "System.Int64": _data = BitConverter.GetBytes((Int64)_item); break;
                    case "System.UInt16": _data = BitConverter.GetBytes((UInt16)_item); break;
                    case "System.UInt32": _data = BitConverter.GetBytes((UInt32)_item); break;
                    case "System.UInt64": _data = BitConverter.GetBytes((UInt64)_item); break;
                    case "Lion.SDK.Ethereum.Address": return ((Address)_item).ToData();
                    case "Lion.SDK.Ethereum.Number": _data = ((Number)_item).ToData();break;
                    case "System.String":
                        byte[] _stringBytes = Encoding.UTF8.GetBytes((string)_item);
                        byte[] _stringlengthBytes = BitConverter.GetBytes(_stringBytes.Length);
                        if (BitConverter.IsLittleEndian) { Array.Reverse(_stringlengthBytes); }

                        _stringBytes = HexPlus.PadRight(_stringBytes, (_stringBytes.Length / 32 + (_stringBytes.Length % 32 > 0 ? 1 : 0)) * 32);
                        _stringlengthBytes = HexPlus.PadLeft(_stringlengthBytes, 32);

                        byte[] _stringResult = HexPlus.Concat(_stringlengthBytes, _stringBytes);
                        _length = _stringResult.Length;
                        return _stringResult;
                    default:
                        throw new Exception($"Type {_item.GetType().ToString()} is not support.");
                }
                if (BitConverter.IsLittleEndian) { Array.Reverse(_data); }
                return _data;
                #endregion
            }
        }
        #endregion
    }
}
