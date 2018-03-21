using System.IO;
using System.IO.Compression;

namespace  Lion.Encrypt
{
    public class GZip
    {
        public static byte[] Compress(byte[] _binary)
        {
            MemoryStream _stream = new MemoryStream();
            GZipStream _zip = new GZipStream(_stream, CompressionMode.Compress);
            _zip.Write(_binary, 0, _binary.Length);
            _zip.Close();
            return _stream.ToArray();
        }

        public static byte[] Decompress(byte[] _binary,int _bufferSize = 4096)
        {
            MemoryStream _stream = new MemoryStream();

            GZipStream _zip = new GZipStream(new MemoryStream(_binary), CompressionMode.Decompress);
            byte[] _buffer = new byte[_bufferSize];
            int _count;
            while ((_count = _zip.Read(_buffer, 0, _buffer.Length)) != 0)
            {
                _stream.Write(_buffer, 0, _count);
            }
            _zip.Close();
            return _stream.ToArray();
        }
    }
}
