using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        private static string Temp = "";

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
            Temp = _settingss["Temp"].Value<string>();
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
        public static bool SendSingleText(string _from, string[] _tos, string _text, out string _msgid, bool _online = false, bool _sync = false)
        {
            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "txt";
            _data["body"] = new JObject() { ["msg"] = _text };
            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            Console.WriteLine(_result);
            _msgid = _result["data"][_tos[0]].Value<string>();
            return true;
        }
        #endregion

        #region SendSingleImage
        public static bool SendSingleImage(string _from, string[] _tos, string _filename, out string _msgid, bool _online = false, bool _sync = false)
        {
            string _file = _filename.Substring(_filename.IndexOf("_") + 1);
            string _path = $"{Temp}/{_filename}";
            Image _image = Image.FromFile(_path);

            JObject _upload =  Upload(_path, _file);
            string _uuid = _upload["entities"]["uuid"].Value<string>();
            string _secret = _upload["entities"]["share-secret"].Value<string>();

            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "img";
            _data["body"] = new JObject()
            {
                ["filename"] = _file,
                ["secret"] = _secret,
                ["size"] = new JObject() { ["width"] = _image.Width, ["height"] = _image.Height },
                ["url"] = $"https://{Host}/{OrgName}/{AppName}/chatfiles/{_uuid}"
            };

            _image.Dispose();

            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _msgid = _result["data"][_tos[0]].Value<string>();
            return true;
        }
        #endregion

        #region SendSingleCommand
        public static bool SendSingleCommand(string _from, string[] _tos, string _cmd, out string _msgid, bool _online = false, bool _sync = false)
        {
            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "cmd";
            _data["body"] = new JObject() { ["action"] = _cmd };
            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _msgid = _result["data"][_tos[0]].Value<string>();
            return true;
        }
        #endregion

        #region TempFile
        public static bool TempFile(string _botId, string _filename, byte[] _binary, out string _name)
        {
            string _path = $"{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}-{_botId}_{_filename}";
            File.WriteAllBytes($"{Temp}/{_path}", _binary);
            _name = _path;
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

        #region Upload
        public static JObject Upload(string _path, string _filename)
        {
            WebClientPlus _web = new WebClientPlus(5000);
            _web.Headers["Content-Type"] = "multipart/form-data";
            _web.Headers["Authorization"] = $"Bearer {AppToken}";
            byte[] _result = _web.UploadFile($"https://{Host}/{OrgName}/{AppName}{_path}", _filename);
            _web.Dispose();

            return JObject.Parse(Encoding.UTF8.GetString(_result));
        }
        #endregion
    }
}
