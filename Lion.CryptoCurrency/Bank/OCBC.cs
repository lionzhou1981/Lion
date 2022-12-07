using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.CryptoCurrency.Bank
{
    public class OCBC
    {
        public static JObject ExchangeRage()
        {
            try
            {
                WebClientPlus _web = new WebClientPlus(5000);
                string _result = _web.DownloadString("https://www.ocbc.com/fxrates/bootstrap.json");
                JObject _json = JObject.Parse(_result);

                JObject _rates = new JObject();
                JArray _list = _json["fxRatesSgd"].Value<JArray>();
                foreach (JObject _item in _list)
                {
                    string _base = _item["baseCurrencyCode"].Value<string>();
                    decimal _rate = _item["middleExchangeRate"].Value<decimal>();
                    _rates[$"SGD{_base}"] = _rate;
                }

                return _rates;
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Lion.CryptoCurrency.Bank.OCBC.ExchangeRage:{_ex}");

                return null;
            }
        }
    }
}
