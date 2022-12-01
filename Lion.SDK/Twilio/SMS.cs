using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Lion.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Twilio
{
    public class SMS
    {
        private static string Url = "https://api.twilio.com/2010-04-01";

        public static string Account = "";
        public static string Secret = "";
        public static string ServiceId = "";

        public static bool Send(string _phone, string _text)
        {
            string _data = $"MessagingServiceSid={Uri.EscapeDataString(ServiceId)}&To={Uri.EscapeDataString(_phone)}&Body={Uri.EscapeDataString(_text)}";
            string _url = $"{Url}/Accounts/{Account}/Messages.json";

            HttpClient _http = new HttpClient(5000);
            _http.BeginResponse("POST", _url, "");
            _http.Request.Credentials = new NetworkCredential(Account, Secret);
            _http.EndResponse(Encoding.UTF8.GetBytes(_data));
            string _result = _http.GetResponseString(Encoding.UTF8);

            JObject _resultJson = JObject.Parse(_result);
            return _resultJson.ContainsKey("status") && _resultJson["status"].Value<string>() == "accepted";
        }
    }
}
