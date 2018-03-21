using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace  Lion.Encrypt
{
    public class Serialization
    {
        #region Decode
        public static object Decode(byte[] _source)
        {
            MemoryStream _stream = new MemoryStream(_source);
            BinaryFormatter _binaryFormatter = new BinaryFormatter();
            object _return = _binaryFormatter.Deserialize(_stream);
            _stream.Close();

            return _return;
        }
        #endregion

        #region Encode
        public static byte[] Encode(object _source)
        {
            MemoryStream _stream = new MemoryStream();
            BinaryFormatter _binaryFormatter = new BinaryFormatter();
            _binaryFormatter.Serialize(_stream, _source);
            byte[] _return = _stream.GetBuffer();
            _stream.Close();

            return _return;
        }
        #endregion
    }
}
