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
                return Address.Private2Public(PrivateKey, false, true);
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

        public CompressedPrivateKey(string _uncompressed)
        {
            PrivateKey = _uncompressed;
            RawPrivateKey = _uncompressed;
        }
    }
}
