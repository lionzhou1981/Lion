using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class XMR
    {
        const int AddressLength = 95;
        const int PaymentIdLength = 64;
        public static bool IsAddress(string _address)
        {
            _address = _address.Trim();
            var _paymentid = _address.Contains(":") ? _address.Split(':')[1] : "";
            _address = _address.Contains(":") ? _address.Split(':')[0] : _address;
            if (string.IsNullOrEmpty(_address) || _address.Length != AddressLength ||
                _address[0] != '4' ||
                _address[1] < '0' || _address[1] > 'B'
            )
            {
                return false;
            }
            for (var i = 2; i < _address.Length; i++)
            {
                var _currentChar = _address[i];
                if (_currentChar < '0' || _currentChar > 'z')
                {
                    return false;
                }
            }
            if(!string.IsNullOrWhiteSpace(_paymentid))
            {
                if (_paymentid.Length != PaymentIdLength)
                    return false;

                for (var i = _paymentid.Length - 1; i >= 0; i--)
                {
                    var currentChar = _paymentid[i];
                    if (currentChar < '0' || char.ToUpper(currentChar) > 'F')
                    {
                        return false;
                    }
                }

                return true;
            }
            return true;
        }
    }
}
