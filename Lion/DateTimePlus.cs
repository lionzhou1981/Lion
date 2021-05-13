using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lion
{
    public class DateTimePlus
    {
        public static DateTime JSTime2DateTime(long _value)
        {
            return DateTime.Parse("1970-1-1 0:0:0.0").AddSeconds(_value);
        }

        public static long DateTime2JSTime(DateTime _time)
        {
            return (long)(_time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
        public static DateTime UnixTime2DateTime(long _value)
        {
            return DateTime.Parse("1970-1-1 0:0:0.0").AddMilliseconds(_value);
        }

        public static long DateTime2UnixTime(DateTime _time)
        {
            return (long)(_time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        private static string[] chinese_tg = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
        private static string[] chinese_dz = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
        private static string[] chinese_sx = { "鼠", "牛", "虎", "免", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" };
        private static string[] chinese_month = { "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊" };
        private static string[] chinese_day = { "初", "十", "廿", "三" };
        private static string[] chinese_days = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        public static string GetChineseYear(int _year)
        {
            if (_year < 4) { return ""; }

            int _tgIndex = (_year - 4) % 10;
            int _dzIndex = (_year - 4) % 12;

            return string.Concat(chinese_tg[_tgIndex], chinese_dz[_dzIndex], "[", chinese_sx[_dzIndex], "]");
        }

        public static string GetChineseMonth(int _month)
        {
            if (_month > 12 || _month < 1) { return ""; }

            return chinese_month[_month - 1];
        }

        public static string GetChineseDay(int _day)
        {
            if (_day < 1 || _day > 31) { return ""; }

            if (_day != 20 && _day != 30)
            {
                return string.Concat(chinese_day[(_day - 1) / 10], chinese_days[(_day - 1) % 10]);
            }
            else
            {
                return string.Concat(chinese_days[(_day - 1) / 10], chinese_day[1]);
            }
        }

        public static string GetChineseDate(DateTime _datetime)
        {
            ChineseLunisolarCalendar _cale = new ChineseLunisolarCalendar();

            int _year = _cale.GetYear(_datetime);
            int _month = _cale.GetMonth(_datetime);
            int _day = _cale.GetDayOfMonth(_datetime);
            int _leapMonth = _cale.GetLeapMonth(_year);

            bool _isleap = false;

            if (_leapMonth > 0)
            {
                if (_leapMonth == _month)
                {
                    _isleap = true;
                    _month--;
                }
                else if (_month > _leapMonth)
                {
                    _month--;
                }
            }

            return string.Concat(GetChineseYear(_year), "年", _isleap ? "闰" : string.Empty, GetChineseMonth(_month), "月", GetChineseDay(_day));
        }
    }
}
