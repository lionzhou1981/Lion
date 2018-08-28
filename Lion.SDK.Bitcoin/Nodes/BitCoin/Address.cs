using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lion.SDK.Bitcoin.Nodes.BitCoin
{
    public static class Address
    {
        public static bool IsAddress(string _address)
        {
            string _convertedAddress = "";
            if(Decode(_address,ref _convertedAddress))
            {
                if (string.IsNullOrWhiteSpace(_convertedAddress))
                {
                    var _lastValidate = _convertedAddress.Substring(_convertedAddress.Length - 6);
                    var _addressSub = _convertedAddress.Substring(2, _convertedAddress.Length - 8);
                    var _version = _convertedAddress.Substring(0, 2);
                    if (_addressSub.Length != 40)
                        return false;
                }
            }
            return false;
        }

        static string Base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        static bool Decode(string _address, ref string _resultAddress)
        {
            try
            {
                int i = 0;
                while (i < _address.Length)
                {
                    if (_address[i] == 0 || !Char.IsWhiteSpace(_address[i]))
                        break;
                    i++;
                }
                int _zeros = 0;
                while (_address[i] == '1')
                {
                    _zeros++;
                    i++;
                }
                byte[] _byte256 = new byte[(_address.Length - i) * 733 / 1000 + 1];
                while (i < _address.Length && !Char.IsWhiteSpace(_address[i]))
                {
                    int ch = Base58Chars.IndexOf(_address[i]);
                    if (ch == -1)
                        return false;
                    int carry = Base58Chars.IndexOf(_address[i]);
                    for (int k = _byte256.Length - 1; k >= 0; k--)
                    {
                        carry += 58 * _byte256[k];
                        _byte256[k] = (byte)(carry % 256);
                        carry /= 256;
                    }
                    i++;
                }
                while (i < _address.Length && Char.IsWhiteSpace(_address[i]))
                    i++;
                if (i != _address.Length)
                    return false;
                int j = 0;
                while (j < _byte256.Length && _byte256[j] == 0)
                    j++;
                var _result = new byte[_zeros + (_byte256.Length - j)];
                for (int kk = 0; kk < _result.Length; kk++)
                {
                    if (kk < _zeros)
                        _result[kk] = 0x00;
                    else
                        _result[kk] = _byte256[j++];
                }
                _resultAddress = BitConverter.ToString(_result).Replace("-", "");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
