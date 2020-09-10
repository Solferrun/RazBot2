using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace RazBot
{
    public static class Wit
    {
        private static readonly string appToken = "656QZVOGZXONQLH3F2AOAYPDOA3JRMRI";
        private static readonly HttpClient httpClient = new HttpClient();

        public static Dictionary<string, dynamic> GetIntent(string text)
        {
            string baseUrl = "https://api.wit.ai/message?v=20200730&q=";
            string encodedText = HttpUtility.UrlEncode(text);
            var url = baseUrl + encodedText;
            var headers = new WebHeaderCollection() { $"Authorization: Bearer {appToken}" };

            JObject response = WebTools.GetJObjectResponse(url, headers: headers);
            Console.WriteLine(response);
            try
            {
                if (response.HasValues && response["intents"].Any())
                {
                    var responseDict = response.ToObject<Dictionary<string, dynamic>>();
                    var mainIntent = responseDict["intents"].First;
                    string description = mainIntent["name"].ToString();
                    double confidence = double.Parse(mainIntent["confidence"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                    return new Dictionary<string, dynamic>()
                    {
                        ["description"] = description,
                        ["confidence"] = confidence
                    };
                }
                else
                {
                    return new Dictionary<string, dynamic>();
                }
            }
            catch (Exception e)
            {
                BotTools.LogToSessionLogFile(e.ToString());
                return new Dictionary<string, dynamic>();
            }
        }

        public static List<string> ListIntents()
        {
            string url = "https://api.wit.ai/intents?v=20200730";

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";
            httpWebRequest.Headers.Add("Authorization", $"Bearer {appToken}");

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                var resultObj = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(result);
                var intentNames = resultObj.Select(dict => dict["name"]);
                return intentNames.ToList();
            }
        }
        
        public static void TrainUtterance(string utteranceIn, string intentIn)
        {
            string url = "https://api.wit.ai/utterances?v=20200730";

            var dataList = new List<dynamic>()
            {
                new
                {
                    text = utteranceIn,
                    intent = intentIn,
                    entities = new List<Dictionary<string, string>>(),
                    traits = new List<string>()
                }
            };
            
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("Authorization", $"Bearer {appToken}");

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(dataList);
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                if (result.Contains("\"sent\":true"))
                {
                    BotTools.LogLine($"Asked WitAi to train: \"{utteranceIn}\" -> {intentIn}");
                }
                else
                {
                    BotTools.LogLine("Failed to send training to WitAi");
                }
            }
        }

        public static void AddIntent(string intentIn)
        {
            string url = "https://api.wit.ai/intents?v=20200730";
            var data = new { name = intentIn };

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("Authorization", $"Bearer {appToken}");

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }

        public static void DeleteIntent(string intentName)
        {
            var url = $"https://api.wit.ai/intents/{intentName}?v=20200730";

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "DELETE";
            httpWebRequest.Headers.Add("Authorization", $"Bearer {appToken}");

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }
    }
}
