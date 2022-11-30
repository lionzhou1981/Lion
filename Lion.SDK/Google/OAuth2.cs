using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Google
{
    public class OAuth2
    {
        private string CliendId = "";
        private string AuthKey = "";

        public OAuth2(string _clientId, string _authKey)
        {
            CliendId = _clientId;
            AuthKey = _authKey;
        }

        const string AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        /// <summary>
        /// 获取OAuth验证跳转
        /// </summary>
        /// <param name="_redirectUrl">验证结果跳转地址</param>
        /// <param name="_state">可用于验证跳转来源的随机数</param>
        /// <returns></returns>
        public string GetAuthUrl(string _authCodeRedirectUrl, string _state)
        {
            Dictionary<string, string> _paras = new Dictionary<string, string>();
            _paras.Add("client_id", CliendId);
            _paras.Add("scope", System.Net.WebUtility.UrlEncode("https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile openid"));
            _paras.Add("response_type", "code");
            _paras.Add("access_type", "online");
            _paras.Add("state", _state);
            _paras.Add("include_granted_scopes", "true");
            _paras.Add("redirect_uri", System.Net.WebUtility.UrlEncode(_authCodeRedirectUrl));
            return $"{AuthUrl}?{string.Join("&", _paras.ToList().Select(t => String.Join("=", t.Key, t.Value)))}";
        }

        const string TokenUrl = "https://oauth2.googleapis.com/token";
        const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        private DateTime tokenExpireTime = DateTime.Now;
        public DateTime TokenExpireTime { get => tokenExpireTime; set => tokenExpireTime = value; }

        private string accessToken = "";
        public string AccessToken { get => accessToken; set => accessToken = value; }
        public string RedirectUrl { get => redirectUrl; set => redirectUrl = value; }

        private string redirectUrl = "";

        private string Code = "";

        private void RefreshToken()
        {
            try
            {
                using Lion.Net.HttpClient _wc = new Net.HttpClient(60 * 1000);
                var _response = _wc.GetResponseString("POST", TokenUrl, "", $"code={Code}&client_id={CliendId}&client_secret={AuthKey}&grant_type=authorization_code&redirect_uri={System.Net.WebUtility.UrlEncode(RedirectUrl)}");
                var _value = JObject.Parse(_response);
                AccessToken = _value["access_token"].ToString();
                TokenExpireTime = DateTime.Now.AddMinutes(-1).AddSeconds(_value["expires_in"].Value<int>());
            }
            catch
            {
                throw new Exception("Code error!token get error!");
            }
        }

        /// <summary>
        /// 用户信息
        /// </summary>
        /// <param name="_code"></param>
        /// <returns>json{"id","email"}</returns>
        public string GetUserInfo(string _redirectUrl, string _code)
        {
            RedirectUrl = _redirectUrl;
            if (Code != _code)
            {
                Code = _code;
                RefreshToken();
            }
            else if (TokenExpireTime < DateTime.Now)
            {
                Code = _code;
                RefreshToken();
            }            
            using Lion.Net.WebClientPlus _wc = new Net.WebClientPlus(60 * 1000, true);
            return _wc.DownloadString($"{UserInfoUrl}?access_token={AccessToken}");
        }
    }
}
