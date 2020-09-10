using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RazBot
{
    static class GuildWars
    {
        private static Dictionary<string, string> RegionKeys { get; } = new Dictionary<string, string>()
        {
            ["na"] = "C2F5B4C3-8717-C84F-B96E-DF15917563847928F0D2-A25B-4ED2-A786-85AA1A981297",
            ["eu"] = "7BEBEEB7-6B28-D94E-BF94-386393694939CEE078AE-CDB6-47EF-B1DA-B6851481D90F"
        };
        private static string BaseURL { get; } = "https://api.guildwars2.com/v2";

        public static string GetMostRecentCharacter(string region = "na")
        {
            JArray characters = GetCharacters(region);
            return characters.HasValues ? characters[0].ToObject<string>() : null;
        }

        public static void FetchInventoryData()
        {
            try
            {
                // Fetch NA item data
                var inventoryData = new Dictionary<string, Dictionary<string, dynamic>>()
                {
                    ["na"] = new Dictionary<string, dynamic>(),
                    ["eu"] = new Dictionary<string, dynamic>()
                };

                List<string> naCharacterNames = GetCharacters("na").ToObject<List<string>>();

                foreach (var characterName in naCharacterNames)
                {
                    string encodedCharacterName = characterName.Replace(" ", "%20");

                    JObject upperInventoryData = WebTools.GetJObjectResponse($"{BaseURL}/characters/{encodedCharacterName}/inventory", DefaultHeaders("na"));
                    if (upperInventoryData != null && upperInventoryData.HasValues)
                    {
                        JToken bags = upperInventoryData["bags"];
                        inventoryData["na"][characterName] = new List<Dictionary<string, dynamic>>();

                        foreach (JToken bag in bags)
                        {
                            if (bag.HasValues)
                            {
                                JToken bagContents = bag["inventory"];
                                foreach (JToken item in bagContents)
                                {
                                    inventoryData["na"][characterName].Add(item.ToObject<Dictionary<string, dynamic>>());
                                }
                            }
                        }
                    }
                }

                JArray naSharedBankData = WebTools.GetJArrayResponse($"{BaseURL}/account/bank", DefaultHeaders("na"));
                JArray naSharedInventoryData = WebTools.GetJArrayResponse($"{BaseURL}/account/inventory", DefaultHeaders("na"));
                JArray naMaterialsData = WebTools.GetJArrayResponse($"{BaseURL}/account/materials", DefaultHeaders("na"));
                JArray naWalletData = WebTools.GetJArrayResponse($"{BaseURL}/account/wallet", DefaultHeaders("na"));

                inventoryData["na"]["Shared Bank"] = naSharedBankData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["na"]["Shared Inventory"] = naSharedInventoryData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["na"]["Materials"] = naMaterialsData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["na"]["Wallet"] = naWalletData.ToObject<List<Dictionary<string, dynamic>>>();

                // Fetch EU item data
                List<string> euCharacterNames = GetCharacters("eu").ToObject<List<string>>();

                foreach (var characterName in euCharacterNames)
                {
                    string encodedCharacterName = characterName.Replace(" ", "%20");

                    JObject upperInventoryData = WebTools.GetJObjectResponse($"{BaseURL}/characters/{encodedCharacterName}/inventory", DefaultHeaders("eu"));
                    if (upperInventoryData != null && upperInventoryData.HasValues)
                    {
                        JToken bags = upperInventoryData["bags"];
                        inventoryData["eu"][characterName] = new List<Dictionary<string, dynamic>>();

                        foreach (JToken bag in bags)
                        {
                            if (bag.HasValues)
                            {
                                JToken bagContents = bag["inventory"];
                                foreach (JToken item in bagContents)
                                {
                                    inventoryData["eu"][characterName].Add(item.ToObject<Dictionary<string, dynamic>>());
                                }
                            }
                        }
                    }
                }

                JArray euSharedBankData = WebTools.GetJArrayResponse($"{BaseURL}/account/bank", DefaultHeaders("eu"));
                JArray euSharedInventoryData = WebTools.GetJArrayResponse($"{BaseURL}/account/inventory", DefaultHeaders("eu"));
                JArray euMaterialsData = WebTools.GetJArrayResponse($"{BaseURL}/account/materials", DefaultHeaders("eu"));
                JArray euWalletData = WebTools.GetJArrayResponse($"{BaseURL}/account/wallet", DefaultHeaders("eu"));

                inventoryData["eu"]["Shared Bank"] = euSharedBankData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["eu"]["Shared Inventory"] = euSharedInventoryData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["eu"]["Materials"] = euMaterialsData.ToObject<List<Dictionary<string, dynamic>>>();
                inventoryData["eu"]["Wallet"] = euWalletData.ToObject<List<Dictionary<string, dynamic>>>();

                BotTools.WriteToJson(inventoryData, "resources\\inventory_data");
            }
            catch (NullReferenceException e)
            {
                BotTools.LogToSessionLogFile(e.ToString());
            }
        }

        public static int GetCauliflowerCount(string region = "na")
        {
            var inventoryData = LoadInventoryData();
            if(inventoryData.Count > 0)
            {
                int cf_total = 0;
                foreach (var characterName in inventoryData[region].Keys)
                {
                    try
                    {
                        List<Dictionary<string, dynamic>> items = inventoryData[region][characterName];
                        int cf_count = 0;
                        foreach (var item in items.Where(i => i != null && i.ContainsKey("id") && i["id"] == 12532))
                        {
                            cf_count += item["count"];
                        }
                        if (cf_count > 0)
                        {
                            cf_total += cf_count;
                        }
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                    {
                        JArray items = inventoryData[region][characterName];
                        int cf_count = 0;
                        foreach (var item in items.ToObject<List<Dictionary<string, dynamic>>>().Where(i => i != null && i.ContainsKey("id") && i["id"] == 12532))
                        {
                            cf_count += item["count"];
                        }
                        if (cf_count > 0)
                        {
                            cf_total += cf_count;
                            BotTools.LogLine($"{characterName}: {cf_count}");
                        }
                    }
                }

                return cf_total;
            }
            else
            {
                return -1;
            }
        }

        public static string CountKP(string region = "na")
        {
            var inventoryData = LoadInventoryData();
            var ms = CountMagnetite(region, inventoryData);
            var gc = CountGaeting(region, inventoryData);
            var li = CountInsights(region, inventoryData);
            return $"MS: {ms} | GC: {gc} | LI: {li}";
        }

        public static int CountInsights(string region = "na", Dictionary<string, Dictionary<string, dynamic>> inventoryData = null)
        {
            var itemValueMap = new Dictionary<int, int>
            {
                [77302] = 1, [77401] = 1, [77449] = 1, [91147] = 1, [91160] = 1, [91182] = 1, [91184] = 1, [91186] = 1, [91187] = 1,
                [91191] = 1, [91203] = 1, [91215] = 1, [91233] = 1, [91252] = 1, [91262] = 1, [91267] = 1, [88485] = 1, [91138] = 1,
                [91157] = 1, [91166] = 1, [91172] = 1, [91195] = 1, [91200] = 1, [91211] = 1, [91220] = 1, [91237] = 1, [91241] = 1,
                [91244] = 1, [91260] = 1, [80111] = 25, [80131] = 25, [80145] = 25, [80161] = 25, [80190] = 25, [80205] = 25,
                [80248] = 25, [80252] = 25, [80254] = 25, [80277] = 25, [80281] = 25, [80296] = 25, [80356] = 25, [80384] = 25,
                [80399] = 25, [80435] = 25, [80557] = 25, [80578] = 25
            };

            if (inventoryData == null)
            {
                inventoryData = LoadInventoryData();
            }
            int total = 0;

            foreach (string character in inventoryData[region].Keys)
            {
                List<Dictionary<string, dynamic>> characterInventory = inventoryData[region][character].ToObject<List<Dictionary<string, dynamic>>>();
                foreach (Dictionary<string, dynamic> item in characterInventory.Where(i => i != null && itemValueMap.ContainsKey((int)i["id"])))
                {
                    total += itemValueMap[(int)item["id"]] * (int)item["count"];
                }
            }

            return total;
        }

        public static int CountMagnetite(string region = "na", Dictionary<string, Dictionary<string, dynamic>> inventoryData = null)
        {
            if (inventoryData == null)
            {
                inventoryData = LoadInventoryData();
            }
            List<Dictionary<string, int>> wallet = inventoryData[region]["Wallet"].ToObject<List<Dictionary<string, int>>>();
            var magnetite = wallet.First(currency => currency["id"] == 28);
            return magnetite["value"];
        }

        public static int CountGaeting(string region = "na", Dictionary<string, Dictionary<string, dynamic>> inventoryData = null)
        {
            if (inventoryData == null)
            {
                inventoryData = LoadInventoryData();
            }
            List<Dictionary<string, int>> wallet = inventoryData[region]["Wallet"].ToObject<List<Dictionary<string, int>>>();
            var magnetite = wallet.First(currency => currency["id"] == 39);
            return magnetite["value"];
        }

        public static string GetInventoryUsage()
        {
            return null;
        }

        private static JArray GetCharacters(string region = "na")
        {
            return WebTools.GetJArrayResponse($"{BaseURL}/characters", headers: DefaultHeaders(region));
        }

        private static WebHeaderCollection DefaultHeaders(string region)
        {
            return new WebHeaderCollection() { $"Authorization: Bearer {RegionKeys[region]}" };
        }

        private static Dictionary<string, Dictionary<string, dynamic>> LoadInventoryData()
        {
            if (File.Exists($"{BotTools.BasePath}\\resources\\inventory_data.json"))
            {
                return BotTools.ReadFromJson<Dictionary<string, Dictionary<string, dynamic>>>("resources\\inventory_data");
            }
            else
            {
                return new Dictionary<string, Dictionary<string, dynamic>>();
            }
        }
    }
}
