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
                return Address.Private2Public(PrivateKey, false, false);
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

        public HexPrivateKey(string _hex)
        {
            PrivateKey = _hex;
            RawPrivateKey = _hex;
        }


    }
}
