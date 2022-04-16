using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency
{
    public interface IPrivateKey
    {
        public string PublicKey { get; }
        public string PrivateKey { get;  }

        public bool Compressed { get; }

        public string RawPrivateKey { get; }

    }
}
