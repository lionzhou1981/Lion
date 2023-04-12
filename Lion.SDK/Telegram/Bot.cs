using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Telegram
{
    public class InlineButtonTypes
    {
        public const string Url = "url";
        public const string CallBackButton = "callback_data";
    }

    public class Bot
    {
        private string token;
        private string urlTemplate = "https://api.telegram.org/bot{{TOKEN}}/{{METHOD_NAME}}";

        public Bot(string _token) => this.token = _token;

        private string BuildUrl(string _method) => this.urlTemplate.Replace("{{TOKEN}}", this.token).Replace("{{METHOD_NAME}}", _method);

        #region SendText
        public (bool, JObject) SendText(string _channelOrUserId, string _textMsg, bool _disableLinkePreview, bool _disableNotify, bool _protectContent, JObject _replyButtons = null, string _parseMode = "", int _replayMsgId = 0)
        {
            JObject _data = new JObject
            {
                ["chat_id"] = long.Parse(_channelOrUserId),
                ["text"] = _textMsg,
                ["disable_web_page_preview"] = _disableLinkePreview,
                ["disable_notification"] = _disableNotify,
                ["protect_content"] = _protectContent
            };

            if (_replyButtons != null) { _data["reply_markup"] = _replyButtons; }
            if (!string.IsNullOrWhiteSpace(_parseMode)) { _data["parse_mode"] = _parseMode; }
            if (_replayMsgId > 0) { _data["reply_to_message_id"] = _replayMsgId; }

            return SendWithMethod("sendMessage", _data);
        }
        #endregion

        #region SendWithMethod
        public (bool, JObject) SendWithMethod(string _method = "sendMessage", JObject _data = null)
        {
            string _url = this.BuildUrl(_method);
            return Call(_url, _data);
        }
        #endregion

        #region Call
        private (bool, JObject) Call(string _url, JObject _data)
        {
            try
            {
                HttpClient _client = new HttpClient(60 * 1000);
                _client.ContentType = "application/json";
                byte[] _bytes = Encoding.UTF8.GetBytes(_data.ToString(Newtonsoft.Json.Formatting.None));
                string _json = _client.GetResponseString("POST", _url, "", _bytes);
                return (true, JObject.Parse(_json));
            }
            catch (Exception exception)
            {
                return (false, new JObject { ["errormsg"] = exception.Message, ["errorstacktrace"] = exception.StackTrace });
            }
        }
        #endregion

        #region AddKeyboardButtons
        public static  JObject AddKeyboardButtons(JObject _replyButtons, List<string> _buttons)
        {
            JArray _jbuttons = new JArray();
            _jbuttons.Add(JArray.FromObject(_buttons));
            _replyButtons["keyboard"] = _jbuttons;
            return _replyButtons;
        }
        #endregion

        #region AddInlineButtons
        public static JObject AddInlineButtons(JObject _inlineKeyboard, string _text, string _type = "url", string _value = "")
        {
            JArray _buttons = new JArray();
            try
            {
                if (_inlineKeyboard.ContainsKey("inline_keyboard")) { _buttons = _inlineKeyboard["inline_keyboard"].Value<JArray>().First.Value<JArray>(); }
            }
            catch { }

            _buttons.Add(new JObject() { ["text"] = _text, [_type] = _value });

            JArray _setted = new JArray();
            _setted.Add(_buttons);
            _inlineKeyboard["inline_keyboard"] = _setted;
            return _inlineKeyboard;
        }
        #endregion

        #region AddWebappButtons
        public static JObject AddWebappButtons(JObject _inlineKeyboard, string _text, string _type = "url", string _value = "")
        {
            JArray _buttons = new JArray();
            try
            {
                if (_inlineKeyboard.ContainsKey("inline_keyboard")) { _buttons = _inlineKeyboard["inline_keyboard"].Value<JArray>().First.Value<JArray>(); }
            }
            catch { }

            JObject _childData = new JObject();
            _childData[_type] = _value;
            _buttons.Add(new JObject() { ["text"] = _text, ["web_app"] = _childData });

            JArray _setted = new JArray();
            _setted.Add(_buttons);
            _inlineKeyboard["inline_keyboard"] = _setted;

            return _inlineKeyboard;
        }
        #endregion

        #region BuildTelegramTable
        public static string BuildTelegramTable(
            List<string> _tables,
            string _tableSep = "|",
            char _arraySep = ';',
            int _maxColumnWidth = 0, 
            bool _fixedColumnWidth = false,
            bool _autoColumnWidth = false,
            int _minimumColumnWidth = 4,
            int _columnPadRight = 0, 
            int _columnPadLeft = 0,
            bool _beginEndBorders = true)
        {
            IList<string> _preTable = new List<string>() { "<pre>" };
            IList<int> _columnsWidth = new List<int>();
            string _firstLine = _tables[0];
            string[] _lines = _firstLine.Split(_arraySep);

            if (_fixedColumnWidth && _maxColumnWidth == 0)
            {
                throw new Exception("For fixedColumnWidth usage must set maxColumnWidth > 0");
            }
            else if (_fixedColumnWidth && _maxColumnWidth > 0)
            {
                for (int i = 0; i < _lines.Length; i++) { _columnsWidth.Add(_maxColumnWidth + _columnPadRight + _columnPadLeft); }
            }
            else
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    string _columnData = _lines[i].Trim();
                    int _columnFullLength = _columnData.Length;

                    if (_autoColumnWidth)
                    {
                        _tables.ForEach(t => { _columnFullLength = t.Split(_arraySep)[i].Length > _columnFullLength ? t.Split(_arraySep)[i].Length : _columnFullLength; });
                    }

                    _columnFullLength = _columnFullLength < _minimumColumnWidth ? _minimumColumnWidth : _columnFullLength;
                    int columnWidth = _columnFullLength + _columnPadRight + _columnPadLeft;

                    if (_maxColumnWidth > 0 && columnWidth > _maxColumnWidth) { columnWidth = _maxColumnWidth; }
                    _columnsWidth.Add(columnWidth);
                }
            }

            for (int i = 0; i < _tables.Count; i++)
            {
                string _line = _tables[i];
                _lines = _line.Split(_arraySep);

                string[] _fullLine = new string[_lines.Length + (_beginEndBorders ? 2 : 0)];
                if (_beginEndBorders) _fullLine[0] = "";

                for (int j = 0; j < _lines.Length; j++)
                {
                    string _data = _lines[j].Trim();
                    int _dataLength = _data.Length;
                    int _columnWidth = _columnsWidth[j];
                    int _columnSizeWithoutTrimSize = _columnWidth - _columnPadRight - _columnPadLeft;
                    int _dataCharsToRead = _columnSizeWithoutTrimSize > _dataLength ? _dataLength : _columnSizeWithoutTrimSize;
                    
                    string _columnData = _data.Substring(0, _dataCharsToRead);
                    _columnData = _columnData.PadRight(_columnData.Length + _columnPadRight);
                    _columnData = _columnData.PadLeft(_columnData.Length + _columnPadLeft);
                    
                    string column = _columnData.PadRight(_columnWidth);
                    _fullLine[j + (_beginEndBorders ? 1 : 0)] = column;
                }

                if (_beginEndBorders) { _fullLine[_fullLine.Length - 1] = ""; }
                
                if (i != 0)
                {
                    _preTable.Add(string.Join(_tableSep, _fullLine));
                }
                else
                {
                    _preTable.Add(string.Join(" ", _fullLine));
                }
            }
            _preTable.Add("</pre>");
            return string.Join("\r\n", _preTable);
        }
        #endregion
    }
}
