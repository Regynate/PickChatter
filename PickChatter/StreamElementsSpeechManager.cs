using System;
using System.Collections.Generic;

namespace PickChatter
{
    internal abstract class StreamElementsSpeechManager : ISpeechManager
    {
        public bool SpeechSpeaking { get; private set; }

        public abstract List<string> AvailableVoices { get; }

        protected abstract string VoiceID { get; }

        public abstract string CurrentVoice { get; }

        public event EventHandler<EventArgs>? StateChanged;

        private readonly Queue<string> messages = new();

        private readonly MediaPlayer player = new();

        private void SpeakImpl(string message)
        {
            SpeechSpeaking = true;
            StateChanged?.Invoke(this, new());

            string url = $"https://api.streamelements.com/kappa/v2/speech?voice={VoiceID}&text={Uri.EscapeDataString(message)}";
            player.Open(url);
        }

        public void Speak(string message)
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

        public void Stop()
        {
            SpeechSpeaking = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
            messages.Clear();
            player.Stop();
        }

        protected StreamElementsSpeechManager()
        {
            player.MediaOpened += (_, _) => player.Play();

            player.MediaEnded += (_, _) =>
            {
                if (messages.Count == 0)
                {
                    SpeechSpeaking = false;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SpeakImpl(messages.Dequeue());
                }
            };

            player.MediaFailed += (_, args) =>
            {
                SpeechSpeaking = false;
                StateChanged?.Invoke(this, EventArgs.Empty);
            };
        }
    }
}
