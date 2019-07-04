using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Lion;
using Lion.Encrypt;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Bitcoin
{
    public class Address
    {
        public string Text = "";
        public string PublicKey = "";
        public string PrivateKey = "";
    }
}