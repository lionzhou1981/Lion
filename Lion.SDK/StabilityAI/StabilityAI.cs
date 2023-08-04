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
        public static void Init(JObject _settingss)
        {
            Auth = _settingss["Auth"].Value<string>();
            Host = _settingss["Host"].Value<string>();
            Engine = _settingss["Engine"].Value<string>();
            Timeout = _settingss["Timeout"].Value<int>();
        }
        #endregion

        #region SingleText2Image
        public static bool SingleText2Image(string[] _prompts,int _width, int _height, int _steps, string _style, int _cfg_scale, out byte[] _result)
        {
            string _path = $"/v1/generation/{Engine}/text-to-image";

            JArray _promptList = new JArray();
            foreach (string _prompt in _prompts) { _promptList.Add(new JObject() { ["text"] = _prompt }); }

            JObject _data = new JObject();
            _data["text_prompts"] = _promptList;
            _data["cfg_scale"] = _cfg_scale;
            _data["height"] = _height;
            _data["width"] = _width;
            _data["samples"] = 1;
            _data["steps"] = _steps;
            //3d-model analog-film anime cinematic comic-book digital-art enhance fantasy-art isometric line-art low-poly modeling-compound neon-punk origami photographic pixel-art tile-texture
            _data["style_preset"] = _style;

            try
            {
                _result = CallForByteArray(_path, _data);
                return true;
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
            Console.WriteLine(_url);

            WebClientPlus _web = new WebClientPlus(Timeout);
            _web.Headers["Content-Type"] = "application/json";
            _web.Headers["Authorization"] = $"Bearer {Auth}";
            _web.Headers["Accept"] = $"image/png";
            byte[] _result = _web.UploadData(_url, Encoding.UTF8.GetBytes(_data.ToString(Formatting.None)));
            _web.Dispose();

            return _result;
        }
        #endregion
    }

}
