using System;
using System.Collections.Generic;
using System.Linq;

namespace RazBot
{
    static class Instagram
    {
        private static string RootUrl { get; set; } = "https://graph.instagram.com";
        private static string UserId { get; set; } = "17841427977291681";
        private static string AccessToken { get; set; } = BotTools.Settings["instagram_token"];

        public static List<Dictionary<string, dynamic>> GetPosts()
        {
            string url = $"{RootUrl}/{UserId}/media?access_token={AccessToken}&fields=permalink,caption,media_url";
            
            var response = WebTools.GetJObjectResponse(url);
            try
            {
                if (response != null && response.HasValues && response["data"].Any())
                {
                    return response["data"].ToObject<List<Dictionary<string, dynamic>>>();
                }
                else
                {
                    return new List<Dictionary<string, dynamic>>() {};
                }
            }
            catch (Exception e)
            {
                BotTools.LogToSessionLogFile(e.ToString());
                return new List<Dictionary<string, dynamic>>() {};
            }
        }
    }
}
