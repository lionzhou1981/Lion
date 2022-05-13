using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Mailgun
{
    public class NotifyReceiver
    {
        public static bool Verify(string _signKey, JObject _received)
        {
            try
            {
                var _message = $"{_received["timestamp"]}{_received["token"]}";
                return BitConverter.ToString(Lion.Encrypt.SHA.EncodeHMACSHA256(_signKey, _message)).ToLower().Replace("-", "") == _received["signature"].ToString().ToLower();
            }
            catch
            {
                return false;
            }
        }
    }
}
