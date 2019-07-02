using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.IO.Compression;

namespace Lion.Net
{
    public static class HttpFormRequest
    {
        public static bool PostFormData(string url, Dictionary<string, string> _headers, Dictionary<string, string> _formDicts, out string _result, CredentialCache _credentialCache = null, int _timeOut = 60 * 1000)
        {
            _result = "";
            try
            {
                var _formStream = new MemoryStream();
                var _request = (HttpWebRequest)WebRequest.Create(url);
                var _formboundary = "----" + DateTime.Now.ToUniversalTime().Ticks;
                var _beginBoundary = Encoding.ASCII.GetBytes("--" + _formboundary + "\r\n");
                var _endBoundary = Encoding.ASCII.GetBytes("\r\n--" + _formboundary + "--\r\n");
                _request.Method = "POST";
                _request.Timeout = _timeOut;
                if (_credentialCache != null)
                    _request.Credentials = _credentialCache;
                _headers.ToList().ForEach(t =>
                {
                    if (t.Key == "Host")
                        _request.Host = t.Value;
                    else if (t.Key == "Accept")
                        _request.Accept = t.Value;
                    else if (t.Key == "User-Agent")
                        _request.UserAgent = t.Value;
                    else
                        _request.Headers.Add(t.Key, t.Value);
                });
                _request.ContentType = "multipart/form-data; boundary=" + _formboundary;
                var _formdataformat = "\r\n--" + _formboundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                foreach (byte[] _formitembytes in from string key in _formDicts.Keys
                                                  select string.Format(_formdataformat, key, _formDicts[key])
                                                     into _formitem
                                                  select Encoding.UTF8.GetBytes(_formitem))
                {
                    _formStream.Write(_formitembytes, 0, _formitembytes.Length);
                }
                _formStream.Write(_endBoundary, 0, _endBoundary.Length);
                _request.ContentLength = _formStream.Length;

                var _formDataBuffer = new byte[_formStream.Length];
                _formStream.Position = 0;
                _formStream.Read(_formDataBuffer, 0, _formDataBuffer.Length);
                _formStream.Close();

                var _requestStream = _request.GetRequestStream();
                _requestStream.Write(_formDataBuffer, 0, _formDataBuffer.Length);
                _requestStream.Close();

                var _response = (HttpWebResponse)_request.GetResponse();
                Stream _responseStream = _response.GetResponseStream();
                if (_response.ContentEncoding.ToLower().Contains("gzip"))
                    _responseStream = new GZipStream(_responseStream, CompressionMode.Decompress);

                StreamReader reader = new StreamReader(_responseStream, Encoding.UTF8);
                var _responseContent = reader.ReadToEnd();
                _response.Close();
                _request.Abort();
                _result = _responseContent;
                return true;
            }
            catch (Exception _e)
            {
                _result = _e.Message + "|" + _e.StackTrace;
                return false;
            }
        }
    }
}
