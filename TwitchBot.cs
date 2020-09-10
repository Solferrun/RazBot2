using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Api;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using System.Net;
using System.IO;
using TwitchLib.Client.Extensions;
using TwitchLib.Api.Helix.Models.Moderation.CheckAutoModStatus;
using AngleSharp.Common;

namespace RazBot
{
    public enum StreamState
    {
        Online       = 0,
        Offline       = 1,
        OnlineHosting  = 6,
        OfflineHosting  = 9,
        OnlineNotHosting = 10,
        OfflineNotHosting = 11
    }

    public static class TwitchBot
    {
        public static TwitchCommands Commands { get; set; }
        public static TwitchClient Client { get; set; }
        public static TwitchPubSub PubSub { get; set; }
        public static TwitchAPI API { get; set; }
        public static bool Connecting { get; set; } = false;
        private static ConnectionCredentials Credentials { get; set; }
        private static string CurrentChannel { get; set; }
        public static StreamState StreamStatus { get; set; } = StreamState.OfflineNotHosting;

        static TwitchBot()
        {
            Client = NewTwitchConnection();
            Commands = new TwitchCommands();
            PubSub = new TwitchPubSub();
        }

        public static void Start()
        {
            Client.Initialize(Credentials);

            Client.OnConnected += Client_OnConnected;
            Client.OnDisconnected += Client_OnDisconnected;
            Client.OnJoinedChannel += Client_OnJoinedChannel;
            Client.OnLeftChannel += Client_OnLeftChannel;

            Client.OnMessageReceived += Client_OnMessageReceived;
            Client.OnWhisperReceived += Client_OnWhisperReceived;
            Client.OnModeratorsReceived += Client_OnModeratorsReceived;

            PubSub.OnPubSubServiceConnected += Pubsub_OnPubSubServiceConnected;
            PubSub.OnListenResponse += PubSub_OnListenResponse;
            PubSub.OnStreamUp += PubSub_OnStreamUp;
            PubSub.OnStreamDown += PubSub_OnStreamDown;
            PubSub.ListenToVideoPlayback("maerictv");

            Client.OnHostingStarted += Client_OnHostingStarted;
            Client.OnHostingStopped += Client_OnHostingStopped;
            Client.OnBeingHosted += Client_OnBeingHosted;
            Client.OnRaidNotification += Client_OnRaidNotification;

            Client.OnNewSubscriber += Client_OnNewSubscriber;
            Client.OnReSubscriber += Client_OnReSubscriber;
            Client.OnCommunitySubscription += Client_OnCommunitySubscription;
            Client.OnGiftedSubscription += Client_OnGiftedSubscription;

            for (int i = 0; i < 99; i++)
            {
                try
                {
                    Client.Connect();
                    PubSub.Connect();
                }
                catch (Exception e)
                {
                    BotTools.LogToSessionLogFile(e.ToString());
                }
            }

            StreamStatus = GetStreamOnline() ? StreamState.Online : StreamState.Offline;
        }

        private static TwitchClient NewTwitchConnection()
        {
            Credentials = new ConnectionCredentials("razbot", BotTools.Settings["twitch_token"]);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            return new TwitchClient(customClient);
        }

        public static void LeaveChannel()
        {
            Client.LeaveChannel(CurrentChannel.ToLower());
        }

        public static void JoinChannel(string channelToJoin)
        {
            if (!Client.IsConnected)
            {
                Client = NewTwitchConnection();
            }
            CurrentChannel = channelToJoin;
            Client.JoinChannel(channelToJoin.ToLower());
        }


        public static void SendMessage(string message)
        {
            if (Client.IsConnected)
            {
                if (BotTools.Settings["debug_output"] == true)
                {
                    BotTools.LogLine($"[T] RazBot: {message}");
                }
                else
                {
                    Client.SendMessage(CurrentChannel, $"/me ~ {message}");
                }
            }
        }

        ////////////
        // EVENTS //
        ////////////

        private static void Pubsub_OnPubSubServiceConnected(object sender, System.EventArgs e)
        {
            PubSub.SendTopics();
        }

        private static void PubSub_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
                BotTools.LogLine($"Failed to listen! Response: {e.Response.Error}");
        }

        private static void PubSub_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            StreamStatus = (int)StreamStatus % 3 == 0 ? StreamState.OnlineHosting : StreamState.OnlineNotHosting;
            BotTools.LogLine("Stream went Online");
        }

        private static void PubSub_OnStreamDown(object sender, OnStreamDownArgs e)
        {
            StreamStatus = (int)StreamStatus % 3 == 0 ? StreamState.OfflineHosting : StreamState.OfflineNotHosting;
            BotTools.LogLine("Stream went Offline");
            Music.ClearRequests();
        }

        private static void Client_OnHostingStarted(object sender, OnHostingStartedArgs e)
        {
            StreamStatus = (int)StreamStatus % 2 == 0 ? StreamState.OnlineHosting : StreamState.OfflineHosting;
            BotTools.LogLine($"Host started: {e.HostingStarted.TargetChannel}");
        }

        private static void Client_OnHostingStopped(object sender, OnHostingStoppedArgs e)
        {
            StreamStatus = (int)StreamStatus % 2 == 0 ? StreamState.OnlineNotHosting : StreamState.OfflineNotHosting;
            BotTools.LogLine($"Host stopped: {e.HostingStopped.HostingChannel}");
        }

        private static void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            string hostWord = e.BeingHostedNotification.IsAutoHosted ? "Auto-hosted" : "Hosted";
            BotTools.LogLine($"{hostWord} by: {e.BeingHostedNotification.HostedByChannel} with {e.BeingHostedNotification.Viewers} viewers");
        }

        private static void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            BotTools.LogLine($"Raid from: {e.RaidNotification.DisplayName}, Viewers: {e.RaidNotification.MsgParamViewerCount}, MsgParamDisplayName:{e.RaidNotification.MsgParamDisplayName}");
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            BotTools.LogLine("RazBot is online");
        }

        private static void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            BotTools.LogLine("RazBot lost connection to Twitch, reconnecting...");
            Client = NewTwitchConnection();
            Client.JoinChannel(CurrentChannel.ToLower());
        }

        private static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            BotTools.LogLine($"RazBot is connected to Twitch [{e.Channel}]");
            Client.GetChannelModerators(Client.GetJoinedChannel(e.Channel));

        }

        private static void Client_OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            string onlineMessage = $"RazBot has disconnected from Twitch [{e.Channel}]";
            BotTools.LogLine($"RazBot: {onlineMessage}");
        }

        private static void Client_OnModeratorsReceived(object sender, OnModeratorsReceivedArgs e)
        {
            var mods = String.Join(", ", e.Moderators);
            BotTools.LogLine($"Channel Moderators: {mods}");
            BotTools.WriteToJson(e.Moderators, "resources\\moderators");
        }

        private static void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Username != Client.TwitchUsername)
            {
                var message = new TwitchMessage(e.ChatMessage);
                e.ChatMessage = null;
				
                // Respond to commands/intent
                if (message.BotResponse != null && message.BotResponse.Length > 0)
                {
                    foreach (string line in message.BotResponse)
                    {
                        TwitchBot.SendMessage(line);
                    }
                }
                else if (message.WitResponse != null && message.WitResponse.Length > 0)
                {
                    foreach (string line in message.WitResponse)
                    {
                        TwitchBot.SendMessage(line);
                    }
                }

                // Fill music requests
                if (message.HasSongRedeem)
                {
                    SendMessage(Music.FillSongRequest(message.Message.Replace(" ", ""), message.DisplayName));
                }
            }
        }

        private static void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == CurrentChannel || BotTools.ReadFromJson<List<string>>("resources\\moderators").Contains(e.WhisperMessage.Username))
            {
                BotTools.LogLine($"○ {e.WhisperMessage.DisplayName}: {e.WhisperMessage.Message.Trim()}");
            }
        }

        private static void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            {
                BotTools.LogLine($"{e.Subscriber.DisplayName} subscribed with Twitch Prime");
            }
            else if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier1)
            {
                BotTools.LogLine($"{e.Subscriber.DisplayName} subscribed at Tier 1");
            }
            else if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier2)
            {
                BotTools.LogLine($"{e.Subscriber.DisplayName} subscribed at Tier 2");
            }
            else if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Tier3)
            {
                BotTools.LogLine($"{e.Subscriber.DisplayName} subscribed at Tier 3");
            }
            else
            {
                BotTools.LogLine($"{e.Subscriber.DisplayName} subscribed");
            }
        }

        private static void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            int monthCount = e.ReSubscriber.Months;
            string monthWord = monthCount > 1 ? "months" : "month";
            BotTools.LogLine($"{e.ReSubscriber.DisplayName} subscribed ({monthCount} {monthWord})");
        }

        private static void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            string giftee = e.GiftedSubscription.MsgParamRecipientDisplayName;
            string gifter = e.GiftedSubscription.DisplayName;
            BotTools.LogLine($"{gifter} gifted a sub to {giftee}");
        }

        private static void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            int giftAmount = e.GiftedSubscription.MsgParamMassGiftCount;
            string subWords = giftAmount > 1 ? $"{giftAmount} subscriptions" : "a subscription";

            BotTools.LogLine($"{e.GiftedSubscription.DisplayName} gifted {subWords} " +
						     $"({e.GiftedSubscription.MsgParamSenderCount} total)");
        }

        private static bool GetStreamOnline()
        {
            for (int i = 20; i > 0; i--)
            {
                string url = "https://decapi.me/twitch/uptime/maerictv";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    if (result.Contains("offline"))
                    {
                        BotTools.LogLine($"Stream reported as offline");
                        return false;
                    }
                    else if (result.Contains("seconds") || result.Contains("minutes") || result.Contains("hours"))
                    {
                        BotTools.LogLine($"Stream reported as online");
                        return true;
                    }
                }
                BotTools.LogLine($"Failed to fetch stream status. Retrying {i} more times...");
            }
            BotTools.LogLine($"Failed to fetch stream status. Treating stream as offline.");
            return false;
        }
    }
}

