using System;
using System.Linq;

namespace Lion
{
    public class HexPlus
    {
        #region HexToInt32 


        public static int HexToInt32(string _hex)
        {
            _hex = "0" + (_hex.StartsWith("0x", StringComparison.Ordinal) ? _hex.Substring(2) : _hex);
            return int.Parse(_hex, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        #endregion

        #region ByteArrayToHexString
        /// <summary>
        /// Convert byte[] to hex string.
        /// </summary>
        /// <returns>hex string</returns>
        /// <param name="_byteArray">byte[]</param>
        /// <param name="_lower">Is low case</param>
        public static string ByteArrayToHexString(byte[] _byteArray, bool _lower = true)
        {
            string _result = string.Empty;
            if (_byteArray != null || _byteArray.Length > 0)
            {
                foreach (byte _byte in _byteArray)
                {
                    _result += string.Format("{0:X2}", _byte);
                }
            }
            if (_lower) { _result = _result.ToLower(); }
            return _result;
        }
        #endregion

        #region HexStringToByteArray
        /// <summary>
        /// Convert hex string to byte[]
        /// </summary>
        /// <returns>byte[]</returns>
        /// <param name="_hex">hex string.</param>
        public static byte[] HexStringToByteArray(string _hex)
        {
            return Enumerable.Range(0, _hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(_hex.Substring(x, 2), 16)).ToArray();
            byte[] bytes = null;
            if (String.IsNullOrEmpty(_hex))
                bytes = new byte[0];
            else
            {
                int string_length = _hex.Length;
                int character_index = (_hex.StartsWith("0x", StringComparison.Ordinal)) ? 2 : 0; // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                int number_of_characters = string_length - character_index;

                bool add_leading_zero = false;
                if (0 != (number_of_characters % 2))
                {
                    add_leading_zero = true;
                    number_of_characters += 1;  // Leading '0' has been striped from the string presentation.
                }
                bytes = new byte[number_of_characters / 2]; // Initialize our byte array to hold the converted string.
                int write_index = 0;
                if (add_leading_zero)
                {
                    bytes[write_index++] = FromCharacterToByte(_hex[character_index], character_index);
                    character_index += 1;
                }
                for (int read_index = character_index; read_index < _hex.Length; read_index += 2)
                {
                    byte upper = FromCharacterToByte(_hex[read_index], read_index, 4);
                    byte lower = FromCharacterToByte(_hex[read_index + 1], read_index + 1);
                    bytes[write_index++] = (byte)(upper | lower);
                }
            }
            return bytes;
            //return Enumerable.Range(0, _hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(_hex.Substring(x, 2), 16)).ToArray();
        }

        static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            byte value = (byte)character;
            if (((0x40 < value) && (0x47 > value)) || ((0x60 < value) && (0x67 > value)))
            {
                if (0x40 == (0x40 & value))
                {
                    if (0x20 == (0x20 & value))
                        value = (byte)(((value + 0xA) - 0x61) << shift);
                    else
                        value = (byte)(((value + 0xA) - 0x41) << shift);
                }
            }
            else if ((0x29 < value) && (0x40 > value))
                value = (byte)((value - 0x30) << shift);
            else
                throw new InvalidOperationException(String.Format("Character '{0}' at index '{1}' is not valid alphanumeric character.", character, index));

            return value;
        }
        #endregion

        #region Concat
        public static byte[] Concat(params byte[][] _values)
        {
            byte[] _result = new byte[0];
            foreach (byte[] _value in _values)
            {
                int _start = _result.Length;
                Array.Resize(ref _result, _result.Length + _value.Length);
                Array.Copy(_value, 0, _result, _start, _value.Length);
            }
            return _result;
        }
        #endregion

        #region PadLeft
        public static byte[] PadLeft(byte[] _source, int _length, byte _fill = 0x00)
        {
            if (_source.Length > _length) { return _source; }

            byte[] _result = new byte[_length];
            for (int i = 0; i < _result.Length; i++) { _result[i] = _fill; }

            Array.Copy(_source, 0, _result, _result.Length - _source.Length, _source.Length);

            return _result;
        }
        #endregion

        #region PadRight
        public static byte[] PadRight(byte[] _source, int _length, byte _fill = 0x00)
        {
            if (_source.Length > _length) { return _source; }

            byte[] _result = new byte[_length];
            for (int i = 0; i < _result.Length; i++) { _result[i] = _fill; }

            Array.Copy(_source, 0, _result, 0, _source.Length);

            return _result;
        }
        #endregion
    }
}
