using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Agora
{
    internal class ByteBuffer
    {
        private const int MAX_LENGTH = 1024;
        private byte[] TEMP_BYTE_ARRAY = new byte[MAX_LENGTH];
        private int CURRENT_LENGTH = 0;
        private int CURRENT_POSITION = 0;
        private byte[] RETURN_ARRAY;

        public ByteBuffer() => this.Initialize();

        public ByteBuffer(byte[] _bytes)
        {
            this.Initialize();
            this.PushByteArray(_bytes);
        }

        public void Initialize()
        {
            TEMP_BYTE_ARRAY.Initialize();
            CURRENT_LENGTH = 0;
            CURRENT_POSITION = 0;
        }

        public int Length { get => CURRENT_LENGTH; }

        public int Position
        {
            get => CURRENT_POSITION;
            set => CURRENT_POSITION = value;
        }

        public byte[] ToByteArray()
        {
            RETURN_ARRAY = new byte[CURRENT_LENGTH];
            Array.Copy(TEMP_BYTE_ARRAY, 0, RETURN_ARRAY, 0, CURRENT_LENGTH);
            return RETURN_ARRAY;
        }

        public void PushByte(byte _byte) => TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = _byte;

        public void PushByteArray(byte[] _byteArray)
        {
            _byteArray.CopyTo(TEMP_BYTE_ARRAY, CURRENT_LENGTH);
            CURRENT_LENGTH += _byteArray.Length;
        }

        public void PushUInt16(UInt16 _num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((_num & 0x00ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0xff00) >> 8) & 0xff);
        }

        public void PushInt(UInt32 _num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((_num & 0x000000ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0x0000ff00) >> 8) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0x00ff0000) >> 16) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0xff000000) >> 24) & 0xff);
        }
        public void PushLong(long _num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((_num & 0x000000ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0x0000ff00) >> 8) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0x00ff0000) >> 16) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((_num & 0xff000000) >> 24) & 0xff);
        }

        public byte PopByte() => TEMP_BYTE_ARRAY[CURRENT_POSITION++];

        public UInt16 PopUInt16()
        {
            if (CURRENT_POSITION + 1 >= CURRENT_LENGTH) { return 0; }

            UInt16 _result = (UInt16)(TEMP_BYTE_ARRAY[CURRENT_POSITION] | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 8);
            CURRENT_POSITION += 2;
            return _result;
        }

        public uint PopUInt()
        {
            if (CURRENT_POSITION + 3 >= CURRENT_LENGTH) { return 0; }
                
            uint _result = (uint)(TEMP_BYTE_ARRAY[CURRENT_POSITION] | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 8 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 2] << 16 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 3] << 24);
            CURRENT_POSITION += 4;
            return _result;
        }

        public long PopLong()
        {
            if (CURRENT_POSITION + 3 >= CURRENT_LENGTH) { return 0; }

            long _result = (long)(TEMP_BYTE_ARRAY[CURRENT_POSITION] << 24 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 16 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 2] << 8 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 3]);
            CURRENT_POSITION += 4;
            return _result;
        }

        public byte[] PopByteArray(int _length)
        {
            if (CURRENT_POSITION + _length > CURRENT_LENGTH) { return new byte[0]; }

            byte[] _result = new byte[_length];
            Array.Copy(TEMP_BYTE_ARRAY, CURRENT_POSITION, _result, 0, _length);
            CURRENT_POSITION += _length;
            return _result;
        }

        public byte[] PopByteArray2(int _length)
        {
            if (CURRENT_POSITION <= _length) { return new byte[0]; }

            byte[] _result = new byte[_length];
            Array.Copy(TEMP_BYTE_ARRAY, CURRENT_POSITION - Length, _result, 0, _length);
            CURRENT_POSITION -= _length;
            return _result;
        }
        public ByteBuffer Put(ushort _value)
        {
            this.PushUInt16(_value);
            return this;
        }

        public ByteBuffer Put(uint _value)
        {
            this.PushLong(_value);
            return this;
        }

        public ByteBuffer Put(byte[] _value)
        {
            Put((ushort)_value.Length);
            this.PushByteArray(_value);
            return this;
        }

        public ByteBuffer Copy(byte[] _value)
        {
            this.PushByteArray(_value);
            return this;
        }

        public ByteBuffer PutIntMap(Dictionary<ushort, uint> _extra)
        {
            Put((ushort)_extra.Count);

            foreach (KeyValuePair<ushort,uint> _item in _extra)
            {
                Put(_item.Key);
                Put(_item.Value);
            }
            return this;
        }

        public ushort ReadShort() =>  this.PopUInt16();

        public uint ReadInt() => this.PopUInt();

        public byte[] ReadBytes()
        {
            ushort _length = ReadShort();
            return this.PopByteArray(_length);
        }

        public Dictionary<ushort, uint> ReadIntMap()
        {
            Dictionary<ushort, uint> _map = new Dictionary<ushort, uint>();

            ushort _length = ReadShort();

            for (short i = 0; i < _length; ++i)
            {
                ushort _key = ReadShort();
                uint _value = ReadInt();
                _map.Add(_key, _value);
            }

            return _map;
        }
    }
}
