using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Lion.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.ChuangLan
{
    public class ChuangLanSDK
    {
        private static string Url = "https://smssh1.253.com/msg/v1/send/json ";
        public static string Key = "";
        public static string Secret = "";

        public static void Init(JObject _settings)
        {
            Key = _settings["Key"].Value<string>();
            Secret = _settings["Secret"].Value<string>();
        }


        public static bool Send(string _phone,string _text)
        {
            JObject _json = new JObject();
            _json["account"] = Key;
            _json["password"] = Secret;
            _json["phone"] = _phone;
            _json["msg"] = _text;
            _json["report"] = "true";

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", Url, "");
            _http.Request.Credentials = new NetworkCredential(Key, Secret);
            _http.Request.ContentType = "application/json";
            _http.EndResponse(Encoding.UTF8.GetBytes(_json.ToString(Formatting.None)));

            string _result = _http.GetResponseString(Encoding.UTF8);

            JObject _resultJson = JObject.Parse(_result);
            return _resultJson.ContainsKey("code") && _resultJson["code"].Value<string>() == "0";
        }
    }
}
