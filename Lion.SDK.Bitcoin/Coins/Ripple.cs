﻿using HtmlAgilityPack;
using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Lion.SDK.Bitcoin.Coins
{
    //XRP
    public class Ripple
    {
        const string Alphabet = "rpshnaf39wBUDNEGHJKLM4PQRST7VWXYZ2bcdeCg65jkm8oFqi1tuvAxyz";
        //private static char[] _mAlphabet;

        //private static  int[] _mIndexes;

        public static bool IsAddress(string _address)
        {
            var _factAddress = (_address.Contains(":") ? _address.Split(':')[0].Trim() : _address);
            var _tag = (_address.Contains(":") ? _address.Split(':')[1].Trim() : "");
            long _tagValue = 0;
            if (!string.IsNullOrWhiteSpace(_tag) && !long.TryParse(_tag, out _tagValue))
                return false;

            var _mIndexes = BuildIndexes(Alphabet.ToCharArray());
            try
            {
                DecodeAndCheck(_factAddress, _mIndexes);
                return true;
            }
            catch
            {
                return false;
            }

        }

        static int[] BuildIndexes(char[] _mAlphabets)
        {
            var _mIndexes = new int[128];

            for (int i = 0; i < _mIndexes.Length; i++)
            {
                _mIndexes[i] = -1;
            }
            for (int i = 0; i < _mAlphabets.Length; i++)
            {
                _mIndexes[_mAlphabets[i]] = i;
            }
            return _mIndexes;
        }

        static byte[] Decode(string input, int[] _mindexes)
        {
            if (input.Length == 0)
            {
                return new byte[0];
            }
            byte[] input58 = new byte[input.Length];
            // Transform the String to a base58 byte sequence
            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];

                int digit58 = -1;
                if (c >= 0 && c < 128)
                {
                    digit58 = _mindexes[c];
                }
                if (digit58 < 0)
                {
                    throw new Exception("Illegal character " + c + " at " + i);
                }

                input58[i] = (byte)digit58;
            }
            // Count leading zeroes
            var zeroCount = 0;
            while (zeroCount < input58.Length && input58[zeroCount] == 0)
            {
                ++zeroCount;
            }
            // The encoding
            var temp = new byte[input.Length];
            var j = temp.Length;

            var startAt = zeroCount;
            while (startAt < input58.Length)
            {
                var mod = DivMod256(input58, startAt);
                if (input58[startAt] == 0)
                {
                    ++startAt;
                }

                temp[--j] = mod;
            }
            // Do no add extra leading zeroes, move j to first non null byte.
            while (j < temp.Length && temp[j] == 0)
            {
                ++j;
            }

            return CopyOfRange(temp, j - zeroCount, temp.Length);
        }

        static byte DivMod256(IList<byte> number58, int startAt)
        {
            var remainder = 0;
            for (var i = startAt; i < number58.Count; i++)
            {
                var digit58 = number58[i] & 0xFF;
                var temp = remainder * 58 + digit58;

                number58[i] = (byte)(temp / 256);

                remainder = temp % 256;
            }

            return (byte)remainder;
        }

        static byte[] DecodeAndCheck(string input, int[] _mindexes)
        {
            byte[] buffer = Decode(input, _mindexes);
            if (buffer.Length < 4)
            {
                throw new Exception("Input too short");
            }

            byte[] toHash = CopyOfRange(buffer, 0, buffer.Length - 4);
            byte[] hashed = CopyOfRange(DoubleDigest(toHash), 0, 4);
            byte[] checksum = CopyOfRange(buffer, buffer.Length - 4, buffer.Length);

            if (!ArrayEquals(checksum, hashed))
            {
                throw new Exception("Checksum does not validate");
            }
            return buffer;
        }

        static byte[] CopyOfRange(byte[] source, int from_, int to)
        {
            var range = new byte[to - from_];
            Array.Copy(source, from_, range, 0, range.Length);
            return range;
        }

        static bool ArrayEquals(
            IReadOnlyCollection<byte> a,
            IReadOnlyList<byte> b)
        {
            if (a.Count != b.Count) return false;
            return !a.Where((t, i) => t != b[i]).Any();
        }

        static byte[] DoubleDigest(byte[] buffer)
        {
            var hash = SHA256.Create();
            return hash.ComputeHash(hash.ComputeHash((buffer)));
        }

        #region GetCurrentHeight
        public static string GetCurrentHeight()
        {
            try
            {
                //string _url = "https://data.ripple.com/v2/health/importer?verbose=true";
                string _url = "https://data.ripple.com/v2/ledgers/";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                //return _json["last_validated_ledger"].Value<string>();
                return _json["ledger"]["ledger_index"].Value<string>();
            }
            catch (Exception)
            {
                return "";
            }
        }
        #endregion

        
        #region CheckTxidBalance
        public static string CheckTxidBalance(string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                string _url = $"https://data.ripple.com/v2/accounts/{_address}/balances";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["balances"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    string _currency = _item["currency"].Value<string>();
                    if (_currency.ToLower() != "xrp") { continue; }
                    _jToken = _item;
                    break;
                }
                if (_jToken == null)
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken["value"].Value<string>();
                //_outBalance = Common.Change2Decimal(_value);
                _outBalance = decimal.Parse(_value);
                //if (!_outBalance.ToString().Contains("."))
                //{
                //    //_outBalance = _outBalance / 1000000000000000000M;
                //    _outBalance = _outBalance / 1000000M;
                //}
                if (_outBalance < _balance)
                {
                    return _error;
                }

                return "";
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex.Message);
                return _error;
            }
        }
        #endregion
        
        /*
        #region CheckTxidBalance
        public static string CheckTxidBalance(string _address, decimal _balance, out decimal _outBalance)
        {
            _outBalance = 0M;
            string _error = "";
            try
            {
                //get info
                _error = "get info";
                //string _url = $"https://data.ripple.com/v2/accounts/{_address}/stats/value?limit=1&descending=true";
                string _url = $"https://data.ripple.com/v2/accounts/{_address}/transactions?&limit=1&descending=true";
                WebClientPlus _webClient = new WebClientPlus(10000);
                string _result = _webClient.DownloadString(_url);
                _webClient.Dispose();
                JObject _json = JObject.Parse(_result);
                JArray _jArray = JArray.Parse(_json["transactions"][0]["meta"]["AffectedNodes"].ToString());
                JToken _jToken = null;
                foreach (var _item in _jArray)
                {
                    if (_item.ToString().Contains("CreatedNode"))
                    {
                        _jToken = _item["CreatedNode"]["NewFields"];
                        break;
                    }
                    string _account = _item["ModifiedNode"]["FinalFields"]["Account"].Value<string>().Trim();
                    if (_account != _address) { continue; }
                    _jToken = _item["ModifiedNode"]["FinalFields"];
                    break;
                }
                if (_jToken == null)
                {
                    return _error;
                }

                //balance
                _error = "balance";
                string _value = _jToken["Balance"] + "";
                _outBalance = Common.Change2Decimal(_value);
                if (!_outBalance.ToString().Contains("."))
                {
                    //_outBalance = _outBalance / 1000000000000000000M;
                    _outBalance = _outBalance / 1000000M;
                }
                if (_outBalance < _balance)
                {
                    return _error;
                }

                return "";
            }
            catch (Exception _ex)
            {
                return _error;
            }
        }
        #endregion
        */
    }
}
