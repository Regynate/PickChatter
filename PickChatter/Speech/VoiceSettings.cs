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
        public string MicrosoftVoice { get; set; } = MicrosoftSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        public string AmazonVoice { get; set; } = AmazonSpeechManager.Instance.AvailableVoices.FirstOrDefault("");
        
        public string ElevenlabsVoice { get; set; } = ElevenlabsSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        public string GoogleVoice { get; set; } = GoogleSpeechManager.Instance.AvailableVoices.FirstOrDefault("");

        public SpeechManager.SpeechSynthesisType SpeechSynthesisType { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
