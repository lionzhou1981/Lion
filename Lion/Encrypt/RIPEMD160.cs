using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Encrypt
{
    public abstract class RIPEMD160 : System.Security.Cryptography.HashAlgorithm
    {
        public RIPEMD160()
        {
        }

        public new static RIPEMD160 Create()
        {
            return new RIPEMD160Managed();
        }

        public new static RIPEMD160 Create(string hashname)
        {
            return new RIPEMD160Managed();
        }
    }
}
