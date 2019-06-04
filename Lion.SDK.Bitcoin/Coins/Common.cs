using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{
    public class Common
    {
        #region Change2Decimal
        public static decimal Change2Decimal(string _value)
        {
            decimal _return = 0M;
            try
            {
                if (_value.Contains("E") || _value.Contains("e"))
                {
                    _return = Convert.ToDecimal(Decimal.Parse(_value.ToString(), System.Globalization.NumberStyles.Float));
                }
                else
                {
                    _return = decimal.Parse(_value);
                }
            }
            catch (Exception)
            {
                return 0M;
            }
            return _return;
        }
        #endregion

        #region CheckRate
        public static decimal CheckRate(decimal _value, decimal _standard, string _format = "0.0000")
        {
            decimal _rate = (_standard - _value) / _standard;
            _rate = Math.Abs(_rate);
            _rate = decimal.Parse(_rate.ToString(_format));
            return _rate;
        }
        public static string ChangePercentage(decimal _value, string _format = "0.00")
        {
            decimal _percentage = 0M;

            _percentage = _value * 100M;
            _percentage = decimal.Parse(_percentage.ToString(_format));
            return $"{_percentage}%";
        }
        #endregion
    }
}
