using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lion.SDK.Ethereum
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
            string _body = "";

            for (int i = 0; i < this.Count; i++)
            {
                _head[i] = HexPlus.ByteArrayToHexString(this.ToData(this[i], ref _body)).PadLeft(64, '0');
            }

            return this.MethodId + String.Concat(_head) + _body;
        }
        #endregion

        #region ToData(object,ref string)
        private byte[] ToData(object _item, ref string _body)
        {
            byte[] _position = BitConverter.GetBytes(_body.Length);

            if (_item is Array)
            {
                #region array
                Array _array = (Array)_item;
                _body += HexPlus.ByteArrayToHexString(BitConverter.GetBytes(_array.Length));

                for (int i = 0; i < _array.Length; i++)
                {
                    string _subBody = "";
                    string _subData = HexPlus.ByteArrayToHexString(this.ToData(_array.GetValue(i), ref _subBody));

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
                            _body += _subData;
                            break;

                        case "System.String":
                            _body += _subBody;
                            break;
                    }
                }
                #endregion
            }
            else
            {
                #region single
                string _data = "";
                switch (_item.GetType().ToString())
                {
                    case "System.Bool": return BitConverter.GetBytes((bool)_item);
                    case "System.Int16": return BitConverter.GetBytes((Int16)_item);
                    case "System.Int32": return BitConverter.GetBytes((Int32)_item);
                    case "System.Int64": return BitConverter.GetBytes((Int64)_item);
                    case "System.UInt16": return BitConverter.GetBytes((UInt16)_item);
                    case "System.UInt32": return BitConverter.GetBytes((UInt32)_item);
                    case "System.UInt64": return BitConverter.GetBytes((UInt64)_item);
                    case "Lion.SDK.Ethereum.Address": return ((Address)_item).ToData();

                    case "System.String":
                        _data += HexPlus.ByteArrayToHexString(BitConverter.GetBytes(((string)_item).Length)).PadLeft(64, '0');
                        _data += HexPlus.ByteArrayToHexString(Encoding.UTF8.GetBytes((string)_item));
                        break;
                }
                _body += _data.PadRight((_data.Length / 64 + (_data.Length % 64 > 0 ? 1 : 0)) * 64, '0');
                #endregion
            }
            return _position;
        }
        #endregion
    }
}
