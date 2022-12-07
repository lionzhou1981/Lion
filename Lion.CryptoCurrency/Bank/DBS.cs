using Lion.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.CryptoCurrency.Bank
{
    public class DBS
    {
        public static JObject ExchangeRage()
        {
            try
            {
                WebClientPlus _web = new WebClientPlus(5000);
                string _result = _web.DownloadString($"https://www.dbs.com.sg/sg-rates-api/v1/api/sgrates/getCurrencyConversionRates?FETCH_LATEST={DateTimePlus.DateTime2UnixTime(DateTime.UtcNow)}");
                JObject _json = JObject.Parse(_result);
                JArray _list = _json["results"]["assets"][0]["recData"].Value<JArray>();

                JObject _rates = new JObject();
                foreach (JObject _item in _list)
                {
                    string _code1 = _item["currency"].Value<string>();
                    string _code2 = _item["quoteCurrency"].Value<string>();
                    decimal _middle = (_item["ttSell"].Value<decimal>() + _item["ttBuy"].Value<decimal>()) / 2;

                    _rates[$"{_code2}{_code1}"] = _middle;
                }

                return _rates;
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Lion.CryptoCurrency.Bank.DBS.ExchangeRage:{_ex}");

                return null;
            }
        }
    }
}
