using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Google
{
    public class reCaptcha
    {
        private string Secret;
        const string UrlToVerify = "https://www.google.com/recaptcha/api/siteverify";
        public reCaptcha(string _secret)
        {
            Secret = _secret;
        }

        public bool Verify(string _responseToken)
        {
            Dictionary<string, string> _dicForms = new Dictionary<string, string>();
            _dicForms.Add("secret", Secret);
            _dicForms.Add("response", _responseToken);
            string _result = "";
            if(Lion.Net.HttpClient.PostAsFormData(UrlToVerify, new Dictionary<string, string>(), _dicForms, out _result))
            {
                var _resultJson = JObject.Parse(_result);
                return _resultJson.ContainsKey("success") && _resultJson["success"].Value<bool>();
            }
            return false;            
        }


        
    }
}
