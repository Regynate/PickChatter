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

        protected override string? VoiceID => _voiceIDs.TryGetValue(CurrentVoice, out string? value) ? value : null;

        protected override string GetAudioUrl(string message)
        {
            if (CurrentVoice is null)
            {
                return "";
            }

            return $"http://localhost:8876/11labs?message={Uri.EscapeDataString(message)}&voice={VoiceID}";
        }

        public string GetVoiceByID(string voiceID)
        {
            if (_voiceIDs.ContainsValue(voiceID))
            {
                return _voiceIDs.FirstOrDefault(x => x.Value == voiceID).Key;
            }

            return "";
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
