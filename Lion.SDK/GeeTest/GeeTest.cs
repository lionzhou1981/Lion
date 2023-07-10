using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Lion.SDK.GeeTest
{
    public static class GeeTest
    {
        static string Id = "";
        static string Key = "";
        const string ApiServer = "http://gcaptcha4.geetest.com/validate?captcha_id=";
        public static void Init(string _id,string _key)
        {
            Id = _id;
            Key = _key;
        }

        public static bool Test(string _lotNumber,string _captchaOutput,string _passToken,string _genTime)
        {
            using var _hmacsha256 = new HMACSHA256(UTF8Encoding.UTF8.GetBytes(Key));
            byte[] _signed = _hmacsha256.ComputeHash(UTF8Encoding.UTF8.GetBytes(_lotNumber));
            var _form = new Dictionary<string, string>();
            _form["lot_number"] = _lotNumber;
            _form["captcha_output"] = _captchaOutput;
            _form["pass_token"] = _passToken;
            _form["gen_time"] = _genTime;
            _form["sign_token"] = HexPlus.ByteArrayToHexString(_signed);
            try
            {
                HttpClient.PostAsFormData(ApiServer + Id, new Dictionary<string, string>(), _form, out string _resp);
                var _re = JObject.Parse(_resp);
                return _re["result"].ToString() == "success";
            }
            catch
            {
                return false;
            }
        }
    }
}
