using System;
using System.Collections.Generic;
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
    }
}
