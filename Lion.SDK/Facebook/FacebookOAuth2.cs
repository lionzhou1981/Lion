using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;
using Lion.Net;

namespace Lion.SDK.Facebook
{
    public class FacebookOAuth2
    {
        const string AuthUrl = "https://www.facebook.com/v15.0/dialog/oauth";
        const string TokenUrl = "https://graph.facebook.com/v15.0/oauth/access_token";
        const string UserInfoUrl = "https://graph.facebook.com/v15.0/";
        const string DebugTokenUrl = "https://graph.facebook.com/debug_token";

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
            _paras.Add("state", _state);
            _paras.Add("response_type", "code");
            _paras.Add("redirect_uri", System.Net.WebUtility.UrlEncode(redirectUrl));
            return $"{AuthUrl}?{string.Join("&", _paras.ToList().Select(t => String.Join("=", t.Key, t.Value)))}";
        }

        private static (string, DateTime) RefreshToken(string _code)
        {
            try
            {
                using WebClientPlus _web = new WebClientPlus(60 * 1000, true);
                _web.Proxy = new WebProxy("127.0.0.1", 1082);
                string _response = _web.DownloadString($"{TokenUrl}?client_id={cliendId}&client_secret={authKey}&redirect_uri={redirectUrl}&code={_code}");
                JObject _value = JObject.Parse(_response);
                string _token = _value["access_token"].ToString();
                DateTime _time = DateTime.Now.AddMinutes(-1).AddSeconds(_value["expires_in"].Value<int>());
                return (_token, _time);
            }
            catch
            {
                throw new Exception("Code error! token get error!");
            }
        }

        public static string GetUserInfo(string _code)
        {
            var _token = RefreshToken(_code);

            using WebClientPlus _web = new WebClientPlus(60 * 1000, true);
            _web.Proxy = new WebProxy("127.0.0.1", 1082);
            var _userId = "";
            try
            {
                var _tokenResult = JObject.Parse(_web.DownloadString($"{DebugTokenUrl}?input_token={_token.Item1}&access_token={cliendId}|{authKey}"));
                _userId = _tokenResult["data"]["user_id"].ToString();
            }
            catch
            {
                throw new Exception("get user token error");
            }
            try
            {
                return _web.DownloadString($"{UserInfoUrl}/{_userId}?access_token={cliendId}|{authKey}&fields=id,name,email");
            }
            catch
            {
                throw new Exception("get user info error");
            }
        }
    }
}
