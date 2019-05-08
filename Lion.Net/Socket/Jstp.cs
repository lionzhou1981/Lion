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
    public class Jstp : ISocketProtocol
    {
        private string code = "";
        public string Code { get { return this.code; } set { this.code = value; } }

        private string key = "";

        private uint keepAlive = 0;
        public uint KeepAlive { get { return this.keepAlive; } set { this.keepAlive = value; } }

        public Jstp(string _code, string _key)
        {
            this.code = _code;
            this.key = _key;
        }

        public object KeepAlivePackage { get { return JObject.Parse("{\"id\":\"ping\"}"); } }

        #region IsKeepAlivePackage
        public bool IsKeepAlivePackage(object _object, object _socket)
        {
            JObject _json = (JObject)_object;
            string _id = _json.ContainsKey("id") ? _json["id"].Value<string>() : "";
            return _id == "ping" || _id == "pong";
        }
        #endregion

        public bool Check(byte[] _byteArray, bool _completely = false, SocketSession _session = null)
        {
            return this.DePackage(_byteArray, out uint _package) != null;
        }

        #region EnPackage
        public byte[] EnPackage(object _object, SocketSession _session = null)
        {
            string _json = ((JObject)_object).ToString(Newtonsoft.Json.Formatting.None);
            string _data = OpenSSLAes.Encode(_json, this.key) + "\n";

            return Encoding.UTF8.GetBytes(_data);
        }
        #endregion

        #region DePackage
        public object DePackage(byte[] _byteArray, out uint _packageSize, bool _completely = false, SocketSession _session = null)
        {
            string _data = Encoding.UTF8.GetString(_byteArray);

            int _index = _data.IndexOf('\n');
            _packageSize = uint.Parse((_index + 1).ToString());
            if (_index <= -1) { return null; }

            try
            {
                string _source = _data.Substring(0, _index);
                return JObject.Parse(OpenSSLAes.Decode(_source, this.key));
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
