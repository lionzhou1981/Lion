using System;
using Lion;
using Lion.Encrypt;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Bitcoin
    {
        public static bool IsAddress(string _address, out byte? _version)
        {
            try
            {
                byte[] _bytes = Base58.Decode(_address);
                if (_bytes.Length != 25) { throw new Exception(); }
                _version = _bytes[0];

                byte[] _byteBody = new byte[21];
                Array.Copy(_bytes, 0, _byteBody, 0, 21);
                byte[] _byteCheck = new byte[4];
                Array.Copy(_bytes, 21, _byteCheck, 0, 4);
                string _checkSum = HexPlus.ByteArrayToHexString(_byteCheck);

                byte[] _sha256A = SHA.EncodeSHA256(_byteBody);
                byte[] _sha256B = SHA.EncodeSHA256(_sha256A);
                Array.Copy(_sha256B, 0, _byteCheck, 0, 4);
                string _caleSum = HexPlus.ByteArrayToHexString(_byteCheck);

                return _checkSum == _caleSum;
            }
            catch
            {
                _version = null;
                return false;
            }
        }
    }
}