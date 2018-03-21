using System.Net;

namespace Lion
{
    public class IPAddressPlus
    {
        public static long StringToLong(string _ip)
        {
            IPAddress _ipAddress = null;
            if (IPAddress.TryParse(_ip, out _ipAddress))
            {
                byte[] _bytes = _ipAddress.GetAddressBytes();
                long _a = long.Parse(((int)_bytes[0]).ToString());
                long _b = long.Parse(((int)_bytes[1]).ToString());
                long _c = long.Parse(((int)_bytes[2]).ToString());
                long _d = long.Parse(((int)_bytes[3]).ToString());
                return _a * 256 * 256 * 256 + _b * 256 * 256 + _c * 256 + _d;
            }
            else
            {
                return 0;
            }
        }

        public static string Long2String(long _ip)
        {
            int[] _bytes = new int[4];
            _bytes[0] = (int)((_ip >> 24) & 0xff);
            _bytes[1] = (int)((_ip >> 16) & 0xff);
            _bytes[2] = (int)((_ip >> 8) & 0xff);
            _bytes[3] = (int)(_ip & 0xff);

            string _return =
                _bytes[0].ToString() + "." +
                _bytes[1].ToString() + "." +
                _bytes[2].ToString() + "." +
                _bytes[3].ToString();

            return _return;
        }
    }
}
