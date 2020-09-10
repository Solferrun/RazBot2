using System;
using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;

namespace RazBot
{
    public class TwitchMessage
    {
        public List<KeyValuePair<string, string>> BadgeInfo { get; set; }
        public List<KeyValuePair<string, string>> Badges { get; set; }
        public int Bits { get; set; }
        public double BitsInDollars { get; set; }
        public string BotUsername { get; set; }
        public string Channel { get; set; }
        public CheerBadge CheerBadge { get; set; }
        public System.Drawing.Color Color { get; set; }
        public string ColorHex { get; set; }
        public string CustomRewardId { get; set; }
        public bool HasSongRedeem { get; set; }
        public string DisplayName { get; set; }
        public string EmoteReplacedMessage { get; set; }
        public EmoteSet EmoteSet { get; set; }
        public string Id { get; set; }
        public bool IsBroadcaster { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsMe { get; set; }
        public bool IsModerator { get; set; }
        public bool HasModPrivileges { get; set; }
        public bool IsSkippingSubMode { get; set; }
        public bool IsSubscriber { get; set; }
        public bool IsTurbo { get; set; }
        public bool IsVip { get; set; }
        public string Message { get; set; }
        public TwitchLib.Client.Enums.Noisy Noisy { get; set; }
        public string RawIrcMessage { get; set; }
        public string RoomId { get; set; }
        public int SubscribedMonthCount { get; set; }
        public string TmiSentTs { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public TwitchLib.Client.Enums.UserType UserType { get; set; }
        public bool HasInvokation { get; set; }
        public bool HasQuestion { get; set; }
        public string[] BotResponse { get; set; }
        public string[] WitResponse { get; set; }
        public string Token { get; set; }
        public string[] Args { get; set; }
        public string ArgsString { get; set; }
        public string[] Mentions { get; set; }
        public string Intent { get; set; }

        public TwitchMessage() { }



        public TwitchMessage(ChatMessage chatMessage)
        {
            Badges = chatMessage.Badges;
            Bits = chatMessage.Bits;
            BitsInDollars = chatMessage.BitsInDollars;
            BotUsername = chatMessage.BotUsername;
            Channel = chatMessage.Channel;
            CheerBadge = chatMessage.CheerBadge;
            Color = chatMessage.Color;
            ColorHex = chatMessage.ColorHex;
            CustomRewardId = chatMessage.CustomRewardId;
            HasSongRedeem = false;
            DisplayName = chatMessage.DisplayName;
            EmoteReplacedMessage = chatMessage.EmoteReplacedMessage;
            EmoteSet = chatMessage.EmoteSet;
            Id = chatMessage.Id;
            IsBroadcaster = chatMessage.IsBroadcaster;
            IsHighlighted = chatMessage.IsHighlighted;
            IsMe = chatMessage.IsMe;
            IsModerator = chatMessage.IsModerator;
            HasModPrivileges = (chatMessage.IsModerator || chatMessage.IsBroadcaster);
            IsSkippingSubMode = chatMessage.IsSkippingSubMode;
            IsSubscriber = chatMessage.IsSubscriber;
            IsTurbo = chatMessage.IsTurbo;
            IsVip = chatMessage.IsVip;
            Message = chatMessage.Message;
            Noisy = chatMessage.Noisy;
            RawIrcMessage = chatMessage.RawIrcMessage;
            RoomId = chatMessage.RoomId;
            SubscribedMonthCount = chatMessage.SubscribedMonthCount;
            TmiSentTs = chatMessage.TmiSentTs;
            UserId = chatMessage.UserId;
            Username = chatMessage.Username;
            UserType = chatMessage.UserType;
            HasInvokation = Message.StartsWith("!");
            HasQuestion = Message.Replace("?v=", "").Replace("?list=", "").Contains('?');
            Mentions = GetMentions();
            ArgsString = null;
            Intent = null;
            Token = null;
            Args = null;

            // Command invokation?
            if (HasInvokation)
            {
                ParseToken();
                var ignoredTokens = new string[] { "discord", "dl", "scrapper" };
                if (!ignoredTokens.Contains(Token))
                {
                    BotResponse = TwitchBot.Commands.TryInvoke(Token, this);
                }
                else
                {
                    BotTools.LogLine($"{DisplayName} invoked \"{Token}\" ->\n\t|| TOKEN: {Token}");
                }
            }

            // Wit sees intent?
            var noTokenResponse = BotResponse == null || BotResponse.Length == 0;
            var tokenNotHandled = Token == null || !TwitchBot.Commands.TokenExists(Token);
            var witHandleable = HasInvokation || (HasQuestion && BotTools.Settings["handle_questions"]);
            if (noTokenResponse && 
                tokenNotHandled && 
                witHandleable)
            {
                var intent = GetIntent();
                if (!String.IsNullOrEmpty(intent))
                {
                    Intent = intent;
                    WitResponse = TwitchBot.Commands.TryInvoke(Intent, this);
                }
            }

            // Music redeem?
            if (!String.IsNullOrEmpty(CustomRewardId) && CustomRewardId == BotTools.Settings["song_redeem_id"])
            {
                HasSongRedeem = true;
            }
        }

        public TwitchMessage(string invoker, string messageContent)
        {
            Badges = null;
            Bits = 0;
            BitsInDollars = 0.0D;
            BotUsername = "RazBot";
            Channel = "maerictv";
            CheerBadge = null;
            Color = System.Drawing.Color.Black;
            ColorHex = "ff000000";
            CustomRewardId = "none";
            HasSongRedeem = false;
            DisplayName = invoker;
            EmoteReplacedMessage = messageContent;
            EmoteSet = null;
            Id = "";
            IsBroadcaster = true;
            IsHighlighted = false;
            IsMe = true;
            IsModerator = false;
            HasModPrivileges = true;
            IsSkippingSubMode = false;
            IsSubscriber = true;
            IsTurbo = false;
            IsVip = false;
            Message = messageContent;
            Noisy = TwitchLib.Client.Enums.Noisy.NotSet;
            RawIrcMessage = messageContent;
            RoomId = "";
            SubscribedMonthCount = 0;
            TmiSentTs = "";
            UserId = "";
            Username = "RazBot";
            UserType = TwitchLib.Client.Enums.UserType.Broadcaster;
            HasInvokation = Message.StartsWith("!");
            HasQuestion = Message.Replace("?v=", "").Replace("?list=", "").Contains('?');
            Mentions = GetMentions();
            ArgsString = null;
            Intent = null;
            Token = null;
            Args = null;

            // Command invokation?
            if (HasInvokation)
            {
                ParseToken();
                var ignoredTokens = new string[] { "discord", "dl", "scrapper" };
                if (!ignoredTokens.Contains(Token))
                {
                    BotResponse = TwitchBot.Commands.TryInvoke(Token, this);
                }
                else
                {
                    BotTools.LogLine($"{DisplayName} invoked \"{Token}\" ->\n\t|| TOKEN: {Token}");
                }
            }

            // Wit sees intent?
            var noTokenResponse = BotResponse == null || BotResponse.Length == 0;
            var tokenNotHandled = Token == null || !TwitchBot.Commands.TokenExists(Token);
            var witHandleable = HasInvokation || (HasQuestion && BotTools.Settings["handle_questions"]);
            if (noTokenResponse &&
                tokenNotHandled &&
                witHandleable)
            {
                var intent = GetIntent();
                if (!String.IsNullOrEmpty(intent))
                {
                    Intent = intent;
                    WitResponse = TwitchBot.Commands.TryInvoke(Intent, this);
                }
            }

            // Music redeem?
            if (!String.IsNullOrEmpty(CustomRewardId) && CustomRewardId == BotTools.Settings["song_redeem_id"])
            {
                HasSongRedeem = true;
            }
        }

        private void ParseToken()
        {
            if (!String.IsNullOrEmpty(Message))
            {
                var splitIndex = Message.IndexOf(" ");
                if (splitIndex > -1)
                {
                    Token = Message.Substring(1, splitIndex - 1).ToLower();
                    ArgsString = Message.Substring(splitIndex);
                    var split = Message.Split(' ');
                    Args = split.Skip(1).ToArray<string>();
                }
                else
                {
                    Token = Message.Substring(1, Message.Length - 1).ToLower();
                }
            }
        }

        private string GetIntent()
        {
            string description = "";
            double confidence = 0;
            if (!String.IsNullOrEmpty(Message))
            {
                var intent = Wit.GetIntent(Message.ToLower());
                if (intent.Count() > 0)
                {
                    description = intent["description"];
                    confidence = intent["confidence"];
                }
            }

            if (!String.IsNullOrEmpty(description) && confidence >= BotTools.Settings["intent_thresshold"])
            {
                return description;
            }
            else
            {
                BotTools.NewUtterances.Add(Message.ToLower());
                return null;
            }
        }

        private string[] GetMentions()
        {
            var split = Message.Split(' ');
            var rawMentions = split.Where(w => w.StartsWith("@") && w.Length > 1);
            var mentionNames = rawMentions.Select(w => w.Substring(1, w.Length - 1));
            return mentionNames.ToArray<string>();
        }
    }
}
