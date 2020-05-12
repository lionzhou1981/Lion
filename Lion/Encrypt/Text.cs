using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lion.Encrypt
{
    public class Text
    {
        public static string Words = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string Encode(BigInteger _number, int _pattern, string _words="")
        {
            IList<char> _list1 = _words == "" ? Text.Words.ToList() : _words.ToList();
            IList<char> _list2 = new List<char>();

            int _index = 0;
            while (_list1.Count > 0)
            {
                _index += _pattern;
                if (_index >= _list1.Count) { _index =  _index % _list1.Count; }

                _list2.Add(_list1[_index]);
                _list1.RemoveAt(_index);
            }

            string _text = "";
            BigInteger _divisor = _list2.Count;
            while (true)
            {
                _index = int.Parse(BigInteger.Remainder(_number, _divisor).ToString());
                _number = _number / _divisor;
                _text += _list2[_index];
                if (_number == 0) { break; }
            }
            return _text;
        }

        public static BigInteger Decode(string _text,int _pattern, string _words="")
        {
            IList<char> _list1 = _words == "" ? Text.Words.ToList() : _words.ToList();
            IList<char> _list2 = new List<char>();

            int _index = 0;
            while (_list1.Count > 0)
            {
                _index += _pattern;
                if (_index >= _list1.Count) { _index = _index % _list1.Count; }

                _list2.Add(_list1[_index]);
                _list1.RemoveAt(_index);
            }

            BigInteger _number = 0;
            for (int i = _text.Length - 1; i >= 0; i--)
            {
                _index = _list2.IndexOf(_text[i]);

                if (i == _text.Length - 1)
                {
                    _number += _index;
                }
                else
                {
                    _number = _number * _list2.Count + _index;
                }

            }
            return _number;
        }
    }
}
