using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Google
{
    public class GoogleReCaptcha
    {
        public static bool Verify(string _responseToken, string _secret)
        {
            Dictionary<string, string> _dicForms = new Dictionary<string, string>();
            _dicForms.Add("secret", _secret);
            _dicForms.Add("response", _responseToken);
            string _result = "";
            if(Lion.Net.HttpClient.PostAsFormData("https://www.google.com/recaptcha/api/siteverify", new Dictionary<string, string>(), _dicForms, out _result))
            {
                var _resultJson = JObject.Parse(_result);
                return _resultJson.ContainsKey("success") && _resultJson["success"].Value<bool>();
            }
            return false;            
        }
    }
}
