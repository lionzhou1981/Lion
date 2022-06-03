using System;
using System.Globalization;
using System.Numerics;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Number
    {
        public BigInteger Integer;
        public decimal Value;
        public int Decimal;

        public Number(uint _value)
        {
            this.Decimal = 0;
            this.Value = _value;
            this.Integer = BigInteger.Parse(_value.ToString());
        }

        public Number(decimal _value, int _decimal = 18)
        {
            this.Decimal = _decimal;
            this.Value = _value;
            this.Integer = BigInteger.Parse(_value.ToString($"0.{"".PadRight(_decimal, '0')}").Replace(".", ""));
        }

        public Number(BigInteger _integer, int _decimal = 18)
        {
            this.Decimal = _decimal;
            this.Integer = _integer;

            string _value = _integer.ToString().PadLeft(_decimal, '0');
            if (_value.Length > _decimal)
            {
                _value = _value[0..(_value.Length - _decimal)] + "." + _value[(_value.Length - _decimal)..];
            }
            else
            {
                _value = "0." + _value[(_value.Length - _decimal)..];
            }
            this.Value = decimal.Parse(_value);
        }

        public Number(string _hex, int _decimal = 18)
        {
            this.Decimal = _decimal;
            this.Integer = BigInteger.Parse(_hex, NumberStyles.AllowHexSpecifier);
        }

        public byte[] ToBytes()
        {
            byte[] _result = this.Integer.ToByteArray();
            if (_result.Length > 32) { throw new Exception("Value is overflow."); }

            return _result;
        }

        public string ToHex()
        {
            return HexPlus.ByteArrayToHexString(ToBytes());
        }

        public BigInteger ToGWei()
        {
            return BigInteger.Parse(this.Value.ToString($"0.{"".PadRight(Decimal, '0')}").Replace(".", ""));
        }
    }
}
