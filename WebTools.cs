using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace RazBot
{
    public static class WebTools
    {
        public static JObject GetJObjectResponse(string url, WebHeaderCollection headers = null)
        {
            try
            {
                Console.WriteLine($"JObject Fetching @ {url}...");
                WebRequest request = WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                if (headers != null) request.Headers.Add(headers);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    return JObject.Parse(responseFromServer);
                }
            }
            catch (WebException)
            {
                return null;
            }
        }

        public static JArray GetJArrayResponse(string url, WebHeaderCollection headers = null)
        {
            try
            {
                Console.WriteLine($"JArray Fetching @ {url}...");
                WebRequest request = WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                if (headers != null) request.Headers.Add(headers);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream dataStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    return JArray.Parse(responseFromServer);
                }
            }
            catch (WebException)
            {
                return null;
            }
        }

        public static string GetTwitchEndpointResponse(string endpoint)
        {
            for (int i = 3; i > 0; i--)
            {
                string url = $"https://decapi.me/twitch/{endpoint}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    if (!String.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }
                BotTools.LogLine($"Failed to fetch data from \"{endpoint}\" endpoint. Retrying {i} more times...");
            }
            BotTools.LogLine($"Failed to fetch data from \"{endpoint}\" endpoint.");
            return null;
        }
    }
}
