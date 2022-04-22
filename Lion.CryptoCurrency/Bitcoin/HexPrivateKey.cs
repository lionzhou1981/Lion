using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class HexPrivateKey : IPrivateKey
    {
        public string PublicKey
        {
            get
            {
                return Lion.CryptoCurrency.Bitcoin.Address.Private2Public(PrivateKey, false, false);
            }
        }

        public string PrivateKey { get; }

        public bool Compressed
        {
            get
            {
                return false;
            }
        }

        public string RawPrivateKey { get; }

        public string Address { get; }
        public HexPrivateKey(string _address,string _hex)
        {
            Address = _address;
            PrivateKey = _hex;
            RawPrivateKey = _hex;
        }


    }
}
