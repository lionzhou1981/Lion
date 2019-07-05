using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lion.SDK.Mailgun
{
    public class MailSender
    {
        string apiKey;
        string domain;
        public MailSender(string _apiKey, string _domain)
        {
            apiKey = _apiKey;
            domain = _domain;
        }

        public string ApiKey { get => apiKey; set => apiKey = value; }
        public string Domain { get => domain; set => domain = value; }

        public bool Send(string _subject, string _from, string _to, string _nickName, string _content)
        {
            ServicePointManager.Expect100Continue = false;
            CredentialCache _credentialCache = new CredentialCache();
            _credentialCache.Add(new Uri("https://api.mailgun.net/v3"), "Basic", new NetworkCredential("api", ApiKey));
            Dictionary<string, string> _headers = new Dictionary<string, string>();
            _headers.Add("key", ApiKey);
            Dictionary<string, string> _formdata = new Dictionary<string, string>();
            _formdata.Add("domain", Domain);
            _formdata.Add("from", _from);
            _formdata.Add("to", _to);
            _formdata.Add("subject", _subject);
            _formdata.Add("text", _content);
            string _result = "";
            if (Lion.Net.HttpClient.PostAsFormData("https://api.mailgun.net/v3/mail.isdice.com/messages", _headers, _formdata, out _result, _credentialCache, 120 * 1000))
            {
                return !string.IsNullOrWhiteSpace(JObject.Parse(_result)["id"].ToString());
            }
            return false;
        }
    }
}
