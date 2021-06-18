using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Live.com_Сombiner
{
    class GetSmsActivate
    {
        public static string api_key = "60255Ab4610d452df4f6fc5Ae1164110";
        public static (string tzid, string number) GetNumber(int countryCode)
        {
            try
            {
                using (var request = new HttpRequest())
                {
                    request.UserAgent = Http.ChromeUserAgent();
                    request.EnableEncodingContent = true;

                    string Response = request.Get("https://sms-activate.ru/stubs/handler_api.php?api_key=" + api_key + "&action=getNumber&service=ig&ref=&country=" + countryCode).ToString();

                    if (Response.Contains("NO_NUMBERS"))
                        return (null, null);

                    string tzid = Response.BetweenOrEmpty(":", ":");
                    string number = Response.Replace(":" + Response.BetweenOrEmpty(":", ":"), "").AfterOrEmpty(":");

                    return (tzid, number);
                }
            }
            catch { };
            return (null, null);
        }

        public static string GetCode(string tzid)
        {
            using (var request = new HttpRequest())
            {
                request.UserAgent = Http.ChromeUserAgent();
                request.EnableEncodingContent = true;

                string Response = request.Get("https://sms-activate.ru/stubs/handler_api.php?api_key=" + api_key + "&action=setStatus&status=1&id=" + tzid).ToString();

                for (int i = 0; i < 85; i++)
                {
                    Response = request.Get("https://sms-activate.ru/stubs/handler_api.php?api_key=" + api_key + "&action=getStatus&id=" + tzid).ToString();

                    if (Response.Contains("STATUS_CANCEL") || Response.Contains("NO_ACTIVATION") || Response.Contains("ERROR_SQL") || Response.Contains("BAD_KEY") || Response.Contains("BAD_ACTION"))
                        break;
                    if (Response.Contains("STATUS_OK"))
                        return Response.AfterOrEmpty(":");

                    Thread.Sleep(5000);
                }
                request.Get("https://sms-activate.ru/stubs/handler_api.php?api_key=" + api_key + "&action=setStatus&status=8&id=" + tzid).ToString();
                return (null);
            }
        }
        public static void Status(string tzid, int Status) // 1 - готовы принять смс, 3 - запросить еще один код, 6 - завершить активацию, 8 - отмена.
        {
            using (var request = new HttpRequest())
            {
                request.UserAgent = Http.ChromeUserAgent();
                request.EnableEncodingContent = true;

                request.Get("https://sms-activate.ru/stubs/handler_api.php?api_key=" + api_key + $"&action=setStatus&status={Status}&id=" + tzid).ToString();
            }
        }
    }
}
