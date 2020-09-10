using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RazBot
{
    public class TwitchCommands
    {
        public MainWindow MainWindow { get; set; }

        public Dictionary<string, Func<TwitchMessage, string[]>> Map { get; }
        public Dictionary<string, string> CustomMap { get; set; }
        private Dictionary<string, DateTime> LastCall { get; set; }

        public TwitchCommands()
        {
            Map = new Dictionary<string, Func<TwitchMessage, string[]>>()
            {
                ["add"] = AddCommand,
                ["build"] = GetBuild,
                ["cauliflower"] = GetCauliflower,
                ["choose"] = GetChoice,
                ["commands"] = GetCommands,
                ["edit"] = EditCommand,
                ["fortune"] = GetFortune,
                ["hello"] = GetGreeting,
                ["icecream"] = GetIceCream,
                ["kp"] = GetKP,
                ["owo"] = GetOwo,
                ["pets"] = GetPets,
                ["playlist"] = GetPlaylist,
                ["random"] = GetRandom,
                ["remove"] = RemoveCommand,
                ["shoutout"] = Shoutout,
                ["skip"] = SkipSong,
                ["song"] = GetSong,
                ["songrequest"] = RequestSong,
                ["time"] = GetTime,
                ["uptime"] = GetUptime,
                ["volume"] = AdjustVolume
            };

            LastCall = new Dictionary<string, DateTime>();
            CustomMap = LoadCustomCommands();
        }

        public string[] TryInvoke(string token, TwitchMessage message)
        {
            if (Map.ContainsKey(token))
            {
                string invokeReport = $"{message.DisplayName} invoked \"{token}\" ->";
                if (message.HasInvokation) invokeReport += $"\n\t|| TOKEN: {message.Token}";
                if (message.HasQuestion) invokeReport += $"\n\t|| QUESTION: {message.Message}";
                if (message.Intent != null) invokeReport += $"\n\t|| INTENT: {message.Intent}";
                if (message.Args != null && message.Args.Length > 0) invokeReport += $"\n\t|| ARGS: [{String.Join(", ", message.Args)}]";
                BotTools.LogLine(invokeReport);
                return Map[token](message);
            }
            else if (TwitchBot.Commands.CustomMap.ContainsKey(token))
            {
                string invokeReport = $"{message.DisplayName} invoked \"{token}\" ->";
                if (message.HasInvokation) invokeReport += $"\n\t|| TOKEN: {message.Token}";
                if (message.HasQuestion) invokeReport += $"\n\t|| QUESTION: {message.Message}";
                if (message.Intent != null) invokeReport += $"\n\t|| INTENT: {message.Intent}";
                if (message.Args != null && message.Args.Length > 0) invokeReport += $"\n\t|| ARGS: [{String.Join(", ", message.Args)}]";
                BotTools.LogLine(invokeReport);
                return new string[] { TwitchBot.Commands.CustomMap[token].Replace("{user}", message.DisplayName) };
            }
            else
            {
                return null;
            }
        }

        private bool CooldownDone(string token, int cooldown = 1)
        {
            if (LastCall.ContainsKey(token))
            {
                int cooldownTimeRemaining = cooldown - (int)(DateTime.Now - LastCall[token]).TotalSeconds;
                if (cooldownTimeRemaining <= 0)
                {
                    LastCall[token] = DateTime.Now;
                    return true;
                }
                else
                {
                    BotTools.LogLine($"Invocation of \"{token}\" was blocked: {cooldownTimeRemaining}s cooldown remaining");
                    return false;
                }
            }
            else
            {
                LastCall[token] = DateTime.Now;
                return true;
            }
        }

        public void SaveCustomCommands()
        {
            BotTools.WriteToJson(CustomMap, "resources\\custom_commands");
        }

        public bool TokenExists(string token)
        {
            return Map.ContainsKey(token) || CustomMap.ContainsKey(token);
        }

        private Dictionary<string, string> LoadCustomCommands()
        {
            if (File.Exists($"{BotTools.BasePath}\\resources\\custom_commands.json"))
            {
                var functionDict = BotTools.ReadFromJson<Dictionary<string, string>>("resources\\custom_commands");
                return functionDict;
            }
            else
            {
                return new Dictionary<string, string>();
            }
        }

        private string[] AddCommand(TwitchMessage message)
        {
            if (message.HasModPrivileges)
            {
                if (message.Args != null && message.Args.Count() > 1)
                {
                    string newToken = message.Args[0].ToLower();
                    if (Char.IsLetter(newToken[0]) && newToken.All(c => Char.IsLetterOrDigit(c) || c == '_'))
                    {
                        string commandBody = String.Join(" ", message.Args.Skip(1));
                        var intentList = Wit.ListIntents();
                        if (!intentList.Contains(newToken))
                        {
                            // Save local
                            CustomMap[newToken] = commandBody;
                            SaveCustomCommands();
                            // Teach to wit
                            Wit.AddIntent(newToken);
                            Wit.TrainUtterance($"!{newToken}", newToken);
                            return new string[] { $"Added command: !{newToken}" };
                        }
                        else
                        {
                            return new string[] { $"Command already exists: !{newToken}" };
                        }
                    }
                    else
                    {
                        return new string[] { $"Command tokens should start with a letter, and contain only letters numbers or underscores." };
                    }
                }
                else
                {
                    return new string[] { "Add command format: !add mycommand This is what my command returns!" };
                }
            }
            else
            {
                return null;
            }
        }

        private string[] RemoveCommand(TwitchMessage message)
        {
            if (message.HasModPrivileges && !string.IsNullOrEmpty(message.ArgsString))
            {
                string targetToken = message.ArgsString;
                if (TwitchBot.Commands.CustomMap.Keys.Contains(targetToken))
                {
                    TwitchBot.Commands.CustomMap.Remove(targetToken);
                    TwitchBot.Commands.SaveCustomCommands();
                    Wit.DeleteIntent(targetToken);
                    return new string[] { $"Removed command: {targetToken}" };
                }
            }
            return null;
        }

        private string[] EditCommand(TwitchMessage message)
        {
            if (message.HasModPrivileges)
            {
                if (message.Args != null && message.Args.Count() > 1)
                {
                    string refToken = message.Args[0].ToLower();
                    string commandBody = String.Join(" ", message.Args.Skip(1));
                    if (CustomMap.ContainsKey(refToken))
                    {
                        // Save local
                        CustomMap[refToken] = commandBody;
                        SaveCustomCommands();
                        return new string[] { $"Command updated: !{refToken}" };
                    }
                    else
                    {
                        return new string[] { $"Custom command doesn't exist: !{refToken}" };
                    }
                }
                else
                {
                    return new string[] { "Edit command format: !edit mycommand This is what my command returns!" };
                }
            }
            else
            {
                return null;
            }
        }

        private string[] GetBuild(TwitchMessage message)
        {
            if (File.Exists($"{BotTools.BasePath}\\resources\\builds.json"))
            {
                string characterName = GuildWars.GetMostRecentCharacter();
                var builds = BotTools.ReadFromJson<Dictionary<string, string>>("resources\\builds");
                if (builds.ContainsKey(characterName))
                {
                    return new string[] { $"{characterName}'s build: {builds[characterName]}" };
                }
                else
                {
                    return new string[] { $"No build data for {characterName}" };
                }
            }
            else
            {
                return new string[] { "I don't know about any builds yet!" };
            }
        }

        private string[] GetCauliflower(TwitchMessage message)
        {
            if (message.Message.Contains("eu"))
            {
                return new string[] { $"[EU] Maeric's Cauliflower Score: {GuildWars.GetCauliflowerCount("eu")}" };
            }
            else
            {
                return new string[] { $"[NA] Maeric's Cauliflower Score: {GuildWars.GetCauliflowerCount("na")}" };
            }
        }

        private string[] GetChoice(TwitchMessage message)
        {
            var input = message.ArgsString.Replace(", or ", ", ");
            input = input.Replace(" or ", ", ");
            var choices = input.Split(',');
            Random rdm = new Random();
            var choice = choices[rdm.Next(choices.Length)].Trim();
            return new string[] { $"I choose {choice}!" };
        }

        private string[] GetCommands(TwitchMessage message)
        {
            string lowerMessage = message.Message.ToLower();

            if (lowerMessage.Contains("custom") || lowerMessage.Contains("special"))
            {
                int charLimit = 500;
                var result = new List<string>();
                string line = $"Special Commands: {CustomMap.Keys.First()}";

                foreach (string key in CustomMap.Keys.Skip(1))
                {
                    if (line.Length + key.Length + 7 > charLimit)
                    {
                        result.Add(line);
                        line = key;
                    }
                    else
                    {
                        line += $", {key}";
                    }
                }

                if (line.Length > 0)
                {
                    result.Add(line);
                }

                return result.ToArray();
            }
            else
            {
                return new string[] { "https://sites.google.com/view/razbot/ -- For a list of special commands, use !special" };
            }
        }

        private string[] GetFortune(TwitchMessage message)
        {
            var fortuneResponses = new List<string>()
            {
                "It is certain.", "It is decidedly so.", "Without a doubt.", "Yes - definitely.", "You may rely on it.",
                "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again.",
                "Ask again later.", "Better not to tell you now.", "Cannot predict now.", "Concentrate and ask again.",
                "Don\"t count on it.", "My reply is no.", "My sources say no.", "Outlook not so good.", "Very doubtful."
            };
            string response = fortuneResponses[new Random().Next(fortuneResponses.Count)];
            return new string[] { response };
        }
        private string[] GetGreeting(TwitchMessage message) { return new string[] { $"Hello, {message.DisplayName}!" }; }

        private string[] GetIceCream(TwitchMessage message)
        {
            var flavors = new List<string>
            {
                "Chocolate", "Vanilla", "Strawberry", "Chocolate Chip", "Mint Chocolate Chip", "Caramel Fudge", "Coffee-Flavored", "Peanut Butter & Banana",
                "Butter Pecan", "Rocky Road", "Cherry Garcia", "Avocado", "Pistachio", "Taco-Flavored", "Pineapple Pizza-Flavored", "French Vanilla",
                "Cookie Dough", "Cookies and Cream", "Chunky Monkey", "Orange Cream", "Cinnamon-Raisin"
            };

            var containers = new List<string>
            {
                "on a Cone", "in a Bowl", "on a Stick", "in a Bucket", "Over Rice", "in a Cup", "on the Floor", "in a 1-Gallon Carton", "in a Bag", 
                "in a Box", "on a Giant Cookie"
            };

            Random rand = new Random();
            var icecream = $"{flavors[rand.Next(flavors.Count)]} Ice Cream {containers[rand.Next(containers.Count)]}";
            
            if (message.Mentions != null && message.Mentions.Length > 0)
            {
                return new string[] { $"{message.Mentions[0]} got a {icecream} from {message.DisplayName}!" };
            }
            else
            {
                return new string[] { $"{message.DisplayName} got a {icecream}!" };
            }
        }

        private string[] GetKP(TwitchMessage message)
        {
            if(CooldownDone("kp", 5))
            {
                if (message.Message.Contains("eu"))
                {
                    return new string[] { $"[EU] {GuildWars.CountKP("eu")}" };
                }
                else
                {
                    return new string[] { $"[NA] {GuildWars.CountKP("na")}" };
                }
            }
            else
            {
                return null;
            }
        }

        private string[] GetOwo(TwitchMessage message)
        {
            try
            {
                var subs = new Dictionary<string, string>
                {
                    ["this"] = "dis",
                    ["fuck"] = "fweek",
                    ["ck"] = "cz",
                    ["shit"] = "pewps",
                    ["damn"] = "pewpin",
                    ["penis"] = "pp",
                    ["who'd"] = "who would",
                    ["what'd"] = "what did",
                    ["where'd"] = "where did",
                    ["when'd"] = "when did",
                    ["why'd"] = "why did",
                    ["ain't"] = "isn't",
                    ["isn't"] = "innint",
                    ["weren't"] = "wuwwent",
                    ["wasn't"] = "wunnent",
                    ["wouldn't"] = "woonent",
                    ["how'd"] = "how did",
                    ["can't"] = "can not",
                    ["au"] = "awu",
                    ["oy"] = "owoy",
                    ["r"] = "w",
                    ["l"] = "w",
                    ["kn"] = "n",
                    ["sch"] = "skw",
                    ["ove"] = "uv",
                    ["?"] = "?? ʘwʘ",
                    ["!"] = "! ✧w✧",
                    ["<3"] = "♥w♥",
                    ["oo"] = "oow",
                    ["you"] = "yoo",
                    ["have"] = "hab",
                    ["with"] = "wif",
                    ["who"] = "hoo",
                    ["what"] = "wat",
                    ["where"] = "wheow",
                    ["when"] = "wen",
                    ["why"] = "wy",
                    ["would"] = "woowd",
                    ["'ve"] = " have",
                    ["n't"] = " not"
                };

                var output = message.ArgsString.ToLower();
                foreach (var sub in subs)
                {
                    output = output.Replace(sub.Key, sub.Value);
                }

                output = Regex.Replace(output, @"(?<=\s|^)heww(?=\s|$)", "hecz");
                output = Regex.Replace(output, "(?<=o)([aei])", "w$1");
                output = Regex.Replace(output, "(?<=u)([aeiou])", "w$1");
                output = Regex.Replace(output, "(?<= )the ", "da ");
                output = Regex.Replace(output, "(.)th([aeu ].)", "$1d$2");
                output = Regex.Replace(output, "(.)th([^aeu ].)", "$1t$2");
                output = Regex.Replace(output, "(.)o([^o])", "$1ow$2");
                output = Regex.Replace(output, @"(\w)'we", "$1 awe");
                output = Regex.Replace(output, @"(\w)'d", "$1 woowd");
                output = output.Replace("iw", "iow");
                output = output.Replace("v", "b");
                output = output.Replace("owu", "ow");

                return new string[] { output };
            }
            catch
            {
                return new string[] { };
            }
        }

        private string[] GetPets(TwitchMessage message)
        {
            var pastPets = BotTools.ReadFromJson<List<string>>("resources\\raz_pets");
            return new string[] { $"Random Raz pets! -> {pastPets[new Random().Next(pastPets.Count)]}" };
        }

        private string[] Shoutout(TwitchMessage message)
        {
            if (message.Mentions != null && message.Mentions.Length > 0)
            {
                var lastPlaying = WebTools.GetTwitchEndpointResponse($"game/{message.Mentions[0]}");
                return new string[] { $"Check out {message.Mentions[0]}'s channel, and give them a follow!" +
                    $"They were last seen playing {lastPlaying} at https://www.twitch.tv/{message.Mentions[0]}" };
            }
            else if (message.Args != null && message.Args.Length > 0)
            {
                var lastPlaying = WebTools.GetTwitchEndpointResponse($"game/{message.Args[0]}");
                return new string[] { $"Check out {message.Args[0]}'s channel, and give them a follow! " +
                    $"They were last seen playing {lastPlaying} at https://www.twitch.tv/{message.Args[0]}" };
            }
            return new string[] { "I didn\'t see any mentions, so I\'ll just shoutout myself! RazBot2.0 razCool" };
        }

        private string[] SkipSong(TwitchMessage message)
        {
            if (message.HasModPrivileges && CooldownDone("skip", 2))
            {
                MainWindow.SkipSong();
                BotTools.LogLine($"Song skipped by {message.DisplayName}");
                return new string[] { };
            }
            else
            {
                return null;
            }
        }

        private string[] GetSong(TwitchMessage message)
        {
            if (Music.LoadedSong != null && CooldownDone("song", 25))
            {
                string playing = Music.LoadedSong.Requestor == "RazBot" ? "Playing" : $"Playing {Music.LoadedSong.Requestor}'s Request";
                return new string[] { $"{playing}: {Music.LoadedSong.Title} -> {Music.LoadedSong.URL}" };
            }
            else
            {
                return null;
            }
        }

        private string LastPlaylistResult { get; set; }
        private string[] GetPlaylist(TwitchMessage message)
        {
            if (CooldownDone("playlist", 300))
            {
                if (BotTools.Settings["loaded_playlist"] != null && Music.Playlists[BotTools.Settings["loaded_playlist"]].Songs.Count > 0)
                {
                    string line = String.Join("\n", Music.Playlists[Music.LoadedPlaylist].Songs.Select((s, i) => $"{s.URL} || {s.Title}"));
                    
                    if (line.Length <= 500)
                    {
                        LastPlaylistResult = line;
                        return new string[] { line };
                    }    
                    else
                    {
                        LastPlaylistResult = $"Current playlist: {Pastebin.MakePasteFile(BotTools.Settings["loaded_playlist"], line)}";
                        return new[] { LastPlaylistResult };
                    }
                }
                else
                {
                    return new string[] { "There are no songs in the playlist! razHands" };
                }
            }
            else
            {
                return new[] { LastPlaylistResult };
            }
        }

        private string[] GetRandom(TwitchMessage message)
        {
            try
            {
                var start = Int32.Parse(message.Args[0]);
                var end = Int32.Parse(message.Args[1]) + 1;
                var choice = new Random().Next(start, end);
                return new string[] { $"I choose {choice}!" };
            }
            catch (Exception)
            {
                return new string[] { "Format example: !random 1 100" };
            }
        }

        private string[] GetRequestQueue(TwitchMessage _1)
        {
            if (CooldownDone("songrequest", 30))
            {
                var requestList = Music.Playlists["request"].Songs.Select(s => s.Title).ToList();
                if (Music.QueuedSong != null && Music.QueuedSong.IsRequest)
                {
                    requestList.Insert(0, Music.QueuedSong.Title);
                }
                if (Music.DownloadedSong != null && Music.DownloadedSong.IsRequest)
                {
                    requestList.Insert(0, Music.DownloadedSong.Title);
                }

                if (requestList.Count > 0)
                {
                    var response = new List<string>();

                    string allTitles = String.Join(", \n", requestList.Select((s, i) => $"[{i + 1}] {s}"));
                    string line = "Requests: ";

                    foreach (string song in allTitles.Split('\n'))
                    {
                        if (line.Length + song.Length > 500)
                        {
                            response.Add(line);
                            line = song;
                        }
                        else
                        {
                            line += song;
                        }
                    }

                    response.Add(line);
                    return response.ToArray<string>();
                }
                else
                {
                    return new string[] { "Requests: None!" };
                }
            }
            else
            {
                return null; //TODO: Maybe pastebin link?
            }
        }

        private string[] RequestSong(TwitchMessage message)
        {
            if (message.HasModPrivileges && message.Args != null)
            {
                string response = Music.FillSongRequest(message.ArgsString.Replace(" ", ""), message.DisplayName, bypassVet: !message.HasModPrivileges);
                if (response != null)
                {
                    return new string[] { response };
                }
                else
                {
                    return new string[] { };
                }
            }
            else
            {
                return GetRequestQueue(message);
            }
        }

        private string[] GetTime(TwitchMessage message)
        {
            if (CooldownDone("time", 10))
            {
                return new string[] { $"It's currently {DateTime.Now.ToShortTimeString()} EST for Maeric" };
            }
            else
            {
                return null;
            }
        }

        private string[] GetUptime(TwitchMessage message) 
        {
            return new string[] { "Uptime: " + WebTools.GetTwitchEndpointResponse(endpoint: "uptime/maerictv") };
        }

        private string[] AdjustVolume(TwitchMessage message)
        {
            if (message.HasModPrivileges)
            {
                try
                {
                    int newVolume = 0;
                    string argsString = message.ArgsString.Replace(" ", "");
                    if (argsString.Contains("+") || argsString.Contains("-"))
                    {
                        newVolume = MainWindow.AdjustVolume(Int32.Parse(argsString));
                    }
                    else
                    {
                        newVolume = MainWindow.SetVolume(Int32.Parse(argsString));
                    }
                    return new string[] { $"Current Volume: {newVolume}" };
                }
                catch (Exception)
                {
                    return new string[] { $"Current Volume: {MainWindow.AdjustVolume()}" };
                }
            }
            else
            {
                return null;
            }
        }
    }
}
