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
            byte[] _block = new byte[1024];
            while (true)
            {
                int _count = _zip.Read(_block, 0, _block.Length);
                if (_count <= 0)
                    break;
                else
                    _stream.Write(_block, 0, _count);
            }
            
            _zip.Close();
            return _stream.ToArray();
        }
    }
}
