using System;
using System.IO;
using System.Net;

namespace Lion.Net
{
    public class HttpClient : IDisposable
    {
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();

        public string CodeName { get; set; } = "utf-8";

        public string ContentType { get; set; } = "application/x-www-form-urlencoded";

        public bool IsSession { get; set; } = false;

        public IWebProxy Proxy { get; set; } = null;

        public string X_Forwarded_For { get; set; } = null;

        public string Client_IP { get; set; } = null;

        public int Timeout;

        public HttpWebRequest Request;

        public HttpWebResponse Response;

        public string UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1;)";

        public HttpClient(int _timeout) { this.Timeout = _timeout; }

        #region GetResponseByteArray
        public byte[] GetResponseByteArray(string _method, string _url, string _referer, string _data)
        {
            return GetResponseByteArray(_method, _url, _referer, System.Text.Encoding.GetEncoding(this.CodeName).GetBytes(_data));
        }

        public byte[] GetResponseByteArray(string _method, string _url, string _referer, byte[] _data)
        {
            byte[] _return = new byte[0];
            BinaryReader _reader = new BinaryReader(this.GetResponse(_method, _url, _referer, _data));
            _return = _reader.ReadBytes((int)this.Response.ContentLength);
            _reader.Close();
            return _return;
        }

        public byte[] GetResponseByteArray()
        {
            byte[] _return = new byte[0];
            BinaryReader _reader = new BinaryReader(this.Response.GetResponseStream());
            _return = _reader.ReadBytes((int)this.Response.ContentLength);
            _reader.Close();
            return _return;
        }
        #endregion

        #region GetResponseString
        public string GetResponseString(string _method, string _url, string _referer, string _data)
        {
            return GetResponseString(_method, _url, _referer, System.Text.Encoding.GetEncoding(this.CodeName).GetBytes(_data));
        }

        public string GetResponseString(string _method, string _url, string _referer, byte[] _data)
        {
            string _return = "";
            StreamReader _reader = new StreamReader(this.GetResponse(_method, _url, _referer, _data), System.Text.Encoding.GetEncoding(this.CodeName));
            _return = _reader.ReadToEnd();
            _reader.Close();
            return _return;
        }
        public string GetResponseString(System.Text.Encoding _encoding)
        {
            string _return = "";
            StreamReader _reader = new StreamReader(this.Response.GetResponseStream(), _encoding);
            _return = _reader.ReadToEnd();
            _reader.Close();
            return _return;
        }
        #endregion

        #region GetResponse
        public Stream GetResponse(string _method, string _url, string _referer, string _data)
        {
            return GetResponse(_method, _url, _referer, System.Text.Encoding.GetEncoding(this.CodeName).GetBytes(_data));
        }

        public Stream GetResponse(string _method, string _url, string _referer, byte[] _postData)
        {
            this.BeginResponse(_method, _url, _referer);
            this.EndResponse(_postData);
            Stream _return = this.Response.GetResponseStream();
            return _return;
        }
        #endregion

        #region BeginResponse
        public void BeginResponse(string _method, string _url, string _referer)
        {
            this.Request = (HttpWebRequest)WebRequest.Create(_url);
            this.Request.Timeout = this.Timeout;
            this.Request.ReadWriteTimeout = this.Timeout;
            this.Request.UserAgent = this.UserAgent;
            this.Request.Method = _method;
            this.Request.ContentType = this.ContentType;
            this.Request.Referer = _referer;
            this.Request.ContentLength = 0;
            if (this.X_Forwarded_For != null)
            {
                this.Request.Headers["X_Forwarded_For"] = this.X_Forwarded_For;
            }
            if (this.Proxy != null)
            {
                this.Request.Proxy = this.Proxy;
            }
            if (this.IsSession)
            {
                this.Request.CookieContainer = this.CookieContainer;
            }
        }
        #endregion

        #region EndResponse
        public HttpStatusCode EndResponse()
        {
            return this.EndResponse(new byte[0]);
        }
        public HttpStatusCode EndResponse(byte[] _postData)
        {
            if (this.Request.Method != "GET" && _postData.Length > 0)
            {
                byte[] _byteArray = _postData;
                this.Request.ContentLength = _byteArray.Length;
                Stream _stream = this.Request.GetRequestStream();
                _stream.Write(_byteArray, 0, _byteArray.Length);
                _stream.Close();
            }

            try
            {
                this.Response = (HttpWebResponse)this.Request.GetResponse();
            }
            catch (WebException _ex)
            {
                this.Response = (HttpWebResponse)_ex.Response;
            }
            return this.Response.StatusCode;
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            this.CookieContainer = null;
            this.Proxy = null;
            try
            {
                this.Request.GetResponse().Close();
            }
            catch { }
            this.Request = null;
            try
            {
                this.Response.GetResponseStream().Close();
            }
            catch { }
            this.Response = null;
        }
        #endregion
    }
}
