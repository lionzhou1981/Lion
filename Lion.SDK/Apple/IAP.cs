using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lion.Net;

namespace Lion.SDK.Apple
{
    public interface IAP
    {
        public const string Host = "";
        public static string Issuer = "";
        public static string KeyId = "";
        public static string BundleId = "";
        public static string PrivateKey = "";
        public static bool Sandbox = false;
        public static string Url_StoreKit = "https://api.storekit.itunes.apple.com";
        public static string Url_StoreKit_Sandbox = "https://api.storekit-sandbox.itunes.apple.com";
        public static string Url_Verify = "https://buy.itunes.apple.com";
        public static string Url_Verify_Sandbox = "https://sandbox.itunes.apple.com";

        private static string token = "";
        private static DateTime tokenTime = DateTime.UtcNow;
        private static int tokenExpireMinute = 30;

        #region Init
        public static void Init(JObject _setting)
        {
            Issuer = _setting["Issuer"].Value<string>();
            KeyId = _setting["KeyId"].Value<string>();
            BundleId = _setting["BundleId"].Value<string>();
            PrivateKey = _setting["PrivateKey"].Value<string>();
        }
        #endregion

        #region Token
        public static string Token
        {
            get
            {
                if (token == "" || (DateTime.UtcNow - tokenTime).TotalMinutes > tokenExpireMinute) { token = BuildToken(); }
                return token;
            }
        }
        #endregion

        #region BuildToken
        public static string BuildToken(int _expire = 3000)
        {
            CngKey _cngKey = CngKey.Import(Convert.FromBase64String(PrivateKey), CngKeyBlobFormat.Pkcs8PrivateBlob);
            ECDsaSecurityKey _securityKey = new ECDsaSecurityKey(new ECDsaCng(_cngKey)) { KeyId = KeyId };
            SigningCredentials _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.EcdsaSha256);

            SecurityTokenDescriptor _descriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                IssuedAt = DateTime.UtcNow,
                SigningCredentials = _signingCredentials,
                Expires = DateTime.UtcNow.AddMinutes(tokenExpireMinute),
                Audience = "appstoreconnect-v1",
                Subject = new ClaimsIdentity(new[] { new Claim("sub", BundleId) })
            };
            JwtSecurityTokenHandler  _jwtHandler = new JwtSecurityTokenHandler();
            string _token = _jwtHandler.CreateEncodedJwt(_descriptor);
            return _token;
        }
        #endregion

        #region GetTransaction
        public static string GetTransaction(string _txid)
        {
            WebClientPlus _client = new WebClientPlus(10000);
            _client.Headers.Add("Authorization", $"Bearer {Token}");
            string _result = _client.DownloadString($"{(Sandbox ? Url_StoreKit_Sandbox : Url_StoreKit)}/inApps/v1/history/{_txid}");
            _client.Dispose();

            return _result;
        }
        #endregion

        #region VerifyReceipt
        public static JObject VerifyReceipt(string _content)
        {
            try
            {
                JObject _data = new JObject() { ["receipt-data"] = _content };

                WebClientPlus _client = new WebClientPlus(10000);
                _client.Headers.Add("Content-Type", $"application/json");
                string _result = _client.UploadString($"{(Sandbox ? Url_Verify_Sandbox : Url_Verify)}/verifyReceipt", _data.ToString(Formatting.None));
                _client.Dispose();

                return JObject.Parse(_result);
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Lion.SDK.Apple.IAP.VerifyReceipt : {_ex}");
                return null;
            }

        }
        #endregion
    }
}
