using Leaf.xNet;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Live.com_Сombiner
{
    class GetMailKit
    {
        public static int CountRequests = 100, PauseOfRequest = 5000;

        #region Метод для прочтения всех сообщений
        public static bool ReadMessages((string Email, string Password) Email, string password, HttpRequest request)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (var client = new ImapClient())
                    {
                        #region выдача прокси
                        if (request.Proxy != null)
                        {
                            string Host = request.Proxy.Host.ToString(),
                                Port = request.Proxy.Port.ToString();

                            if (request.Proxy.Host != null && request.Proxy.Username == null)
                            {

                                switch (request.Proxy.Type.ToString())
                                {
                                    case "HTTP":
                                        client.ProxyClient = new MailKit.Net.Proxy.HttpProxyClient(Host, int.Parse(Port));
                                        break;
                                    case "Socks4":
                                        client.ProxyClient = new MailKit.Net.Proxy.Socks4Client(Host, int.Parse(Port));
                                        break;
                                    case "Socks5":
                                        client.ProxyClient = new MailKit.Net.Proxy.Socks5Client(Host, int.Parse(Port));
                                        break;
                                }
                            }
                            if (request.Proxy.Host != null && request.Proxy.Username != null && request.Proxy.Password != null)
                            {
                                var nc = new NetworkCredential(request.Proxy.Username.ToString(), request.Proxy.Password.ToString());

                                switch (request.Proxy.Type.ToString())
                                {
                                    case "HTTP":
                                        client.ProxyClient = new MailKit.Net.Proxy.HttpProxyClient(Host, int.Parse(Port), nc);
                                        break;
                                    case "Socks4":
                                        client.ProxyClient = new MailKit.Net.Proxy.Socks4Client(Host, int.Parse(Port), nc);
                                        break;
                                    case "Socks5":
                                        client.ProxyClient = new MailKit.Net.Proxy.Socks5Client(Host, int.Parse(Port), nc);
                                        break;
                                }
                            }
                        }
                        #endregion

                        client.ServerCertificateValidationCallback = (object sender,
                        X509Certificate certificate,
                        X509Chain chain,
                        SslPolicyErrors sslPolicyErrors) => true;

                        client.Connect("imap.gmail.com", 993, true);
                        client.Authenticate(Email.Email, Email.Password);

                        // Читаем все сообщения
                        var inbox = client.Inbox;
                        inbox.Open(FolderAccess.ReadWrite);
                        inbox.AddFlags(UniqueIdRange.All, MessageFlags.Seen, true);

                        //for (int i = 0; i < inbox.Count; i++)
                        //    inbox.AddFlags(i, MessageFlags.Seen, false);
                        client.Disconnect(true);

                        SaveData.WriteToLog($"{Email.Email}:{password}", "Прочитали сообщения на почте");
                        return true;
                    }
                }
                catch { };
            }
            SaveData.WriteToLog($"{Email.Email}:{password}", "Не смогли прочитать сообщения на почте");
            return false;
        }
        #endregion

        #region Парсим код из сообщения
        public static string GetCode((string Email, string Password) Email, string password, HttpRequest request)
        {
            try
            {
                using (var client = new ImapClient())
                {
                    #region выдача прокси
                    if (request.Proxy != null)
                    {
                        string Host = request.Proxy.Host.ToString(),
                            Port = request.Proxy.Port.ToString();

                        if (request.Proxy.Host != null && request.Proxy.Username == null)
                        {

                            switch (request.Proxy.Type.ToString())
                            {
                                case "HTTP":
                                    client.ProxyClient = new MailKit.Net.Proxy.HttpProxyClient(Host, int.Parse(Port));
                                    break;
                                case "Socks4":
                                    client.ProxyClient = new MailKit.Net.Proxy.Socks4Client(Host, int.Parse(Port));
                                    break;
                                case "Socks5":
                                    client.ProxyClient = new MailKit.Net.Proxy.Socks5Client(Host, int.Parse(Port));
                                    break;
                            }
                        }
                        if (request.Proxy.Host != null && request.Proxy.Username != null && request.Proxy.Password != null)
                        {
                            var nc = new NetworkCredential(request.Proxy.Username.ToString(), request.Proxy.Password.ToString());

                            switch (request.Proxy.Type.ToString())
                            {
                                case "HTTP":
                                    client.ProxyClient = new MailKit.Net.Proxy.HttpProxyClient(Host, int.Parse(Port), nc);
                                    break;
                                case "Socks4":
                                    client.ProxyClient = new MailKit.Net.Proxy.Socks4Client(Host, int.Parse(Port), nc);
                                    break;
                                case "Socks5":
                                    client.ProxyClient = new MailKit.Net.Proxy.Socks5Client(Host, int.Parse(Port), nc);
                                    break;
                            }
                        }
                    }
                    #endregion

                    client.ServerCertificateValidationCallback = (object sender,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors) => true;

                    client.Connect("imap.gmail.com", 993, true);
                    client.Authenticate(Email.Email, Email.Password);

                    var inbox = client.Inbox;

                    string code = "";

                    for (int i = 0; i < CountRequests; i++)
                    {
                        inbox.Open(FolderAccess.ReadWrite);
                        foreach (var uid in inbox.Search(SearchQuery.NotSeen))
                        {
                            if (inbox.GetMessage(uid).Subject.Contains("is your Instagram code"))
                            {
                                string subject = inbox.GetMessage(uid).Subject;
                                code = subject.Substring(0, subject.IndexOf(" "));
                                inbox.AddFlags(uid, MessageFlags.Seen, true);
                            }
                            inbox.AddFlags(uid, MessageFlags.Seen, true);
                        }
                        if (code.Length > 0)
                            break;
                        Thread.Sleep(PauseOfRequest);
                    }
                    client.Disconnect(true);
                    SaveData.WriteToLog($"{Email.Email}:{password}", $"Спарсили код {code}");
                    return code;
                }
            }
            catch { };
            SaveData.WriteToLog($"{Email.Email}:{password}", "Не смогли считать код из почты");
            return null;
        }
        #endregion
    }
}
