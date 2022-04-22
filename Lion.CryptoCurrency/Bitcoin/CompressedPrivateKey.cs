using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class CompressedPrivateKey : IPrivateKey
    {
        public string PublicKey
        {
            get
            {
                return Lion.CryptoCurrency.Bitcoin.Address.Private2Public(PrivateKey, false, true);
            }
        }

        public string PrivateKey { get; }

        public bool Compressed
        {
            get
            {
                return true;
            }
        }

        public string RawPrivateKey { get; }

        public string Address { get; }

        public CompressedPrivateKey(string _address,string _uncompressed)
        {
            Address = _address;
            PrivateKey = _uncompressed;
            RawPrivateKey = _uncompressed;
        }
    }
}
