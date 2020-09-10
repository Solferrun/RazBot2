using System;
using System.IO;
using System.Net;

namespace RazBot
{
    public static class Pastebin
    {
        private static string LoginUrl { get; } = "https://pastebin.com/api/api_login.php";
        private static string PostUrl { get; } = "https://pastebin.com/api/api_post.php";
        private static string Key { get; } = BotTools.Settings["pastebin_token"];
        private static string User { get; } = BotTools.Settings["pastebin_user"];
        private static string Pass { get; } = BotTools.Settings["pastebin_pass"];
        public static string MakePasteFile(string fileTitle, string fileText)
        {
            var request = (HttpWebRequest)WebRequest.Create(LoginUrl);

            var loginData = "api_dev_key=" + Uri.EscapeDataString(Key);
            loginData += "&api_user_name=" + Uri.EscapeDataString(User);
            loginData += "&api_user_password=" + Uri.EscapeDataString(Pass);
            var data = System.Text.Encoding.ASCII.GetBytes(loginData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            string tempToken = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create paste
            request = (HttpWebRequest)WebRequest.Create(PostUrl);

            var pasteData = "api_option=" + Uri.EscapeDataString("paste");
            pasteData += "&api_dev_key=" + Uri.EscapeDataString(Key);
            pasteData += "&api_paste_code=" + Uri.EscapeDataString(fileText);
            pasteData += "&api_paste_name=" + Uri.EscapeDataString(fileTitle);
            pasteData += "&api_paste_expirate_date=" + Uri.EscapeDataString("10m");
            pasteData += "&api_user_key=" + Uri.EscapeDataString(tempToken);
            pasteData += "&api_paste_format=" + Uri.EscapeDataString("text");
            data = System.Text.Encoding.ASCII.GetBytes(pasteData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            response = (HttpWebResponse)request.GetResponse();

            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
