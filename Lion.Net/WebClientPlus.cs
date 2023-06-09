using System;
using System.IO;
using System.Net;

namespace Lion.Net
{
    public class WebClientPlus : WebClient
    {
        private int timeout = 0;
        private WebResponse webResponse = null;
        private bool throwWebException = true;
        private bool allowAutoRedirect = true;

        public WebClientPlus(int _timeout, bool _throwWebException = true, bool _allowAutoRedirect=true) : base()
        {
            this.timeout = _timeout;
            this.throwWebException = _throwWebException;
            this.allowAutoRedirect = _allowAutoRedirect;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest _request = (HttpWebRequest)base.GetWebRequest(address);
            _request.Timeout = this.timeout;
            _request.ReadWriteTimeout = this.timeout;
            _request.AllowAutoRedirect = this.allowAutoRedirect;
            return _request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                this.webResponse = base.GetWebResponse(request);
            }
            catch (WebException _ex)
            {
                if (this.throwWebException) { throw _ex; }
                this.webResponse = _ex.Response;
            }

            return this.webResponse;
        }

        public HttpStatusCode HttpStatusCode
        {
            get
            {
                if (this.webResponse == null)
                {
                    return HttpStatusCode.NotImplemented;
                }
                else
                {
                    return ((HttpWebResponse)this.webResponse).StatusCode;
                }
            }
        }

        public string GetResponseString(System.Text.Encoding _encoder)
        {

            BinaryReader _reader = new BinaryReader(this.webResponse.GetResponseStream());
            byte[] _result = _reader.ReadBytes((int)this.webResponse.ContentLength);
            _reader.Close();

            return _encoder.GetString(_result);
        }
    }
}
