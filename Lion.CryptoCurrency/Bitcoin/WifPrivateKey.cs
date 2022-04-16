using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Bitcoin
{
    public class WifPrivateKey : IPrivateKey
    {
        public string PublicKey
        {
            get
            {
                return Address.Private2Public(PrivateKey, false, Compressed);
            }
        }

        public string PrivateKey
        {
            get; 
        }
        public bool Compressed
        {
            get
            {
                return compressed;
            }
        }

        public string RawPrivateKey { get; }

        private bool mainNet;
        private bool compressed;

        public WifPrivateKey(string _wifKey)
        {
            PrivateKey = Address.Wif2Private(_wifKey, out mainNet, out compressed);
            RawPrivateKey = _wifKey;
        }
    }
}
