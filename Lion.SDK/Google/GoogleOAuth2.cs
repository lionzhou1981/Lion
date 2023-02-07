using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using Lion.Net;
using System.Net;

namespace Lion.SDK.Google
{
    public class GoogleOAuth2
    {
        const string AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        const string TokenUrl = "https://oauth2.googleapis.com/token";
        const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        private static string cliendId = "";
        private static string authKey = "";
        private static string redirectUrl = "";

        public static void Init(JObject _json)
        {
            cliendId = _json["Id"].Value<string>();
            authKey = _json["Key"].Value<string>();
            redirectUrl = _json["Url"].Value<string>();
        }

        public static string GetAuthUrl(string _state)
        {
            Dictionary<string, string> _paras = new Dictionary<string, string>();
            _paras.Add("client_id", cliendId);
            _paras.Add("scope", System.Net.WebUtility.UrlEncode("https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile openid"));
            _paras.Add("response_type", "code");
            _paras.Add("access_type", "online");
            _paras.Add("state", _state);
            _paras.Add("include_granted_scopes", "true");
            _paras.Add("redirect_uri", System.Net.WebUtility.UrlEncode(redirectUrl));
            return $"{AuthUrl}?{string.Join("&", _paras.ToList().Select(t => String.Join("=", t.Key, t.Value)))}";
        }

        private static (string, DateTime) RefreshToken(string _code)
        {
            try
            {
                using HttpClient _http = new Net.HttpClient(60 * 1000);
                var _response = _http.GetResponseString("POST", TokenUrl, "", $"code={_code}&client_id={cliendId}&client_secret={authKey}&grant_type=authorization_code&redirect_uri={System.Net.WebUtility.UrlEncode(redirectUrl)}");
                var _value = JObject.Parse(_response);
                string _token = _value["access_token"].ToString();
                DateTime _time = DateTime.Now.AddMinutes(-1).AddSeconds(_value["expires_in"].Value<int>());
                return (_token, _time);
            }
            catch
            {
                throw new Exception("Code error! Token get error!");
            }
        }

        public static string GetUserInfo(string _code)
        {
            var _token =  RefreshToken(_code);
            using WebClientPlus _http = new WebClientPlus(10000, true);
            return _http.DownloadString($"{UserInfoUrl}?access_token={_token.Item1}");
        }
    }
}
