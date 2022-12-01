using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;

namespace Lion.SDK.Facebook
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

        const string AuthUrl = "https://www.facebook.com/v15.0/dialog/oauth";
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
            _paras.Add("state", _state);
            _paras.Add("response_type", "code");
            _paras.Add("redirect_uri", System.Net.WebUtility.UrlEncode(_authCodeRedirectUrl));
            return $"{AuthUrl}?{string.Join("&", _paras.ToList().Select(t => String.Join("=", t.Key, t.Value)))}";
        }

        const string TokenUrl = "https://graph.facebook.com/v15.0/oauth/access_token";
        const string UserInfoUrl = "https://graph.facebook.com/v15.0/";
        const string DebugTokenUrl = "https://graph.facebook.com/debug_token";

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
                using Lion.Net.WebClientPlus _wc = new Net.WebClientPlus(60 * 1000, true);
                _wc.Proxy = new WebProxy("127.0.0.1", 1082);
                var _response = _wc.DownloadString($"{TokenUrl}?client_id={CliendId}&client_secret={AuthKey}&redirect_uri={RedirectUrl}&code={Code}");
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
                throw new Exception("Token expired");
            }
            using Lion.Net.WebClientPlus _wc = new Net.WebClientPlus(60 * 1000, true);
            _wc.Proxy = new WebProxy("127.0.0.1", 1082);
            var _userId = "";
            try
            {
                var _tokenResult = JObject.Parse(_wc.DownloadString($"{DebugTokenUrl}?input_token={AccessToken}&access_token={CliendId}|{AuthKey}"));
                _userId = _tokenResult["data"]["user_id"].ToString();
            }
            catch
            {
                throw new Exception("get user token error");
            }
            try
            {
                return _wc.DownloadString($"{UserInfoUrl}/{_userId}?access_token={CliendId}|{AuthKey}&fields=id,name,email");
            }
            catch
            {
                throw new Exception("get user info error");
            }
        }
    }
}
