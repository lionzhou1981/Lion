using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace Lion.SDK.Ethereum
{
    public class Number
    {
        public BigInteger Integer;

        public int Decimal;

        public Number(decimal _value, int _decimal = 18)
        {
            this.Decimal = _decimal;
            string[] _parts = _value.ToString().Split('.');
            string _text = _parts.Length > 0 ? _parts[1] : "";
            _text = _text.PadRight(_decimal, '0');
            if (_text.Length > _decimal)
            {
                _text = _text.Substring(0, _decimal);
            }
            this.Integer = BigInteger.Parse(_parts[0] + _text);
        }
        public Number(BigInteger _integer, int _decimal = 18)
        {
            this.Integer = _integer;
            this.Decimal = _decimal;
        }

        public static Number Parse(string _hex, int _decimal = 18)
        {
            return new Number(BigInteger.Parse(_hex, System.Globalization.NumberStyles.AllowHexSpecifier), _decimal);
        }

        public byte[] ToData()
        {
            byte[] _result = this.Integer.ToByteArray();
            if (_result.Length > 32)
            {
                throw new Exception("Value is overflow.");
            }
            return _result;
        }
    }
}
