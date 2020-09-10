using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;

namespace RazBot
{
    public static class DiscordBot
    {
        private static DiscordWebhookClient RazChannelWebhook { get; set; }
        private static DiscordWebhookClient DJChannelWebhook { get; set; }
        private static DiscordWebhookClient DJVettingChannelWebhook { get; set; }
        private static Dictionary<ulong, SongData> RequestMap { get; set; } = new Dictionary<ulong, SongData>();
        private static bool RequestPosting { get; set; } = false;

        private static DiscordSocketClient Client { get; set; }

        static DiscordBot()
        {
            Client = new DiscordSocketClient();
            RazChannelWebhook = new DiscordWebhookClient(BotTools.Settings["raz_channel_webhook"]);
            DJChannelWebhook = new DiscordWebhookClient(BotTools.Settings["dj_channel_webhook"]);
            DJVettingChannelWebhook = new DiscordWebhookClient(BotTools.Settings["dj_vetting_webhook"]);
        }

        public static void Start()
        {
            Client.Ready += Client_OnReady;
            Client.ReactionAdded += Client_OnReactionAdded;
            Client.MessageReceived += Client_OnMessageReceived;

            Task.Run(async () => {
                await Client.LoginAsync(TokenType.Bot, BotTools.Settings["discord_token"]);
            });
            Task.Run(async () => {
                await Client.StartAsync();
            });
        }

        public static void PostToPetsChannel(Dictionary <string, dynamic> post)
        {
            if (!BotTools.Settings["debug_output"])
            {
                var builder = new EmbedBuilder
                {
                    Title = "RAZ PETS!",
                    Description = post["caption"],
                    Color = Color.Purple,
                    ImageUrl = post["media_url"],
                    Url = post["permalink"]
                };
                builder.AddField("Instagram", post["permalink"]);
                RazChannelWebhook.SendMessageAsync(text: "", embeds: new[] { builder.Build() });
            }
            else
            {
                BotTools.LogLine($"[D] RazBot: NEW RAZ PETS! -> {post["permalink"]}");
            }
        }

        public static void PostToDJChannel(string postContent)
        {
            if (BotTools.Settings["announce_songs"] && !BotTools.Settings["debug_output"])
            {
                DJChannelWebhook.SendMessageAsync(text: postContent);
            }
            else
            {
                BotTools.LogLine("[D] RazBot: " + postContent);
            }
        }

        public static async void PostVetRequestToDJChannel(SongData songData)
        {
            RequestPosting = true;
            string messageContent = $"**Request from {songData.Requestor}:**\n{songData.URL}";
            ulong messageID = await DJVettingChannelWebhook.SendMessageAsync(text: messageContent);
            RequestMap[messageID] = songData;
            RequestPosting = false;
        }

        private static Task Client_OnReady()
        {
            BotTools.LogLine($"{Client.CurrentUser} is connected to Discord [{String.Join(",", Client.Guilds)}]");

            return Task.CompletedTask;
        }

        private static async Task Client_OnMessageReceived(SocketMessage message)
        {
            var user = message.Author as SocketGuildUser;
            var requiredRole = (user as IGuildUser).Guild.Roles.FirstOrDefault(x => x.Name == "~Moderator~");
            if (user.Roles.Contains(requiredRole))
            {
                var messageSend = new TwitchMessage(message.Author.Username, message.Content);
                if (messageSend.BotResponse != null && messageSend.BotResponse.Length > 0)
                {
                    if (!BotTools.Settings["debug_output"])
                    {
                        await message.Channel.SendMessageAsync(String.Join("\n", messageSend.BotResponse));
                    }
                    else
                    {
                        BotTools.LogLine("[D] RazBot: " + String.Join("\n", messageSend.BotResponse));
                    }
                }
                else if (messageSend.WitResponse != null && messageSend.WitResponse.Length > 0)
                {
                    if (!BotTools.Settings["debug_output"])
                    {
                        await message.Channel.SendMessageAsync(String.Join("\n", messageSend.WitResponse));
                    }
                    else
                    {
                        BotTools.LogLine("[D] RazBot: " + String.Join("\n", messageSend.WitResponse));
                    }
                }
            }

            while (RequestPosting) ;
            if (RequestMap.ContainsKey(message.Id))
            {
                foreach (var emoji in new[] { "👍", "👎" }) // "✅" "▶️" // "👍" "👎" // "⛔" "⏏️"
                {
                    await message.AddReactionAsync(new Emoji(emoji));
                }
            }
        }

        private static async Task Client_OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await cache.GetOrDownloadAsync();
            
            if (!reaction.User.Value.IsBot && RequestMap.ContainsKey(message.Id))
            {
                SongData songData = RequestMap[message.Id];
                if (reaction.Emote.Name == "👍")
                {
                    BotTools.LogLine($"{songData.Requestor}'s request was approved by {reaction.User.Value.Username}");
                    await DJVettingChannelWebhook.SendMessageAsync($"*{songData.Requestor}'s request was approved by {reaction.User.Value.Username}*");
                    RequestMap.Remove(message.Id);
                    try
                    {
                        Music.GetPlaylist("request").AddSong(songData);
                        var placement = Music.GetPlaylist("request").Songs.Count;
                        var placementWord = BotTools.GetPlacementWord(placement);
                        TwitchBot.SendMessage($"@{songData.Requestor}, Your request is {placementWord} in the queue! razCool");
                        Music.QueuedSong = Music.DownloadedSong = null; 
                        Music.SavePlaylists();
                        await message.RemoveAllReactionsAsync();
                    }
                    catch (ArgumentException)
                    {
                        BotTools.LogLine($"{songData.Requestor}'s request rejected: Already in queue");
                        TwitchBot.SendMessage($"Sorry @{songData.Requestor}, that request is already queued!");
                    }
                    catch (FormatException)
                    {
                        BotTools.LogLine($"{songData.Requestor}'s request rejected: Bad url");
                        TwitchBot.SendMessage($"Sorry @{songData.Requestor}, I can't access any video with that url!");
                    }
                }
                else if (reaction.Emote.Name == "👎")
                {
                    BotTools.LogLine($"{RequestMap[message.Id].Requestor}'s request was declined by {reaction.User.Value.Username}");
                    await DJVettingChannelWebhook.SendMessageAsync($"*{RequestMap[message.Id].Requestor}'s request was declined by {reaction.User.Value.Username}*");
                    TwitchBot.SendMessage($"@{songData.Requestor}, Your request was declined! razHands");
                    RequestMap.Remove(message.Id);
                    await message.RemoveAllReactionsAsync();
                }
            }
        }
    }
}
