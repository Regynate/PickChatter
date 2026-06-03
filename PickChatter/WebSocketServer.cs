using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using TwitchLib.PubSub.Models.Responses;

namespace PickChatter
{
    public class ConnectionOpenEventArgs : EventArgs
    {
        public ConnectionOpenEventArgs(IWebSocketConnection connection)
        {
            Connection = connection;
        }

        public IWebSocketConnection Connection { get; }
    }

    internal class WebSocketServer
    {
        private readonly List<IWebSocketConnection> connections = new List<IWebSocketConnection>();

        public event EventHandler<ConnectionOpenEventArgs>? ConnectionOpen;

        public class AudioEventArgs : EventArgs
        {
            public AudioEventArgs(string id)
            {
                ID = id;
            }
            public string ID { get; }
        }

        public event EventHandler<AudioEventArgs>? AudioEnded;
        public event EventHandler<AudioEventArgs>? AudioStarted;

        private readonly Fleck.WebSocketServer wss;

        private WebSocketServer()
        {
            wss = new Fleck.WebSocketServer($"ws://0.0.0.0:9871");
            // Set the websocket listeners
            wss.Start(connection =>
            {
                connection.OnOpen += () => OnWsConnectionOpen(connection);
                connection.OnClose += () => OnWSConnectionClose(connection);
                connection.OnMessage += (message) => OnWSConnectionMessage(connection, message);
            });
        }

        private void SendConnectionMessage(IWebSocketConnection connection, string message)
        {
            // If the connection is not available for some reason, we just close it
            if (!connection.IsAvailable)
            {
                connection.Close();
            }
            else
            {
                connection.Send(message);
            }
        }

        private void SendConnectionMessage(IWebSocketConnection connection, object message)
        {
            SendConnectionMessage(connection, JsonConvert.SerializeObject(message));
        }

        private void Broadcast(object message)
        {
            connections.ForEach(connection =>
            {
                SendConnectionMessage(connection, message);
            });
        }

        private void OnWSConnectionClose(IWebSocketConnection connection)
        {
            try
            {
                connections.Remove(connection);
            }
            catch (Exception)
            {

            }
        }

        private void OnWsConnectionOpen(IWebSocketConnection connection)
        {
            try
            {
                connections.Add(connection);
                ConnectionOpen?.Invoke(this, new(connection));
            }
            catch (Exception)
            {

            }
        }

        private void OnWSConnectionMessage(IWebSocketConnection connection, string message)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(message);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("type", out JsonElement type) && root.TryGetProperty("id", out JsonElement id))
                {
                    switch (type.GetString())
                    {
                        case "audioEnded":
                        {
                            AudioEnded?.Invoke(this, new(id.GetString()!));
                            break;
                        }
                        case "audioStarted":
                        {
                            AudioStarted?.Invoke(this, new(id.GetString()!));
                            break;
                        }
                        case "addChatterTab":
                        {
                            //todo: add response to command
                            ChatterPickerList.Instance.AddChatterPicker(id.GetString());
                            break;
                        }
                        case "removeChatterTab":
                        {
                            ChatterPickerList.Instance.RemovePicker(id.GetString()!);
                            break;
                        }
                        case "pickChatter":
                        {
                            if (root.TryGetProperty("chatter", out JsonElement chatter))
                            {
                                foreach (var picker in ChatterPickerList.Instance.GetPickers(id.GetString()!))
                                {
                                    picker.PickChatter(chatter.GetString()!);
                                }
                            }
                            break;
                        }
                        case "pickRandomChatter":
                        {
                            foreach (var picker in ChatterPickerList.Instance.GetPickers(id.GetString()!))
                            {
                                picker.PickRandomChatter();
                            }

                            break;
                        }
                        case "setVoice":
                        {
                            foreach (var picker in ChatterPickerList.Instance.GetPickers(id.GetString()!))
                            {
                                if (root.TryGetProperty("voiceType", out JsonElement voiceType) &&
                                    root.TryGetProperty("voice", out JsonElement voice))
                                {
                                    var settings = picker.VoiceSettings;
                                    settings.SpeechSynthesisType = Enum.Parse<SpeechManager.SpeechSynthesisType>(voiceType.GetString()!, true);
                                    settings.VoiceName = voice.GetString()!;
                                    picker.VoiceSettings = settings;
                                }
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void StopAudio(string id)
        {
            Broadcast(new { type = "audioStop", id });
        }

        public void StopAudio(IWebSocketConnection connection, string id)
        {
            SendConnectionMessage(connection, new { type = "audioStop", id });
        }

        public bool SendAudioUrl(string id, string url)
        {
            Broadcast(new { type = "audioUrl", id, url });
            return connections.Count > 0;
        }

        public void SendAudioUrl(IWebSocketConnection connection, string id, string url)
        {
            SendConnectionMessage(connection, new { type = "audioUrl", id, url });
        }

        public void SendMessage(string id, string message, string color, string tokenized_message)
        {
            Broadcast(new { type = "message", id, message, color, tokenized_message });
        }

        public void SendMessage(IWebSocketConnection connection, string id, string message, string color, string tokenized_message)
        {
            SendConnectionMessage(connection,
                new { type = "message", message, color, tokenized_message });
        }

        public void SendChatter(string id, string chatter)
        {
            Broadcast(new { type = "chatter", id, chatter });
        }

        public void SendChatter(IWebSocketConnection connection, string id, string chatter)
        {
            SendConnectionMessage(connection, new { type = "chatter", id, chatter });
        }

        internal void SendRemainingTime(string id, string time)
        {
            Broadcast(new { type = "remaining-time", id, time });
        }

        internal void SendRemainingTime(IWebSocketConnection connection, string id, string time)
        {
            SendConnectionMessage(connection, new { type = "remaining-time", id, time });
        }

        private static readonly WebSocketServer instance = new WebSocketServer();
        public static WebSocketServer Instance => instance;
    }
}
