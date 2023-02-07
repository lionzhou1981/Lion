using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Lion.SDK.Telegram
{
    public class InlineButtonTypes
    {
        public const string Url = "url";
        public const string CallBackButton = "callback_data";
    }
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
        public static string BuildTelegramTable(
            List<string> _tables,
            string _tableSep = "|", char _arraySep = ';',
            int _maxColumnWidth = 0, bool _fixedColumnWidth = false, bool _autoColumnWidth = false,
            int _minimumColumnWidth = 4, int _columnPadRight = 0, int _columnPadLeft = 0,
            bool _beginEndBorders = true)
        {
            var _preTable = new List<string>() { "<pre>" };
            var _columnsWidth = new List<int>();
            var _firstLine = _tables[0];
            var _lines = _firstLine.Split(_arraySep);

            if (_fixedColumnWidth && _maxColumnWidth == 0) throw new Exception("For fixedColumnWidth usage must set maxColumnWidth > 0");
            else if (_fixedColumnWidth && _maxColumnWidth > 0)
            {
                for (var i = 0; i < _lines.Length; i++)
                    _columnsWidth.Add(_maxColumnWidth + _columnPadRight + _columnPadLeft);
            }
            else
            {
                for (var i = 0; i < _lines.Length; i++)
                {
                    var _columnData = _lines[i].Trim();
                    var _columnFullLength = _columnData.Length;

                    if (_autoColumnWidth)
                        _tables.ForEach(t =>
                        {
                            _columnFullLength = t.Split(_arraySep)[i].Length > _columnFullLength ? t.Split(_arraySep)[i].Length : _columnFullLength;
                        });

                    _columnFullLength = _columnFullLength < _minimumColumnWidth ? _minimumColumnWidth : _columnFullLength;
                    var columnWidth = _columnFullLength + _columnPadRight + _columnPadLeft;
                    if (_maxColumnWidth > 0 && columnWidth > _maxColumnWidth)
                        columnWidth = _maxColumnWidth;
                    _columnsWidth.Add(columnWidth);
                }
            }

            for (int i = 0; i < _tables.Count; i++)
            {
                var line = _tables[i];
                _lines = line.Split(_arraySep);

                var _fullLine = new string[_lines.Length + (_beginEndBorders ? 2 : 0)];
                if (_beginEndBorders) _fullLine[0] = "";

                for (var j = 0; j < _lines.Length; j++)
                {
                    var _data = _lines[j].Trim();
                    var _dataLength = _data.Length;
                    var _columnWidth = _columnsWidth[j];
                    var _columnSizeWithoutTrimSize = _columnWidth - _columnPadRight - _columnPadLeft;
                    var _dataCharsToRead = _columnSizeWithoutTrimSize > _dataLength ? _dataLength : _columnSizeWithoutTrimSize;
                    var _columnData = _data.Substring(0, _dataCharsToRead);
                    _columnData = _columnData.PadRight(_columnData.Length + _columnPadRight);
                    _columnData = _columnData.PadLeft(_columnData.Length + _columnPadLeft);
                    var column = _columnData.PadRight(_columnWidth);
                    _fullLine[j + (_beginEndBorders ? 1 : 0)] = column;
                }

                if (_beginEndBorders) _fullLine[_fullLine.Length - 1] = "";
                if (i != 0)
                    _preTable.Add(string.Join(_tableSep, _fullLine));
                else
                    _preTable.Add(string.Join(" ", _fullLine));
            }
            _preTable.Add("</pre>");
            return string.Join("\r\n", _preTable);
        }

        public JObject AddWebappButtons(JObject _inlineKeyboard, string _text,string _type = "url",string value = "")
        {
            JArray _buttons = new JArray();
            try
            {
                if (_inlineKeyboard.ContainsKey("inline_keyboard"))
                    _buttons = _inlineKeyboard["inline_keyboard"].Value<JArray>().First.Value<JArray>();
            }
            catch { }
            JObject _childData = new JObject();
            _childData[_type] = value;
            _buttons.Add(new JObject()
            {
                ["text"] = _text,
                ["web_app"] = _childData
            }) ;
            var _setted = new JArray();
            _setted.Add(_buttons);
            _inlineKeyboard["inline_keyboard"] = _setted;
            return _inlineKeyboard;
        }

        public JObject AddInlineButtons(JObject _inlineKeyboard, string _text, string _type = "url", string value = "")
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
                [_type] = value
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

        public Tuple<bool, JObject> SendWithMethod(string _method = "sendMessage", JObject _sendObj = null)
        {
            string url = this.BuildUrl(_method);
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
