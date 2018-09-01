using System;
using Lion;
using Lion.Encrypt;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Bitcoin
    {
        #region IsAddress
        public static bool IsAddress(string _address, out byte? _version)
        {
            try
            {
                if (_address.StartsWith("bc1") || _address.StartsWith("tb1"))
                {
                    #region Bech32
                    if (_address.Length == 42)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x00 : 0x6F);
                    }
                    else if (_address.Length == 62)
                    {
                        _version = (byte?)(_address.StartsWith("bc1") ? 0x05 : 0xC4);
                    }
                    else
                    {
                        _version = null;
                        return false;
                    }

                    try
                    {
                        Bech32.Bech32Decode(_address, out byte[] _hrp);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                    #endregion
                }
                else
                {
                    #region Base58
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
                    #endregion
                }
            }
            catch
            {
                _version = null;
                return false;
            }
        }
        #endregion
    }
}