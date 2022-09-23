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

        public JObject AddInlineButtons(JObject _inlineKeyboard, string _text,string url)
        {
            JArray _buttons = new JArray();
            try
            {
                if (_inlineKeyboard.ContainsKey("inline_keyboard"))
                    _buttons = _inlineKeyboard["inline_keyboard"].Value<JArray>().First.Value<JArray>();
            }
            catch { }
            _buttons.Add(new JObject()
            {
                ["text"] = _text,
                ["url"] = url
            });
            var _setted = new JArray();
            _setted.Add(_buttons);
            _inlineKeyboard["inline_keyboard"] = _setted;
            return _inlineKeyboard;
        }

        public JObject AddKeyBoardButtons(JObject _replyButtons, List<string> _buttons)
        {
            var _jbuttons = new JArray();
            _jbuttons.Add(JArray.FromObject(_buttons));
            _replyButtons["keyboard"] = _jbuttons;
            return _replyButtons;
        }


        public Tuple<bool, JObject> Send(string _channelOrUserId, string _textMsg, bool _disableLinkePreview, bool _disableNotify, bool _protectContent, JObject _replyButtons = null, string _parseMode = "", int _replayMsgId = 0)
        {
            JObject _sendObj = new JObject
            {
                ["chat_id"] = long.Parse(_channelOrUserId),
                ["text"] = _textMsg,
                ["disable_web_page_preview"] = _disableLinkePreview,
                ["disable_notification"] = _disableNotify,
                ["protect_content"] = _protectContent
            };
            if(_replyButtons!= null)
            {
                _sendObj["reply_markup"] = _replyButtons;
            }
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
