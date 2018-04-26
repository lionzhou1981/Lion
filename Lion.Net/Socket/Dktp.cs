using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lion.Encrypt;
using Newtonsoft.Json.Linq;

namespace Lion.Net.Sockets
{
    public class Dktp : ISocketProtocol
    {
        public string Code = "";
        private string socketKey = "";
        private string socketTxKey = "";
        private string socketRxKey = "";
        private string socketChecksum = "";
        private RSA rsa;

        public Dktp(string _code, string _key, string _rsaPub, string _rsaPri)
        {
            this.Code = _code;
            this.socketKey = _key;
            this.rsa = new RSA(RSAType.RSA, System.Text.Encoding.UTF8, _rsaPri, _rsaPub);
        }

        private uint keepAlive = 0;
        public uint KeepAlive { get { return this.keepAlive; } set { this.keepAlive = value; } }

        public object KeepAlivePackage { get { return JObject.Parse("{\"id\":\"10001\"}"); } }

        #region Check
        public bool Check(byte[] _byteArray, bool _completely = false, SocketSession _session = null)
        {
            if (_byteArray.Length < 20) { return false; }

            byte[] _lengthArray = new byte[4];
            Array.Copy(_byteArray, _lengthArray, 4);
            if (BitConverter.IsLittleEndian) { Array.Reverse(_lengthArray); }
            int _length = BitConverter.ToInt32(_lengthArray, 0);

            if (_byteArray.Length < _length) { return false; }

            string _key = "";
            if (_session == null)
            {
                _key = this.socketRxKey == "" ? this.socketKey : this.socketRxKey;
            }
            else
            {
                _key = (_session["RxKey"] + "") == "" ? this.socketKey : (_session["RxKey"] + "");
            }
            if (_key == "") { throw new Exception("DePackage failed: RxKey is empty."); }

            byte[] _checkArray = new byte[_length - 16];
            Array.Copy(_byteArray, _checkArray, _checkArray.Length);
            string _check = System.Text.Encoding.UTF8.GetString(MD5.Encode2ByteArray(_checkArray));

            byte[] _md5Array = new byte[16];
            Array.Copy(_byteArray, _length - 16, _md5Array, 0, 16);
            string _md5 = System.Text.Encoding.UTF8.GetString(_md5Array);
            if (_check != _md5) { return false; }

            try
            {
                byte[] _contentArray = new byte[_length - 20];
                Array.Copy(_byteArray, 4, _contentArray, 0, _contentArray.Length);
                string _decoded = System.Text.Encoding.UTF8.GetString(DES.Decode2ByteArray(_contentArray, _key, System.Security.Cryptography.CipherMode.CBC));
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region DePackage
        public object DePackage(byte[] _byteArray, out uint _packageSize, bool _completely, SocketSession _session = null)
        {
            if (!this.Check(_byteArray, _completely, _session)) { throw new Exception("Can not depackage this stream."); }

            string _key = "";
            if (_session == null)
            {
                _key = this.socketRxKey == "" ? this.socketKey : this.socketRxKey;
            }
            else
            {
                _key = (_session["RxKey"] + "") == "" ? this.socketKey : (_session["RxKey"] + "");
            }
            if (_key == "") { throw new Exception("DePackage failed: RxKey is empty."); }

            byte[] _lengthArray = new byte[4];
            Array.Copy(_byteArray, _lengthArray, 4);
            if (BitConverter.IsLittleEndian) { Array.Reverse(_lengthArray); }
            _packageSize = BitConverter.ToUInt32(_lengthArray, 0); 

            byte[] _checkArray = new byte[_packageSize - 16];
            Array.Copy(_byteArray, _checkArray, _checkArray.Length);
            string _check = System.Text.Encoding.UTF8.GetString(MD5.Encode2ByteArray(_checkArray));

            byte[] _md5Array = new byte[16];
            Array.Copy(_byteArray, int.Parse(_packageSize.ToString()) - 16, _md5Array, 0, 16);
            string _md5 = System.Text.Encoding.UTF8.GetString(_md5Array);

            if (_check != _md5) { return ""; }

            try
            {
                byte[] _contentArray = new byte[_packageSize - 20];
                Array.Copy(_byteArray, 4, _contentArray, 0, _contentArray.Length);
                string _decoded = System.Text.Encoding.UTF8.GetString(DES.Decode2ByteArray(_contentArray, _key, System.Security.Cryptography.CipherMode.CBC));
                JObject _json = JObject.Parse(_decoded);
                return _json;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region EnPackage
        public byte[] EnPackage(object _object, SocketSession _session = null)
        {
            string _key = "";
            if (_session == null)
            {
                _key = this.socketTxKey == "" ? this.socketKey : this.socketTxKey;
            }
            else
            {
                _key = _session["TxKey"] + "";
            }
            if (_key == "") { throw new Exception("EnPackage failed: TxKey is empty."); }

            string _content = ((JObject)_object).ToString(Newtonsoft.Json.Formatting.None);
            byte[] _contentArray = DES.Encode2ByteArray(System.Text.Encoding.UTF8.GetBytes(_content), _key, System.Security.Cryptography.CipherMode.CBC);
            uint _length = uint.Parse(_contentArray.Length.ToString()) + 4 + 16;
            byte[] _lengthArray = BitConverter.GetBytes(_length);
            if (BitConverter.IsLittleEndian) { Array.Reverse(_lengthArray); }

            byte[] _byteArray = new byte[_length - 16];
            Array.Copy(_lengthArray, _byteArray, 4);
            Array.Copy(_contentArray, 0, _byteArray, 4, _contentArray.Length);

            byte[] _md5Array = MD5.Encode2ByteArray(_byteArray);
            Array.Resize(ref _byteArray, _byteArray.Length + 16);
            Array.Copy(_md5Array, 0, _byteArray, _byteArray.Length - 16, 16);

            return _byteArray;
        }
        #endregion

        #region IsKeepAlivePackage
        public bool IsKeepAlivePackage(object _object, object _socket)
        {
            JObject _json = (JObject)_object;
            if (_json["id"] == null) { return false; }
            if (_json["id"].Value<string>() == "10002") { return true; }
            if (_json["id"].Value<string>() != "10001") { return false; }

            _json = new JObject();
            _json["id"] = "10002";

            if (_socket is SocketEngine) { ((SocketEngine)_socket).SendPackage(_json); }
            if (_socket is SocketSession) { ((SocketSession)_socket).SendPackage(_json); }

            return true;
        }
        #endregion

        #region SendFirstPackage
        public void SendFirstPackage(SocketEngine _socket)
        {
            Random _random = new Random(RandomPlus.RandomSeed);
            this.socketRxKey = _random.Next(10000000, 100000000).ToString();
            this.socketChecksum= _random.Next(10000000, 100000000).ToString();

            Console.WriteLine("KEY :" + this.socketRxKey);

            JObject _json = new JObject();
            _json["code"] = this.Code;
            _json["time"] = DateTimePlus.DateTime2JSTime(DateTime.UtcNow).ToString();
            _json["num"] = this.socketChecksum;
            _json["key"] = this.socketRxKey;

            _socket.SendPackage(_json);
        }
        #endregion

        #region ReceiveFirstPackage
        public bool ReceiveFirstPackage(JObject _json, SocketEngine _socket)
        {
            if (this.socketTxKey != "") { return false; }

            Console.WriteLine("FK:" + this.socketTxKey);
            if (!this.rsa.Verify(this.socketChecksum, _json["check"].Value<string>()))
            {
                Console.WriteLine("RSA Failed");
                this.socketTxKey = "";
                return true;
            }
            this.socketTxKey = _json["key"].Value<string>();
            Console.WriteLine("FK:" + this.socketTxKey);

            JObject _result = new JObject();
            _result["check"] = this.rsa.Sign(_json["num"].Value<string>());

            _socket.SendPackage(_result);
            _socket.Handshaked = true;
            return true;
        }
        public bool ReceiveFirstPackage(JObject _json, SocketSession _session)
        {
            if (_session["TxKey"] + "" != "" && _session["Checksum"] + "" == "DONE") { return false; }

            if (_session["RxKey"] + "" == "")
            {
                DateTime _time = DateTimePlus.JSTime2DateTime(long.Parse(_json["time"].Value<string>()));
                if (Math.Abs((_time - DateTime.UtcNow).TotalSeconds) > 300) { _session.Disconnect(); return true; }

                _session["TxKey"] = _json["key"].Value<string>();

                Random _random = new Random(RandomPlus.RandomSeed);
                string _rxKey = _random.Next(10000000, 100000000).ToString();
                _session["RxKey"] = _rxKey;
                string _checksum = _random.Next(10000000, 100000000).ToString();
                _session["Checksum"] = _checksum;

                JObject _result = new JObject();
                _result["check"] = this.rsa.Sign(_json["num"].Value<string>());
                _result["num"] = _checksum;
                _result["key"] = _rxKey;

                _session.SendPackage(_result);
            }
            else if (_session["Checksum"] + "" != "DONE")
            {
                if (!this.rsa.Verify(_session["Checksum"] + "", _json["check"].Value<string>()))
                {
                    this.socketTxKey = "";
                    return true;
                }

                _session["Checksum"] = "DONE";
                _session.Handshaked = true;
            }
            return true;
        }
        #endregion
    }
}
