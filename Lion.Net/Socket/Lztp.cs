using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Lion.Net.Sockets
{
    public enum FieldType : byte
    {
        Undefined = 0x00,
        Null = 0x01,
        Boolean = 0x02,
        Int8 = 0x03,
        UInt8 = 0x04,
        Int16 = 0x05,
        UInt16 = 0x06,
        Int32 = 0x07,
        UInt32 = 0x08,
        Single = 0x09,
        Double = 0x0A,
        StringASCII = 0x0B,
        StringUTF8 = 0x0C,
        ByteArray = 0x0D,
        DataTable = 0x0E,
        Int64 = 0x10,
        UInt64 = 0x11,
        DateTime = 0x1A,
        Guid = 0x1B,
        StringArray = 0x1C,
        Decimal = 0x1D
    }

    public class Lztp : ISocketProtocol
    {
        private byte[] Head = new byte[0];
        private UInt32 CRCKey = 0xEDB88320;
        private UInt32[] CRCTable;

        #region 构造函数
        public Lztp(string _head, uint _keepAlive = 0)
        {
            if (_head.Length != 4) { throw new Exception("Protocol string must be 4-length char."); }

            byte[] _bytes = new byte[4];
            _bytes[0] = (byte)_head[0];
            _bytes[1] = (byte)_head[1];
            _bytes[2] = (byte)_head[2];
            _bytes[3] = (byte)_head[3];
            this.Head = _bytes;

            this.InitCRCTable();

            this.KeepAlive = _keepAlive;
        }

        public Lztp(byte _head1, byte _head2, byte _head3, byte _head4, uint _keepAlive = 0)
        {
            this.Head = new byte[]{ _head1, _head2, _head3, _head4 };

            this.InitCRCTable();

            this.KeepAlive = _keepAlive;
        }

        public Lztp(UInt32 _head, uint _keepAlive = 0)
        {
            byte[] _bytes = BitConverter.GetBytes(_head);
            Array.Reverse(_bytes);
            this.Head = _bytes;

            this.InitCRCTable();

            this.KeepAlive = _keepAlive;
        }
        #endregion

        #region InitCRCTable
        private void InitCRCTable()
        {
            this.CRCTable = new UInt32[256];
            UInt32 _crc;
            for (UInt32 i = 0; i < 256; i++)
            {
                _crc = i;
                for (UInt32 j = 0; j < 8; j++)
                {
                    _crc = ((_crc & 1) == 1 ? ((_crc >> 1) ^ this.CRCKey) : (_crc >> 1));
                }
                this.CRCTable[i] = _crc;
            }
        }
        #endregion

        #region KeepAlive
        public uint KeepAlive { get; set; } = 0;
        #endregion

        #region KeepAlivePackage
        public object KeepAlivePackage
        {
            get
            {
                return new LztpPackage(ushort.MaxValue, ushort.MaxValue, uint.MaxValue, new object[] { DateTime.Now.ToString("yyyyMMddHHmmssfff") });
            }
        }
        #endregion

        #region IsKeepAlivePackage
        public bool IsKeepAlivePackage(object _object, object _socket = null)
        {
            LztpPackage _packageCurrent = (LztpPackage)_object;
            LztpPackage _packageKeepAlive = (LztpPackage)this.KeepAlivePackage;
            return (_packageCurrent.Command1 == _packageKeepAlive.Command1 && _packageCurrent.Command2 == _packageKeepAlive.Command2 && _packageCurrent.CommandId == _packageKeepAlive.CommandId);
        }
        #endregion

        #region Check
        /// <summary>
        /// 实现接口：是否是一个完整的包
        /// </summary>
        /// <param name="_byteArray">检查的数据流</param>
        /// <param name="_completely">是否完全检查数据包</param>
        /// <returns>是否检测成功</returns>
        public bool Check(byte[] _byteArray, bool _completely, SocketSession _session = null)
        {
            // 检查读取头大小
            if (_byteArray == null || _byteArray.Length < 64)
            {
                return false;
            }
            if (_byteArray[0] != this.Head[0] || _byteArray[1] != this.Head[1] || _byteArray[2] != this.Head[2] || _byteArray[3] != this.Head[3])
            {
                return false;
            }

            // 读取头部信息
            byte[] _packageHeader = new byte[64];
            Array.Copy(_byteArray, 0, _packageHeader, 0, 64);

            byte[] _packageLengthArray = new byte[4];
            Array.Copy(_packageHeader, 8, _packageLengthArray, 0, 4);
            uint _packageLength = this.ByteArrayToUInt32(_packageLengthArray);

            byte[] _packageHeaderCRC32 = new byte[4];
            Array.Copy(_packageHeader, 60, _packageHeaderCRC32, 0, 4);

            // 检查头部CRC32
            byte[] _packageHeaderContent = new byte[60];
            Array.Copy(_packageHeader, 0, _packageHeaderContent, 0, 60);
            if (this.GetCRC32(_packageHeaderContent) != this.ByteArrayToUInt32(_packageHeaderCRC32))
            {
                return false;
            }

            // 检查数据头CRC32
            try
            {
                if (_completely && _packageLength > uint.Parse("64"))
                {
                    if (uint.Parse(_byteArray.Length.ToString()) < _packageLength) { return false; }

                    byte _packageBodyType = _byteArray[64];

                    byte[] _packageBodyLengthArray = new byte[4];
                    Array.Copy(_byteArray, 65, _packageBodyLengthArray, 0, 4);
                    uint _packageBodyLength = this.ByteArrayToUInt32(_packageBodyLengthArray);

                    byte[] _packageBodyCRC32Array = new byte[4];
                    Array.Copy(_byteArray, 69, _packageBodyCRC32Array, 0, 4);
                    uint _packageBodyCRC32 = this.ByteArrayToUInt32(_packageBodyCRC32Array);

                    byte[] _packageBodyFieldCountArray = new byte[4];
                    Array.Copy(_byteArray, 73, _packageBodyFieldCountArray, 0, 4);
                    uint _packageBodyFieldCount = this.ByteArrayToUInt32(_packageBodyFieldCountArray);

                    byte[] _packageBodyArray = new byte[_packageBodyLength];
                    Array.Copy(_byteArray, (long)(77 + _packageBodyFieldCount), _packageBodyArray, 0, _packageBodyLength);
                    if (this.GetCRC32(_packageBodyArray) != _packageBodyCRC32) { return false; }
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        #endregion

        #region EnPackage
        /// <summary>
        /// 数据打包
        /// </summary>
        /// <param name="_objects">需要打包的对象</param>
        /// <returns>打包后的数据</returns>
        public byte[] EnPackage(object _object, SocketSession _session = null)
        {
            if (_object == null) { return new byte[0]; }

            LztpPackage _package = (LztpPackage)_object;
            if (_package.TimestampSend == 0)
            {
                _package.TimestampSend = this.GetTimestamp(DateTime.UtcNow);
            }
            else
            {
                _package.TimestampResponse = this.GetTimestamp(DateTime.UtcNow);
            }

            MemoryStream _body_stream = new MemoryStream();
            byte[] _body_Fields = new byte[_package.Fields.Length];

            #region Body_Fields,Body_Content
            for (int i = 0; i < _package.Fields.Length; i++)
            {
                _body_Fields[i] = this.AppendToByteArray(ref _body_stream, _package.Fields[i]);
            }
            byte[] _body_Content = _body_stream.ToArray();
            _body_stream.Close();
            #endregion

            byte[] _body_Header = new byte[13 + _body_Fields.Length];
            #region Body_Header
            _body_Header[0] = 0x00;
            Array.Copy(this.UInt32ToByteArray(UInt32.Parse(_body_Content.Length.ToString())), 0, _body_Header, 1, 4);
            Array.Copy(this.UInt32ToByteArray(this.GetCRC32(_body_Content)), 0, _body_Header, 5, 4);
            Array.Copy(this.UInt32ToByteArray(UInt32.Parse(_package.Fields.Length.ToString())), 0, _body_Header, 9, 4);
            Array.Copy(_body_Fields, 0, _body_Header, 13, _body_Fields.Length);
            #endregion

            byte[] _packageArray = new byte[64 + _body_Header.Length + _body_Content.Length];
            byte[] _head_Content = new byte[60];
            #region Head_Content
            Array.Copy(this.Head, 0, _head_Content, 0, 4);
            Array.Copy(this.UInt32ToByteArray(_package.Version), 0, _head_Content, 4, 4);
            Array.Copy(this.UInt32ToByteArray(UInt32.Parse(_packageArray.Length.ToString())), 0, _head_Content, 8, 4);
            Array.Copy(this.UInt32ToByteArray(this.GetCRC32(_body_Header)), 0, _head_Content, 12, 4);
            Array.Copy(this.UInt16ToByteArray(_package.Command1), 0, _head_Content, 16, 2);
            Array.Copy(this.UInt16ToByteArray(_package.Command2), 0, _head_Content, 18, 2);
            Array.Copy(this.UInt32ToByteArray(_package.CommandId), 0, _head_Content, 20, 4);
            Array.Copy(this.DoubleToByteArray(_package.TimestampSend), 0, _head_Content, 24, 8);
            Array.Copy(this.DoubleToByteArray(_package.TimestampReceive), 0, _head_Content, 32, 8);
            Array.Copy(this.DoubleToByteArray(_package.TimestampResponse), 0, _head_Content, 40, 8);
            for (int i = 48; i < 60; i++)
            {
                _head_Content[i] = 0x0;
            }
            #endregion

            byte[] _head_CRC32 = new byte[4];
            #region Head_CRC32
            Array.Copy(this.UInt32ToByteArray(this.GetCRC32(_head_Content)), 0, _head_CRC32, 0, 4);
            #endregion

            Array.Copy(_head_Content, 0, _packageArray, 0, _head_Content.Length);
            Array.Copy(_head_CRC32, 0, _packageArray, _head_Content.Length, _head_CRC32.Length);
            Array.Copy(_body_Header, 0, _packageArray, _head_Content.Length + _head_CRC32.Length, _body_Header.Length);
            Array.Copy(_body_Content, 0, _packageArray, _head_Content.Length + _head_CRC32.Length + _body_Header.Length, _body_Content.Length);

            return _packageArray;
        }
        #endregion

        #region DePackage
        /// <summary>
        /// 数据解包
        /// </summary>
        /// <param name="_byteArray">解包的数据流</param>
        /// <param name="_packageSize">输出数据包整体大小</param>
        /// <returns>解包后的对象</returns>
        public object DePackage(byte[] _byteArray, out uint _packageSize, bool _completely, SocketSession _session = null)
        {
            if (!this.Check(_byteArray, _completely)) { throw new Exception("Can not depackage this stream."); }

            LztpPackage _package = new LztpPackage();
            byte[] _packageHeader = new byte[64];
            Array.Copy(_byteArray, 0, _packageHeader, 0, 64);
            byte[] _packageVersion = new byte[4];
            Array.Copy(_packageHeader, 4, _packageVersion, 0, 4);
            byte[] _packageLength = new byte[4];
            Array.Copy(_packageHeader, 8, _packageLength, 0, 4);
            byte[] _packageBodyCRC32 = new byte[4];
            Array.Copy(_packageHeader, 12, _packageBodyCRC32, 0, 4);
            byte[] _packageCommand1 = new byte[2];
            Array.Copy(_packageHeader, 16, _packageCommand1, 0, 2);
            byte[] _packageCommand2 = new byte[2];
            Array.Copy(_packageHeader, 18, _packageCommand2, 0, 2);
            byte[] _packageCommandId = new byte[4];
            Array.Copy(_packageHeader, 20, _packageCommandId, 0, 4);
            byte[] _packageTimestampSend = new byte[8];
            Array.Copy(_packageHeader, 24, _packageTimestampSend, 0, 8);
            byte[] _packageTimestampReceive = new byte[8];
            Array.Copy(_packageHeader, 32, _packageTimestampReceive, 0, 8);
            byte[] _packageTimestampResponse = new byte[8];
            Array.Copy(_packageHeader, 40, _packageTimestampResponse, 0, 8);
            byte[] _packageHeaderCRC32 = new byte[4];
            Array.Copy(_packageHeader, 60, _packageHeaderCRC32, 0, 4);

            _package.Version = this.ByteArrayToUInt32(_packageVersion);
            _package.Length = this.ByteArrayToUInt32(_packageLength);
            _package.BodyHeadCRC32 = this.ByteArrayToUInt32(_packageBodyCRC32);
            _package.Command1 = this.ByteArrayToUInt16(_packageCommand1);
            _package.Command2 = this.ByteArrayToUInt16(_packageCommand2);
            _package.CommandId = this.ByteArrayToUInt32(_packageCommandId);
            _package.TimestampSend = this.ByteArrayToDouble(_packageTimestampSend);
            _package.TimestampReceive = this.GetTimestamp(DateTime.Now);
            _package.TimestampResponse = this.ByteArrayToDouble(_packageTimestampResponse);
            _package.HeadCRC32 = this.ByteArrayToUInt32(_packageHeaderCRC32);
            _package.BodyType = new byte();
            _package.BodyLength = 0;
            _package.BodyCRC32 = 0;
            _package.FieldCount = 0;
            _package.Fields = new object[0];
            _package.FieldTypes = new byte[0];

            _package.ReceivedLength = _byteArray.Length - 64;
            if (_package.ReceivedLength > _package.Length - 64)
            {
                _package.ReceivedLength = int.Parse(_package.Length.ToString()) - 64;
            }

            if (_completely && _package.Length > 64)
            {
                _package.BodyType = _byteArray[64];
                //string _byteString = "";
                //for (int i = 0; i < _byteArray.Length; i++)
                //{
                //    _byteString += i > 0 && i % 8 == 0 ? "\n" : "";
                //    _byteString += _byteArray[i].ToString("X2") + " ";
                //}
                if (_package.BodyType == 0x00)
                {
                    #region 0x00 数据包
                    byte[] _bodyLength = new byte[4];
                    Array.Copy(_byteArray, 65, _bodyLength, 0, 4);
                    _package.BodyLength = this.ByteArrayToUInt32(_bodyLength);

                    byte[] _bodyCRC32 = new byte[4];
                    Array.Copy(_byteArray, 69, _bodyCRC32, 0, 4);
                    _package.BodyCRC32 = this.ByteArrayToUInt32(_bodyCRC32);

                    byte[] _fieldCount = new byte[4];
                    Array.Copy(_byteArray, 73, _fieldCount, 0, 4);
                    _package.FieldCount = this.ByteArrayToUInt32(_fieldCount);

                    byte[] _fieldTypes = new byte[_package.FieldCount];
                    Array.Copy(_byteArray, 77, _fieldTypes, 0, _fieldTypes.Length);
                    _package.FieldTypes = _fieldTypes;
                    if (_package.FieldCount != _package.FieldTypes.Length) { throw new Exception("Field count and type length not match."); }
                    if (_package.FieldCount > 100000) { throw new Exception("Field count can not more then 100000."); }

                    _package.Fields = new object[_package.FieldCount];
                    uint _position = 77 + (uint)_package.FieldTypes.Length;
                    uint _fieldSize = 0;
                    for (UInt32 i = 0; i < _fieldTypes.Length; i++)
                    {
                        _package.Fields[i] = this.GetFromByteArray(_byteArray, _position, _package.FieldTypes[i], ref _fieldSize);
                        _position += _fieldSize;
                    }
                    #endregion
                }
                else if (_package.BodyType == 0x01)
                {
                    _packageSize = 0;
                }
            }

            _packageSize = _package.Length;
            return _package;
        }
        #endregion

        #region GetFromByteArray
        private object GetFromByteArray(byte[] _byteArray, uint _start, byte _byteType, ref uint _fieldSize)
        {
            object _return = null;
            switch ((FieldType)_byteType)
            {
                case FieldType.Undefined:
                    #region Undefined
                    {
                        _fieldSize = 0;
                        break;
                    }
                #endregion
                case FieldType.Null:
                    #region Null
                    {
                        _fieldSize = 0;
                        _return = null;
                        break;
                    }
                #endregion
                case FieldType.Boolean:
                    #region Boolean
                    {
                        _fieldSize = 1;
                        byte[] _field_Bollean = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Bollean, 0, _fieldSize);
                        _return = this.ByteArrayToBoolean(_field_Bollean);
                        break;
                    }
                #endregion
                case FieldType.Int8:
                    #region Int8
                    {
                        _fieldSize = 1;
                        byte[] _field_Int8 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Int8, 0, _fieldSize);
                        _return = this.ByteArrayToInt8(_field_Int8);
                        break;
                    }
                #endregion
                case FieldType.UInt8:
                    #region UInt8
                    {
                        _fieldSize = 1;
                        byte[] _field_UInt8 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_UInt8, 0, _fieldSize);
                        _return = this.ByteArrayToUInt8(_field_UInt8);
                        break;
                    }
                #endregion
                case FieldType.Int16:
                    #region Int16
                    {
                        _fieldSize = 2;
                        byte[] _field_Int16 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Int16, 0, _fieldSize);
                        _return = this.ByteArrayToInt16(_field_Int16);
                        break;
                    }
                #endregion
                case FieldType.UInt16:
                    #region UInt16
                    {
                        _fieldSize = 2;
                        byte[] _field_UInt16 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_UInt16, 0, _fieldSize);
                        _return = this.ByteArrayToUInt16(_field_UInt16);
                        break;
                    }
                #endregion
                case FieldType.Int32:
                    #region Int32
                    {
                        _fieldSize = 4;
                        byte[] _field_Int32 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Int32, 0, _fieldSize);
                        _return = this.ByteArrayToInt32(_field_Int32);
                        break;
                    }
                #endregion
                case FieldType.UInt32:
                    #region UInt32
                    {
                        _fieldSize = 4;
                        byte[] _field_UInt32 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_UInt32, 0, _fieldSize);
                        _return = this.ByteArrayToUInt32(_field_UInt32);
                        break;
                    }
                #endregion
                case FieldType.Int64:
                    #region Int64
                    {
                        _fieldSize = 8;
                        byte[] _field_Int64 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Int64, 0, _fieldSize);
                        _return = this.ByteArrayToInt64(_field_Int64);
                        break;
                    }
                #endregion
                case FieldType.UInt64:
                    #region UInt64
                    {
                        _fieldSize = 8;
                        byte[] _field_UInt64 = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_UInt64, 0, _fieldSize);
                        _return = this.ByteArrayToUInt64(_field_UInt64);
                        break;
                    }
                #endregion
                case FieldType.Single:
                    #region Single
                    {
                        _fieldSize = 4;
                        byte[] _field_Single = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Single, 0, _fieldSize);
                        _return = this.ByteArrayToSingle(_field_Single);
                        break;
                    }
                #endregion
                case FieldType.Double:
                    #region Double
                    {
                        _fieldSize = 8;
                        byte[] _field_Double = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Double, 0, _fieldSize);
                        _return = this.ByteArrayToDouble(_field_Double);
                        break;
                    }
                #endregion
                case FieldType.StringASCII:
                    #region StringASCII
                    {
                        byte[] _field_ASCII_Length = new byte[4];
                        Array.Copy(_byteArray, _start, _field_ASCII_Length, 0, 4);
                        UInt32 _field_ASCII_Size = this.ByteArrayToUInt32(_field_ASCII_Length);
                        _fieldSize = 4 + _field_ASCII_Size;

                        if (_field_ASCII_Size == 0)
                        {
                            _return = "";
                        }
                        else
                        {
                            byte[] _fieldString = new byte[_field_ASCII_Size];
                            Array.Copy(_byteArray, _start + 4, _fieldString, 0, _field_ASCII_Size);
                            _return = this.ByteArrayToStringASCII(_fieldString);
                        }
                        break;
                    }
                #endregion
                case FieldType.StringUTF8:
                    #region StringUTF8
                    {
                        byte[] _field_UTF8_Length = new byte[4];
                        Array.Copy(_byteArray, _start, _field_UTF8_Length, 0, 4);
                        UInt32 field_UTF8_Size = this.ByteArrayToUInt32(_field_UTF8_Length);
                        _fieldSize = 4 + field_UTF8_Size;

                        if (field_UTF8_Size == 0)
                        {
                            _return = "";
                        }
                        else
                        {
                            byte[] _field_UTF8_String = new byte[field_UTF8_Size];
                            Array.Copy(_byteArray, _start + 4, _field_UTF8_String, 0, field_UTF8_Size);
                            _return = this.ByteArrayToStringUTF8(_field_UTF8_String);
                        }
                        break;
                    }
                #endregion
                case FieldType.StringArray:
                    #region StringArray
                    {
                        byte[] _field_StringArray_Size = new byte[4];
                        Array.Copy(_byteArray, _start, _field_StringArray_Size, 0, 4);
                        UInt32 _field_StringArray_SizeValue = this.ByteArrayToUInt32(_field_StringArray_Size);

                        string[] _stringArray = new string[_field_StringArray_SizeValue];
                        uint _size = 4;
                        for (int r = 0; r < _field_StringArray_SizeValue; r++)
                        {
                            byte[] _field_StringArray_Item_Size = new byte[4];
                            Array.Copy(_byteArray, _start + _size, _field_StringArray_Item_Size, 0, 4);
                            UInt32 _field_Array_Size = this.ByteArrayToUInt32(_field_StringArray_Item_Size);
                            _size += 4;

                            byte[] _field_UTF8_String = new byte[_field_Array_Size];
                            Array.Copy(_byteArray, _start + _size, _field_UTF8_String, 0, _field_Array_Size);
                            _stringArray[r] = this.ByteArrayToStringUTF8(_field_UTF8_String);
                            _size += _field_Array_Size;
                        }
                        _fieldSize = _size;
                        _return = _stringArray;
                        break;
                    }
                #endregion
                case FieldType.ByteArray:
                    #region ByteArray
                    {
                        byte[] _field_ByteArray_Length = new byte[4];
                        Array.Copy(_byteArray, _start, _field_ByteArray_Length, 0, 4);
                        UInt32 _field_ByteArray_Size = this.ByteArrayToUInt32(_field_ByteArray_Length);
                        _fieldSize = 4 + _field_ByteArray_Size;

                        byte[] _field_ByteArray = new byte[_field_ByteArray_Size];
                        Array.Copy(_byteArray, _start + 4, _field_ByteArray, 0, _field_ByteArray_Size);
                        _return = _field_ByteArray;
                        break;
                    }
                #endregion
                case FieldType.DataTable:
                    #region DataTable
                    {
                        byte[] _field_DataTable_Size = new byte[4];
                        Array.Copy(_byteArray, _start, _field_DataTable_Size, 0, 4);

                        byte[] _field_DataTable_ColCountSize = new byte[4];
                        Array.Copy(_byteArray, _start + 4, _field_DataTable_ColCountSize, 0, 4);
                        UInt32 _field_DataTable_ColCount = this.ByteArrayToUInt32(_field_DataTable_ColCountSize);

                        byte[] _field_DataTable_RowCountSize = new byte[4];
                        Array.Copy(_byteArray, _start + 8, _field_DataTable_RowCountSize, 0, 4);
                        UInt32 _field_DataTable_RowCount = this.ByteArrayToUInt32(_field_DataTable_RowCountSize);

                        byte[] _field_DataTable_ColTypes = new byte[_field_DataTable_ColCount];
                        Array.Copy(_byteArray, _start + 12, _field_DataTable_ColTypes, 0, _field_DataTable_ColTypes.Length);
                        _fieldSize = 12 + _field_DataTable_ColCount;

                        UInt32 _columnNamesSize = 0;
                        string[] _columnNames = (string[])this.GetFromByteArray(_byteArray, _start + _fieldSize, (byte)FieldType.StringArray, ref _columnNamesSize);
                        _fieldSize += _columnNamesSize;

                        DataTable _dataTable = new DataTable();
                        for (int c = 0; c < _field_DataTable_ColCount; c++)
                        {
                            _dataTable.Columns.Add(_columnNames[c], this.GetTypeFromByte(_field_DataTable_ColTypes[c]));
                        }
                        for (int r = 0; r < _field_DataTable_RowCount; r++)
                        {
                            DataRow _row = _dataTable.NewRow();
                            for (int c = 0; c < _field_DataTable_ColCount; c++)
                            {
                                uint _size = 0;
                                _row[c] = this.GetFromByteArray(_byteArray, _start + _fieldSize, _field_DataTable_ColTypes[c], ref _size);
                                _fieldSize += _size;
                            }
                            _dataTable.Rows.Add(_row);
                        }
                        _return = _dataTable;
                        break;
                    }
                #endregion
                case FieldType.DateTime:
                    #region DateTime
                    {
                        byte[] _field_DateTime_Length = new byte[4];
                        Array.Copy(_byteArray, _start, _field_DateTime_Length, 0, 4);
                        UInt32 _field_DateTime_Size = this.ByteArrayToUInt32(_field_DateTime_Length);
                        _fieldSize = 4 + _field_DateTime_Size;

                        byte[] _field_DateTime_String = new byte[_field_DateTime_Size];
                        Array.Copy(_byteArray, _start + 4, _field_DateTime_String, 0, _field_DateTime_Size);
                        _return = this.StringToDateTime(this.ByteArrayToStringUTF8(_field_DateTime_String));
                        break;
                    }
                #endregion
                case FieldType.Guid:
                    #region Guid
                    {
                        byte[] _field_Guid_ByteArray = new byte[16];
                        Array.Copy(_byteArray, _start, _field_Guid_ByteArray, 0, 16);
                        _return = new Guid(_field_Guid_ByteArray);
                        _fieldSize = 16;
                        break;
                    }
                #endregion
                case FieldType.Decimal:
                    #region Decimal
                    {
                        _fieldSize = 8;
                        byte[] _field_Decimal = new byte[_fieldSize];
                        Array.Copy(_byteArray, _start, _field_Decimal, 0, _fieldSize);
                        _return = Decimal.Parse(this.ByteArrayToDouble(_field_Decimal).ToString());
                        break;
                    }
                #endregion
                default:
                    throw new Exception("Missing type:" + _byteType.ToString());
            }
            return _return;
        }
        #endregion

        #region AppendToByteArray
        private byte AppendToByteArray(ref MemoryStream _stream, object _object)
        {
            if (_object == null) { return (byte)FieldType.Null; }
            Type _type = _object.GetType();
            if (_type == typeof(bool))
            {
                byte[] _buffer = BitConverter.GetBytes((bool)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Int16))
            {
                byte[] _buffer = this.Int16ToByteArray((Int16)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(UInt16))
            {
                byte[] _buffer = this.UInt16ToByteArray((UInt16)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Int32))
            {
                byte[] _buffer = this.Int32ToByteArray((Int32)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(UInt32))
            {
                byte[] _buffer = this.UInt32ToByteArray((UInt32)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Int64))
            {
                byte[] _buffer = this.Int64ToByteArray((Int64)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(UInt64))
            {
                byte[] _buffer = this.UInt64ToByteArray((UInt64)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Single))
            {
                byte[] _buffer = this.SingleToByteArray((Single)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Double))
            {
                byte[] _buffer = this.DoubleToByteArray((Double)_object);
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(string))
            {
                byte[] _byteArrayString = System.Text.Encoding.UTF8.GetBytes(_object.ToString());
                byte[] _byteArrayStringSize = this.UInt32ToByteArray(UInt32.Parse(_byteArrayString.Length.ToString()));
                _stream.Write(_byteArrayStringSize, 0, _byteArrayStringSize.Length);
                _stream.Write(_byteArrayString, 0, _byteArrayString.Length);
            }
            else if (_type == typeof(byte[]))
            {
                byte[] _byteContent = (byte[])_object;
                byte[] _byteArraySize = this.UInt32ToByteArray(UInt32.Parse(_byteContent.Length.ToString()));
                _stream.Write(_byteArraySize, 0, _byteArraySize.Length);
                _stream.Write(_byteContent, 0, _byteContent.Length);
            }
            else if (_type == typeof(string[]))
            {
                string[] _item = (string[])_object;

                byte[] _byteContent = this.StringArrayToByteArray(_item);
                //byte[] _byteArraySize = this.UInt32ToByteArray(UInt32.Parse(_byteContent.Length.ToString()));
                //_stream.Write(_byteArraySize, 0, _byteArraySize.Length);
                _stream.Write(_byteContent, 0, _byteContent.Length);
            }
            else if (_type == typeof(DataTable))
            {
                DataTable _dataTable = (DataTable)_object;

                byte[] _byteContent = this.DataTableToByteArray(_dataTable);
                byte[] _byteArraySize = this.UInt32ToByteArray(UInt32.Parse(_byteContent.Length.ToString()));
                _stream.Write(_byteArraySize, 0, _byteArraySize.Length);
                _stream.Write(_byteContent, 0, _byteContent.Length);
            }
            else if (_type == typeof(DateTime))
            {
                byte[] _byteArrayString = System.Text.Encoding.ASCII.GetBytes(this.DateTimeToString((DateTime)_object));
                byte[] _byteArrayStringSize = this.UInt32ToByteArray(UInt32.Parse(_byteArrayString.Length.ToString()));
                _stream.Write(_byteArrayStringSize, 0, _byteArrayStringSize.Length);
                _stream.Write(_byteArrayString, 0, _byteArrayString.Length);
            }
            else if (_type == typeof(Guid))
            {
                byte[] _buffer = ((Guid)_object).ToByteArray();
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else if (_type == typeof(Decimal))
            {
                byte[] _buffer = this.DoubleToByteArray(Double.Parse(_object.ToString()));
                _stream.Write(_buffer, 0, _buffer.Length);
            }
            else
            {
                return (byte)FieldType.Null;
            }
            return this.GetByteFromType(_type);
        }
        #endregion

        #region GetByteFromType
        private byte GetByteFromType(Type _type)
        {
            if (_type == typeof(Nullable))
            {
                return 0x01;
            }
            else if (_type == typeof(bool))
            {
                return 0x02;
            }
            else if (_type == typeof(Int16))
            {
                return 0x05;
            }
            else if (_type == typeof(UInt16))
            {
                return 0x06;
            }
            else if (_type == typeof(Int32))
            {
                return 0x07;
            }
            else if (_type == typeof(UInt32))
            {
                return 0x08;
            }
            else if (_type == typeof(Int64))
            {
                return 0x10;
            }
            else if (_type == typeof(UInt64))
            {
                return 0x11;
            }
            else if (_type == typeof(Single))
            {
                return 0x09;
            }
            else if (_type == typeof(Double))
            {
                return 0x0A;
            }
            else if (_type == typeof(string))
            {
                return 0x0C;
            }
            else if (_type == typeof(byte[]))
            {
                return 0x0D;
            }
            else if (_type == typeof(DataTable))
            {
                return 0x0E;
            }
            else if (_type == typeof(DateTime))
            {
                return 0x1A;
            }
            else if (_type == typeof(Guid))
            {
                return 0x1B;
            }
            else if (_type == typeof(string[]))
            {
                return 0x1C;
            }
            else if (_type == typeof(Decimal))
            {
                return 0x1D;
            }
            else
            {
                return 0x00;
            }
        }
        #endregion

        #region GetTypeFromByte
        private Type GetTypeFromByte(byte _byte)
        {
            if (_byte == 0x01)
            {
                return typeof(Nullable);
            }
            else if (_byte == 0x02)
            {
                return typeof(bool);
            }
            else if (_byte == 0x05)
            {
                return typeof(Int16);
            }
            else if (_byte == 0x06)
            {
                return typeof(UInt16);
            }
            else if (_byte == 0x07)
            {
                return typeof(Int32);
            }
            else if (_byte == 0x08)
            {
                return typeof(UInt32);
            }
            else if (_byte == 0x09)
            {
                return typeof(Single);
            }
            else if (_byte == 0x0A)
            {
                return typeof(Double);
            }
            else if (_byte == 0x0C)
            {
                return typeof(string);
            }
            else if (_byte == 0x0D)
            {
                return typeof(byte[]);
            }
            else if (_byte == 0x0E)
            {
                return typeof(DataTable);
            }
            else if (_byte == 0x1A)
            {
                return typeof(DateTime);
            }
            else if (_byte == 0x1B)
            {
                return typeof(Guid);
            }
            else if (_byte == 0x1C)
            {
                return typeof(string[]);
            }
            else if (_byte == 0x1D)
            {
                return typeof(Decimal);
            }
            else
            {
                return typeof(object);
            }
        }
        #endregion

        #region ByteArrayToBoolean
        private bool ByteArrayToBoolean(byte[] _byteArray)
        {
            return BitConverter.ToBoolean(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToDouble
        private Double ByteArrayToDouble(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToDouble(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToInt8
        private int ByteArrayToInt8(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToInt32(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToInt16
        private Int16 ByteArrayToInt16(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToInt16(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToInt32
        private Int32 ByteArrayToInt32(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToInt32(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToInt64
        private Int64 ByteArrayToInt64(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToInt64(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToUInt64
        private UInt64 ByteArrayToUInt64(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToUInt64(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToSingle
        private Single ByteArrayToSingle(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToSingle(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToStringASCII
        private string ByteArrayToStringASCII(byte[] _byteArray)
        {
            return System.Text.Encoding.ASCII.GetString(_byteArray);
        }
        #endregion

        #region ByteArrayToStringUTF8
        private string ByteArrayToStringUTF8(byte[] _byteArray)
        {
            return System.Text.Encoding.UTF8.GetString(_byteArray);
        }
        #endregion

        #region ByteArrayToUInt8
        private uint ByteArrayToUInt8(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToUInt32(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToUInt16
        private UInt16 ByteArrayToUInt16(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToUInt16(_byteArray, 0);
        }
        #endregion

        #region ByteArrayToUInt32
        private UInt32 ByteArrayToUInt32(byte[] _byteArray)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return BitConverter.ToUInt32(_byteArray, 0);
        }
        #endregion

        #region DoubleToByteArray
        private byte[] DoubleToByteArray(Double _double)
        {
            byte[] _byteArray = BitConverter.GetBytes(_double);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region Int16ToByteArray
        private byte[] Int16ToByteArray(Int16 _int16)
        {
            byte[] _byteArray = BitConverter.GetBytes(_int16);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region Int32ToByteArray
        private byte[] Int32ToByteArray(Int32 _int32)
        {
            byte[] _byteArray = BitConverter.GetBytes(_int32);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region Int64ToByteArray
        private byte[] Int64ToByteArray(Int64 _int64)
        {
            byte[] _byteArray = BitConverter.GetBytes(_int64);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region SingleToByteArray
        private byte[] SingleToByteArray(Single _single)
        {
            byte[] _byteArray = BitConverter.GetBytes(_single);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region UInt16ToByteArray
        private byte[] UInt16ToByteArray(UInt16 _uint16)
        {
            byte[] _byteArray = BitConverter.GetBytes(_uint16);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region UInt32ToByteArray
        private byte[] UInt32ToByteArray(UInt32 _uint32)
        {
            byte[] _byteArray = BitConverter.GetBytes(_uint32);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region UInt64ToByteArray
        private byte[] UInt64ToByteArray(UInt64 _uint64)
        {
            byte[] _byteArray = BitConverter.GetBytes(_uint64);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byteArray);
            }
            return _byteArray;
        }
        #endregion

        #region StringArrayToByteArray
        private byte[] StringArrayToByteArray(string[] _stringArray)
        {
            MemoryStream _stream_string = new MemoryStream();
            this.AppendToByteArray(ref _stream_string, UInt32.Parse(_stringArray.Length.ToString()));
            for (int j = 0; j < _stringArray.Length; j++)
            {
                this.AppendToByteArray(ref _stream_string, _stringArray[j]);
            }
            byte[] _byteArray = _stream_string.ToArray();
            _stream_string.Close();

            return _byteArray;
        }
        #endregion

        #region DataTableToByteArray
        private byte[] DataTableToByteArray(DataTable _dataTable)
        {
            MemoryStream _stream_table = new MemoryStream();
            byte[] _byteColumnCount = this.UInt32ToByteArray(UInt32.Parse(_dataTable.Columns.Count.ToString()));
            byte[] _byteRowsCount = this.UInt32ToByteArray(UInt32.Parse(_dataTable.Rows.Count.ToString()));
            _stream_table.Write(_byteColumnCount, 0, _byteColumnCount.Length);
            _stream_table.Write(_byteRowsCount, 0, _byteRowsCount.Length);

            byte[] _byteColumns = new byte[_dataTable.Columns.Count];
            string[] _columnNames = new string[_dataTable.Columns.Count];
            for (int i = 0; i < _dataTable.Columns.Count; i++)
            {
                _byteColumns[i] = this.GetByteFromType(_dataTable.Columns[i].DataType);
                _columnNames[i] = _dataTable.Columns[i].ColumnName;
            }
            _stream_table.Write(_byteColumns, 0, _byteColumns.Length);

            byte[] _byteColumnsName = this.StringArrayToByteArray(_columnNames.ToArray());
            _stream_table.Write(_byteColumnsName, 0, _byteColumnsName.Length);

            for (int i = 0; i < _dataTable.Rows.Count; i++)
            {
                for (int j = 0; j < _dataTable.Columns.Count; j++)
                {
                    if (_dataTable.Rows[i][j] == null)
                    {
                        throw new Exception("DataTable Row:" + i + " Column:" + j + " is null!");
                    }
                    else
                    {
                        this.AppendToByteArray(ref _stream_table, _dataTable.Rows[i][j]);
                    }
                }
            }
            byte[] _byteArray = _stream_table.ToArray();
            _stream_table.Close();

            return _byteArray;
        }
        #endregion

        #region GetCRC32
        private UInt32 GetCRC32(byte[] _byteArray)
        {
            int _count = _byteArray.Length;
            UInt32 _crc = 0xFFFFFFFF;
            for (int i = 0; i < _count; i++)
            {
                _crc = CRCTable[(_crc ^ _byteArray[i]) & 0xFF] ^ (_crc >> 8);
            }
            return ~_crc;
        }
        #endregion

        #region StringToDateTime
        public DateTime StringToDateTime(string _timeString)
        {
            return DateTime.Parse(_timeString);
        }
        #endregion

        #region DateTimeToString
        public string DateTimeToString(DateTime _dateTime)
        {
            return _dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
        #endregion

        #region GetTimestamp
        public Double GetTimestamp(DateTime _dateTime)
        {
            return (_dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
        #endregion
    }

    #region LztpPackage
    public class LztpPackage : IDisposable
    {
        /// <summary>
        /// 2.协议版本
        /// </summary>
        public uint Version = 1;
        /// <summary>
        /// 3.数据包大小
        /// </summary>
        public uint Length;
        /// <summary>
        /// 4.数据包CRC32
        /// </summary>
        public uint BodyHeadCRC32;
        /// <summary>
        /// 5.命令代码1
        /// </summary>
        public ushort Command1;
        /// <summary>
        /// 6.命令代码2
        /// </summary>
        public ushort Command2;
        /// <summary>
        /// 7.命令编号
        /// </summary>
        public uint CommandId;
        /// <summary>
        /// 8.发送是的时间戳
        /// </summary>
        public double TimestampSend = 0;
        /// <summary>
        /// 9.接收到的时间戳
        /// </summary>
        public double TimestampReceive = 0;
        /// <summary>
        /// 10.返回是的时间戳
        /// </summary>
        public double TimestampResponse = 0;
        /// <summary>
        /// 12.头部
        /// </summary>
        public uint HeadCRC32;

        /// <summary>
        /// a.数据类型 0x0:Package 0x1:Stream
        /// </summary>
        public byte BodyType;
        /// <summary>
        /// b.数据部分长度
        /// </summary>
        public uint BodyLength;
        /// <summary>
        /// c.数据部分CRC32
        /// </summary>
        public uint BodyCRC32;
        /// <summary>
        /// d.字段总数量
        /// </summary>
        public uint FieldCount;
        /// <summary>
        /// f.各字段类型
        /// </summary>
        public byte[] FieldTypes;
        /// <summary>
        /// g.字段详细内容
        /// </summary>
        public object[] Fields;

        /// <summary>
        /// z.接收到的数据长度
        /// </summary>
        public int ReceivedLength = 0;

        public LztpPackage() { }
        public LztpPackage(ushort _command1, ushort _command2, uint _commandId, object[] _fields)
        {
            this.Command1 = _command1;
            this.Command2 = _command2;
            this.CommandId = _commandId;
            this.Fields = _fields;
        }

        public void Dispose()
        {
            this.FieldTypes = null;
            this.Fields = null;
        }
    }
    #endregion
}
