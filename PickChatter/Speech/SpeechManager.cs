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
using System.Windows;

namespace PickChatter
{
    public class SpeechManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public enum SpeechSynthesisType
        {
            MicrosoftSynthesis,
            AmazonSynthesis,
            ElevenlabsSynthesis,
            GoogleSynthesis
        }

        private SpeechSynthesisType _type;

        public SpeechSynthesisType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    UpdateManager();
                    PropertyChanged?.Invoke(this, new(nameof(Type)));
                }
            }
        }

        private ISpeechManager manager = new MicrosoftSpeechManager();

        public bool SpeechSpeaking => manager.SpeechSpeaking;

        public string SpeakButtonText => manager.SpeechSpeaking ? "Stop" : "Speak";

        public List<string> AvailableVoices => manager.AvailableVoices;

        public string CurrentVoice
        {
            get => manager.CurrentVoice;
            set
            {
                if (manager.AvailableVoices.Contains(value))
                {
                    manager.CurrentVoice = value;
                }
            }
        }

        public void SetSettings(VoiceSettings settings)
        {
            Type = settings.SpeechSynthesisType;
            CurrentVoice = settings.VoiceName;
        }

        public void Speak(string id, string message)
        {
            if (!string.IsNullOrEmpty(message) && SettingsManager.Instance.SpeechSynthesisEnabled)
            {
                manager.Speak(id, message);
            }
        }

        public void Stop(string id)
        {
            manager.Stop(id);
        }

        private void OnStateChanged(object? sender, EventArgs args)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new(nameof(SpeechSpeaking)));
                PropertyChanged?.Invoke(this, new(nameof(SpeakButtonText)));
            });
        }

        private void UpdateManager()
        {
            manager.StateChanged -= OnStateChanged;

            manager = Type switch
            {
                SpeechSynthesisType.AmazonSynthesis => new AmazonSpeechManager(),
                SpeechSynthesisType.ElevenlabsSynthesis => new ElevenlabsSpeechManager(),
                SpeechSynthesisType.GoogleSynthesis => new GoogleSpeechManager(),
                SpeechSynthesisType.MicrosoftSynthesis or _ => new MicrosoftSpeechManager()
            };

            manager.StateChanged += OnStateChanged;
        }

        public SpeechManager()
        {
            UpdateManager();
        }
    }
}
