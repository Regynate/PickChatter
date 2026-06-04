using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    public class VoiceSettings : INotifyPropertyChanged
    {
        private string _microsoftVoice =
            MicrosoftSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        private string _amazonVoice =
            AmazonSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        private string _elevenlabsVoice =
            ElevenlabsSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        private string _googleVoice =
            GoogleSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        private SpeechManager.SpeechSynthesisType _speechSynthesisType = 0;

        public string MicrosoftVoice
        {
            get => _microsoftVoice;
            set => SetProperty(ref _microsoftVoice, value, nameof(VoiceName));
        }

        public string AmazonVoice
        {
            get => _amazonVoice;
            set => SetProperty(ref _amazonVoice, value, nameof(VoiceName));
        }

        public string ElevenlabsVoice
        {
            get => _elevenlabsVoice;
            set => SetProperty(ref _elevenlabsVoice, value, nameof(VoiceName));
        }

        public string GoogleVoice
        {
            get => _googleVoice;
            set => SetProperty(ref _googleVoice, value, nameof(VoiceName));
        }

        public SpeechManager.SpeechSynthesisType SpeechSynthesisType
        {
            get => _speechSynthesisType;
            set => SetProperty(ref _speechSynthesisType, value, nameof(VoiceName));
        }

        public string VoiceName
        {
            get
            {
                return SpeechSynthesisType switch
                {
                    SpeechManager.SpeechSynthesisType.AmazonSynthesis => AmazonVoice,
                    SpeechManager.SpeechSynthesisType.ElevenlabsSynthesis => ElevenlabsVoice,
                    SpeechManager.SpeechSynthesisType.GoogleSynthesis => GoogleVoice,
                    _ => MicrosoftVoice
                };
            }
            set
            {
                switch (SpeechSynthesisType)
                {
                    case SpeechManager.SpeechSynthesisType.AmazonSynthesis:
                        AmazonVoice = value;
                        break;
                    case SpeechManager.SpeechSynthesisType.ElevenlabsSynthesis:
                        ElevenlabsVoice = value;
                        break;
                    case SpeechManager.SpeechSynthesisType.GoogleSynthesis:
                        GoogleVoice = value;
                        break;
                    default:
                        MicrosoftVoice = value;
                        break;
                }
            }
        }

        public void SetVoiceID(string voiceID)
        {
            switch (SpeechSynthesisType)
            {
                case SpeechManager.SpeechSynthesisType.ElevenlabsSynthesis:
                    ElevenlabsVoice = ElevenlabsSpeechManager.Instance.GetVoiceByID(voiceID);
                    break;
                case SpeechManager.SpeechSynthesisType.AmazonSynthesis:
                case SpeechManager.SpeechSynthesisType.GoogleSynthesis:
                default:
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(
            ref T field,
            T value,
            params string[] dependentProperties)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            OnPropertyChanged();

            foreach (var property in dependentProperties)
            {
                OnPropertyChanged(property);
            }

            return true;
        }
    }
}
