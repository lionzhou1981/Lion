using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Lion.Encrypt
{
    public class AISMessage
    {
        #region Encode
        public static string Encode(JObject _json) { return ""; }
        #endregion

        #region Decode
        public static JObject Decode(params string[] _messages)
        {
            string _content = "";
            int _contentKey = 0;

            for (int i = 0; i < _messages.Length; i++)
            {
                string _message = _messages[i];
                if (_message[0] != '!')
                {
                    throw new Exception("Wrong message head.");
                }

                int _checksumIndex = _message.IndexOf("*");
                if (_checksumIndex == -1)
                {
                    throw new Exception("Checksum missing.");
                }

                string _line = _message.Substring(0, _checksumIndex);
                if (Checksum(_line) != Convert.ToInt32(_message.Substring(_checksumIndex + 1), 16))
                {
                    throw new Exception("Checksum failed.");
                }

                string[] _items = _line.Split(',');

                _content += _items[5];
                _contentKey = Convert.ToInt32(_items[6]);
            }

            StringBuilder _sb = new StringBuilder();
            foreach (char _char in _content)
            {
                int _byte = (byte)_char - 48;
                if (_byte > 40) { _byte -= 8; }

                _sb.Append(Convert.ToString(_byte, 2).PadLeft(6, '0'));
            }

            int _left = (_sb.Length + _contentKey) % 6;
            if (_left != 0) { _contentKey += 6 - _left; }
            if (_contentKey > 0) { _sb.Append(new string('0', _contentKey)); }

            string _payload = _sb.ToString();
            uint _messageType = Convert.ToUInt32(_payload.Substring(0, 6), 2);
            //Console.WriteLine($"{_messageType} - {_payload.Length}");

            JObject _result = new JObject();
            switch (_messageType)
            {
                case 1:
                case 2:
                case 3: _result = Message_1_3(_messageType, _payload); break;
                case 4: _result = Message_4(_messageType, _payload); break;
                case 5: _result = Message_5(_messageType, _payload); break;
                case 6: _result = Message_6(_messageType, _payload); break;
                case 7: _result = Message_7(_messageType, _payload); break;
                case 8: _result = Message_8(_messageType, _payload); break;
                case 9: _result = Message_9(_messageType, _payload); break;
                case 10: _result = Message_10(_messageType, _payload); break;
                case 11: _result = Message_11(_messageType, _payload); break;
                case 12: _result = Message_12(_messageType, _payload); break;
                case 13: _result = Message_13(_messageType, _payload); break;
                case 14: _result = Message_14(_messageType, _payload); break;
                case 15: _result = Message_15(_messageType, _payload); break;
                case 16: _result = Message_16(_messageType, _payload); break;
                case 17: _result = Message_17(_messageType, _payload); break;
                case 18: _result = Message_18(_messageType, _payload); break;
                case 19: _result = Message_19(_messageType, _payload); break;
                case 20: _result = Message_20(_messageType, _payload); break;
                case 21: _result = Message_21(_messageType, _payload); break;
                case 22: _result = Message_22(_messageType, _payload); break;
                case 23: _result = Message_23(_messageType, _payload); break;
                case 24: _result = Message_24(_messageType, _payload); break;
                case 25: _result = Message_25(_messageType, _payload); break;
                case 26: _result = Message_26(_messageType, _payload); break;
                case 27: _result = Message_27(_messageType, _payload); break;
            }
            return _result;
        }
        #endregion

        #region Message_1_3
        private static JObject Message_1_3(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["status"] = Convert.ToUInt32(_payload.Substring(38, 4), 2);
            _result["turn"] = Convert2Double(_payload.Substring(42, 8));
            _result["speed"] = ((decimal)Convert.ToUInt32(_payload.Substring(50, 10), 2)) / 10;
            _result["accuracy"] = _payload.Substring(60, 1);
            _result["lng"] = Convert2Double(_payload.Substring(61, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(89, 27)) / 600000;
            _result["course"] = ((decimal)Convert.ToUInt32(_payload.Substring(116, 12), 2)) / 10;
            _result["heading"] = Convert.ToUInt32(_payload.Substring(128, 9), 2);
            _result["second"] = Convert.ToUInt32(_payload.Substring(137, 6), 2);
            _result["maneuver"] = Convert.ToUInt32(_payload.Substring(143, 2), 2);
            _result["raim"] = _payload.Substring(148, 1);
            _result["radio"] = _payload.Substring(149);

            return _result;
        }
        #endregion

        #region Message_4
        private static JObject Message_4(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["year"] = Convert.ToUInt32(_payload.Substring(38, 14), 2);
            _result["month"] = Convert.ToUInt32(_payload.Substring(52, 4), 2);
            _result["day"] = Convert.ToUInt32(_payload.Substring(56, 5), 2);
            _result["hour"] = Convert.ToUInt32(_payload.Substring(61, 5), 2);
            _result["minute"] = Convert.ToUInt32(_payload.Substring(66, 6), 2);
            _result["second"] = Convert.ToUInt32(_payload.Substring(72, 6), 2);
            _result["accuracy"] = _payload.Substring(78, 1);
            _result["lng"] = Convert2Double(_payload.Substring(79, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(107, 27)) / 600000;
            _result["epfd"] = Convert.ToUInt32(_payload.Substring(134, 4), 2);
            _result["raim"] = _payload.Substring(148, 1);
            _result["radio"] = _payload.Substring(149, 19);

            return _result;
        }
        #endregion

        #region Message_5
        private static JObject Message_5(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["ais_version"] = Convert.ToUInt32(_payload.Substring(38, 2), 2);
            _result["imo"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["callsign"] = Convert2String(_payload.Substring(70, 42));
            _result["shipname"] = Convert2String(_payload.Substring(112, 120));
            _result["shiptype"] = Convert.ToUInt32(_payload.Substring(232, 8), 2);
            _result["to_bow"] = Convert.ToUInt32(_payload.Substring(240, 9), 2);
            _result["to_stern"] = Convert.ToUInt32(_payload.Substring(249, 9), 2);
            _result["to_port"] = Convert.ToUInt32(_payload.Substring(258, 6), 2);
            _result["to_starboard"] = Convert.ToUInt32(_payload.Substring(264, 6), 2);
            _result["epfd"] = Convert.ToUInt32(_payload.Substring(270, 4), 2);
            _result["month"] = Convert.ToUInt32(_payload.Substring(274, 4), 2);
            _result["day"] = Convert.ToUInt32(_payload.Substring(278, 5), 2);
            _result["hour"] = Convert.ToUInt32(_payload.Substring(283, 5), 2);
            _result["minute"] = Convert.ToUInt32(_payload.Substring(288, 6), 2);
            _result["draught"] = ((decimal)Convert.ToUInt32(_payload.Substring(294, 8), 2)) / 10;
            _result["destination"] = Convert2String(_payload.Substring(302, 120));
            _result["dte"] = _payload.Substring(422, 1);
            return _result;
        }
        #endregion

        #region Message_6
        private static JObject Message_6(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["seqno"] = Convert.ToUInt32(_payload.Substring(38, 2), 2);
            _result["dest_mmsi"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["retransmit"] = _payload.Substring(70, 1);
            _result["dac"] = Convert.ToUInt32(_payload.Substring(72, 10), 2);
            _result["fid"] = Convert.ToUInt32(_payload.Substring(82, 6), 2);
            _result["data"] = _payload.Substring(88);
            return _result;
        }
        #endregion

        #region Message_7
        private static JObject Message_7(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);

            if (_payload.Length >= 72)
            {
                _result["mmsi1"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
                _result["mmsiseq1"] = Convert.ToUInt32(_payload.Substring(70, 2), 2);
            }
            if (_payload.Length >= 104)
            {
                _result["mmsi2"] = Convert.ToUInt32(_payload.Substring(72, 30), 2);
                _result["mmsiseq2"] = Convert.ToUInt32(_payload.Substring(102, 2), 2);
            }
            if (_payload.Length >= 136)
            {
                _result["mmsi3"] = Convert.ToUInt32(_payload.Substring(104, 30), 2);
                _result["mmsiseq3"] = Convert.ToUInt32(_payload.Substring(134, 2), 2);
            }
            if (_payload.Length >= 168)
            {
                _result["mmsi4"] = Convert.ToUInt32(_payload.Substring(136, 30), 2);
                _result["mmsiseq4"] = Convert.ToUInt32(_payload.Substring(166, 2), 2);
            }
            return _result;
        }
        #endregion

        #region Message_8
        private static JObject Message_8(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["dac"] = Convert.ToUInt32(_payload.Substring(40, 10), 2);
            _result["fid"] = Convert.ToUInt32(_payload.Substring(50, 6), 2);
            _result["data"] = _payload.Substring(56);
            return _result;
        }
        #endregion

        #region Message_9
        private static JObject Message_9(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["alt"] = Convert.ToUInt32(_payload.Substring(38, 12), 2);
            _result["speed"] = Convert.ToUInt32(_payload.Substring(50, 10), 2);
            _result["accuracy"] = _payload.Substring(60, 1);
            _result["lng"] = Convert2Double(_payload.Substring(61, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(89, 27)) / 600000;
            _result["course"] = ((decimal)Convert.ToUInt32(_payload.Substring(116, 12), 2)) / 10;
            _result["second"] = Convert.ToUInt32(_payload.Substring(128, 6), 2);
            _result["regional"] = Convert.ToUInt32(_payload.Substring(134, 8), 2);
            _result["dte"] = _payload.Substring(142, 1);
            _result["assigned"] = _payload.Substring(146, 1);
            _result["raim"] = _payload.Substring(147, 1);
            _result["radio"] = _payload.Substring(148, 20);
            return _result;
        }
        #endregion

        #region Message_10
        private static JObject Message_10(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["dest_mmsi"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            return _result;
        }
        #endregion

        #region Message_11
        private static JObject Message_11(uint _type, string _payload)
        {
            return Message_4(_type, _payload);
        }
        #endregion

        #region Message_12
        private static JObject Message_12(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["seqno"] = Convert.ToUInt32(_payload.Substring(38, 2), 2);
            _result["dest_mmsi"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["retransmit"] = _payload.Substring(70, 1);
            _result["text"] = Convert2String(_payload.Substring(72));
            return _result;
        }
        #endregion

        #region Message_13
        private static JObject Message_13(uint _type, string _payload)
        {
            return Message_7(_type, _payload);
        }
        #endregion

        #region Message_14
        private static JObject Message_14(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["text"] = Convert2String(_payload.Substring(40));
            return _result;
        }
        #endregion

        #region Message_15
        private static JObject Message_15(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["mmsi1"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["type1_1"] = Convert.ToUInt32(_payload.Substring(70, 6), 2);
            _result["offset1_1"] = Convert.ToUInt32(_payload.Substring(76, 12), 2);

            if (_payload.Length >= 107)
            {
                _result["type1_1"] = Convert.ToUInt32(_payload.Substring(90, 6), 2);
                _result["offset1_1"] = Convert.ToUInt32(_payload.Substring(96, 12), 2);
            }
            if (_payload.Length >= 157)
            {
                _result["mmsi2"] = Convert.ToUInt32(_payload.Substring(110, 30), 2);
                _result["type2_1"] = Convert.ToUInt32(_payload.Substring(140, 6), 2);
                _result["offset2_1"] = Convert.ToUInt32(_payload.Substring(146, 12), 2);
            }

            return _result;
        }
        #endregion

        #region Message_16
        private static JObject Message_16(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["mmsi1"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["offset1"] = Convert.ToUInt32(_payload.Substring(70, 12), 2);
            _result["increment1"] = Convert.ToUInt32(_payload.Substring(82, 10), 2);
            if (_payload.Length >= 143)
            {
                _result["mmsi2"] = Convert.ToUInt32(_payload.Substring(92, 30), 2);
                _result["offset2"] = Convert.ToUInt32(_payload.Substring(122, 12), 2);
                _result["increment2"] = Convert.ToUInt32(_payload.Substring(134, 10), 2);
            }
            return _result;
        }
        #endregion

        #region Message_17
        private static JObject Message_17(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["lng"] = Convert2Double(_payload.Substring(40, 18)) / 600;
            _result["lat"] = Convert2Double(_payload.Substring(58, 17)) / 600;
            _result["data"] = _payload.Substring(80);

            return _result;
        }
        #endregion

        #region Message_18
        private static JObject Message_18(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["speed"] = ((decimal)Convert.ToUInt32(_payload.Substring(46, 10), 2)) / 10;
            _result["accuracy"] = _payload.Substring(56, 1);
            _result["lng"] = Convert2Double(_payload.Substring(57, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(85, 27)) / 600000;
            _result["course"] = ((decimal)Convert.ToUInt32(_payload.Substring(112, 12), 2)) / 10;
            _result["heading"] = Convert.ToUInt32(_payload.Substring(124, 9), 2);
            _result["second"] = Convert.ToUInt32(_payload.Substring(133, 6), 2);
            _result["regional"] = Convert.ToUInt32(_payload.Substring(139, 2), 2);
            _result["cs"] = _payload.Substring(141, 1);
            _result["display"] = _payload.Substring(142, 1);
            _result["dsc"] = _payload.Substring(143, 1);
            _result["band"] = _payload.Substring(144, 1);
            _result["msg22"] = _payload.Substring(145, 1);
            _result["assigned"] = _payload.Substring(146, 1);
            _result["raim"] = _payload.Substring(147, 1);
            _result["radio"] = _payload.Substring(148);
            return _result;
        }
        #endregion

        #region Message_19
        private static JObject Message_19(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["reserved"] = Convert.ToUInt32(_payload.Substring(38, 8), 2);
            _result["speed"] = ((decimal)Convert.ToUInt32(_payload.Substring(46, 10), 2)) / 10;
            _result["accuracy"] = _payload.Substring(56, 1);
            _result["lng"] = Convert2Double(_payload.Substring(57, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(85, 27)) / 600000;
            _result["course"] = ((decimal)Convert.ToUInt32(_payload.Substring(112, 12), 2)) / 10;
            _result["heading"] = Convert.ToUInt32(_payload.Substring(124, 9), 2);
            _result["second"] = Convert.ToUInt32(_payload.Substring(133, 6), 2);
            _result["regional"] = Convert.ToUInt32(_payload.Substring(139, 4), 2);
            _result["shipname"] = Convert2String(_payload.Substring(143, 120));
            _result["shiptype"] = Convert.ToUInt32(_payload.Substring(263, 8), 2);
            _result["to_bow"] = Convert.ToUInt32(_payload.Substring(271, 9), 2);
            _result["to_stern"] = Convert.ToUInt32(_payload.Substring(280, 9), 2);
            _result["to_port"] = Convert.ToUInt32(_payload.Substring(289, 6), 2);
            _result["to_starboard"] = Convert.ToUInt32(_payload.Substring(295, 6), 2);
            _result["epfd"] = Convert.ToUInt32(_payload.Substring(301, 4), 2);
            _result["raim"] = _payload.Substring(305, 1);
            _result["dte"] = _payload.Substring(306, 1);
            _result["assigned"] = Convert.ToUInt32(_payload.Substring(307, 1), 2);

            return _result;
        }
        #endregion

        #region Message_20
        private static JObject Message_20(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["offset1"] = Convert.ToUInt32(_payload.Substring(40, 12), 2);
            _result["number1"] = Convert.ToUInt32(_payload.Substring(52, 4), 2);
            _result["timeout1"] = Convert.ToUInt32(_payload.Substring(56, 3), 2);
            _result["increment1"] = Convert.ToUInt32(_payload.Substring(59, 11), 2);

            if (_payload.Length >= 99)
            {
                _result["offset2"] = Convert.ToUInt32(_payload.Substring(70, 12), 2);
                _result["number2"] = Convert.ToUInt32(_payload.Substring(82, 4), 2);
                _result["timeout2"] = Convert.ToUInt32(_payload.Substring(86, 3), 2);
                _result["increment2"] = Convert.ToUInt32(_payload.Substring(89, 11), 2);
            }
            if (_payload.Length >= 129)
            {
                _result["offset3"] = Convert.ToUInt32(_payload.Substring(100, 12), 2);
                _result["number3"] = Convert.ToUInt32(_payload.Substring(112, 4), 2);
                _result["timeout3"] = Convert.ToUInt32(_payload.Substring(116, 3), 2);
                _result["increment3"] = Convert.ToUInt32(_payload.Substring(119, 11), 2);
            }
            if (_payload.Length >= 159)
            {
                _result["offset4"] = Convert.ToUInt32(_payload.Substring(130, 12), 2);
                _result["number4"] = Convert.ToUInt32(_payload.Substring(142, 4), 2);
                _result["timeout4"] = Convert.ToUInt32(_payload.Substring(146, 3), 2);
                _result["increment4"] = Convert.ToUInt32(_payload.Substring(149, 11), 2);
            }

            return _result;
        }
        #endregion

        #region Message_21
        private static JObject Message_21(uint _type, string _payload)
        {
            if (_payload.Length < 271) { return null; }

            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["aid_type"] = Convert.ToUInt32(_payload.Substring(38, 5), 2);
            _result["name"] = Convert2String(_payload.Substring(43, 120));
            _result["accuracy"] = _payload.Substring(163, 1);
            _result["lng"] = Convert2Double(_payload.Substring(164, 28)) / 600000;
            _result["lat"] = Convert2Double(_payload.Substring(192, 27)) / 600000;
            _result["to_bow"] = Convert.ToUInt32(_payload.Substring(219, 9), 2);
            _result["to_stern"] = Convert.ToUInt32(_payload.Substring(228, 9), 2);
            _result["to_port"] = Convert.ToUInt32(_payload.Substring(237, 6), 2);
            _result["to_starboard"] = Convert.ToUInt32(_payload.Substring(243, 6), 2);
            _result["epfd"] = Convert.ToUInt32(_payload.Substring(249, 4), 2);
            _result["second"] = Convert.ToUInt32(_payload.Substring(253, 6), 2);
            _result["off_position"] = _payload.Substring(259, 1);
            _result["regional"] = Convert.ToUInt32(_payload.Substring(260, 8), 2);
            _result["raim"] = _payload.Substring(268, 1);
            _result["virtual_aid"] = _payload.Substring(269, 1);
            _result["assigned"] = _payload.Substring(270, 1);
            if (_payload.Length >= 272)
            {
                _result["name_ext"] = Convert2String(_payload.Substring(272));
            }
            return _result;
        }
        #endregion

        #region Message_22
        private static JObject Message_22(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["channel_a"] = Convert.ToUInt32(_payload.Substring(40, 12), 2);
            _result["channel_b"] = Convert.ToUInt32(_payload.Substring(52, 12), 2);
            _result["txrx"] = Convert.ToUInt32(_payload.Substring(64, 4), 2);
            _result["power"] = _payload.Substring(68, 1);
            _result["ne_lon"] = Convert2Double(_payload.Substring(69, 18)) / 600;
            _result["ne_lat"] = Convert2Double(_payload.Substring(87, 17)) / 600;
            _result["sw_lon"] = Convert2Double(_payload.Substring(104, 18)) / 600;
            _result["sw_lat"] = Convert2Double(_payload.Substring(122, 17)) / 600;
            _result["addressed"] = _payload.Substring(139, 1);
            _result["band_a"] = _payload.Substring(140, 1);
            _result["band_b"] = _payload.Substring(141, 1);
            _result["zonesize"] = Convert.ToUInt32(_payload.Substring(142, 3), 2);
            return _result;
        }
        #endregion

        #region Message_23
        private static JObject Message_23(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["ne_lon"] = Convert2Double(_payload.Substring(40, 18)) / 600;
            _result["ne_lat"] = Convert2Double(_payload.Substring(58, 17)) / 600;
            _result["sw_lon"] = Convert2Double(_payload.Substring(75, 18)) / 600;
            _result["sw_lat"] = Convert2Double(_payload.Substring(93, 17)) / 600;
            _result["station_type"] = Convert.ToUInt32(_payload.Substring(110, 4), 2);
            _result["ship_type"] = Convert.ToUInt32(_payload.Substring(114, 8), 2);
            _result["txrx"] = Convert.ToUInt32(_payload.Substring(144, 2), 2);
            _result["interval"] = Convert.ToUInt32(_payload.Substring(146, 4), 2);
            _result["quiet"] = Convert.ToUInt32(_payload.Substring(150, 4), 2);
            return _result;
        }
        #endregion

        #region Message_24
        private static JObject Message_24(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["partno"] = Convert.ToUInt32(_payload.Substring(38, 2), 2);
            if (_result["partno"].Value<int>() == 0)
            {
                _result["shipname"] = Convert2String(_payload.Substring(40, 120));
            }
            else
            {
                _result["shiptype"] = Convert.ToUInt32(_payload.Substring(40, 8), 2);
                _result["vendorid"] = Convert2String(_payload.Substring(48, 18));
                _result["model"] = Convert.ToUInt32(_payload.Substring(66, 4), 2);
                _result["serial"] = Convert.ToUInt32(_payload.Substring(70, 20), 2);
                _result["callsign"] = Convert2String(_payload.Substring(90, 42));
                _result["to_bow"] = Convert.ToUInt32(_payload.Substring(132, 9), 2);
                _result["to_stern"] = Convert.ToUInt32(_payload.Substring(141, 9), 2);
                _result["to_port"] = Convert.ToUInt32(_payload.Substring(150, 6), 2);
                _result["to_starboard"] = Convert.ToUInt32(_payload.Substring(156, 6), 2);
                _result["mothership_mmsi"] = Convert.ToUInt32(_payload.Substring(132, 30), 2);
            }
            return _result;
        }
        #endregion

        #region Message_25
        private static JObject Message_25(uint _type, string _payload)
        {
            if (_payload.Length < 76) { return null; }

            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["addressed"] = _payload.Substring(38, 1);
            _result["structured"] = _payload.Substring(39, 1);
            _result["dest_mmsi"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["app_id"] = Convert.ToUInt32(_payload.Substring(70, 16), 2);
            _result["data"] = _payload.Substring(76);
            return _result;
        }
        #endregion

        #region Message_26
        private static JObject Message_26(uint _type, string _payload)
        {
            if (_payload.Length < 76) { return null; }

            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["addressed"] = _payload.Substring(38, 1);
            _result["structured"] = _payload.Substring(39, 1);
            _result["dest_mmsi"] = Convert.ToUInt32(_payload.Substring(40, 30), 2);
            _result["app_id"] = Convert.ToUInt32(_payload.Substring(70, 16), 2);
            _result["data"] = _payload.Substring(76);
            return _result;
        }
        #endregion

        #region Message_27
        private static JObject Message_27(uint _type, string _payload)
        {
            JObject _result = new JObject();
            _result["type"] = _type;
            _result["repeat"] = Convert.ToUInt32(_payload.Substring(6, 2), 2);
            _result["mmsi"] = Convert.ToUInt32(_payload.Substring(8, 30), 2);
            _result["accuracy"] = _payload.Substring(38, 1);
            _result["raim"] = _payload.Substring(39, 1);
            _result["status"] = Convert.ToUInt32(_payload.Substring(40, 4), 2);
            _result["lng"] = Convert2Double(_payload.Substring(44, 18)) / 600;
            _result["lat"] = Convert2Double(_payload.Substring(62, 17)) / 600;
            _result["speed"] = Convert.ToUInt32(_payload.Substring(79, 6), 2);
            _result["course"] = Convert.ToUInt32(_payload.Substring(85, 9), 2);
            _result["gnss"] = Convert.ToUInt32(_payload.Substring(94, 1), 2);
            return _result;
        }
        #endregion

        #region Checksum
        private static int Checksum(string _text)
        {
            string _data = _text.Substring(1);
            int _result = 0;
            foreach (char _char in _data) { _result ^= (byte)_char; }
            return Convert.ToInt32(_result.ToString("X"), 16);
        }
        #endregion

        #region Convert2Double
        private static double Convert2Double(string _raw)
        {
            double _result = (double)Convert.ToInt64(_raw, 2);
            if (_raw.StartsWith("1")) { _result = _result - Math.Pow(2, _raw.Length); }
            return _result;
        }
        #endregion

        #region Convert2String
        private static string Convert2String(string _raw)
        {
            string _result = string.Empty;
            for (var i = 0; i < _raw.Length / 6; i++)
            {
                byte _byte = Convert.ToByte(_raw.Substring(i * 6, 6), 2);
                if (_byte < 32) { _byte = (byte)(_byte + 64); }
                if (_byte != 64) { _result = _result + (char)_byte; }
            }
            return _result.Trim();
        }
        #endregion
    }
}