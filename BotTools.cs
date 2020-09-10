using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace RazBot
{
    static class BotTools
    {
        public static List<string> ConsoleBuffer { get; set; } = new List<string>();
        public static List<string> NewUtterances { get; set; } = new List<string>();
        public static string BasePath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\RazBot Files";
        public static Dictionary<string, string[]> Playlists { get; set; }
        public static Dictionary<string, dynamic> Settings { get; set; }
        public static string SessionLogFile { get; set; } = null;

        static BotTools()
        {
            Directory.CreateDirectory($"{BasePath}");
            Directory.CreateDirectory($"{BasePath}\\resources");
            Directory.CreateDirectory($"{BasePath}\\logs");
            Directory.CreateDirectory($"{BasePath}\\datasources");
            Directory.CreateDirectory($"{BasePath}\\playlists");
            FilesInit();

            Playlists = new Dictionary<string, string[]>();

            SessionLogFile = $"{BasePath}\\logs\\log-{DateTime.Now:yyMMdd}.txt";
            using (StreamWriter sw = File.AppendText(SessionLogFile))
            {
                sw.WriteLine($"\t--Start of Session [{DateTime.Now:HH:mm:ss}]--");
            }
        }

        public static void LogLine(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return;
            }

            if (s.StartsWith("○"))
            {
                ConsoleBuffer.Add(s);
            }
            else
            {
                ConsoleBuffer.Add("• " + s);
            }
            LogToSessionLogFile(s);
        }

        public static void LogToSessionLogFile(string s)
        {
            bool writeFail = true;
            for (int i = 0; i < 10 && writeFail; i++)
            {
                try
                {
                    if (SessionLogFile != null)
                    {
                        using (StreamWriter sw = File.AppendText(SessionLogFile))
                        {
                            sw.WriteLine($"{DateTime.Now:HH:mm:ss.ff} • {s}");
                        }
                    }
                    writeFail = false;
                }
                catch { }
            }
        }

        public static void ClearConsoleBuffer()
        {
            ConsoleBuffer = new List<string>();
        }

        public static void DumpToTextFile(string content, string relativeFilePath)
        {
            string path = $"{BasePath}\\{relativeFilePath}.txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine(content);
                sw.Close();
            }
        }

        public static void WriteToJson(object obj, string relativeFilePath)
        {
            bool writeFail = true;
            for (int i = 0; i < 10 && writeFail; i++)
            {
                try
                {
                    string path = $"{BasePath}\\{relativeFilePath}.json";

                    var json = JsonConvert.SerializeObject(obj);

                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(json);
                        sw.Close();
                    }
                    writeFail = false;
                }
                catch { }
            }
        }

        public static T ReadFromJson<T>(string relativeFilePath)
        {
            string path = $"{BasePath}\\{relativeFilePath}.json";
            string json = File.ReadAllText(path);
            var obj = JsonConvert.DeserializeObject<T>(json);

            return obj;
        }

        public static T ReadFromJsonString<T>(string jsonString)
        {
            string path = $"{BasePath}\\{jsonString}.json";
            string json = File.ReadAllText(path);
            var obj = JsonConvert.DeserializeObject<T>(json);

            return obj;
        }

        public static string GetPlacementWord(int placement)
        {
            if (placement == 1)
            {
                return "next";
            }
            else if (new[] { 11, 12, 13 }.Contains(placement))
            {
                return $"{placement}th";
            }
            else
            {
                switch (placement % 10)
                {
                    case 1:
                        return $"{placement}st";
                    case 2:
                        return $"{placement}nd";
                    case 3:
                        return $"{placement}rd";
                    default:
                        return $"{placement}th";
                }
            }
        }

        public static void SaveSettings()
        {
            BotTools.WriteToJson(Settings, "resources\\bot_settings");
        }

        public static void FilesInit()
        {
            if (File.Exists($"{BasePath}\\resources\\bot_settings.json"))
            {
                Settings = BotTools.ReadFromJson<Dictionary<string, dynamic>>("resources\\bot_settings");
            }
            else
            {
                string json = Properties.Resources.bot_options;
                var settings = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                Settings = settings;
                WriteToJson(Settings, "resources\\bot_settings");
            }
            
            if (!File.Exists($"{BasePath}\\resources\\builds.json"))
            {
                string buildsJson = Properties.Resources.builds;
                var builds = JsonConvert.DeserializeObject<Dictionary<string, string>>(buildsJson);
                WriteToJson(builds, "resources\\builds");
            }

            if (!File.Exists($"{BasePath}\\resources\\custom_commands.json"))
            {
                string customCommandsJson = Properties.Resources.custom_commands;
                var customCommands = JsonConvert.DeserializeObject<Dictionary<string, string>>(customCommandsJson);
                WriteToJson(customCommands, "resources\\custom_commands");
            }

            if (!File.Exists($"{BasePath}\\resources\\inventory_data.json"))
            {
                string inventoryDataJson = Properties.Resources.inventory_data;
                var inventoryData = JsonConvert.DeserializeObject<Dictionary<string, Playlist>>(inventoryDataJson);
                WriteToJson(inventoryData, "resources\\inventory_data");
            }

            if (!File.Exists($"{BasePath}\\resources\\playlists.json"))
            {
                string playlistsJson = Properties.Resources.playlists;
                var playlists = JsonConvert.DeserializeObject<Dictionary<string, Playlist>>(playlistsJson);
                WriteToJson(playlists, "resources\\playlists");
            }

            if (!File.Exists($"{BasePath}\\resources\\raz_pets.json"))
            {
                string razPetsJson = Properties.Resources.raz_pets;
                var razPets = JsonConvert.DeserializeObject<List<string>>(razPetsJson);
                WriteToJson(razPets, "resources\\raz_pets");
            }

            if (!File.Exists($"{BasePath}\\resources\\moderators.json"))
            {
                string moderatorsJson = Properties.Resources.moderators;
                var moderators = JsonConvert.DeserializeObject<List<string>>(moderatorsJson);
                WriteToJson(moderators, "resources\\moderators");
            }
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
}
