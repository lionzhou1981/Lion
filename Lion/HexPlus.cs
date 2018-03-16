using System;
using System.Linq;

namespace Lion
{
    public class HexPlus
    {
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

        public static byte[] HexStringToByteArray(string _hex)
        {
            return Enumerable.Range(0, _hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(_hex.Substring(x, 2), 16)).ToArray();
        }
    }
}
