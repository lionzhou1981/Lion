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
    }
}
