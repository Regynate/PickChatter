using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Client.Events;
using System.Threading;
using TwitchLib.Client.Models;

namespace PickChatter
{
    internal sealed class ChatterPicker : INotifyPropertyChanged
    {
        private static readonly ChatterPicker instance = new();
        public static ChatterPicker Instance { get => instance; }

        public class MessageChangedEventArgs : EventArgs
        {
            public string Message { get; }
            public string Color { get; }

            public MessageChangedEventArgs(string message, string color)
            {
                Message = message;
                Color = color;
            }
        }

        public class ChatterChangedEventArgs : EventArgs
        {
            public string Chatter { get; }

            public ChatterChangedEventArgs(string chatter)
            {
                Chatter = chatter;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void NotifyMessageChanged()
        {
            MessageChanged?.Invoke(this, new(LastMessage ?? "", currentChatter?.Color ?? ""));
            NotifyPropertyChanged(nameof(LastMessage));
        }

        private void NotifyChatterChanged()
        {
            ChatterChanged?.Invoke(this, new(ChatterName ?? ""));
            NotifyPropertyChanged(nameof(ChatterName));
            NotifyMessageChanged();
        }

        public event EventHandler<MessageChangedEventArgs>? MessageChanged;
        public event EventHandler<ChatterChangedEventArgs>? ChatterChanged;

        private class Message
        {
            public Message(string content, DateTime timestamp)
            {
                Content = content;
                Timestamp = timestamp;
            }

            public string Content { get; }
            public DateTime Timestamp { get; }
        }

        private class Chatter
        {
            public Chatter(string username, string displayname)
            {
                Username = username;
                DisplayName = displayname;
            }

            public Chatter(string username, ChatMessage message) : this(username, message.DisplayName)
            {
                Update(message);
            }

            private readonly List<Message> messages = new();

            public string Username { get; }
            public string DisplayName { get; private set; }
            private string? color;
            public string Color { get => color ?? "#ff7f50"; }
            public bool IsSubscriber { get; private set; }
            public int SubscriberTime { get; private set; }
            public bool IsModerator { get; private set; }
            public bool IsVIP { get; private set; }
            public bool HasMessage { get => messages.Count > 0; }
            public DateTime Timestamp { get => HasMessage ? messages.Last().Timestamp : DateTime.MinValue; }
            public string LastMessage { get => HasMessage ? messages.Last().Content : ""; }

            public void Update(ChatMessage message)
            {
                messages.Add(new Message(message.Message, DateTime.Now));
                DisplayName = message.DisplayName;
                color = message.ColorHex;
                IsSubscriber = message.IsSubscriber;
                IsModerator = message.IsModerator;
                IsVIP = message.IsVip;
                SubscriberTime = message.SubscribedMonthCount;
            }

            public int MessageCount()
            {
                return messages.Count;
            }

            public int MessageCount(TimeSpan timeLimit)
            {
                var now = DateTime.Now;
                return messages.Count - messages.FindLastIndex(m => now - m.Timestamp > timeLimit) - 1;
            }

            public bool ContainsMessage(TimeSpan timeLimit, Predicate<string> selector)
            {
                int count = MessageCount(timeLimit);
                if (count == 0)
                {
                    return false;
                }
                return messages.FindLastIndex(messages.Count - 1, count, m => selector(m.Content)) != -1;
            }
        }

        private int processedMessagesCount = 0;

        private Chatter? currentChatter = null;

        public string? ChatterName
        {
            get
            {
                if (currentChatter == null)
                {
                    return null;
                }

                if (currentChatter.DisplayName.ToLower() == currentChatter.Username)
                {
                    return currentChatter.DisplayName;
                }

                return currentChatter.DisplayName + " (" + currentChatter.Username + ")";
            }
        }

        public string? LastMessage => currentChatter?.LastMessage;

        public string StatusBarString => $"Messages: {processedMessagesCount}, Users: {chatters.Count}, Filtered: {GetFilteredChatters().Count}";

        public void ClearChatters()
        {
            chatters.Clear();
            processedMessagesCount = 0;
            currentChatter = null;
            NotifyChatterChanged();
        }

        private readonly Dictionary<string, Chatter> chatters = new();

        private ChatterPicker()
        {
            TwitchClient.Instance.MessageReceived += OnMessageReceived;
            TwitchClient.Instance.UserBanned += (_, args) => OnUserBanned(args.UserBan.Username);
            TwitchClient.Instance.UserTimedOut += (_, args) => OnUserBanned(args.UserTimeout.Username);
            Task.Run(() =>
            {
                while (true)
                {
                    NotifyPropertyChanged(nameof(StatusBarString));
                    Thread.Sleep(200);
                }
            }
            );
        }

        private void OnUserBanned(string username)
        {
            if (chatters.TryGetValue(username, out Chatter? chatter))
            {
                processedMessagesCount -= chatter.MessageCount();
                chatters.Remove(username);
            }
        }

        private bool Rule1(Chatter chatter)
        {
            if (!SettingsManager.Instance.Rule1Enabled)
            {
                return true;
            }
            return SettingsManager.Instance.Rule1MessageCount <= 
                chatter.MessageCount(TimeSpan.FromMinutes(SettingsManager.Instance.Rule1TimeLimit));
        }

        private bool Rule2(Chatter chatter)
        {
            if (!SettingsManager.Instance.Rule2Enabled)
            {
                return true;
            }

            return chatter.ContainsMessage(
                    TimeSpan.FromMinutes(SettingsManager.Instance.Rule2TimeLimit),
                    s =>
                    {
                        string sComp = SettingsManager.Instance.Rule2String;

                        if (!SettingsManager.Instance.Rule2CaseSensitive)
                        {
                            s = s.ToLower();
                            sComp = sComp.ToLower();
                        }

                        switch ((SettingsManager.StringContainType) SettingsManager.Instance.Rule2ContainType)
                        {
                            case SettingsManager.StringContainType.Equals:
                                return s.Equals(sComp);
                            case SettingsManager.StringContainType.Contains:
                                return s.Contains(sComp);
                            case SettingsManager.StringContainType.StartsWith:
                                return s.StartsWith(sComp);
                            case SettingsManager.StringContainType.EndsWith:
                                return s.EndsWith(sComp);
                            default:
                                return false;
                        }
                    });
        }
        
        private bool Rule3(Chatter chatter)
        {
            if (!SettingsManager.Instance.Rule3Enabled)
            {
                return true;
            }

            return SettingsManager.Instance.Rule3Moderator && chatter.IsModerator ||
                SettingsManager.Instance.Rule3Subscriber && chatter.IsSubscriber && 
                    chatter.SubscriberTime >= SettingsManager.Instance.Rule3SubscriberTime ||
                SettingsManager.Instance.Rule3VIP && chatter.IsVIP;
        }

        private bool Rule4(Chatter chatter)
        {
            return !SettingsManager.Instance.ExcludeUsersEnabled || 
                !SettingsManager.Instance.ExcludeUsersString
                .ToLower()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(chatter.Username);
        }

        private bool SatisfiesRules(Chatter chatter)
        {
            return chatter.HasMessage && Rule1(chatter) && Rule2(chatter) && Rule3(chatter) && Rule4(chatter);
        }

        private Dictionary<string, Chatter> GetFilteredChatters()
        {
            return new(chatters.ToList().Where(c => SatisfiesRules(c.Value)));
        }

        private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            // TODO: don't like it very much
            if (SettingsManager.Instance.ExcludeCommandsEnabled && e.ChatMessage.Message.StartsWith('!'))
            {
                return;
            }

            string username = e.ChatMessage.Username;

            if (chatters.ContainsKey(username))
            {
                chatters[username].Update(e.ChatMessage);
            }
            else 
            {
                chatters.Add(username, new Chatter(username, e.ChatMessage));
            }

            if (username == currentChatter?.Username && SettingsManager.Instance.ChatterMode == (int)SettingsManager.ChatterModeType.Chatter)
            { 
                NotifyMessageChanged();
            }

            processedMessagesCount++;
        }

        public bool PickRandomChatter()
        {
            var filtered = GetFilteredChatters();

            if (filtered.Count > 0)
            {
                PickChatter(
                    filtered
                    .Select(kvp => kvp.Value)
                    .Random()
                    .Username);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void PickChatter(string? name)
        {
            if (name == null)
            {
                currentChatter = null;
                NotifyChatterChanged();
                return;
            }

            string username = name.ToLower();

            if (chatters.ContainsKey(username))
            {
                currentChatter = chatters[username];
            }
            else
            {
                chatters.Add(username, new Chatter(username, name));
                currentChatter = chatters[username];
            }

            NotifyChatterChanged();
        }
    }
}
