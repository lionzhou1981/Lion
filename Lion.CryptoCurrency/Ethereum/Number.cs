using System;
using System.Globalization;
using System.Numerics;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Number
    {
        public BigInteger Integer;
        public int Decimal;

        public Number(decimal _value, int _decimal = 18)
        {
            this.Decimal = _decimal;
            this.Integer = BigInteger.Parse(_value.ToString($"0.{"".PadRight(_decimal, '0')}").Replace(".",""));
        }
        public Number(BigInteger _integer, int _decimal = 18)
        {
            this.Decimal = _decimal;
            this.Integer = _integer;
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
    }
}
