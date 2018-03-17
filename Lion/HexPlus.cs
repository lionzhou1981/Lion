using System;
using System.Linq;

namespace Lion
{
    /// <summary>
    /// A class deal with Hex
    /// </summary>
    public class HexPlus
    {
        #region ByteArrayToHexString
        /// <summary>
        /// Convert byte[] to hex string.
        /// </summary>
        /// <returns>hex string</returns>
        /// <param name="_byteArray">byte[]</param>
        /// <param name="_lower">Is low case</param>
        public static string ByteArrayToHexString(byte[] _byteArray, bool _lower = true)
        {
            string _result = string.Empty;
            if (_byteArray != null || _byteArray.Length > 0)
            {
                foreach (byte _byte in _byteArray)
                {
                    _result += string.Format("{0:X2}", _byte);
                }
            }
            if (_lower) { _result = _result.ToLower(); }
            return _result;
        }
        #endregion

        #region HexStringToByteArray
        /// <summary>
        /// Convert hex string to byte[]
        /// </summary>
        /// <returns>byte[]</returns>
        /// <param name="_hex">hex string.</param>
        public static byte[] HexStringToByteArray(string _hex)
        {
            return Enumerable.Range(0, _hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(_hex.Substring(x, 2), 16)).ToArray();
        }
        #endregion

        #region Concat
        public static byte[] Concat(params byte[][] _values)
        {
            byte[] _result = new byte[0];
            foreach (byte[] _value in _values)
            {
                int _start = _result.Length;
                Array.Resize(ref _result, _result.Length + _value.Length);
                Array.Copy(_value, 0, _result, _start, _value.Length);
            }
            return _result;
        }
        #endregion

        #region PadLeft
        public static byte[] PadLeft(byte[] _source, int _length, byte _fill = 0x00)
        {
            if (_source.Length > _length) { return _source; }

            byte[] _result = new byte[_length];
            for (int i = 0; i < _result.Length; i++) { _result[i] = _fill; }

            Array.Copy(_source, 0, _result, _result.Length - _source.Length, _source.Length);

            return _result;
        }
        #endregion

        #region PadRight
        public static byte[] PadRight(byte[] _source, int _length, byte _fill = 0x00)
        {
            if (_source.Length > _length) { return _source; }

            byte[] _result = new byte[_length];
            for (int i = 0; i < _result.Length; i++) { _result[i] = _fill; }

            Array.Copy(_source, 0, _result, 0, _source.Length);

            return _result;
        }
        #endregion
    }
}
