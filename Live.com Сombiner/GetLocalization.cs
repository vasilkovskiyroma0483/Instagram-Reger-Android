using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live.com_Сombiner
{
    class GetLocalization
    {
        public static Random rand = new Random((int)DateTime.Now.Ticks);

        public static (string country, string Accept_Language, int CountryNumber) get(ProxyClient proxyClient)
        {
            string language,
                countryCode;
            int CountryNumber;
            try
            {
                using (var request = new HttpRequest())
                {
                    request.UserAgent = Http.ChromeUserAgent();
                    request.EnableEncodingContent = true;
                    request.Proxy = proxyClient;
                    request.IgnoreProtocolErrors = true;
                    countryCode = request.Get("http://www.geoplugin.net/json.gp").ToString().BetweenOrEmpty("geoplugin_countryCode\":\"", "\"");
                    language = GetAcceptLanguage(countryCode);
                    CountryNumber = GetNumberCode(countryCode);
                }
                return (countryCode, language, CountryNumber);
            }
            catch { };
            return (null, null, -1);
        }

        #region Получает Accept Language
        public static List<string> defaultLanguage = new List<string>() { "en", "us", "es", "de", "ru" };
        public static string GetAcceptLanguage(string countryCode)
        {
            string language = null;

            switch (countryCode.ToUpper())
            {
                case "DJ":
                case "ER":
                case "ET":
                    language = "aa"; break;
                case "AE":
                case "BH":
                case "DZ":
                case "EG":
                case "IQ":
                case "JO":
                case "KW":
                case "LB":
                case "LY":
                case "MA":
                case "OM":
                case "QA":
                case "SA":
                case "SD":
                case "SY":
                case "TN":
                case "YE":
                    language = "ar"; break;
                case "AZ":
                    language = "az"; break;
                case "BY":
                    language = "be"; break;
                case "BG":
                    language = "bg"; break;
                case "BD":
                    language = "bn"; break;
                case "BA":
                    language = "bs"; break;
                case "CZ":
                    language = "cs"; break;
                case "DK":
                    language = "da"; break;
                case "AT":
                case "CH":
                case "DE":
                case "LU":
                    language = "de"; break;
                case "MV":
                    language = "dv"; break;
                case "BT":
                    language = "dz"; break;
                case "GR":
                    language = "el"; break;
                case "AG":
                case "AI":
                case "AQ":
                case "AS":
                case "AU":
                case "BB":
                case "BW":
                case "CA":
                case "GB":
                case "IE":
                case "KE":
                case "NG":
                case "NZ":
                case "PH":
                case "SG":
                case "US":
                case "ZA":
                case "ZM":
                case "ZW":
                    language = "en"; break;
                case "AD":
                case "AR":
                case "BO":
                case "CL":
                case "CO":
                case "CR":
                case "CU":
                case "DO":
                case "EC":
                case "ES":
                case "GT":
                case "HN":
                case "MX":
                case "NI":
                case "PA":
                case "PE":
                case "PR":
                case "PY":
                case "SV":
                case "UY":
                case "VE":
                    language = "es"; break;
                case "EE":
                    language = "et"; break;
                case "IR":
                    language = "fa"; break;
                case "FI":
                    language = "fi"; break;
                case "FO":
                    language = "fo"; break;
                case "BE":
                case "FR":
                case "SN":
                    language = "fr"; break;
                case "IL":
                    language = "he"; break;
                case "IN":
                    language = "hi"; break;
                case "HR":
                    language = "hr"; break;
                case "HT":
                    language = "ht"; break;
                case "HU":
                    language = "hu"; break;
                case "AM":
                    language = "hy"; break;
                case "ID":
                    language = "id"; break;
                case "IS":
                    language = "is"; break;
                case "IT":
                    language = "it"; break;
                case "JP":
                    language = "ja"; break;
                case "GE":
                    language = "ka"; break;
                case "KZ":
                    language = "kk"; break;
                case "GL":
                    language = "kl"; break;
                case "KH":
                    language = "km"; break;
                case "KR":
                    language = "ko"; break;
                case "KG":
                    language = "ky"; break;
                case "UG":
                    language = "lg"; break;
                case "LA":
                    language = "lo"; break;
                case "LT":
                    language = "lt"; break;
                case "LV":
                    language = "lv"; break;
                case "MG":
                    language = "mg"; break;
                case "MK":
                    language = "mk"; break;
                case "MN":
                    language = "mn"; break;
                case "MY":
                    language = "ms"; break;
                case "MT":
                    language = "mt"; break;
                case "MM":
                    language = "my"; break;
                case "NP":
                    language = "ne"; break;
                case "AW":
                case "NL":
                    language = "nl"; break;
                case "NO":
                    language = "no"; break;
                case "PL":
                    language = "pl"; break;
                case "AF":
                    language = "ps"; break;
                case "AO":
                case "BR":
                case "PT":
                    language = "pt"; break;
                case "RO":
                    language = "ro"; break;
                case "RU":
                case "UA":
                    language = "ru"; break;
                case "RW":
                    language = "rw"; break;
                case "AX":
                    language = "se"; break;
                case "SK":
                    language = "sk"; break;
                case "SI":
                    language = "sl"; break;
                case "SO":
                    language = "so"; break;
                case "AL":
                    language = "sq"; break;
                case "ME":
                case "RS":
                    language = "sr"; break;
                case "SE":
                    language = "sv"; break;
                case "TZ":
                    language = "sw"; break;
                case "LK":
                    language = "ta"; break;
                case "TJ":
                    language = "tg"; break;
                case "TH":
                    language = "th"; break;
                case "TM":
                    language = "tk"; break;
                case "CY":
                case "TR":
                    language = "tr"; break;
                case "PK":
                    language = "ur"; break;
                case "UZ":
                    language = "uz"; break;
                case "VN":
                    language = "vi"; break;
                case "CN":
                case "HK":
                case "TW":
                    language = "zh"; break;
                default:
                    language = defaultLanguage[rand.Next(defaultLanguage.Count)]; break;
            }
            return language;
        }
        #endregion

        #region Получаем ID страны для sms-Activate
        public static int GetNumberCode(string countryCode)
        {
            int NumberCode;

            switch (countryCode.ToUpper())
            {
                case "DJ": NumberCode = 168; break;
                case "ER": NumberCode = 176; break;
                case "ET": NumberCode = 71; break;
                case "AE": NumberCode = 95; break;
                case "BH": NumberCode = 145; break;
                case "DZ": NumberCode = 58; break;
                case "EG": NumberCode = 21; break;
                case "IQ": NumberCode = 47; break;
                case "JO": NumberCode = 116; break;
                case "KW": NumberCode = 100; break;
                case "LB": NumberCode = 153; break;
                case "LY": NumberCode = 102; break;
                case "MA": NumberCode = 37; break;
                case "OM": NumberCode = 107; break;
                case "QA": NumberCode = 111; break;
                case "SA": NumberCode = 53; break;
                case "SD": NumberCode = 177; break;
                case "TN": NumberCode = 89; break;
                case "YE": NumberCode = 30; break;
                case "BY": NumberCode = 51; break;
                case "BG": NumberCode = 83; break;
                case "BD": NumberCode = 60; break;
                case "BA": NumberCode = 108; break;
                case "CZ": NumberCode = 63; break;
                case "AT": NumberCode = 50; break;
                case "CH": NumberCode = 173; break;
                case "DE": NumberCode = 43; break;
                case "LU": NumberCode = 165; break;
                case "MV": NumberCode = 159; break;
                case "BT": NumberCode = 158; break;
                case "GR": NumberCode = 129; break;
                case "AG": NumberCode = 169; break;
                case "AU": NumberCode = 175; break;
                case "BB": NumberCode = 118; break;
                case "BW": NumberCode = 123; break;
                case "CA": NumberCode = 36; break;
                case "GB": NumberCode = 16; break;
                case "IE": NumberCode = 23; break;
                case "KE": NumberCode = 8; break;
                case "NG": NumberCode = 19; break;
                case "NZ": NumberCode = 67; break;
                case "PH": NumberCode = 4; break;
                case "US": NumberCode = 187; break;
                case "ZA": NumberCode = 31; break;
                case "ZM": NumberCode = 147; break;
                case "ZW": NumberCode = 96; break;
                case "AR": NumberCode = 39; break;
                case "BO": NumberCode = 92; break;
                case "CL": NumberCode = 151; break;
                case "CO": NumberCode = 33; break;
                case "CR": NumberCode = 93; break;
                case "DO": NumberCode = 109; break;
                case "EC": NumberCode = 105; break;
                case "ES": NumberCode = 56; break;
                case "GT": NumberCode = 94; break;
                case "HN": NumberCode = 88; break;
                case "MX": NumberCode = 54; break;
                case "NI": NumberCode = 90; break;
                case "PA": NumberCode = 112; break;
                case "PE": NumberCode = 65; break;
                case "PR": NumberCode = 97; break;
                case "PY": NumberCode = 87; break;
                case "SV": NumberCode = 101; break;
                case "UY": NumberCode = 156; break;
                case "VE": NumberCode = 70; break;
                case "EE": NumberCode = 34; break;
                case "FI": NumberCode = 163; break;
                case "BE": NumberCode = 82; break;
                case "FR": NumberCode = 78; break;
                case "SN": NumberCode = 61; break;
                case "IL": NumberCode = 13; break;
                case "IN": NumberCode = 22; break;
                case "HR": NumberCode = 45; break;
                case "HT": NumberCode = 26; break;
                case "HU": NumberCode = 84; break;
                case "AM": NumberCode = 148; break;
                case "ID": NumberCode = 6; break;
                case "IS": NumberCode = 132; break;
                case "IT": NumberCode = 86; break;
                case "GE": NumberCode = 128; break;
                case "KZ": NumberCode = 2; break;
                case "KH": NumberCode = 24; break;
                case "KR": NumberCode = 190; break;
                case "KG": NumberCode = 11; break;
                case "UG": NumberCode = 75; break;
                case "LA": NumberCode = 25; break;
                case "LT": NumberCode = 44; break;
                case "LV": NumberCode = 49; break;
                case "MK": NumberCode = 183; break;
                case "MN": NumberCode = 72; break;
                case "MY": NumberCode = 7; break;
                case "MM": NumberCode = 5; break;
                case "NP": NumberCode = 81; break;
                case "AW": NumberCode = 179; break;
                case "NL": NumberCode = 48; break;
                case "NO": NumberCode = 174; break;
                case "PL": NumberCode = 15; break;
                case "AF": NumberCode = 74; break;
                case "AO": NumberCode = 76; break;
                case "BR": NumberCode = 73; break;
                case "PT": NumberCode = 117; break;
                case "RO": NumberCode = 32; break;
                case "RU": NumberCode = 0; break;
                case "UA": NumberCode = 1; break;
                case "RW": NumberCode = 140; break;
                case "SK": NumberCode = 141; break;
                case "SI": NumberCode = 59; break;
                case "SO": NumberCode = 149; break;
                case "AL": NumberCode = 155; break;
                case "ME": NumberCode = 171; break;
                case "RS": NumberCode = 29; break;
                case "SE": NumberCode = 46; break;
                case "TZ": NumberCode = 9; break;
                case "LK": NumberCode = 64; break;
                case "TJ": NumberCode = 143; break;
                case "TH": NumberCode = 52; break;
                case "TM": NumberCode = 161; break;
                case "CY": NumberCode = 77; break;
                case "TR": NumberCode = 62; break;
                case "PK": NumberCode = 66; break;
                case "UZ": NumberCode = 40; break;
                case "VN": NumberCode = 10; break;
                case "CN": NumberCode = 3; break;
                case "HK": NumberCode = 14; break;
                case "TW": NumberCode = 55; break;
                case "CG": NumberCode = 150; break;
                case "MO": NumberCode = 20; break;
                case "CI": NumberCode = 27; break;
                case "GM": NumberCode = 28; break;
                case "GH": NumberCode = 38; break;
                case "CM": NumberCode = 41; break;
                case "TD": NumberCode = 42; break;
                case "GN": NumberCode = 68; break;
                case "ML": NumberCode = 69; break;
                case "PG": NumberCode = 79; break;
                case "MZ": NumberCode = 80; break;
                case "MD": NumberCode = 85; break;
                case "TL": NumberCode = 91; break;
                case "TG": NumberCode = 99; break;
                case "JM": NumberCode = 103; break;
                case "TT": NumberCode = 104; break;
                case "SZ": NumberCode = 106; break;
                case "MR": NumberCode = 114; break;
                case "SL": NumberCode = 115; break;
                case "BI": NumberCode = 119; break;
                case "BJ": NumberCode = 120; break;
                case "BN": NumberCode = 121; break;
                case "BS": NumberCode = 122; break;
                case "BZ": NumberCode = 124; break;
                case "CF": NumberCode = 125; break;
                case "DM": NumberCode = 126; break;
                case "GD": NumberCode = 127; break;
                case "GW": NumberCode = 130; break;
                case "GY": NumberCode = 131; break;
                case "KN": NumberCode = 134; break;
                case "LR": NumberCode = 135; break;
                case "LS": NumberCode = 136; break;
                case "MW": NumberCode = 137; break;
                case "NA": NumberCode = 138; break;
                case "NE": NumberCode = 139; break;
                case "SR": NumberCode = 142; break;
                case "MC": NumberCode = 144; break;
                case "RE": NumberCode = 146; break;
                case "BF": NumberCode = 152; break;
                case "GA": NumberCode = 154; break;
                case "MU": NumberCode = 157; break;
                case "GP": NumberCode = 160; break;
                case "LC": NumberCode = 164; break;
                case "VC": NumberCode = 166; break;
                case "GQ": NumberCode = 167; break;
                case "SS": NumberCode = 177; break;
                case "ST": NumberCode = 178; break;
                case "SC": NumberCode = 184; break;
                case "NC": NumberCode = 185; break;
                case "CV": NumberCode = 186; break;
                default: NumberCode = -1; break;
            }
            return NumberCode;
        }
        #endregion
    }
}
