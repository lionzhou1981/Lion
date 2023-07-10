using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Lion.SDK.SmtpMail
{
    public static class SmtpMail
    {
        static string User;
        static string Password;
        static string Host;
        static int Port;

        static bool Inited = false;
        public static void Init(string _user,string _password,string _host,int _port)
        {
            User = _user;
            Password = _password;
            Host = _host;
            Port = _port;
            Inited = true;
        }

        public static bool Send(string _senderName, string _receiver, string _title, string _htmlBody)
        {
            if (!Inited)
                throw new Exception("Not inited");
            try
            {
                SmtpClient _client = new SmtpClient();
                _client.Host = Host;
                _client.Port = Port;
                _client.Credentials = new NetworkCredential(User, Password);
                _client.Send(new MailMessage(User, _receiver)
                {
                    Sender = new MailAddress(User, _senderName),
                    Body = _htmlBody,
                    IsBodyHtml = true,
                    Subject = _title,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Smtp Mail send error:" + ex.Message + "|" + ex.StackTrace);
                return false;
            }
        }

    }
}
