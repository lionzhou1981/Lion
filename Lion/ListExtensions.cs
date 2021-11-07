using System.Collections.Generic;

namespace Lion
{
    public static class ListExtensions
    {
        public static void AddAndPadRight(this List<byte> _bytes, int _length,byte _padByte, params byte[] _addBytes)
        {
            _bytes.AddRange(_addBytes);
            for (int i = 0; _length > _addBytes.Length && i < _length - _addBytes.Length; i++) { _bytes.Add(_padByte); }
        }
    }
}
