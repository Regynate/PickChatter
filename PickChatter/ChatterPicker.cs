using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace PickChatter
{
    public sealed class ChatterPicker : INotifyPropertyChanged
    {
        public class MessageChangedEventArgs : EventArgs
        {
            public string Message { get; }
            public string Color { get; }
            public string TokenizedMessage { get; }

            public MessageChangedEventArgs(string message, string color, string tokenizedMessage)
            {
                Message = message;
                Color = color;
                TokenizedMessage = tokenizedMessage;
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
            Application.Current?.Dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        private void NotifyMessageChanged()
        {
            Application.Current?.Dispatcher.Invoke(() => MessageChanged?.Invoke(this, new(LastMessage ?? "", currentChatter?.Color ?? "", TokenizedLastMessage ?? "")));
            NotifyPropertyChanged(nameof(LastMessage));
        }

        private void NotifyChatterChanged()
        {
            Application.Current?.Dispatcher.Invoke(() => ChatterChanged?.Invoke(this, new(ChatterName ?? "")));
            NotifyPropertyChanged(nameof(ChatterName));
            NotifyMessageChanged();
        }

        private void NotifyMessageDeleted()
        {
            NotifyMessageChanged();
            MessageDeleted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<MessageChangedEventArgs>? MessageChanged;
        public event EventHandler<ChatterChangedEventArgs>? ChatterChanged;
        public event EventHandler<EventArgs>? MessageDeleted;

        private Dictionary<string, Chatter> filteredChatters = new();

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
        public string? TokenizedLastMessage => currentChatter?.TokenizedLastMessage;

        public string StatusBarString => $"Messages: {processedMessagesCount}, Users: {chatters.Count}, Filtered: {filteredChatters.Count}";

        private SpeechManager _speechManager;
        public SpeechManager SpeechManager => _speechManager;

        private VoiceSettings _voiceSettings = new();
        public VoiceSettings VoiceSettings
        {
            get => _voiceSettings;
            set
            {
                _voiceSettings = value;
                UpdateSpeechSettings();
            }
        }

        public void UpdateSpeechSettings()
        {
            _speechManager.SetSettings(_voiceSettings);
        }

        private int _index;
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                NotifyPropertyChanged(nameof(Index));
            }
        }

        private string _id;
        public string ID
        {
            get => IsDefaultID ? $"Chatter_{_index + 1}" : _id;
            set
            {
                var prevDefault = IsDefaultID;
                var newVal = _id != value;

                _id = value;

                if (newVal && !(prevDefault && IsDefaultID))
                {
                    NotifyPropertyChanged(nameof(ID));
                }
            }
        }
        public bool IsDefaultID => string.IsNullOrEmpty(_id) || new Regex("^Chatter_\\d+$").IsMatch(_id);

        public void ClearChatters()
        {
            chatters.Clear();
            filteredChatters.Clear();
            processedMessagesCount = 0;
            currentChatter = null;
            NotifyChatterChanged();
            NotifyMessageDeleted();
        }

        private readonly Dictionary<string, Chatter> chatters = new();

        internal ChatterPicker(string id = "", int index = 0)
        {
            // this is probably a hack but i don't care
            if (_default != null)
            {
                chatters = _default.chatters;
                filteredChatters = _default.filteredChatters;
                processedMessagesCount = _default.processedMessagesCount;
            }

            _id = id;
            _speechManager = new SpeechManager();
            TwitchClient.Instance.MessageReceived += OnMessageReceived;
            TwitchClient.Instance.UserBanned += (_, args) => OnUserBanned(args.UserBan.Username);
            TwitchClient.Instance.UserTimedOut += (_, args) => OnUserBanned(args.UserTimeout.Username);
            TwitchClient.Instance.MessageDeleted += (_, args) => OnMessageDeleted(args.TargetMessageId);
            Task.Run(() =>
            {
                while (true)
                {
                    UpdateFilteredChatters();
                    NotifyPropertyChanged(nameof(StatusBarString));
                    Thread.Sleep(1000);
                }
            }
            );

            WebSocketServer.Instance.AudioEnded += (_, args) =>
            {
                if (args.ID == ID)
                {
                    SpeechManager.OnBrowserAudioEnded();
                }
            };

            WebSocketServer.Instance.AudioStarted += (_, args) =>
            {
                if (args.ID == ID)
                {
                    SpeechManager.OnBrowserAudioStarted();
                }
            };
        }

        private void OnMessageDeleted(string messageID)
        {
            foreach (var chatter in chatters.Values)
            {
                if (chatter.RemoveMessage(messageID))
                {
                    processedMessagesCount--;
                    if (chatter == currentChatter)
                    {
                        NotifyMessageDeleted();
                    }
                    return;
                }
            }

        }

        private void OnUserBanned(string username)
        {
            if (username == currentChatter?.Username)
            {
                PickChatter("");
            }

            if (chatters.TryGetValue(username, out Chatter? chatter))
            {
                chatters.Remove(username);
                processedMessagesCount -= chatter.MessageCount();
            }
        }

        private static bool Rule1(Chatter chatter)
        {
            if (!SettingsManager.Instance.Rule1Enabled)
            {
                return true;
            }
            return SettingsManager.Instance.Rule1MessageCount <=
                chatter.MessageCount(TimeSpan.FromMinutes(SettingsManager.Instance.Rule1TimeLimit));
        }

        private static bool Rule2(Chatter chatter)
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

        private static bool Rule3(Chatter chatter)
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

        private static bool Rule4(Chatter chatter)
        {
            return !SettingsManager.Instance.ExcludeUsersEnabled ||
                !SettingsManager.Instance.ExcludeUsersString
                .ToLower()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(chatter.Username);
        }

        private static bool Rule5(Chatter chatter)
        {
            if (!SettingsManager.Instance.Rule5Enabled)
            {
                return true;
            }

            return !chatter.ChosenBefore
                || DateTime.Now - chatter.LastChosenTime > TimeSpan.FromSeconds(SettingsManager.Instance.Rule5Cooldown);
        }

        private static bool SatisfiesRules(Chatter chatter)
        {
            return chatter.HasMessage && Rule1(chatter) && Rule2(chatter) && Rule3(chatter) && Rule4(chatter) && Rule5(chatter);
        }

        private Dictionary<string, Chatter> GetFilteredChatters()
        {
            return new(chatters.ToList().Where(c => SatisfiesRules(c.Value)));
        }

        private void UpdateFilteredChatters()
        {
            filteredChatters = GetFilteredChatters();
        }

        private void UpdateFilteredChatters(Chatter chatter)
        {
            if (!filteredChatters.ContainsKey(chatter.Username) && SatisfiesRules(chatter))
            {
                filteredChatters.Add(chatter.Username, chatter);
            }
            else
            {
                filteredChatters.Remove(chatter.Username);
            }
        }

        public void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            // TODO: don't like it very much
            if (SettingsManager.Instance.ExcludeCommandsEnabled && e.ChatMessage.Message.StartsWith('!'))
            {
                return;
            }

            string username = e.ChatMessage.Username;

            Chatter chatter;
            if (chatters.ContainsKey(username))
            {
                chatter = chatters[username];
                chatter.Update(e.ChatMessage);
            }
            else
            {
                chatter = new Chatter(username, e.ChatMessage);
                chatters.Add(username, chatter);
            }

            UpdateFilteredChatters(chatters[username]);

            if (username == currentChatter?.Username && SettingsManager.Instance.ChatterMode == (int) SettingsManager.ChatterModeType.Chatter)
            {
                NotifyMessageChanged();
            }

            processedMessagesCount++;
            NotifyPropertyChanged(nameof(StatusBarString));
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
            if (string.IsNullOrEmpty(name))
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

            currentChatter.Choose();

            NotifyChatterChanged();
        }

        public void Dispose()
        {
            if (PropertyChanged is not null)
            {
                foreach (Delegate d in PropertyChanged.GetInvocationList())
                {
                    PropertyChanged -= (PropertyChangedEventHandler) d;
                }
            }

            if (MessageChanged is not null)
            {
                foreach (Delegate d in MessageChanged.GetInvocationList())
                {
                    MessageChanged -= (EventHandler<MessageChangedEventArgs>) d;
                }
            }

            if (ChatterChanged is not null)
            {
                foreach (Delegate d in ChatterChanged.GetInvocationList())
                {
                    ChatterChanged -= (EventHandler<ChatterChangedEventArgs>) d;
                }
            }

            if (MessageDeleted is not null)
            {
                foreach (Delegate d in MessageDeleted.GetInvocationList())
                {
                    MessageDeleted -= (EventHandler<EventArgs>) d;
                }
            }

            PickChatter(null);
        }

        private static ChatterPicker _default = new();
    }
}
