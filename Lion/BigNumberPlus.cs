using System;
using System.Text;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Lion.Encrypt;

namespace Lion
{
    public class BigNumberPlus
    {
        #region HexToBigInt
        public static BigInteger HexToBigInt(string _hex)
        {
            _hex = (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            _hex = (_hex.Length % 2) == 1 || _hex[0] != '0' ? "0" + _hex : _hex;
            return System.Numerics.BigInteger.Parse(_hex, System.Globalization.NumberStyles.AllowHexSpecifier);
        }
        #endregion

        #region BigValueToDecimal
        public static string ETHToDecimal(string _hex, int _fractionPoint = 18)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            var _orgValue = System.Numerics.BigInteger.Parse(_hex, System.Globalization.NumberStyles.AllowHexSpecifier).ToString();
            if (_orgValue.Length < _fractionPoint)
                _orgValue = _orgValue.PadLeft(_fractionPoint + 1, '0');
            return _orgValue.Substring(0, _orgValue.Length - _fractionPoint) + "." + _orgValue.Substring(_orgValue.Length - _fractionPoint);
        }
        #endregion

        #region BigIntToDecimal
        public static string BigIntToDecimal(BigInteger _bigInt, int _fractionPoint = 18)
        {
            var _orgValue = _bigInt.ToString();
            if (_orgValue.Length < _fractionPoint)
                _orgValue = _orgValue.PadLeft(_fractionPoint + 1, '0');
            return _orgValue.Substring(0, _orgValue.Length - _fractionPoint) + "." + _orgValue.Substring(_orgValue.Length - _fractionPoint);
        }

        public static string Max(string _a, string _b)
        {
            var _a0 = _a.Contains(".") ? BigInteger.Parse(_a.Split('.')[0].Trim()) : BigInteger.Zero;
            var _af = _a.Contains(".") ? BigInteger.Parse(_a.Split('.')[1].Trim()) : BigInteger.Parse(_a.Trim());

            var _b0 = _b.Contains(".") ? BigInteger.Parse(_b.Split('.')[0].Trim()) : BigInteger.Zero;
            var _bf = _b.Contains(".") ? BigInteger.Parse(_b.Split('.')[1].Trim()) : BigInteger.Parse(_b.Trim());

            if (_a0 > _b0)
                return _a;
            else if (_a0 < _b0)
                return _b;
            if (_af > _bf)
                return _a;
            if (_af < _bf)
                return _b;
            return _a;
        }
        #endregion

        #region ToETHValue
        public static string ToBigValue(string _value, int _fractionPoint = 18)
        {
            var _orgValue = _value;
            var _isdecimal = _value.ToString().Contains(".");
            BigInteger _fractionValue = System.Numerics.BigInteger.Parse(Convert.ToInt64(Math.Pow(10, _fractionPoint)).ToString());
            if (_isdecimal)
            {
                var _fraction = _value.TrimEnd('0').Split('.')[1];
                if (string.IsNullOrWhiteSpace(_fraction))
                    _orgValue = _value.Split('.')[0].Trim();
                else if (_fractionPoint < _fraction.Length)
                {
                    _orgValue = _value.Split('.')[0].Trim() + _fraction.Substring(0, _fractionPoint);
                    _fractionValue = 1;
                }
                else
                {
                    _orgValue = _value.Split('.')[0].Trim() + _value.Split('.')[1].Substring(0, _fraction.Length).Trim();
                    _fractionValue = System.Numerics.BigInteger.Parse(Convert.ToInt64(Math.Pow(10, _fractionPoint - _fraction.Length)).ToString());
                }
            }
            var _converted = _fractionValue * System.Numerics.BigInteger.Parse(_orgValue);
            return $"0x{_converted.ToString("X").TrimStart('0')}";
        }
        #endregion

        #region BigDecimalMultiply
        public static string BigDecimalMultiply(string _a, string _b)
        {
            var _decimalCounA = _a.Contains(".") ? _a.Trim().Split('.')[1].TrimEnd('0').Length : 0;
            var _decimalCounB = _b.Contains(".") ? _b.Trim().Split('.')[1].TrimEnd('0').Length : 0;
            _a = _a.Contains(".") ? _a.TrimEnd('0').Replace(".", "") : _a;
            _b = _b.Contains(".") ? _b.TrimEnd('0').Replace(".", "") : _b;
            var _factValueA = BigInteger.Parse(_a);
            var _factValueB = BigInteger.Parse(_b);
            var _value = (_factValueA * _factValueB).ToString();
            var _decimals = _decimalCounA + _decimalCounB;
            if (_decimals >= _value.Length)
                _value = "0." + _value.PadLeft(_decimals, '0');
            else if (_decimals > 0)
                _value = _value.Substring(0, _value.Length - _decimals) + "." + _value.Substring(_value.Length - _decimals);
            return _value;
        }
        #endregion

        #region BigDecimalCompare
        public static bool BigDecimalAGreateThanB(string _a, string _b)
        {
            _a = _a.StartsWith(".") ? ("0" + _a) : _a;
            _b = _b.StartsWith(".") ? ("0" + _b) : _b;
            var _decimalCounA = _a.Contains(".") ? _a.Trim().Split('.')[1].TrimEnd('0').Length : 0;
            var _decimalCounB = _b.Contains(".") ? _b.Trim().Split('.')[1].TrimEnd('0').Length : 0;

            var _factValueA = BigInteger.Parse(_a.Contains(".") ? _a.Trim().Split('.')[0] : _a);
            var _factValueB = BigInteger.Parse(_b.Contains(".") ? _b.Trim().Split('.')[0] : _b);
            if (_factValueA > _factValueB)
                return true;
            else if (_factValueA == _factValueB)
            {
                if (_decimalCounA > 0 && _decimalCounB == 0)
                    return true;
                return decimal.Parse("0." + _a.Trim().Split('.')[1].Trim('.')) > decimal.Parse("0." + _b.Trim().Split('.')[1].Trim('.'));
            }

            return false;
        }
        #endregion

    }
}