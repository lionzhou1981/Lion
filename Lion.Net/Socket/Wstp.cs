using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Lion.Encrypt;
using Lion.Net.Sockets;

namespace Lion.Net.Sockets
{
    public class Wstp : ISocketProtocol
    {
        private string code = "";
        public string Code { get { return this.code; } set { this.code = value; } }

        private string name = "";
        public string Name { get { return this.name; } set { this.name = value; } }

        private uint keepAlive = 0;
        public uint KeepAlive { get { return this.keepAlive; } set { this.keepAlive = value; } }

        public Wstp(string _code, string _name)
        {
            this.code = _code;
            this.name = _name;
        }

        public object KeepAlivePackage { get { return ""; } }

        public bool IsKeepAlivePackage(object _object, object _socket) { return false; }

        #region Check
        public bool Check(byte[] _byteArray, bool _completely = false, SocketSession _session = null)
        {
            object _data = this.DePackage(_byteArray, out uint _packageSize, false, _session);
            return _data != null;
        }
        #endregion

        #region EnPackage
        public byte[] EnPackage(object _object, SocketSession _session = null)
        {
            WstpPackage _package = (WstpPackage)_object;

            int _payloadByte = 0;
            if (_package.Binary.Length >= 65536)
            {
                _payloadByte = 8;
            }
            else if (_package.Binary.Length >= 126)
            {
                _payloadByte = 2;
            }

            byte[] _data = new byte[2 + _payloadByte + _package.Binary.Length];
            _data[0] = 0;
            _data[1] = 0;
            _data[0] |= 0x80;
            switch (_package.Type)
            {
                case WstpPackageType.Ping: _data[0] |= 0x09; break;
                case WstpPackageType.Pong: _data[0] |= 0x0A; break;
                case WstpPackageType.Text: _data[0] |= 0x01; break;
                case WstpPackageType.Binary: _data[0] |= 0x02; break;
                case WstpPackageType.Close: _data[0] |= 0x08; break;
            }
            if (_payloadByte == 0)
            {
                _data[1] = (byte)_package.Binary.Length;
                Array.Copy(_package.Binary, 0, _data, 2, _package.Binary.Length);
            }
            else if (_payloadByte == 2)
            {
                _data[1] = 126;

                ushort _size = ushort.Parse(_package.Binary.Length.ToString());
                short _sizeNet = System.Net.IPAddress.HostToNetworkOrder((short)_size);
                byte[] _length = BitConverter.GetBytes(_sizeNet);
                Array.Copy(_length, 0, _data, 2, _length.Length);
                Array.Copy(_package.Binary, 0, _data, 4, _package.Binary.Length);
            }
            else
            {
                _data[1] = 127;

                int _size = _package.Binary.Length;
                long _sizeNet = System.Net.IPAddress.HostToNetworkOrder((long)_size);
                byte[] _length = BitConverter.GetBytes(_sizeNet);
                Array.Copy(_length, 0, _data, 2, _length.Length);
                Array.Copy(_package.Binary, 0, _data, 10, _package.Binary.Length);
            }

            return _data;
        }
        #endregion

        #region DePackage
        public object DePackage(byte[] _byteArray, out uint _packageSize, bool _completely = false, SocketSession _session = null)
        {
            if (_session == null) { _packageSize = uint.Parse(_byteArray.Length.ToString()); return null; }

            string _data = "";
            if (_session["__I__N__I__T__"] == null)
            {
                #region 首次握手
                _data = Encoding.UTF8.GetString(_byteArray);
                int _index = _data.IndexOf("\r\n\r\n");
                if (_index == -1) { _packageSize = 0; return null; }
                if (!_completely && _index > -1) { _packageSize = 0; return new WstpPackage(WstpPackageType.Init); }

                _packageSize = uint.Parse((_index + 4).ToString());
                string[] _lines = _data.Split("\r\n");
                Dictionary<string, string> _header = new Dictionary<string, string>();
                foreach (string _line in _lines)
                {
                    string _key = "";
                    string _value = "";
                    if (_line.StartsWith("GET"))
                    {
                        _key = "Path";
                        _value = _line.Substring(4, _line.IndexOf(" ", 4) - 4);
                    }
                    else
                    {
                        int _splitIndex = _line.IndexOf(":");
                        if (_splitIndex == -1) { continue; }
                        _key = _line.Substring(0, _splitIndex).Trim();
                        _value = _line.Substring(_splitIndex + 1).Trim();
                    }
                    if (_header.ContainsKey(_key))
                    {
                        _header[_key] = _header[_key] + "\n" + _value;
                    }
                    else
                    {
                        _header.Add(_key, _value);
                    }
                }
                if (!_header.ContainsKey("Upgrade") || _header["Upgrade"] != "websocket") { _packageSize = 0; _session.Disconnect(); return null; }

                if (_header.ContainsKey("Sec-WebSocket-Key"))
                {
                    string _wsKey = _header["Sec-WebSocket-Key"];
                    if (_wsKey == "") { _packageSize = 0; _session.Dispose(); return null; }
                    string _wsSecret = SHA.EncodeSHA1ToBase64(_wsKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

                    string _returnData = $"HTTP/1.1 101 Switching Protocols\r\n";
                    _returnData += $"Connection:Upgrade\r\n";
                    _returnData += $"Server:{this.name}\r\n";
                    _returnData += $"Upgrade:WebSocket\r\n";
                    _returnData += $"Date:{DateTime.UtcNow.ToString("r")}\r\n";
                    _returnData += $"Sec-WebSocket-Accept:{_wsSecret}\r\n\r\n";

                    _session.SendBytes(Encoding.UTF8.GetBytes(_returnData));
                    foreach (KeyValuePair<string, string> _item in _header) { _session[$"{_item.Key}"] = _item.Value; }
                    _session["Sec-WebSocket-Secret"] = _wsSecret;
                    _session["__I__N__I__T__"] = true;
                    return new WstpPackage(WstpPackageType.Init);
                }
                else if (_header.ContainsKey("Sec-WebSocket-Key1"))
                {
                    string _origin = _header["Origin"];
                    if (_origin.Length == 0) { _origin = "null"; }

                    string _returnData = $"HTTP/1.1 101 Web Socket Protocol Handshake\r\n";
                    _returnData += $"Upgrade:WebSocket\r\n";
                    _returnData += $"Connection:Upgrade\r\n";
                    _returnData += $"Server:{this.name}\r\n";
                    _returnData += $"Date:{DateTime.UtcNow.ToString("r")}\r\n";
                    _returnData += $"Sec-WebSocket-Origin:{_origin}\r\n\r\n";
                    _returnData += $"Sec-WebSocket-Location:ws://{_header["Host"]}{_header["Path"]}\r\n\r\n";

                    uint _key1 = GetWebSocketKeyValue(_header["Sec-WebSocket-Key1"]);
                    uint _key2 = GetWebSocketKeyValue(_header["Sec-WebSocket-Key2"]);

                    string _keyExt = _lines[_lines.Length - 1];
                    if (_keyExt.Length < 8) { _packageSize = 0; _session.Disconnect(); return null; }

                    byte[] _buffer = new byte[16];
                    byte[] _key1Bytes = BitConverter.GetBytes(_key1);
                    byte[] _key2Bytes = BitConverter.GetBytes(_key2);
                    byte[] _keyExtBytes = Encoding.UTF8.GetBytes(_keyExt);
                    Array.Copy(_key1Bytes, 0, _buffer, 0, _key1Bytes.Length);
                    Array.Copy(_key2Bytes, 0, _buffer, _key2Bytes.Length, _key2Bytes.Length);
                    Array.Copy(_keyExtBytes, 0, _buffer, _key1Bytes.Length + _key2Bytes.Length, _keyExtBytes.Length);
                    _returnData += Encrypt.MD5.Encode(_buffer);

                    _session.SendBytes(Encoding.UTF8.GetBytes(_returnData));
                    foreach (KeyValuePair<string, string> _item in _header) { _session[$"{_item.Key}"] = _item.Value; }
                    _session["__I__N__I__T__"] = true;
                    return new WstpPackage(WstpPackageType.Init);
                }
                #endregion
            }

            int _start = 0;
            _packageSize = 0;
            WstpPackage _package = null;
            if (_byteArray.Length == 0) { return _package; }

            while (true)
            {
                int _fin = (_byteArray[_start] & 0x80) == 0x80 ? 1 : 0;
                if (_fin == 0) { break; }

                int _op = _byteArray[_start] & 0x0F;
                int _len = _byteArray[_start + 1] & 0x7F;

                int _lenByByte = 1;
                int _maskByByte = 4;
                if (_len == 126) { _lenByByte = 3; } else if (_len == 127) { _lenByByte = 9; }
                if ((_byteArray.Length - _start) < (1 + _lenByByte + _maskByByte)) { break; }
                if (_len == 126)
                {
                    byte[] _buffer = new byte[2];
                    Array.Copy(_byteArray, _start + 2, _buffer, 0, _buffer.Length);
                    _len = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_buffer, 0));
                }
                else if (_len == 127)
                {
                    byte[] _buffer = new byte[8];
                    Array.Copy(_byteArray, _start + 2, _buffer, 0, _buffer.Length);
                    _len = (int)System.Net.IPAddress.NetworkToHostOrder((long)BitConverter.ToInt64(_buffer, 0));
                }
                if ((_byteArray.Length - _start) < (1 + _lenByByte + _maskByByte + _len)) { break; }

                if (_op == 8)
                {
                    _session.SendPackage(new WstpPackage(WstpPackageType.Close));
                    _session.Disconnect();
                    _package = new WstpPackage(WstpPackageType.Close);
                }
                else if (_op == 9)
                {
                    _session.SendPackage(new WstpPackage(WstpPackageType.Pong));
                    _package = new WstpPackage(WstpPackageType.Ping);
                }

                byte[] _mask = new byte[_maskByByte];
                Array.Copy(_byteArray, _start + 1 + _lenByByte, _mask, 0, _maskByByte);
                byte[] _payload = new byte[_len];
                Array.Copy(_byteArray, _start + 1 + _lenByByte + _maskByByte, _payload, 0, _len);

                if (_package == null && _op == 1) { _package = new WstpPackage(WstpPackageType.Text); }
                if (_package == null && _op == 2) { _package = new WstpPackage(WstpPackageType.Binary); }
                if (_package == null) { return null; }

                int _binaryLen = _package.Binary.Length;
                Array.Resize(ref _package.Binary, _package.Binary.Length + _payload.Length);
                for (int i = 0; i < _payload.Length; i++) { _package.Binary[_binaryLen + i] = (byte)(_payload[i] ^ _mask[i % 4]); }
                _start = _start + 1 + _lenByByte + _maskByByte + _len;
                if (_fin == 1) { break; }
            }

            _packageSize = uint.Parse(_start.ToString());
            return _package;
        }
        #endregion

        #region GetWebSocketKeyValue
        public uint GetWebSocketKeyValue(string _key)
        {
            uint _result = 0,_space=0;
            string _number = "";

            foreach(char _k in _key)
            {
                if (_k == ' ') { _space++; }
                if (_k >= '0' && _k<='9') { _number+=_k; }
            }
            if (_space > 0) { _result = uint.Parse(_number) / _space; }

            return uint.Parse(System.Net.IPAddress.NetworkToHostOrder(_result).ToString());
        }
        #endregion
    }

    #region WstpPackage
    public class WstpPackage : IDisposable
    {
        public WstpPackageType Type;
        public byte[] Binary = new byte[0];

        public WstpPackage(WstpPackageType _type) { Type = _type; Binary = new byte[0]; }
        public WstpPackage(WstpPackageType _type, byte[] _binary) { Type = _type; Binary = _binary; }

        public void Dispose() { Binary = null; }
    }
    #endregion

    #region WstpPackageType
    public enum WstpPackageType
    {
        Init = 0,
        Text = 1,
        Binary = 2,
        Close = 8,
        Ping = 9,
        Pong = 10
    }
    #endregion

}
