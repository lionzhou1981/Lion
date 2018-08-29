using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Lion.Encrypt
{
    public class Base32
    {
        private static int InByteSize = 8;
        private static int OutByteSize = 5;
        private static string Base32characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        #region Encode
        public static string Encode(byte[] _byteArray)
        {
            if (_byteArray == null || _byteArray.Length == 0) { return string.Empty; }

            StringBuilder _sb = new StringBuilder(_byteArray.Length * Base32.InByteSize / Base32.OutByteSize);

            int _position = 0;
            int _subPosition = 0;
            byte _outputBase32 = 0;
            int _outputPosition = 0;

            while (_position < _byteArray.Length)
            {
                int _count = Math.Min(InByteSize - _subPosition, OutByteSize - _outputPosition);
                _outputBase32 <<= _count;
                _outputBase32 |= (byte)(_byteArray[_position] >> (InByteSize - (_subPosition + _count)));
                _subPosition += _count;

                if (_subPosition >= InByteSize) { _position++; _subPosition = 0; }
                _outputPosition += _count;

                if (_outputPosition >= OutByteSize)
                {
                    _outputBase32 &= 0x1F;
                    _sb.Append(Base32characters[_outputBase32]);
                    _outputPosition = 0;
                }
            }

            if (_outputPosition > 0)
            {
                _outputBase32 <<= (OutByteSize - _outputPosition);
                _outputBase32 &= 0x1F;
                _sb.Append(Base32characters[_outputBase32]);
            }

            return _sb.ToString();
        }
        #endregion

        #region Decode
        public static byte[] Decode(string _base32)
        {
            if (_base32 == null || _base32 == string.Empty) { return new byte[0]; }

            _base32 = _base32.ToUpperInvariant();
            byte[] _output = new byte[_base32.Length * OutByteSize / InByteSize];

            if (_output.Length == 0) { throw new ArgumentException("Specified string is not valid Base32 format because it doesn't have enough data to construct a complete byte array"); }

            int _position = 0;
            int _subPosition = 0;
            int _outputPosition = 0;
            int _outputSubPosition = 0;

            while (_outputPosition < _output.Length)
            {
                int _current = Base32characters.IndexOf(_base32[_position]);

                if (_current < 0) { throw new ArgumentException(string.Format("Specified string is not valid Base32 format because character \"{0}\" does not exist in Base32 alphabet", _base32[_position])); }

                int _count = Math.Min(OutByteSize - _subPosition, InByteSize - _outputSubPosition);
                _output[_outputPosition] <<= _count;
                _output[_outputPosition] |= (byte)(_current >> (OutByteSize - (_subPosition + _count)));
                _outputSubPosition += _count;

                if (_outputSubPosition >= InByteSize) { _outputPosition++; _outputSubPosition = 0; }
                _subPosition += _count;

                if (_subPosition >= OutByteSize) { _position++; _subPosition = 0; }
            }

            return _output;
        }
        #endregion
    }
}