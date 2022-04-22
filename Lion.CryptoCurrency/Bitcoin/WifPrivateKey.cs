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
                return Lion.CryptoCurrency.Bitcoin.Address.Private2Public(PrivateKey, false, Compressed);
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

        public string Address {get;set;}

        private bool mainNet;
        private bool compressed;

        public WifPrivateKey(string _address,string _wifKey)
        {
            Address = _address;
            PrivateKey = Lion.CryptoCurrency.Bitcoin.Address.Wif2Private(_wifKey, out mainNet, out compressed);
            RawPrivateKey = _wifKey;
        }
    }
}
