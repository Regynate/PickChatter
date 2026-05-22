using PickChatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    public class VoiceSettings : INotifyPropertyChanged
    {
        public string MicrosoftVoice { get; set; } = MicrosoftSpeechManager.Instance.AvailableVoices[0];

        public string AmazonVoice { get; set; } = AmazonSpeechManager.Instance.AvailableVoices[0];

        public string GoogleVoice { get; set; } = GoogleSpeechManager.Instance.AvailableVoices[0];

        public SpeechManager.SpeechSynthesisType SpeechSynthesisType { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
