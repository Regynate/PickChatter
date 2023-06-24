using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class MicrosoftSpeechManager : ISpeechManager
    {
        private readonly SpeechSynthesizer synthesizer = new();

        public bool SpeechSpeaking { get; private set; }

        public List<string> AvailableVoices { get => synthesizer.GetInstalledVoices().Select(v => v.VoiceInfo.Name).ToList(); }

        public string CurrentVoice
        {
            get => synthesizer.Voice.Name;
            private set => synthesizer.SelectVoice(value);
        }

        public void Speak(string message)
        {
            synthesizer.SpeakAsync(message);
        }

        public void Stop()
        {
            synthesizer.SpeakAsyncCancelAll();
        }

        public event EventHandler<EventArgs>? StateChanged;

        private MicrosoftSpeechManager()
        {
            synthesizer.StateChanged += (_, args) =>
            {
                SpeechSpeaking = args.State == SynthesizerState.Speaking;
                StateChanged?.Invoke(this, new());
            };

            SettingsManager.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsManager.Instance.MicrosoftVoice))
                {
                    CurrentVoice = SettingsManager.Instance.MicrosoftVoice;
                }
            };

            synthesizer.SetOutputToDefaultAudioDevice();

            if (!string.IsNullOrWhiteSpace(SettingsManager.Instance.MicrosoftVoice))
            {
                synthesizer.SelectVoice(SettingsManager.Instance.MicrosoftVoice);
            }
            else
            {
                SettingsManager.Instance.MicrosoftVoice = synthesizer.Voice.Name;
                SettingsManager.Instance.Save();
            }
        }

        private static readonly MicrosoftSpeechManager instance = new();
        public static MicrosoftSpeechManager Instance => instance;
    }
}
