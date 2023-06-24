using Newtonsoft.Json.Linq;
using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace PickChatter
{
    internal class TwitchClient : INotifyPropertyChanged
    {
        private static TwitchClient instance = new();
        public static TwitchClient Instance { get => instance; }

        private readonly TwitchLib.Client.TwitchClient client;
        private readonly TwitchLib.Api.TwitchAPI api;

        public event EventHandler<OnMessageReceivedArgs>? MessageReceived;
        public event EventHandler<OnUserBannedArgs>? UserBanned;

        public event PropertyChangedEventHandler? PropertyChanged;

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
                            return baseString + "connected to " + client.JoinedChannels[0].Channel;
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

            Task.Run(() =>
            {
                while (true)
                {
                    PropertyChanged?.Invoke(this, new(nameof(StatusBarString)));
                    Thread.Sleep(200);
                }
            });

            client.OnDisconnected += (_, args) => UpdateChannel(); // try to reconnect immediately

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

        public void UpdateChannel()
        {
            UpdateChannel(SettingsManager.Instance.TwitchChannel);
        }

        public void UpdateChannel(string newChannel)
        {
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

        public void Initialize(string username, string oauth, string? channel)
        {
            connectionAttempt = 0;
            try
            {
                client.Initialize(new ConnectionCredentials(username, oauth), channel);
            }
            catch
            {
                App.ShowMessage("Couldn't initialize Twitch client, try again");
                return;
            }
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
            //MessageBox.Show("Connected");
        }

        private void OnError(object? sender, OnErrorEventArgs e)
        {
            App.ShowMessage("Error: " + e.Exception);
        }

        private void OnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            App.ShowMessage("Incorrect login: " + e.Exception);
        }

        private void OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            //MessageBox.Show("Joined channel " + e.Channel);
        }
    }
}
