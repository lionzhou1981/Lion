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
            string[] _header = new string[this.Count];
            string _body = "";

            for (int i = 0; i < this.Count; i++)
            {
                _header[i] = HexPlus.ByteArrayToHexString(this.ToData(this[i], ref _body)).PadLeft(64, '0');
            }

            return this.MethodId + _header + _body;
        }
        #endregion

        #region ToData(object,ref string)
        private byte[] ToData(object _item, ref string _body)
        {
            string _data = "";
            byte[] _position = BitConverter.GetBytes(_body.Length);

            if (_item is Array)
            {
                Array _array = (Array)_item;
                _data += HexPlus.ByteArrayToHexString(BitConverter.GetBytes(_array.Length));

                string _subBody = "";
                for (int i = 0; i < _array.Length; i++)
                {
                    _data += HexPlus.ByteArrayToHexString(this.ToData(_array.GetValue(i), ref _subBody));
                }
            }
            else
            {
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
            }
            return _position;
        }
        #endregion
    }
}
