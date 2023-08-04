using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Agora
{
    public class AgoraChat
    {
        private static string OrgName = "";
        private static string AppName = "";
        private static string AppId = "";
        private static string AppCert = "";
        private static string Host = "";

        private static string appToken = "";
        private static DateTime appTokenTime = DateTime.MinValue;
        private static int appTokenExpireMinute = 60;

        public static string AppToken
        {
            get
            {
                if (appToken == "" || (DateTime.UtcNow - appTokenTime).TotalMinutes > appTokenExpireMinute) { appToken = AgoraChat.BuildAppToken(); }
                return appToken;
            }
        }

        #region Init
        public static void Init(JObject _settingss)
        {
            OrgName = _settingss["OrgName"].Value<string>();
            AppName = _settingss["AppName"].Value<string>();
            AppId = _settingss["AppId"].Value<string>();
            AppCert = _settingss["AppCert"].Value<string>();
            Host = _settingss["Host"].Value<string>();
        }
        #endregion

        #region BuildAppToken
        public static string BuildAppToken(int _expire = 86400)
        {
            AccessToken2 _token = new AccessToken2(AppId, AppCert, _expire);
            AccessToken2.Service _chat = new AccessToken2.ServiceChat();

            _chat.AddPrivilegeChat(AccessToken2.PrivilegeChatEnum.PRIVILEGE_CHAT_APP, _expire);
            _token.AddService(_chat);

            return _token.Build();
        }
        #endregion

        #region BuildUserToken
        public static string BuildUserToken(string _userId, int _expire = 86400)
        {
            AccessToken2 _token = new AccessToken2(AppId, AppCert, _expire);
            AccessToken2.Service _chat = new AccessToken2.ServiceChat(_userId);

            _chat.AddPrivilegeChat(AccessToken2.PrivilegeChatEnum.PRIVILEGE_CHAT_USER, _expire);
            _token.AddService(_chat);

            return _token.Build();
        }
        #endregion

        #region Register
        public static bool Register(string _username, string _password, string _nickname, out string _uuid)
        {
            JObject _data = new JObject();
            _data["username"] = _username;
            _data["password"] = _password;
            _data["nickname"] = _nickname;

            JObject _result = Call("/users", _data);
            _uuid = _result["entities"][0]["uuid"].Value<string>();
            return true;
        }
        #endregion

        #region SendSingleText
        public static bool SendSingleText(string _from, string[] _tos, string _text, out string _msgid, bool _online= false)
        {
            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "txt";
            _data["body"] = new JObject() { ["msg"] = _text };
            if (_online)
            {
                _data["routetype"] = "ROUTE_ONLINE";
                _data["sync_device"] = true;
            }

            JObject _result = Call("/messages/users", _data);
            _msgid = _result["data"]["user2"].Value<string>();
            return true;
        }
        #endregion

        #region Call
        public static JObject Call(string _path, JObject _data)
        {
            WebClientPlus _web = new WebClientPlus(5000);
            _web.Headers["Content-Type"] = "application/json";
            _web.Headers["Authorization"] = $"Bearer {AppToken}";
            string _result = _web.UploadString($"https://{Host}/{OrgName}/{AppName}{_path}", _data.ToString(Formatting.None));
            _web.Dispose();

            return JObject.Parse(_result);
        }
        #endregion
    }
}
