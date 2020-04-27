using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Lion
{
    [Serializable]
    public class BigFloatPlus : IComparable, IComparable<BigFloatPlus>, IEquatable<BigFloatPlus>
    {
        private BigInteger numerator;
        private BigInteger denominator;

        public static readonly BigFloatPlus One = new BigFloatPlus(1);
        public static readonly BigFloatPlus Zero = new BigFloatPlus(0);
        public static readonly BigFloatPlus MinusOne = new BigFloatPlus(-1);
        public static readonly BigFloatPlus OneHalf = new BigFloatPlus(1, 2);

        public int Sign
        {
            get
            {
                switch (numerator.Sign + denominator.Sign)
                {
                    case 2:
                    case -2:
                        return 1;
                    case 0:
                        return -1;
                    default:
                        return 0;
                }
            }
        }

        //constructors
        public BigFloatPlus()
        {
            numerator = BigInteger.Zero;
            denominator = BigInteger.One;
        }
        public BigFloatPlus(string value)
        {
            BigFloatPlus bf = Parse(value);
            this.numerator = bf.numerator;
            this.denominator = bf.denominator;
        }
        public BigFloatPlus(BigInteger numerator, BigInteger denominator)
        {
            this.numerator = numerator;
            if (denominator == 0)
                throw new ArgumentException("denominator equals 0");
            this.denominator = BigInteger.Abs(denominator);
        }
        public BigFloatPlus(BigInteger value)
        {
            this.numerator = value;
            this.denominator = BigInteger.One;
        }
        public BigFloatPlus(BigFloatPlus value)
        {
            if (BigFloatPlus.Equals(value, null))
            {
                this.numerator = BigInteger.Zero;
                this.denominator = BigInteger.One;
            }
            else
            {

                this.numerator = value.numerator;
                this.denominator = value.denominator;
            }
        }
        public BigFloatPlus(ulong value)
        {
            numerator = new BigInteger(value);
            denominator = BigInteger.One;
        }
        public BigFloatPlus(long value)
        {
            numerator = new BigInteger(value);
            denominator = BigInteger.One;
        }
        public BigFloatPlus(uint value)
        {
            numerator = new BigInteger(value);
            denominator = BigInteger.One;
        }
        public BigFloatPlus(int value)
        {
            numerator = new BigInteger(value);
            denominator = BigInteger.One;
        }
        public BigFloatPlus(float value) : this(value.ToString("N99"))
        {
        }
        public BigFloatPlus(double value) : this(value.ToString("N99"))
        {
        }
        public BigFloatPlus(decimal value) : this(value.ToString("N99"))
        {
        }

        //non-static methods
        public BigFloatPlus Add(BigFloatPlus other)
        {
            if (BigFloatPlus.Equals(other, null))
                throw new ArgumentNullException("other");

            this.numerator = this.numerator * other.denominator + other.numerator * this.denominator;
            this.denominator *= other.denominator;
            return this;
        }
        public BigFloatPlus Subtract(BigFloatPlus other)
        {
            if (BigFloatPlus.Equals(other, null))
                throw new ArgumentNullException("other");

            this.numerator = this.numerator * other.denominator - other.numerator * this.denominator;
            this.denominator *= other.denominator;
            return this;
        }
        public BigFloatPlus Multiply(BigFloatPlus other)
        {
            if (BigFloatPlus.Equals(other, null))
                throw new ArgumentNullException("other");

            this.numerator *= other.numerator;
            this.denominator *= other.denominator;
            return this;
        }
        public BigFloatPlus Divide(BigFloatPlus other)
        {
            if (BigInteger.Equals(other, null))
                throw new ArgumentNullException("other");
            if (other.numerator == 0)
                throw new System.DivideByZeroException("other");

            this.numerator *= other.denominator;
            this.denominator *= other.numerator;
            return this;
        }
        public BigFloatPlus Remainder(BigFloatPlus other)
        {
            if (BigInteger.Equals(other, null))
                throw new ArgumentNullException("other");

            //b = a mod n
            //remainder = a - floor(a/n) * n

            BigFloatPlus result = this - Floor(this / other) * other;

            this.numerator = result.numerator;
            this.denominator = result.denominator;


            return this;
        }
        public BigFloatPlus DivideRemainder(BigFloatPlus other, out BigFloatPlus remainder)
        {
            this.Divide(other);

            remainder = BigFloatPlus.Remainder(this, other);

            return this;
        }
        public BigFloatPlus Pow(int exponent)
        {
            if (numerator.IsZero)
            {
                // Nothing to do
            }
            else if (exponent < 0)
            {
                BigInteger savedNumerator = numerator;
                numerator = BigInteger.Pow(denominator, -exponent);
                denominator = BigInteger.Pow(savedNumerator, -exponent);
            }
            else
            {
                numerator = BigInteger.Pow(numerator, exponent);
                denominator = BigInteger.Pow(denominator, exponent);
            }

            return this;
        }
        public BigFloatPlus Abs()
        {
            numerator = BigInteger.Abs(numerator);
            return this;
        }
        public BigFloatPlus Negate()
        {
            numerator = BigInteger.Negate(numerator);
            return this;
        }
        public BigFloatPlus Inverse()
        {
            BigInteger temp = numerator;
            numerator = denominator;
            denominator = temp;
            return this;
        }
        public BigFloatPlus Increment()
        {
            numerator += denominator;
            return this;
        }
        public BigFloatPlus Decrement()
        {
            numerator -= denominator;
            return this;
        }
        public BigFloatPlus Ceil()
        {
            if (numerator < 0)
                numerator -= BigInteger.Remainder(numerator, denominator);
            else
                numerator += denominator - BigInteger.Remainder(numerator, denominator);

            Factor();
            return this;
        }
        public BigFloatPlus Floor()
        {
            if (numerator < 0)
                numerator += denominator - BigInteger.Remainder(numerator, denominator);
            else
                numerator -= BigInteger.Remainder(numerator, denominator);

            Factor();
            return this;
        }
        public BigFloatPlus Round()
        {
            //get remainder. Over divisor see if it is > new BigFloat(0.5)
            BigFloatPlus value = BigFloatPlus.Decimals(this);

            if (value.CompareTo(OneHalf) >= 0)
                this.Ceil();
            else
                this.Floor();

            return this;
        }
        public BigFloatPlus Truncate()
        {
            numerator -= BigInteger.Remainder(numerator, denominator);
            Factor();
            return this;
        }
        public BigFloatPlus Decimals()
        {
            BigInteger result = BigInteger.Remainder(numerator, denominator);

            return new BigFloatPlus(result, denominator);
        }
        public BigFloatPlus ShiftDecimalLeft(int shift)
        {
            if (shift < 0)
                return ShiftDecimalRight(-shift);

            numerator *= BigInteger.Pow(10, shift);
            return this;
        }
        public BigFloatPlus ShiftDecimalRight(int shift)
        {
            if (shift < 0)
                return ShiftDecimalLeft(-shift);
            denominator *= BigInteger.Pow(10, shift);
            return this;
        }
        public double Sqrt()
        {
            return Math.Pow(10, BigInteger.Log10(numerator) / 2) / Math.Pow(10, BigInteger.Log10(denominator) / 2);
        }
        public double Log10()
        {
            return BigInteger.Log10(numerator) - BigInteger.Log10(denominator);
        }
        public double Log(double baseValue)
        {
            return BigInteger.Log(numerator, baseValue) - BigInteger.Log(numerator, baseValue);
        }
        public override string ToString()
        {
            //default precision = 100
            return ToString(100);
        }
        public string ToString(int precision, bool trailingZeros = false)
        {
            Factor();

            BigInteger remainder;
            BigInteger result = BigInteger.DivRem(numerator, denominator, out remainder);

            if (remainder == 0 && trailingZeros)
                return result + ".0";
            else if (remainder == 0)
                return result.ToString();


            BigInteger decimals = (numerator * BigInteger.Pow(10, precision)) / denominator;

            if (decimals == 0 && trailingZeros)
                return result + ".0";
            else if (decimals == 0)
                return result.ToString();

            StringBuilder sb = new StringBuilder();

            while (precision-- > 0 && decimals > 0)
            {
                sb.Append(decimals % 10);
                decimals /= 10;
            }

            if (trailingZeros)
                return result + "." + new string(sb.ToString().Reverse().ToArray());
            else
                return result + "." + new string(sb.ToString().Reverse().ToArray()).TrimEnd(new char[] { '0' });


        }
        public string ToMixString()
        {
            Factor();

            BigInteger remainder;
            BigInteger result = BigInteger.DivRem(numerator, denominator, out remainder);

            if (remainder == 0)
                return result.ToString();
            else
                return result + ", " + remainder + "/" + denominator;
        }

        public string ToRationalString()
        {
            Factor();

            return numerator + " / " + denominator;
        }
        public int CompareTo(BigFloatPlus other)
        {
            if (BigFloatPlus.Equals(other, null))
                throw new ArgumentNullException("other");

            //Make copies
            BigInteger one = this.numerator;
            BigInteger two = other.numerator;

            //cross multiply
            one *= other.denominator;
            two *= this.denominator;

            //test
            return BigInteger.Compare(one, two);
        }
        public int CompareTo(object other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            if (!(other is BigFloatPlus))
                throw new System.ArgumentException("other is not a BigFloatPlus");

            return CompareTo((BigFloatPlus)other);
        }
        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            return this.numerator == ((BigFloatPlus)other).numerator && this.denominator == ((BigFloatPlus)other).denominator;
        }
        public bool Equals(BigFloatPlus other)
        {
            return (other.numerator == this.numerator && other.denominator == this.denominator);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

#pragma warning disable 0108
        //static methods
        public static bool Equals(object left, object right)
        {
            if (left == null && right == null) return true;
            else if (left == null || right == null) return false;
            else if (left.GetType() != right.GetType()) return false;
            else
                return (((BigInteger)left).Equals((BigInteger)right));
        }
#pragma warning restore 0108
        public static string ToString(BigFloatPlus value)
        {
            return value.ToString();
        }

        public static BigFloatPlus Inverse(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Inverse();
        }
        public static BigFloatPlus Decrement(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Decrement();
        }
        public static BigFloatPlus Negate(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Negate();
        }
        public static BigFloatPlus Increment(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Increment();
        }
        public static BigFloatPlus Abs(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Abs();
        }
        public static BigFloatPlus Add(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Add(right);
        }
        public static BigFloatPlus Subtract(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Subtract(right);
        }
        public static BigFloatPlus Multiply(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Multiply(right);
        }
        public static BigFloatPlus Divide(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Divide(right);
        }
        public static BigFloatPlus Pow(BigFloatPlus value, int exponent)
        {
            return (new BigFloatPlus(value)).Pow(exponent);
        }
        public static BigFloatPlus Remainder(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Remainder(right);
        }
        public static BigFloatPlus DivideRemainder(BigFloatPlus left, BigFloatPlus right, out BigFloatPlus remainder)
        {
            return (new BigFloatPlus(left)).DivideRemainder(right, out remainder);
        }
        public static BigFloatPlus Decimals(BigFloatPlus value)
        {
            return value.Decimals();
        }
        public static BigFloatPlus Truncate(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Truncate();
        }
        public static BigFloatPlus Ceil(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Ceil();
        }
        public static BigFloatPlus Floor(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Floor();
        }
        public static BigFloatPlus Round(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Round();
        }
        public static BigFloatPlus Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            value.Trim();
            value = value.Replace(",", "");
            int pos = value.IndexOf('.');
            value = value.Replace(".", "");

            if (pos < 0)
            {
                //no decimal point
                BigInteger numerator = BigInteger.Parse(value);
                return (new BigFloatPlus(numerator)).Factor();
            }
            else
            {
                //decimal point (length - pos - 1)
                BigInteger numerator = BigInteger.Parse(value);
                BigInteger denominator = BigInteger.Pow(10, value.Length - pos);

                return (new BigFloatPlus(numerator, denominator)).Factor();
            }
        }
        public static BigFloatPlus ShiftDecimalLeft(BigFloatPlus value, int shift)
        {
            return (new BigFloatPlus(value)).ShiftDecimalLeft(shift);
        }
        public static BigFloatPlus ShiftDecimalRight(BigFloatPlus value, int shift)
        {
            return (new BigFloatPlus(value)).ShiftDecimalRight(shift);
        }
        public static bool TryParse(string value, out BigFloatPlus result)
        {
            try
            {
                result = BigFloatPlus.Parse(value);
                return true;
            }
            catch (ArgumentNullException)
            {
                result = null;
                return false;
            }
            catch (FormatException)
            {
                result = null;
                return false;
            }
        }
        public static int Compare(BigFloatPlus left, BigFloatPlus right)
        {
            if (BigFloatPlus.Equals(left, null))
                throw new ArgumentNullException("left");
            if (BigFloatPlus.Equals(right, null))
                throw new ArgumentNullException("right");

            return (new BigFloatPlus(left)).CompareTo(right);
        }
        public static double Log10(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Log10();
        }
        public static double Log(BigFloatPlus value, double baseValue)
        {
            return (new BigFloatPlus(value)).Log(baseValue);
        }
        public static double Sqrt(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Sqrt();
        }

        public static BigFloatPlus operator -(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Negate();
        }
        public static BigFloatPlus operator -(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Subtract(right);
        }
        public static BigFloatPlus operator --(BigFloatPlus value)
        {
            return value.Decrement();
        }
        public static BigFloatPlus operator +(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Add(right);
        }
        public static BigFloatPlus operator +(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Abs();
        }
        public static BigFloatPlus operator ++(BigFloatPlus value)
        {
            return value.Increment();
        }
        public static BigFloatPlus operator %(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Remainder(right);
        }
        public static BigFloatPlus operator *(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Multiply(right);
        }
        public static BigFloatPlus operator /(BigFloatPlus left, BigFloatPlus right)
        {
            return (new BigFloatPlus(left)).Divide(right);
        }
        public static BigFloatPlus operator >>(BigFloatPlus value, int shift)
        {
            return (new BigFloatPlus(value)).ShiftDecimalRight(shift);
        }
        public static BigFloatPlus operator <<(BigFloatPlus value, int shift)
        {
            return (new BigFloatPlus(value)).ShiftDecimalLeft(shift);
        }
        public static BigFloatPlus operator ^(BigFloatPlus left, int right)
        {
            return (new BigFloatPlus(left)).Pow(right);
        }
        public static BigFloatPlus operator ~(BigFloatPlus value)
        {
            return (new BigFloatPlus(value)).Inverse();
        }

        public static bool operator !=(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) != 0;
        }
        public static bool operator ==(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) == 0;
        }
        public static bool operator <(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) < 0;
        }
        public static bool operator <=(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) <= 0;
        }
        public static bool operator >(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) > 0;
        }
        public static bool operator >=(BigFloatPlus left, BigFloatPlus right)
        {
            return Compare(left, right) >= 0;
        }

        public static bool operator true(BigFloatPlus value)
        {
            return value != 0;
        }
        public static bool operator false(BigFloatPlus value)
        {
            return value == 0;
        }

        public static explicit operator decimal(BigFloatPlus value)
        {
            if (decimal.MinValue > value) throw new System.OverflowException("value is less than System.decimal.MinValue.");
            if (decimal.MaxValue < value) throw new System.OverflowException("value is greater than System.decimal.MaxValue.");

            return (decimal)value.numerator / (decimal)value.denominator;
        }
        public static explicit operator double(BigFloatPlus value)
        {
            if (double.MinValue > value) throw new System.OverflowException("value is less than System.double.MinValue.");
            if (double.MaxValue < value) throw new System.OverflowException("value is greater than System.double.MaxValue.");

            return (double)value.numerator / (double)value.denominator;
        }
        public static explicit operator float(BigFloatPlus value)
        {
            if (float.MinValue > value) throw new System.OverflowException("value is less than System.float.MinValue.");
            if (float.MaxValue < value) throw new System.OverflowException("value is greater than System.float.MaxValue.");

            return (float)value.numerator / (float)value.denominator;
        }

        //byte, sbyte, 
        public static implicit operator BigFloatPlus(byte value)
        {
            return new BigFloatPlus((uint)value);
        }
        public static implicit operator BigFloatPlus(sbyte value)
        {
            return new BigFloatPlus((int)value);
        }
        public static implicit operator BigFloatPlus(short value)
        {
            return new BigFloatPlus((int)value);
        }
        public static implicit operator BigFloatPlus(ushort value)
        {
            return new BigFloatPlus((uint)value);
        }
        public static implicit operator BigFloatPlus(int value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(long value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(uint value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(ulong value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(decimal value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(double value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(float value)
        {
            return new BigFloatPlus(value);
        }
        public static implicit operator BigFloatPlus(BigInteger value)
        {
            return new BigFloatPlus(value);
        }
        public static explicit operator BigFloatPlus(string value)
        {
            return new BigFloatPlus(value);
        }

        private BigFloatPlus Factor()
        {
            //factoring can be very slow. So use only when neccessary (ToString, and comparisons)

            if (denominator == 1)
                return this;

            //factor numerator and denominator
            BigInteger factor = BigInteger.GreatestCommonDivisor(numerator, denominator);

            numerator /= factor;
            denominator /= factor;

            return this;
        }

        public static BigFloatPlus ToMicroTez(BigFloatPlus source)
        {
            return source * 1000000;
        }

        public static BigFloatPlus ToTez(BigFloatPlus source)
        {
            return source / 1000000;
        }
    }
}
