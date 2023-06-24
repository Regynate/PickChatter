using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech;
using System.Speech.Synthesis;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Threading;

namespace PickChatter
{
    internal class SpeechManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public enum SpeechSynthesisType
        {
            MicrosoftSynthesis,
            AmazonSynthesis,
            GoogleSynthesis
        }

        public SpeechSynthesisType Type => (SpeechSynthesisType)SettingsManager.Instance.SpeechSynthesisType;

        private ISpeechManager manager = MicrosoftSpeechManager.Instance;

        public bool SpeechSpeaking => manager.SpeechSpeaking;

        public string SpeakButtonText => manager.SpeechSpeaking ? "Stop" : "Speak";

        public List<string> AvailableVoices => manager.AvailableVoices;

        public string CurrentVoice => manager.CurrentVoice;
            

        public void Speak(string message)
        {
            if (!string.IsNullOrEmpty(message) && SettingsManager.Instance.SpeechSynthesisEnabled)
            {
                manager.Speak(message);
            }
        }

        public void Stop()
        {
            manager.Stop();
        }

        private void onStateChanged(object? sender, EventArgs args)
        {
            PropertyChanged?.Invoke(this, new(nameof(SpeakButtonText)));
        }

        private void UpdateManager()
        {
            manager.StateChanged -= onStateChanged;

            manager = Type switch
            {
                SpeechSynthesisType.AmazonSynthesis => AmazonSpeechManager.Instance,
                SpeechSynthesisType.GoogleSynthesis => GoogleSpeechManager.Instance,
                SpeechSynthesisType.MicrosoftSynthesis or _ => MicrosoftSpeechManager.Instance
            };

            manager.StateChanged += onStateChanged;
        }    

        private SpeechManager()
        {
            UpdateManager();

            SettingsManager.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsManager.Instance.SpeechSynthesisType))
                {
                    UpdateManager();
                }
            };
        }

        private static readonly SpeechManager instance = new();
        public static SpeechManager Instance { get => instance; }
    }
}
