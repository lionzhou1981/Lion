using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
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
        private static string TempPath = "";

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
            TempPath = _settingss["Temp"].Value<string>();
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
        public static bool SendSingleText(string _from, string[] _tos, string _text, out string _msgid, out string _timestamp, out JObject _payload, bool _online = false, bool _sync = false)
        {
            _payload = new JObject() { ["msg"] = _text };

            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "txt";
            _data["body"] = _payload;
            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _timestamp = _result["timestamp"].Value<string>();
            _msgid = _result["data"][_tos[0]].Value<string>();
            _payload["type"] = "txt";

            return true;
        }
        #endregion

        #region SendSingleImage
        public static bool SendSingleImage(string _from, string[] _tos, string _filename, out string _msgid, out string _timestamp, out JObject _payload, bool _online = false, bool _sync = false, bool _cleanup = true)
        {
            string _file = _filename.Substring(_filename.IndexOf("_") + 1);
            string _path = $"{TempPath}/{_filename}";

            using FileStream _stream = File.OpenRead(_path);
            using IImage _image = PlatformImage.FromStream(_stream);

            JObject _upload = Upload(_path);
            Console.WriteLine(_upload);
            string _uuid = _upload["entities"][0]["uuid"].Value<string>();
            string _secret = _upload["entities"][0]["share-secret"].Value<string>();

            _payload = new JObject()
            {
                ["filename"] = _file,
                ["secret"] = _secret,
                ["size"] = new JObject() { ["width"] = (int)_image.Width, ["height"] = (int)_image.Height },
                ["url"] = $"https://{Host}/{OrgName}/{AppName}/chatfiles/{_uuid}"
            };

            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "img";
            _data["body"] = _payload;

            _image.Dispose();
            _stream.Close();

            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _timestamp = _result["timestamp"].Value<string>();
            _msgid = _result["data"][_tos[0]].Value<string>();
            _payload["type"] = "img";

            if (_cleanup) { File.Delete(_path); }

            return true;
        }
        #endregion

        #region SendSingleVideo
        public static bool SendSingleVideo(
            string _from, string[] _tos,
            string _face, string _faceSecret,
            string _video,string _videoSecret,int _videoLength,long _videoSize,
            out string _msgid, out string _timestamp, out JObject _payload, bool _online = false, bool _sync = false)
        {
            _payload = new JObject()
            {
                ["thumbnail"] = _face.StartsWith("http") ? _face: $"https://{Host}/{OrgName}/{AppName}/chatfiles/{_face}",
                ["length"] = _videoLength,
                ["file_length"] = _videoSize,
                ["url"] = _video.StartsWith("http") ? _video : ($"https://{Host}/{OrgName}/{AppName}/chatfiles/{_video}")
            };
            if (_faceSecret != "") { _payload["thumb_secret"] = _faceSecret; }
            if (_videoSecret != "") { _payload["secret"] = _videoSecret; }

            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "video";
            _data["body"] = _payload;

            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _timestamp = _result["timestamp"].Value<string>();
            _msgid = _result["data"][_tos[0]].Value<string>();
            _payload["type"] = "video";
            return true;
        }
        #endregion

        #region SendSingleCommand
        public static bool SendSingleCommand(string _from, string[] _tos, string _cmd, out string _msgid, out string _timestamp, out JObject _payload, bool _online = false, bool _sync = false)
        {
            _payload = new JObject() { ["action"] = _cmd };

            JObject _data = new JObject();
            _data["from"] = _from;
            _data["to"] = new JArray(_tos);
            _data["type"] = "cmd";
            _data["body"] = _payload;
            if (_online) { _data["routetype"] = "ROUTE_ONLINE"; }
            _data["sync_device"] = _sync;

            JObject _result = Call("/messages/users", _data);
            _timestamp = _result["timestamp"].Value<string>();
            _timestamp = _result["timestamp"].Value<string>();
            _msgid = _result["data"][_tos[0]].Value<string>();
            _payload["type"] = "cmd";
            return true;
        }
        #endregion

        #region PushSingle
    
        public static bool PushSingle(string[] _tos, string _title, string _subTitle, string _content, out string _timestamp, out JObject _payload,out JArray _results)
        {
            _payload = new JObject() {
                ["title"] = _title,
                ["subTitle"] = _subTitle,
                ["content"] = _content,
            };

            JObject _data = new JObject();
            _data["targets"] = new JArray(_tos);
            _data["pushMessage"] = _payload;
            _data["strategy"] = 0;

            JObject _result = Call("/push/single", _data);
            _timestamp = _result["timestamp"].Value<string>();
            _results = _result["data"].Value<JArray>();

            return true;
        }
        #endregion

        #region PushAll
        #endregion

        #region TempFile
        public static bool TempFile(string _botId, string _filename, byte[] _binary, out string _name)
        {
            string _path = $"{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}-{_botId}_{_filename}";
            File.WriteAllBytes($"{TempPath}/{_path}", _binary);
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
        public static JObject Upload(string _path)
        {
            WebClientPlus _web = new WebClientPlus(5000);
            _web.Headers["Authorization"] = $"Bearer {AppToken}";
            _web.Headers["restrict-access"] = $"true";
            byte[] _result = _web.UploadFile($"https://{Host}/{OrgName}/{AppName}/chatfiles", _path);
            _web.Dispose();

            return JObject.Parse(Encoding.UTF8.GetString(_result));
        }
        #endregion
    }
}
