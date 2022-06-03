using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Lion.CryptoCurrency.Ethereum
{
    public class ContractABI : List<object>
    {

        public static ContractABI Decode(Dictionary<string,List<string>> _mathodsAndArgs,string _hexData)
        {
            _hexData = _hexData.StartsWith("0x") ? _hexData.Substring(2) : _hexData;
            var _methodName = _hexData.Substring(0, 8);
            _hexData = _hexData.Substring(8);
            var _methods = _mathodsAndArgs.Where(t => t.Key == _methodName || t.Key == "0x" + _methodName);
            if (_methods.Count() == 0)
                throw new Exception("No method equal the hexdata");
            else if(_methods.Count()>1)
                throw new Exception("Too many method equal the hexdata");
            var _methodArgs = _methods.First().Value;
            object[] _tempArray = new object[_methodArgs.Count];
            for (int i = 0; i < _methodArgs.Count; i++)
            {
                var _type = _methodArgs[i];
                var _value = _hexData.Substring(0, 64);
                if (_type == "address")
                {
                    _value = _value.TrimStart('0');
                    if (_value.Length % 2 != 0)
                        _value = "0" + _value;
                    _tempArray[i] = "0x" + _value;
                }
                else if (!_type.Contains("[") && !_type.Contains("memory") &&!_type.Contains("calldata"))
                    _tempArray[i] = Data2Obj(_type, _hexData);
                _hexData = _hexData.Substring(64);
            }
            //_hexData = _hexData.Substring(_methodArgs.Count * 64);//skip type define
            for (int i=0;i<_methodArgs.Count;i++)
            {
                var _type = _methodArgs[i];
                if (_tempArray[i] != null)
                    continue;
                var _array = _type.Contains("[");
                if (_array)
                {
                    var _isString = _type.Contains("string");
                    var _arrayLen = int.Parse(_hexData.Substring(0, 64), System.Globalization.NumberStyles.HexNumber);
                    int[] _stringZeroPadLength = new int[_arrayLen];
                    _hexData = _hexData.Substring((_isString ? _arrayLen + 1 : 1) * 64);
                    object[] _subArray = new object[_arrayLen];
                    for (int j = 0; j < _arrayLen; j++)
                    {
                        int _dataLength = 64;
                        if (_isString)
                        {
                            int _stringLength = int.Parse(_hexData.Substring(0, 64), System.Globalization.NumberStyles.HexNumber)*2;
                            _dataLength = _stringLength % 64 > 0 ? (_stringLength / 64 + 1) * 64 : _stringLength;
                        }
                        _hexData = _isString ? _hexData.Substring(64) : _hexData;
                        _subArray[j] = Data2Obj(_type, _hexData, _dataLength);
                        _hexData = _hexData.Substring(_dataLength);
                    }
                    _tempArray[i] = _subArray;
                }
                else
                {
                    int _dataLength = _type == "string"? int.Parse(_hexData.Substring(0, 64), System.Globalization.NumberStyles.HexNumber): 64;
                    _hexData = _hexData.Substring(64);
                    _tempArray[i] = Data2Obj(_type, _hexData, _dataLength);
                    _hexData = _hexData.Substring(_dataLength);
                }
            }
            ContractABI _re = new ContractABI("0x" + _methodName);
            _re.AddRange(_tempArray);
            return _re;
        }

        private static Regex RegIntRegion = new Regex("int(\\d+)");
        private static object Data2Obj(string _type,string _hexData,int _dataLength = 64)
        {
            _type = _type.Replace("[", "").Replace("]", "");
            if (_type.Contains("int") && RegIntRegion.IsMatch(_type))
            {
                Match _match = RegIntRegion.Match(_type);
                int _len = int.Parse(_match.Groups[1].Value);
                string _lenAddin = _len >= 8 && _len <= 16 ? "16" : _len >= 32 && _len <= 32 ? "32" : "256";
                _type = _match.Groups[0].Value+ _lenAddin;
            }
            var _value = _hexData.Substring(0, _dataLength);
            switch (_type)
            {
                case "int":
                case "uint":
                case "int16":
                case "uint16":
                    return int.Parse(_value, System.Globalization.NumberStyles.HexNumber);
                case "int32":
                case "uint32":
                    return uint.Parse(_value, System.Globalization.NumberStyles.HexNumber);
                case "int256":
                case "uint256":
                    return ulong.Parse(_value, System.Globalization.NumberStyles.HexNumber);
                case "bool":
                    return int.Parse(_value) == 1;
                case "string":
                    return DataToString(_value);
            }
            return null;
        }

        private static string DataToString(string _hexData)
        {
            StringBuilder _resultString = new StringBuilder();
            for(int i=0;i<_hexData.Length;i+=2)
            {
                var _value = _hexData.Substring(i, 2);
                if (_value == "00")
                    break;
                _resultString.Append((char)int.Parse(_value, System.Globalization.NumberStyles.HexNumber));
            }
            return _resultString.ToString();
        }

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

            int _position = this.Count * 32;
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
                        case "Lion.SDK.Bitcoin.Nodes.Ethereum.Address":
                        case "Lion.SDK.Bitcoin.Nodes.Ethereum.Number": _dataList.Add(HexPlus.PadLeft(_itemData, 32)); break;
                        case "System.String": _dataList.Add(_itemData); break;
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
                    case "System.Int16": _data = BitConverter.GetBytes((short)_item); break;
                    case "System.Int32": _data = BitConverter.GetBytes((int)_item); break;
                    case "System.Int64": _data = BitConverter.GetBytes((long)_item); break;
                    case "System.UInt16": _data = BitConverter.GetBytes((ushort)_item); break;
                    case "System.UInt32": _data = BitConverter.GetBytes((uint)_item); break;
                    case "System.UInt64": _data = BitConverter.GetBytes((UInt64)_item); break;
                    case "Lion.CryptoCurrency.Ethereum.Number": _data = ((Number)_item).ToBytes(); break;
                    case "Lion.CryptoCurrency.Ethereum.Address": return ((Address)_item).ToBytes();
                    case "System.String":
                        byte[] _stringBytes = Encoding.UTF8.GetBytes((string)_item);
                        byte[] _stringlengthBytes = BitConverter.GetBytes(_stringBytes.Length);
                        if (BitConverter.IsLittleEndian) { Array.Reverse(_stringlengthBytes); }

                        _stringBytes = HexPlus.PadRight(_stringBytes, (_stringBytes.Length / 32 + (_stringBytes.Length % 32 > 0 ? 1 : 0)) * 32);
                        _stringlengthBytes = HexPlus.PadLeft(_stringlengthBytes, 32);

                        byte[] _stringResult = HexPlus.Concat(_stringlengthBytes, _stringBytes);
                        _length = _stringResult.Length;
                        return _stringResult;
                    default: throw new Exception($"Type {_item.GetType()} is not support.");
                }
                if (BitConverter.IsLittleEndian) { Array.Reverse(_data); }
                return _data;
                #endregion
            }
        }
        #endregion

        #region Parse(byte[])
        public static ContractABI FromData(string _hex) => FromData(HexPlus.HexStringToByteArray(_hex));

        public static ContractABI FromData(byte[] _bytes)
        {
            return null;
        }
        #endregion
    }
}
