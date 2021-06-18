using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Leaf.xNet;

namespace Live.com_Сombiner
{
    class WorkWithAccount
    {
        #region Свойства класса
        /// <summary>
        /// Паузы, и количество попыток запроса
        /// </summary>
        public static int minPause, maxPause, countRequest, minPauseRegistration, maxPauseRegistration;
        /// <summary>
        /// Перечисление статусов
        /// </summary>
        public enum Status
        {
            True,
            False,
            UnknownError,
            Captcha,
            NoSms
        }
        /// <summary>
        /// Количество аккаунтов для регистрации
        /// </summary>
        public static int CountAccountForRegistration;

        public static Random rand = new Random((int)DateTime.Now.Ticks);
        public static object locker = new object();
        public static object LogOBJ = new object();
        static string mask = "01234567890aebcdf";
        public static List<string> Version = new List<string> { "187.0.0.32.120", "184.0.0.30.117", "177.0.0.30.119" };
        #endregion

        #region Выбор режима работы
        public static void StartWork()
        {
            try
            {
                SaveData.WriteToLog(null, "Начал свою работу");
                RegistrationAccount();
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Запуск метода регистрации и проверка результата
        public static void RegistrationAccount()
        {
            try
            {
                string UserAgent = "",
                        proxyLog = "";
                (string Tzid, string Number) Number = (null, null);
                (string NameSurname, string Password) DataForRegistration = (null, null);
                (string CountryCode, string AcceptLanguage, int CountryNumber) Localization = (null, null, -1);
                ProxyClient proxyClient;

                while (true)
                {
                    #region Выдача данных для регистрации
                    lock (locker)
                    {
                        if (SaveData.UsedRegistration < CountAccountForRegistration)
                        {
                            DataForRegistration = GetNameSurnamePassword.Get();
                            UserAgent = GetUserAgent.get();
                            proxyClient = GetProxy.get();
                            if (String.IsNullOrEmpty(DataForRegistration.NameSurname) || String.IsNullOrEmpty(DataForRegistration.Password))
                                continue;

                            Localization = GetLocalization.get(proxyClient);
                            if (String.IsNullOrEmpty(Localization.CountryCode) || String.IsNullOrEmpty(Localization.AcceptLanguage) || Localization.CountryNumber < 0)
                            {
                                SaveData.WriteToLog($"SYSTEM", "Не смогли локализировать прокси. Меняем прокси");
                                continue;
                            }

                            Number = GetSmsActivate.GetNumber(Localization.CountryNumber);
                            if (String.IsNullOrEmpty(Number.Tzid) || String.IsNullOrEmpty(Number.Number))
                            {
                                SaveData.WriteToLog($"SYSTEM", "Нету номеров под прокси. Меняем прокси");
                                continue;
                            }

                            SaveData.UsedRegistration++;
                            SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}", SaveData.ProcessedRegistrationList);
                        }
                        else
                        {
                            break;
                        }
                        proxyLog = proxyClient == null ? "" : $";{proxyClient.ToString()}";
                    }
                    #endregion

                    #region Вызов метода регистрации, и проверка результата
                    SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Localization.CountryCode}", "Попытка зарегестрировать аккаунт");

                    (Status status, string userAgentOutput, CookieStorage cookie) Data;
                    for (int i = 0; i < countRequest; i++)
                    {
                        Data = GoRegistrationAccount(DataForRegistration.NameSurname, Number, DataForRegistration.Password, UserAgent, proxyClient, Localization.AcceptLanguage);
                        switch (Data.status)
                        {
                            case Status.True:
                                SaveData.GoodRegistration++;
                                SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}", "Аккаунт успешно зарегестрирован");
                                SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Data.userAgentOutput}|{DateTime.Now.ToShortDateString()}", SaveData.GoodRegistrationList);
                                Data.cookie.SaveToFile($"out/cookies/{Number.Number}.jar", true);
                                break;
                            case Status.False:
                                SaveData.InvalidRegistration++;
                                SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}", "Аккаунт не зарегестрирован");
                                SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Data.userAgentOutput}|{DateTime.Now.ToShortDateString()}", SaveData.InvalidRegistrationList);
                                break;
                            case Status.Captcha:
                                SaveData.captcha++;
                                SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}", "Попали на каптчу");
                                SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Data.userAgentOutput}|{DateTime.Now.ToShortDateString()}", SaveData.CaptchaList);
                                break;
                            case Status.NoSms:
                                SaveData.NoSms++;
                                SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}", "Не пришла смс");
                                SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Data.userAgentOutput}|{DateTime.Now.ToShortDateString()}", SaveData.NoSmsList);
                                break;
                            default:
                                SaveData.UnknownError++;
                                SaveData.WriteToLog($"{Number.Number}:{DataForRegistration.Password}", "Неизвестная ошибка.");
                                SaveData.SaveAccount($"{Number.Number}:{DataForRegistration.Password}{proxyLog}|{Data.userAgentOutput}|{DateTime.Now.ToShortDateString()}", SaveData.UnknownErrorList);
                                break;
                        }
                        break;
                    }
                    int sleep = rand.Next(minPauseRegistration, maxPauseRegistration);
                    SaveData.WriteToLog($"System", $"Засыпаем на {sleep / 60000} минут");
                    Thread.Sleep(sleep);
                    #endregion
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        #endregion

        #region Метод регистрации аккаунта
        /// <summary>
        /// Метод регистрации аккаунта
        /// </summary>
        /// <param name="Login">Логин для регистрации</param>
        /// <param name="Password">Пароль для регистрации</param>
        /// <param name="UserAgent">UserAgent</param>
        /// <param name="proxyClient">Прокси</param>
        /// <returns></returns>
        public static (Status status, string userAgentOutput, CookieStorage cookie) GoRegistrationAccount(string nameSurname, (string Tzid, string Number) Number, string password, string userAgent, ProxyClient proxyClient, string acceptLanguage)
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.Cookies = new CookieStorage();
                    request.UseCookies = true;
                    request.Proxy = proxyClient;

                    string version_apk = Version[rand.Next(0, Version.Count)];
                    userAgent = userAgent.Replace("[VERSION]", version_apk).Replace("[Accept-Language]", acceptLanguage); // Готовим User Agent для регистрации.

                    request.UserAgent = "Instagram" + userAgent.AfterOrEmpty("Instagram"); // Вырезаем User Agent Для apk Instagram
                    request["Accept-Language"] = acceptLanguage.Replace("_", "-");
                    string X_IG_App_Locale = acceptLanguage,
                        X_IG_Device_Locale = acceptLanguage,
                        X_IG_Mapped_Locale = acceptLanguage;

                    var UrlParams = new RequestParams();
                    string day = rand.Next(1, 28).ToString(),
                           month = rand.Next(1, 13).ToString(),
                           year = rand.Next(1985, 2003).ToString(),
                           Response;

                    // Создаем Хэши для работы инстаграмма
                    string XIGAndroidID = $"android-{GetHash(16, rand)}",
                        XPigeonSessionId = GetHash(0, rand),
                        waterfall_id = GetHash(0, rand),
                        adid = GetHash(0, rand),
                        phone_id = GetHash(0, rand),
                        X_Ig_Family_Device_Id = GetHash(0, rand);

                    #region Привязываем X_Bloks_Version_Id в зависимости от Версии
                    string X_Bloks_Version_Id = "";
                    switch (version_apk)
                    {
                        case "187.0.0.32.120":
                            X_Bloks_Version_Id = "e097ac2261d546784637b3df264aa3275cb6281d706d91484f43c207d6661931";
                            break;
                        case "184.0.0.30.117":
                            X_Bloks_Version_Id = "befa8522d3a650f9592e33e4540d527c5b93babbdd6233a1bd40e955c9567f30";
                            break;
                        case "177.0.0.30.119":
                            X_Bloks_Version_Id = "38e807d1f50024907c1026934e57bf28a7c421e5ffcdc0a7b0aa31dbd44acd74";
                            break;
                            //case "172.0.0.21.123":
                            //    X_Bloks_Version_Id = "0647a22bbc02f6145d1c3ddd4e87fa47a90e9bf170164de4a4534b45a389e4d6";
                            //    break;
                            //case "170.2.0.30.474":
                            //    X_Bloks_Version_Id = "bfe7510720e920cb359b6fc8e96cfb8323a7127b448ecd0d54dc057e3720e766";
                            //    break;
                            //case "169.1.0.29.135":
                            //    X_Bloks_Version_Id = "fe808146fcbce04d3a692219680092ef89873fda1e6ef41c09a5b6a9852bed94";
                            //    break;
                            //case "167.0.0.24.120":
                            //    X_Bloks_Version_Id = "0e00f30ed0184b9c914a8baad3fe538aa36a9f0faad173486e76af5ee9310d0b";
                            //    break;
                            //case "165.1.0.29.119":
                            //    X_Bloks_Version_Id = "5a6434fa5b288b6b3f3e131afe8c0738e9373c529e2f1b8c36e49335ff4b2413";
                            //    break;
                            //case "159.0.0.40.122":
                            //    X_Bloks_Version_Id = "c76e70c382311c68b2201f168f946d800bbfcb7b6d9e43edbd9342d9a2048377";
                            //    break;
                    }
                    #endregion

                    string X_IG_Capabilities = "3brTvx0=", // 3brTvx8=
                        sn_result = "API_ERROR:+null"; // API_ERROR: class X.9ob:7: 

                    #region Делаем Get запрос на shared_data. Парсинг nonce, Device_Id, Ключи для шифрования.
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("IG-INTENDED-USER-ID", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-IG-Android-ID",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "IG-INTENDED-USER-ID",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Get("https://i.instagram.com/data/shared_data/").ToString();

                    string public_key = Response.BetweenOrEmpty("public_key\":\"", "\""),
                        version = Response.BetweenOrEmpty("version\":\"", "\""),
                        key_id = Response.BetweenOrEmpty("key_id\":\"", "\""),
                        XIGDeviceID = Response.BetweenOrEmpty("device_id\":\"", "\""),
                        nonce = Response.BetweenOrEmpty("nonce\":\"", "\"");
                    #endregion

                    #region Генерируем sn_nonce, генерируем jazoest.
                    List<byte> bytes = new List<byte>();
                    bytes.AddRange(Encoding.Default.GetBytes($"{Number}|{JSTime(true, false)}|"));
                    bytes.AddRange(Convert.FromBase64String(nonce));

                    string jazoest = GetJazo(phone_id),
                           sn_nonce = Convert.ToBase64String(bytes.ToArray());
                    #endregion

                    #region Делаем Get запрос на главную страницу сайта.
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "Ig-Intended-User-Id",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Get("https://b.i.instagram.com/api/v1/zr/token/result/?device_id=" + XIGAndroidID + "&token_hash=&custom_device_id=" + XIGDeviceID + "&fetch_reason=token_expired").ToString();
                    #endregion

                    #region Делаем Post запрос на получение MId.
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    string json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"id\":\"{XIGDeviceID}\",\"server_config_retrieval\":\"1\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("Https://b.i.instagram.com/api/v1/launcher/sync/", json, "application/x-www-form-urlencoded; charset=UTF-8");

                    string csrf_token = request.Cookies.GetCookie("csrftoken", "Https://b.i.instagram.com/api/v1/launcher/sync/").Value.ToString();
                    string xmid = request.Cookies.GetCookie("mid", "Https://b.i.instagram.com/api/v1/launcher/sync/").Value.ToString();
                    #endregion

                    #region Делаем Post запрос на проверку номера.
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"phone_id\":\"{X_Ig_Family_Device_Id}\",\"login_nonce_map\":\"{{}}\",\"phone_number\":\"{Number.Number}\",\"_csrftoken\":\"{csrf_token}\",\"guid\":\"{XIGDeviceID}\",\"device_id\":\"{XIGAndroidID}\",\"prefill_shown\":\"False\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/accounts/check_phone_number/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/check_phone_number/").Value.ToString();
                    #endregion

                    if (!Response.Contains("status\":\"ok\""))
                    {
                        SaveData.WriteToLog($"{Number.Number}:{password}", "Номер не подошел");
                        return (Status.False, userAgent, request.Cookies);
                    }

                    #region Отправляем смс на Номер.
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"phone_id\":\"{X_Ig_Family_Device_Id}\",\"phone_number\":\"{Number.Number}\",\"_csrftoken\":\"{csrf_token}\",\"guid\":\"{XIGDeviceID}\",\"device_id\":\"{XIGAndroidID}\",\"android_build_type\":\"release\",\"waterfall_id\":\"{waterfall_id}\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    request.Post("https://i.instagram.com/api/v1/accounts/send_signup_sms_code/", json, "application/x-www-form-urlencoded; charset=UTF-8");

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/send_signup_sms_code/").Value.ToString();
                    #endregion

                    #region принимаем смс
                    string code = GetSmsActivate.GetCode(Number.Tzid);
                    if (code == null)
                    {
                        GetSmsActivate.Status(Number.Tzid, 6);      // Завершили активацию номера
                        SaveData.WriteToLog($"{Number.Number}:{password}", "Смс не пришла");
                        return (Status.NoSms, userAgent, request.Cookies);
                    }
                    #endregion

                    #region Отправляем запрос с полученным кодом.
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"verification_code\":\"{code}\",\"phone_number\":\"{Number.Number}\",\"_csrftoken\":\"{csrf_token}\",\"guid\":\"{XIGDeviceID}\",\"device_id\":\"{XIGAndroidID}\",\"waterfall_id\":\"{waterfall_id}\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/accounts/validate_signup_sms_code/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/validate_signup_sms_code/").Value.ToString();
                    #endregion

                    #region Отправляем Get запрос. Подтверждение регистрации
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Get("https://i.instagram.com/api/v1/si/fetch_headers/?guid=" + XIGDeviceID.Replace("-", "") + "&challenge_type=signup").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/validate_signup_sms_code/").Value.ToString();
                    #endregion

                    #region Отправляем Post запросы на ввод Имени+Фамилии и парсим логин
                    int i = 0;
                    while (i != nameSurname.Length)
                    {
                        i += rand.Next(2, 4);

                        if (i > nameSurname.Length)
                            i = nameSurname.Length;

                        request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                        request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                        request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                        request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                        request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                        request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                        request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                        request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                        request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                        request.AddHeader("X-IG-WWW-Claim", "0");
                        request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                        request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                        request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                        request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                        request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                        request.AddHeader("X-Ig-Timezone-Offset", "10800");
                        request.AddHeader("X-IG-Connection-Type", "WIFI");
                        request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                        request.AddHeader("X-IG-App-ID", "567067343352427");
                        request.AddHeader("X-Mid", xmid);
                        request.AddHeader("Ig-Intended-User-Id", "0");
                        request.AddHeader("X-FB-HTTP-Engine", "Liger");
                        request.AddHeader("X-FB-Client-IP", "True");
                        request.AddHeader("X-FB-Server-Cluster", "True");
                        request.KeepAlive = false;

                        #region Порядок Хэдеров
                        request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                        #endregion

                        json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"phone_id\":\"{X_Ig_Family_Device_Id}\",\"_csrftoken\":\"{csrf_token}\",\"guid\":\"{XIGDeviceID}\",\"name\":\"{nameSurname.Substring(0, i)}\",\"device_id\":\"{XIGAndroidID}\",\"email\":\"\",\"waterfall_id\":\"{waterfall_id}\"}}")}";

                        Thread.Sleep(rand.Next(minPause, maxPause));
                        Response = request.Post("https://i.instagram.com/api/v1/accounts/username_suggestions/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                        csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/username_suggestions/").Value.ToString();
                    }

                    string Login = GetLogin(Response.BetweenOrEmpty("suggestions\":[", "]"));
                    #endregion

                    #region Отправляем Post запрос на подтверждение Даты
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"_csrftoken={System.Web.HttpUtility.UrlEncode(csrf_token)}&day={System.Web.HttpUtility.UrlEncode(day)}&year={System.Web.HttpUtility.UrlEncode(year)}&month={System.Web.HttpUtility.UrlEncode(month)}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/consent/check_age_eligibility/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/consent/check_age_eligibility/").Value.ToString();
                    #endregion

                    #region Отправляем Post запрос new_user_flow_begins
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"_csrftoken\":\"{csrf_token}\",\"device_id\":\"{XIGDeviceID}\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/consent/new_user_flow_begins/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/consent/new_user_flow_begins/").Value.ToString();
                    #endregion

                    #region Отправляем Post запрос на check_username
                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"_csrftoken\":\"{csrf_token}\",\"username\":\"{Login}\",\"uuid\":\"{XIGDeviceID}\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/users/check_username/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/users/check_username/").Value.ToString();
                    #endregion

                    #region Конечный запрос на регистрацию
                    request.IgnoreProtocolErrors = true;
                    string enc_password = EncryptionService.GetEncryptPassword(password, public_key, key_id, version);

                    request.AddHeader("X-IG-App-Locale", X_IG_App_Locale);
                    request.AddHeader("X-IG-Device-Locale", X_IG_Device_Locale);
                    request.AddHeader("X-IG-Mapped-Locale", X_IG_Mapped_Locale);
                    request.AddHeader("X-Pigeon-Session-Id", XPigeonSessionId);
                    request.AddHeader("X-Pigeon-Rawclienttime", JSTime(false, true));
                    request.AddHeader("X-IG-Bandwidth-Speed-KBPS", rand.Next(7000, 10000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalBytes-B", rand.Next(500000, 1000000).ToString());
                    request.AddHeader("X-IG-Bandwidth-TotalTime-MS", rand.Next(50, 150).ToString());
                    request.AddHeader("X-Bloks-Version-Id", X_Bloks_Version_Id);
                    request.AddHeader("X-IG-WWW-Claim", "0");
                    request.AddHeader("X-Bloks-Is-Layout-RTL", "false");
                    request.AddHeader("X-Bloks-Is-Panorama-Enabled", "true");
                    request.AddHeader("X-IG-Device-ID", XIGDeviceID);
                    request.AddHeader("X-Ig-Family-Device-Id", X_Ig_Family_Device_Id);
                    request.AddHeader("X-IG-Android-ID", XIGAndroidID);
                    request.AddHeader("X-Ig-Timezone-Offset", "10800");
                    request.AddHeader("X-IG-Connection-Type", "WIFI");
                    request.AddHeader("X-IG-Capabilities", X_IG_Capabilities);
                    request.AddHeader("X-IG-App-ID", "567067343352427");
                    request.AddHeader("X-Mid", xmid);
                    request.AddHeader("Ig-Intended-User-Id", "0");
                    request.AddHeader("X-FB-HTTP-Engine", "Liger");
                    request.AddHeader("X-FB-Client-IP", "True");
                    request.AddHeader("X-FB-Server-Cluster", "True");
                    request.KeepAlive = false;

                    #region Порядок Хэдеров
                    request.AddHeadersOrder(new List<string>()
                    {
                    "Host",
                    "Cookie",
                    "X-IG-App-Locale",
                    "X-IG-Device-Locale",
                    "X-IG-Mapped-Locale",
                    "X-Pigeon-Session-Id",
                    "X-Pigeon-Rawclienttime",
                    "X-IG-Bandwidth-Speed-KBPS",
                    "X-IG-Bandwidth-TotalBytes-B",
                    "X-IG-Bandwidth-TotalTime-MS",
                    "X-Bloks-Version-Id",
                    "X-IG-WWW-Claim",
                    "X-Bloks-Is-Layout-RTL",
                    "X-Bloks-Is-Panorama-Enabled",
                    "X-IG-Device-ID",
                    "X-Ig-Family-Device-Id",
                    "X-IG-Android-ID",
                    "X-Ig-Timezone-Offset",
                    "X-IG-Connection-Type",
                    "X-IG-Capabilities",
                    "X-IG-App-ID",
                    "User-Agent",
                    "Accept-Language",
                    "X-Mid",
                    "Ig-Intended-User-Id",
                    "Content-Type",
                    "Content-Length",
                    "Accept-Encoding",
                    "X-FB-HTTP-Engine",
                    "X-FB-Client-IP",
                    "X-FB-Server-Cluster",
                    "Connection"
                    });
                    #endregion

                    json = $"signed_body={System.Web.HttpUtility.UrlEncode($"SIGNATURE.{{\"is_secondary_account_creation\":\"false\",\"jazoest\":\"{jazoest}\",\"tos_version\":\"row\",\"suggestedUsername\":\"\",\"verification_code\":\"{code}\",\"sn_result\":\"{sn_result}\",\"do_not_auto_login_if_credentials_match\":\"true\",\"phone_id\":\"{X_Ig_Family_Device_Id}\",\"enc_password\":\"{enc_password}\",\"phone_number\":\"{Number.Number}\",\"_csrftoken\":\"{csrf_token}\",\"username\":\"{Login}\",\"first_name\":\"{nameSurname}\",\"day\":\"{day}\",\"adid\":\"{adid}\",\"guid\":\"{XIGDeviceID}\",\"year\":\"{year}\",\"device_id\":\"{XIGAndroidID}\",\" uuid\":\"{XIGDeviceID}\",\"month\":\"{month}\",\"sn_nonce\":\"{sn_nonce}\",\"force_sign_up_code\":\"\",\"waterfall_id\":\"{waterfall_id}\",\"qs_stamp\":\"\",\"has_sms_consent\":\"true\",\"one_tap_opt_in\":\"true\"}}")}";

                    Thread.Sleep(rand.Next(minPause, maxPause));
                    Response = request.Post("https://i.instagram.com/api/v1/accounts/create_validated/", json, "application/x-www-form-urlencoded; charset=UTF-8").ToString();

                    csrf_token = request.Cookies.GetCookie("csrftoken", "https://i.instagram.com/api/v1/accounts/create_validated/").Value.ToString();
                    #endregion

                    if (Response.Contains("account_created\":true"))
                    {
                        GetSmsActivate.Status(Number.Tzid, 6);      // Завершили активацию номера
                        return (Status.True, userAgent, request.Cookies);      // Валидный аккаунт
                    }
                    if (Response.Contains("challenge_required"))   // Капча
                    {
                        GetSmsActivate.Status(Number.Tzid, 6);      // Завершили активацию номера
                        return (Status.Captcha, userAgent, request.Cookies);    // Каптча
                    }
                    else
                    {
                        GetSmsActivate.Status(Number.Tzid, 6);      // Завершили активацию номера
                        return (Status.False, userAgent, request.Cookies);    // Не удалось зарегестрировать
                    }
                }
            }
            catch (Exception exception) { SaveData.WriteToLog($"{Number.Number}:{password}", $"Ошибка: {exception.Message}"); };
            GetSmsActivate.Status(Number.Tzid, 6);      // Завершили активацию номера
            return (Status.UnknownError, userAgent, null);     // Неизвестная ошибка
        }
        #endregion

        #region UnixTime
        public static string JSTime(bool cut = false)
        {
            try
            {
                string t = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalMilliseconds.ToString();

                if (t.Contains(","))
                    t = t.Substring(0, t.IndexOf(','));

                if (cut && t.Length > 10) t = t.Remove(t.Length - 3, 3);

                return t;
            }
            catch { }

            return "";
        }
        #endregion

        #region Метод парсинга нужной библиотеки JS
        /// <summary>
        /// Метод парсинга нужной библиотеки JS
        /// </summary>
        /// <param name="librarys">Массив строк с библиотеками JS</param>
        /// <param name="currentLibrarys">Какую библиотеку JS будем искать</param>
        /// <returns></returns>
        public static string ParseCurrentLibrary(string[] librarys, string currentLibrarys)
        {
            try
            {
                foreach (var Librarys in librarys)
                    if (Librarys.Contains(currentLibrarys))
                        return "https://www.instagram.com" + Librarys.BetweenOrEmpty("href=\"", "\"");
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
            return null;
        }
        #endregion

        #region Генерация фейк xMid
        public static string GetXmid()
        {
            int randLength = rand.Next(52, 55);
            string sourceline = "abcdefghijklmnopqrstuvwxyz0123456789", xmid = "";
            for (int i = 0; i < randLength; i++)
                xmid += sourceline[rand.Next(sourceline.Length)];
            return xmid;
        }
        #endregion

        #region генератор Хэшей
        public static string GetHash(int len, Random rnd)
        {
            string id = "";
            int i = 0;
            if (len == 0)
            {
                for (i = 0; i < 8; i++)
                    id += mask[rnd.Next(mask.Length)].ToString();
                id += "-";

                for (i = 0; i < 4; i++)
                    id += mask[rnd.Next(mask.Length)].ToString();
                id += "-4";

                for (i = 0; i < 3; i++)
                    id += mask[rnd.Next(mask.Length)].ToString();
                id += "-";

                for (i = 0; i < 4; i++)
                    id += mask[rnd.Next(mask.Length)].ToString();
                id += "-";


                for (i = 0; i < 12; i++)
                    id += mask[rnd.Next(mask.Length)].ToString();
            }
            else
                while (id.Length < len) id += mask[rnd.Next(mask.Length)];

            Thread.Sleep(0);
            return id;
        }
        #endregion

        #region Парсим Логин
        public static string GetLogin(string logins)
        {
            List<string> Logins = new List<string>();
            try
            {
                while (logins.Contains("username"))
                {
                    Logins.Add(logins.BetweenOrEmpty("username\":\"", "\""));
                    logins = logins.Replace("username\":\"" + logins.BetweenOrEmpty("username\":\"", "\"") + "\"", "");
                }
            }
            catch (Exception exception) { Console.WriteLine(exception.Message); }
            return Logins[rand.Next(Logins.Count)];
        }
        #endregion

        #region Encoding строки для отправки
        public static string HtmlEncode(string text)
        {
            try
            {
                char[] chars = System.Web.HttpUtility.HtmlEncode(text).ToCharArray();
                StringBuilder result = new StringBuilder(text.Length + (int)(text.Length * 0.1));

                foreach (char c in chars)
                {
                    int value = Convert.ToInt32(c);
                    if (value > 127)
                        result.AppendFormat("&#{0};", value);
                    else
                        result.Append(c);
                }
                return result.ToString();
            }
            catch (Exception exception) { Console.WriteLine(exception.Message); }
            return null;
        }
        #endregion

        #region UnixTime
        public static string JSTime(bool cut = false, bool instTime = false)
        {
            try
            {
                string t = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalMilliseconds.ToString();

                if (t.Contains(","))
                    t = t.Substring(0, t.IndexOf(','));

                if (cut && t.Length > 10) t = t.Remove(t.Length - 3, 3);

                if (instTime) t = t.Insert(t.Length - 3, ".");

                return t;
            }
            catch { }

            return "";
        }
        #endregion

        #region Метод генерации Jazoest
        static string GetJazo(string fb)
        {

            string u = "";
            int sum = 0;
            // fb == AQEmFprDdzgc
            try
            {

                foreach (char c in fb)
                {
                    //u += ((int)c).ToString();
                    sum += (int)c;
                }
                u = $"2{sum}";
            }
            catch { }
            return u;
        }
        #endregion

        #region Удаляем пустые куки [не используется]
        public static CookieStorage RemoveEmptyCookies(CookieStorage storage)
        {
            CookieStorage result = new CookieStorage();

            foreach (System.Net.Cookie c in storage.GetCookies("https://instagram.com"))
            {
                if (c.Value.Trim() != "" && c.Value != "\"\"")
                {
                    result.Add(c);
                }
            }

            return result;
        }
        #endregion

        #region Из Hex в Массив байтов [не используется]
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        #endregion
    }
}