using System;
using System.Security.Cryptography;
using System.Text;
using Lion.Net;
using Newtonsoft.Json.Linq;

namespace Lion.SDK.Google
{
    public class GoogleIAP
    {
        #region VerifySignature
        public static bool VerifySignature(string _message, string _signature, string _publicKey)
        {
            try
            {
                byte[] _publicBytes = Convert.FromBase64String(_publicKey);

                RSA _rsa = RSA.Create();
                _rsa.ImportSubjectPublicKeyInfo(_publicBytes, out var _);

                using RSACryptoServiceProvider _rsaProvider = new RSACryptoServiceProvider();
                _rsaProvider.ImportParameters(_rsa.ExportParameters(false));

                return _rsaProvider.VerifyData(
                    Encoding.UTF8.GetBytes(_message),
                    HashAlgorithmName.SHA1.Name,
                    Convert.FromBase64String(_signature));
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex);
                return false;
            }
        }
        #endregion
    }
}

