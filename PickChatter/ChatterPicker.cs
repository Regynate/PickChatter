using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Client.Events;
using System.Threading;

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

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(LastMessage))
            {
                MessageChanged?.Invoke(this, new(LastMessage ?? "", currentChatter?.Color ?? ""));
            }
            if (name == nameof(ChatterName))
            {
                ChatterChanged?.Invoke(this, new(ChatterName ?? ""));
            }
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

            public Chatter(string username, string displayname, string message, string color)
            {
                Username = username;
                DisplayName = displayname;
                this.color = color;
                UpdateMessage(message);
            }

            private readonly List<Message> messages = new();

            public string Username { get; }
            public string DisplayName { get; set; }
            private string? color;
            public string Color { get => color ?? "#ff7f50"; set => color = value; }
            public bool HasMessage { get => messages.Count > 0; }
            public DateTime Timestamp { get => HasMessage ? messages.Last().Timestamp : DateTime.MinValue; }
            public string LastMessage { get => HasMessage ? messages.Last().Content : ""; }

            public void UpdateMessage(string message)
            {
                messages.Add(new Message(message, DateTime.Now));
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

        public string? LastMessage { get => currentChatter?.LastMessage; }

        public string StatusBarString 
        {
            get => $"Messages: {processedMessagesCount}, Users: {chatters.Count}, Filtered: {GetFilteredChatters().Count}";
        }

        public void ClearChatters()
        {
            chatters.Clear();
            processedMessagesCount = 0;
            currentChatter = null;
            OnPropertyChanged(nameof(ChatterName));
            OnPropertyChanged(nameof(LastMessage));
        }

        private readonly Dictionary<string, Chatter> chatters = new();

        private ChatterPicker()
        {
            TwitchClient.Instance.MessageReceived += OnMessageReceived;
            Task.Run(() =>
            {
                while (true)
                {
                    OnPropertyChanged(nameof(StatusBarString));
                    Thread.Sleep(200);
                }
            }
            );
        }

        private bool SatisfiesRules(Chatter chatter)
        {
            bool result = chatter.HasMessage;

            result = result && (!SettingsManager.Instance.Rule1Enabled ||
                SettingsManager.Instance.Rule1MessageCount <= chatter.MessageCount(
                    TimeSpan.FromMinutes(SettingsManager.Instance.Rule1TimeLimit)));

            result = result && (!SettingsManager.Instance.Rule2Enabled ||
                chatter.ContainsMessage(
                    TimeSpan.FromMinutes(SettingsManager.Instance.Rule1TimeLimit),
                    s =>
                    {
                        string sComp = SettingsManager.Instance.Rule2String;

                        if (!SettingsManager.Instance.Rule2CaseSensitive)
                        {
                            s = s.ToLower();
                            sComp = sComp.ToLower();
                        }

                        switch ((SettingsManager.StringContainType)SettingsManager.Instance.Rule2ContainType)
                        {
                            case SettingsManager.StringContainType.Equals:
                                return s.Equals(sComp);
                            case SettingsManager.StringContainType.Contains:
                                return s.Contains(sComp);
                            case SettingsManager.StringContainType.StartsWith:
                                return s.StartsWith(sComp);
                            case SettingsManager.StringContainType.EndsWith:
                                return s.EndsWith(sComp);
                            default: return false;
                        }
                    }));

            return result;
        }

        private Dictionary<string, Chatter> GetFilteredChatters()
        {
            return new(chatters.ToList().Where(c => SatisfiesRules(c.Value)));
        }

        private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            string displayname = e.ChatMessage.DisplayName;
            string username = e.ChatMessage.Username;

            if (chatters.ContainsKey(username))
            {
                chatters[username].UpdateMessage(e.ChatMessage.Message);
                chatters[username].Color = e.ChatMessage.ColorHex;
                chatters[username].DisplayName = displayname;
            }
            else 
            {
                chatters.Add(username, new Chatter(username, displayname, e.ChatMessage.Message, e.ChatMessage.ColorHex));
            }

            if (username == currentChatter?.Username)
            { 
                OnPropertyChanged(nameof(LastMessage));
            }

            processedMessagesCount++;

            OnPropertyChanged(nameof(StatusBarString));
        }

        public void PickRandomChatter()
        {
            var filtered = GetFilteredChatters();

            if (filtered.Count > 0)
            {
                PickChatter(
                    filtered
                    .Select(kvp => kvp.Value)
                    .Random()
                    .Username);
            }
            else
            {
                App.ShowMessage("There are no chatters to select from");
            }
        }

        public void PickChatter(string name)
        {
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

            OnPropertyChanged(nameof(ChatterName));
            OnPropertyChanged(nameof(LastMessage));
            OnPropertyChanged(nameof(StatusBarString));
        }
    }
}
