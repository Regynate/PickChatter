using System;
using System.Collections.Generic;

namespace PickChatter
{
    internal abstract class ServerSpeechManager : ISpeechManager
    {
        private bool appSpeaking = false;
        private bool browserSpeaking = false;

        public bool SpeechSpeaking => appSpeaking || browserSpeaking;

        public abstract List<string> AvailableVoices { get; }

        protected abstract string VoiceID { get; }

        public string CurrentVoice { get; set; }

        public event EventHandler<EventArgs>? StateChanged;

        private readonly Queue<string> messages = new();

        private readonly MediaPlayer player = new();

        protected abstract string GetAudioUrl(string message);

        private void SpeakImpl(string message)
        {
            appSpeaking = true;

            player.Open(GetAudioUrl(message));
        }

        public void Speak(string id, string message)
        {
            if (SettingsManager.Instance.PlayAudioInApp)
            {
                if (!SpeechSpeaking)
                {
                    SpeakImpl(message);
                }
                else
                {
                    messages.Enqueue(message);
                }
            }
            if (SettingsManager.Instance.PlayAudioInBrowser)
            {
                WebSocketServer.Instance.SendAudioUrl(id, GetAudioUrl(message));
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop(string id)
        {
            if (SettingsManager.Instance.PlayAudioInApp)
            {
                appSpeaking = false;
                messages.Clear();
                player.Stop();
            }
            if (SettingsManager.Instance.PlayAudioInBrowser)
            {
                browserSpeaking = false;
                WebSocketServer.Instance.StopAudio(id);
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public ServerSpeechManager()
        {
            player.MediaOpened += (_, _) => player.Play();

            player.MediaEnded += (_, _) =>
            {
                if (messages.Count == 0)
                {
                    appSpeaking = false;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SpeakImpl(messages.Dequeue());
                }
            };

            player.MediaFailed += (_, args) =>
            {
                appSpeaking = false;
                StateChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public void OnBrowserAudioStarted()
        {
            browserSpeaking = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OnBrowserAudioEnded()
        {
            browserSpeaking = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
