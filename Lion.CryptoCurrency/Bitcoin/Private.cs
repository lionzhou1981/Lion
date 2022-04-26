using System;
using System.Collections.Generic;
using System.Text;
using Lion.CryptoCurrency.Bitcoin;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class Private
    {
        public static Private FromWif(string _address, string _wif, out bool _mainnet)
        {
            Private _private = new Private();
            _private.Raw = Bitcoin.Address.Wif2Private(_wif, out _mainnet, out _private.Compressed);
            _private.Public = Bitcoin.Address.Private2Public(_private.Raw, false, _private.Compressed);
            _private.Address = _address;
            _private.Wif = _wif;
            return _private;
        }

        public static Private FromHex(string _address, string _hex, bool _mainnet = true, bool _compressed = false)
        {
            Private _private = new Private();
            _private.Raw = _hex;
            _private.Compressed = _compressed;
            _private.Public = Bitcoin.Address.Private2Public(_private.Raw, false, _compressed);
            _private.Address = _address;
            _private.Wif = Bitcoin.Address.Private2Wif(_hex, _mainnet, _compressed);
            return _private;
        }

        public string Address;
        public string Public;
        public string Raw;
        public bool Compressed;
        public string Wif;
    }
}
