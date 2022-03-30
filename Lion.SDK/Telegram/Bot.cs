using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lion.SDK.Telegram
{
    public class Bot
    {
        // Fields
        private string Token;
        private string urlTemplate = "https://api.telegram.org/bot{{TOKEN}}/{{METHOD_NAME}}";

        // Methods
        public Bot(string _token)
        {
            this.Token = _token;
        }

        private string BuildUrl(string _method) =>
            this.urlTemplate.Replace("{{TOKEN}}", this.Token).Replace("{{METHOD_NAME}}", _method);

        public Tuple<bool, JObject> Send(string _channelOrUserId, string _textMsg, bool _disableLinkePreview, bool _disableNotify, bool _protectContent,string _parseMode = "", int _replayMsgId = 0)
        {
            JObject _sendObj = new JObject
            {
                ["chat_id"] = int.Parse(_channelOrUserId),
                ["text"] = _textMsg,
                ["disable_web_page_preview"] = _disableLinkePreview,
                ["disable_notification"] = _disableNotify,
                ["protect_content"] = _protectContent
            };
            if (!string.IsNullOrWhiteSpace(_parseMode))
                _sendObj["parse_mode"] = _parseMode;
            if (_replayMsgId > 0)
            {
                _sendObj["reply_to_message_id"] = _replayMsgId;
            }
            string url = this.BuildUrl("sendMessage");
            try
            {
                var _client = new Lion.Net.HttpClient(60 * 1000);
                //_client.Proxy = new WebProxy("127.0.0.1:1081");
                _client.ContentType = "application/json";
                byte[] bytes = Encoding.UTF8.GetBytes(_sendObj.ToString(Newtonsoft.Json.Formatting.None));
                string json = _client.GetResponseString("POST", url, url, bytes);
                return new Tuple<bool, JObject>(true, JObject.Parse(json));
            }
            catch (Exception exception)
            {
                return new Tuple<bool, JObject>(true, new JObject
                {
                    ["errormsg"] = exception.Message,
                    ["errorstacktrace"] = exception.StackTrace
                });
            }
        }

    }
}
