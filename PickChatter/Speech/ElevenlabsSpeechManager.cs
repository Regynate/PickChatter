using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace PickChatter
{
    internal class ElevenlabsSpeechManager : ServerSpeechManager
    {
        private static Dictionary<string, string> _voiceIDs => voices.ToDictionary(e => e.Name, e => e.ID);

        public override List<string> AvailableVoices => voices.Select(e => e.Name).ToList();

        protected override string VoiceID => CurrentVoice;

        protected override string GetAudioUrl(string message)
        {
            if (CurrentVoice is null || !_voiceIDs.ContainsKey(VoiceID))
            {
                return "";
            }

            return $"http://localhost:8876/11labs?message={Uri.EscapeDataString(message)}&voice={_voiceIDs[VoiceID]}";
        }

        private static List<(string Name, string ID)> voices = ElevenLabsAPI.GetVoices();

        static ElevenlabsSpeechManager()
        {
            SettingsManager.Instance.PropertyChanged += (sender, property) =>
            {
                if (property.PropertyName == nameof(SettingsManager.Instance.ElevenLabsAPIKey))
                {
                    voices = ElevenLabsAPI.GetVoices();
                }
            };
        }

        public ElevenlabsSpeechManager() { }

        private static ElevenlabsSpeechManager instance = new();
        public static ElevenlabsSpeechManager Instance => instance;
    }
}
