using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Waves
    {
        #region IsAddress
        public static bool IsAddress(string _address)
        {
            try
            {
                var _bytes = Lion.Encrypt.Base58.Decode(_address);
                return _bytes.Count() > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
