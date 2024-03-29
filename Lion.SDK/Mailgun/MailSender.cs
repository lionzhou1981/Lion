﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Mailgun
{
    public class MailSender
    {
        string apiKey;
        string domain;
        string apiHost;
        public MailSender(string _apiKey, string _domain,string _apiHost)
        {
            apiKey = _apiKey;
            domain = _domain;
            apiHost = _apiHost;
        }

        public string ApiKey { get => apiKey; set => apiKey = value; }
        public string Domain { get => domain; set => domain = value; }

        public string ApiHost { get => apiHost; set => apiHost = value; }

        public bool Send(string _subject, string _from, string _to,string _senderName, string _nickName, string _content, bool _ishtml, out string _result)
        {
            _result = "";
            ServicePointManager.Expect100Continue = false;
            CredentialCache _credentialCache = new CredentialCache();
            _credentialCache.Add(new Uri(ApiHost), "Basic", new NetworkCredential("api", ApiKey));
            Dictionary<string, string> _headers = new Dictionary<string, string>();
            _headers.Add("key", ApiKey);
            Dictionary<string, string> _formdata = new Dictionary<string, string>();
            _formdata.Add("domain", Domain);
            _formdata.Add("from",$"{_senderName} <{_from}>");
            _formdata.Add("to", $"{_nickName} <{_to}>");
            _formdata.Add("subject", _subject);
            if (!_ishtml)
                _formdata.Add("text", _content);
            else
                _formdata.Add("html", _content);
            if (Lion.Net.HttpClient.PostAsFormData($"{ApiHost.TrimEnd('/')}/{Domain}/messages", _headers, _formdata, out _result, _credentialCache, 120 * 1000))
            {
                return !string.IsNullOrWhiteSpace(JObject.Parse(_result)["id"].ToString());
            }
            return false;
        }
    }
}
