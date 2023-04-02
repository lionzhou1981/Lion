using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Encrypt;
using Lion.Net;

namespace Lion.SDK.DingTalk
{
    public class DingTalk
    {
        private static string token = "";
        private static string secret = "";
        //priavte static string[] targets=

        public static void Init(JObject _settings)
        {
            token = _settings["Token"].Value<string>();
            secret = _settings["Secret"].Value<string>();
        }

        public static bool SendText(string _text)
        {
            DateTime _now = DateTime.UtcNow.AddHours(5);
            long _timestamp = DateTimePlus.DateTime2UnixTime(_now);
            string _signed = Sign(_timestamp);

            JObject _data = new JObject();
            _data["msgtype"] = "text";
            _data["text"] = new JObject() { ["content"] = _text };

            string _url = $"https://oapi.dingtalk.com/robot/send?access_token={token}&timestamp={_timestamp}&sign={_signed}";

            WebClientPlus _http = new WebClientPlus(5000);
            _http.Headers.Add("Content-Type", "application/json");
            string _result = _http.UploadString(_url, _data.ToString(Formatting.None));
            _http.Dispose();

            return true;
        }
        private static string Sign(long _timestamp)

        {
            return SHA.EncodeHMACSHA256ToBase64($"{_timestamp}\n{secret}", secret, Encoding.UTF8);
        }
    }
}
