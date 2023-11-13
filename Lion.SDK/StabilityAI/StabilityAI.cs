using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.StabilityAI
{
    public class StabilityAI
    {
        //https://api.stability.ai
        //stable-diffusion-xl-1024-v1-0
        private static string Auth = "";
        private static string Host = "";
        private static string Engine = "";
        private static int Timeout = 30000;

        #region Init
        public static void Init(JObject _settings)
        {
            Auth = _settings["Auth"].Value<string>();
            Host = _settings["Host"].Value<string>();
            Engine = _settings["Engine"].Value<string>();
            Timeout = _settings["Timeout"].Value<int>();
        }
        #endregion

        #region SingleText2Image
        public static bool SingleText2Image(string _prompt,string _disprompt,int _width, int _height, int _steps, string _style, int _cfg_scale, out byte[] _result)
        {
            string _path = $"/v1/generation/{Engine}/text-to-image";

            JArray _promptList = new JArray();
            _promptList.Add(new JObject() { ["text"] = _prompt, ["weight"] = 1 });
            if (_disprompt != "") { _promptList.Add(new JObject() { ["text"] = _disprompt, ["weight"] = -1 }); }

            JObject _data = new JObject();
            _data["text_prompts"] = _promptList;
            _data["cfg_scale"] = _cfg_scale;
            _data["height"] = _height;
            _data["width"] = _width;
            _data["samples"] = 1;
            _data["steps"] = _steps;
            //3d-model analog-film anime cinematic comic-book digital-art enhance fantasy-art isometric line-art low-poly modeling-compound neon-punk origami photographic pixel-art tile-texture
            if (_style != "") { _data["style_preset"] = _style; }

            try
            {
                _result = CallForByteArray(_path, _data);
                return _result.Length > 0;
            }
            catch(Exception _ex)
            {
                _result = new byte[0];
                Console.WriteLine($"StabilityAI.SingleText2Image - {_ex.Message}");
                return false;
            }
        }
        #endregion

        #region CallForByteArray
        public static byte[] CallForByteArray(string _path, JObject _data)
        {
            string _url = $"{Host}{_path}";

            byte[] _result = new byte[0];
            WebClientPlus _web = new WebClientPlus(Timeout);
            try
            {
                _web.Headers["Content-Type"] = "application/json";
                _web.Headers["Authorization"] = $"Bearer {Auth}";
                _web.Headers["Accept"] = $"image/png";
                _result = _web.UploadData(_url, Encoding.UTF8.GetBytes(_data.ToString(Formatting.None)));
            }
            catch
            {
                Console.WriteLine(_web.GetResponseString(Encoding.UTF8));
            }

            _web.Dispose();

            return _result;
        }
        #endregion
    }

}
