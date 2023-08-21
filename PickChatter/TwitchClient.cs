using Newtonsoft.Json.Linq;
using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Exceptions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace PickChatter
{
    internal class TwitchClient : INotifyPropertyChanged
    {
        private static readonly TwitchClient instance = new();
        public static TwitchClient Instance { get => instance; }

        private readonly TwitchLib.Client.TwitchClient client;
        private readonly TwitchLib.Api.TwitchAPI api;

        public event EventHandler<OnMessageReceivedArgs>? MessageReceived;
        public event EventHandler<OnUserTimedoutArgs>? UserTimedOut;
        public event EventHandler<OnUserBannedArgs>? UserBanned;
        public event EventHandler<OnMessageClearedArgs>? MessageDeleted;

        public event PropertyChangedEventHandler? PropertyChanged;

        private Dictionary<string, string> emotes = new();
        private bool emotesLoaded = false;

        private int connectionAttempt = 0;

        public string StatusBarString { get
            {
                string baseString = "Twitch client ";
                if (client.IsInitialized)
                {
                    if (client.IsConnected)
                    {
                        if (client.JoinedChannels.Count > 0)
                        {
                            if (emotesLoaded)
                            {
                                return baseString + "connected to " + client.JoinedChannels[0].Channel;
                            }
                            else
                            {
                                return baseString + "connected; loading emotes";
                            }
                        }
                        else
                        {
                            return baseString + "connected; joining channel";
                        }
                    }
                    else
                    {
                        baseString = baseString + "initialized, connecting";
                        if (connectionAttempt > 0)
                        {
                            return baseString + $" (x{connectionAttempt})";
                        }
                        else
                        {
                            return baseString;
                        }
                    }
                }
                else
                {
                    return baseString + "not initialized";
                }
            }
        }

        private TwitchClient()
        {
            client = new();
            api = new();
            api.Settings.ClientId = "nfqg9hggwhx32rxe3zcrr8idhokenp";

            client.OnConnected += OnConnected;
            client.OnError += OnError;
            client.OnIncorrectLogin += OnIncorrectLogin;
            client.OnJoinedChannel += OnJoinedChannel;
            client.OnMessageReceived += (sender, args) => MessageReceived?.Invoke(this, args);
            client.OnUserBanned += (sender, args) => UserBanned?.Invoke(this, args);
            client.OnUserTimedout += (sender, args) => UserTimedOut?.Invoke(this, args);
            client.OnMessageCleared += (sender, args) => MessageDeleted?.Invoke(this, args);

            client.WillReplaceEmotes = true;

            Task.Run(() =>
            {
                while (true)
                {
                    PropertyChanged?.Invoke(this, new(nameof(StatusBarString)));
                    Thread.Sleep(200);
                }
            });

            client.AutoReListenOnException = true;

            HttpServer.Instance.PropertyChanged += (source, args) =>
            {
                if (args.PropertyName == nameof(HttpServer.Instance.Token))
                {
                    string token = HttpServer.Instance.Token ?? "";
                    SettingsManager.Instance.TwitchOauth = token;
                    string username = GetUsername(token);
                    SettingsManager.Instance.TwitchUsername = username;

                    TryInitialize();
                }
            };
        }

        private async Task InitializeEmotes(string channel)
        {
            emotesLoaded = false;
            emotes = new();
            string userId = (await api.Helix.Users.GetUsersAsync(logins: new List<string>() { channel })).Users[0].Id;
                
            var client = new HttpClient();
            var response = await client.GetAsync($"https://api.betterttv.net/3/cached/emotes/global");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jobject = JArray.Parse(await response.Content.ReadAsStringAsync());
                    foreach (var emote in jobject)
                    {
                        emotes.TryAdd(emote["code"]!.ToString(), $"https://cdn.betterttv.net/emote/{emote["id"]}/3x.webp");
                    }
                }
                catch { }
            }

            response = await client.GetAsync($"https://api.betterttv.net/3/cached/users/twitch/{userId}");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jobject = JObject.Parse(await response.Content.ReadAsStringAsync());
                    foreach (var emote in jobject["channelEmotes"]!.Concat(jobject["sharedEmotes"]!))
                    {
                        emotes.TryAdd(emote["code"]!.ToString(), $"https://cdn.betterttv.net/emote/{emote["id"]}/3x.webp");
                    }
                }
                catch { }
            }
                
            response = await client.GetAsync($"https://api.frankerfacez.com/v1/room/id/{userId}");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jobject = JObject.Parse(await response.Content.ReadAsStringAsync());
                    foreach (var emote in jobject.SelectToken($"sets.{jobject["room._id"]}.emoticons")!)
                    {
                        var urls = emote["urls"]!;
                        string? url = urls["4"]?.ToString();
                        if (url == null)
                        {
                            url = urls["2"]?.ToString();
                        }
                        if (url == null)
                        {
                            url = urls["1"]?.ToString();
                        }
                        if (url != null)
                        {
                            emotes.TryAdd(emote["name"]!.ToString(), url.ToString());
                        }
                    }
                }
                catch { }
            }

            client.Dispose();

            emotesLoaded = true;
        }

        public void UpdateChannel(bool updateEmotes = true)
        {
            UpdateChannel(SettingsManager.Instance.TwitchChannel, updateEmotes);
        }

        public void UpdateChannel(string newChannel, bool updateEmotes = true)
        {
            if (updateEmotes)
            {
                Task.Run(() => InitializeEmotes(newChannel));
            }

            if (client.IsInitialized && client.IsConnected)
            {
                if (client.JoinedChannels.Count > 0)
                {
                    if (client.JoinedChannels[0].Channel.Equals(newChannel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }

                    client.JoinedChannels.ToList().ForEach(channel => client.LeaveChannel(channel));
                }
                if (!string.IsNullOrWhiteSpace(newChannel))
                {
                    try
                    {
                        client.JoinChannel(newChannel);
                    }
                    catch
                    {
                        App.ShowMessage("Couldn't join channel, try again");
                        return;
                    }
                }
            }
            else if (!client.IsInitialized)
            {
                App.ShowMessage("Connect to Twitch first");
            }
        }

        private void TryConnect()
        {
            do
            {
                try
                {
                    client.Connect();
                }
                catch
                {
                    connectionAttempt++;
                }
            }
            while (!client.IsConnected);
        }

        public void Initialize(string username, string oauth, string? channel)
        {

            connectionAttempt = 0;
            try
            {
                client.Initialize(new ConnectionCredentials(username, oauth), channel);
                api.Settings.AccessToken = oauth;
            }
            catch
            {
                App.ShowMessage("Couldn't initialize Twitch client, try again");
                return;
            }

            if (channel != null)
            {
                Task.Run(() => InitializeEmotes(channel));
            }

            TryConnect();
        }

        public void Initialize(string username, string oauth)
        {
            if (
                App.ShowMessage(
                    $"A twitch channel to listen to is not set. Would you like to set it to {username}?",
                    "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No
                    ) == MessageBoxResult.Yes
                )
            {
                SettingsManager.Instance.TwitchChannel = username;
                Initialize(username, oauth, username);
            }
            else
            {
                App.ShowMessage("Set the twitch channel in the settings menu");
                Initialize(username, oauth, null);
            }
        }

        public string GetUsername(string oauth)
        {
            var response = api.Auth.ValidateAccessTokenAsync(oauth).Result;

            return response.Login;
        }

        public void TryInitialize()
        {
            string username = SettingsManager.Instance.TwitchUsername;
            string token = SettingsManager.Instance.TwitchOauth;
            string channel = SettingsManager.Instance.TwitchChannel;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(token))
            { 
                if (string.IsNullOrWhiteSpace(channel))
                {
                    Task.Run(() => Initialize(username, token));
                }
                else
                {
                    Task.Run(() => Initialize(username, token, channel));
                }
            }
        }

        private List<IMessageToken> TokenizeMessage(string message, EmoteSet emoteSet)
        {
            Dictionary<string, string> presentEmotes = new();
            foreach (Emote e in emoteSet.Emotes)
            {
                presentEmotes.TryAdd(e.Name, $"https://static-cdn.jtvnw.net/emoticons/v2/{e.Id}/default/light/3.0");
            }

            if (emotesLoaded)
            {
                foreach (var kvp in emotes)
                {
                    presentEmotes.TryAdd(kvp.Key, kvp.Value);
                }
            }

            Regex regex = new Regex(string.Join('|', presentEmotes.Select(e => @"(?<=^|\s)(" + Regex.Escape(e.Key) + @")(?=$|\s)")));

            List<IMessageToken> tokens = new();

            foreach(string split in regex.Split(message))
            {
                if (presentEmotes.TryGetValue(split, out string? url))
                {
                    tokens.Add(new EmoteToken(url));
                }
                else
                {
                    tokens.Add(new StringToken(split));
                }
            }

            return tokens;
        }

        public string ConvertToEmoteJson(string message, EmoteSet emoteSet)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(TokenizeMessage(message, emoteSet).ConvertAll(e => e.ToJsonObject()));
        }

        public void StartBrowserAuth()
        {
            string url = "https://id.twitch.tv/oauth2/authorize?" + 
                "client_id=" + api.Settings.ClientId + 
                "&redirect_uri=" + HttpServer.Instance.TwitchURL +
                "&state=" + HttpServer.Instance.GenerateStateString() +
                "&force_verify=false&response_type=token&scope=chat:read";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void OnConnected(object? sender, OnConnectedArgs e)
        {
            if (client.JoinedChannels.Count == 0)
            {
                UpdateChannel();
            }
        }

        private void OnError(object? sender, OnErrorEventArgs e)
        {
            App.ShowMessage("Error: " + e.Exception);
        }

        private void OnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            if (App.ShowMessage("The login token is incorrect or expired. Try to reconnect now?", "Error", 
                MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                StartBrowserAuth();
            }
        }

        private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            //MessageBox.Show("Joined channel " + e.Channel);
        }
    }
}
