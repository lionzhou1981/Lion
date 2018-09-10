using System;
using System.Collections.Generic;
using System.Text;
using Lion.Encrypt;
using System.Linq;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Eos
    {
        public static bool IsAddress(string _pubKey)
        {
            try
            {
                if (!_pubKey.StartsWith("EOS"))
                    return false;
                var _decoded = Lion.Encrypt.Base58.Decode(_pubKey.Substring(3));
                var _checksum = HexPlus.ByteArrayToHexString(_decoded.ToList().Skip(_decoded.Length - 4).Take(4).ToArray());
                var _keys = _decoded.ToList().Take(_decoded.Length - 4).ToArray();
                var _encoded = HexPlus.ByteArrayToHexString(GetRMD160Hash(_keys).Take(4).ToArray());
                return _encoded == _checksum;
            }
            catch
            {
                return false;
            }
        }

        static byte[] GetRMD160Hash(byte[] myByte)
        {
            RIPEMD160 _r160 = RIPEMD160Managed.Create();
            byte[] _encrypted = _r160.ComputeHash(myByte);
            ; return _encrypted;
        }
    }
}
