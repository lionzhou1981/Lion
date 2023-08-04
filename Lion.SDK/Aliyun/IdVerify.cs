using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Aliyun
{
    public static class IdVerify
    {
        static string Code;
        static bool Inited = false;
        const string ApiUrl = "https://dfphone3.market.alicloudapi.com/verify_id_name_phone";
        public static void Init(string _code)
        {
            Code = _code;
            Inited = true;
        }
        public static bool Verify(string _name,string _mobile,string _idNum)
        {
            if (!Inited)
                throw new Exception("Not inited");
            HttpClient _client = new HttpClient(60 * 1000);
            _client.Headers.Add("Authorization", $"APPCODE {Code}");
            _client.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            var _postData = $"id_number={_idNum}&name={_name}&phone_number={_mobile}";
            var _result = JObject.Parse(_client.GetResponseString("POST", ApiUrl, ApiUrl, _postData));
            return _result["state"].ToString() == "1";
        }
    }
}
