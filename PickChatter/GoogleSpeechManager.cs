using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class GoogleSpeechManager : StreamElementsSpeechManager
    {
        private readonly Dictionary<string, string> voices = new()
        {
            { "Zoe",      "en-AU-Standard-A" },
            { "Luke",     "en-AU-Standard-B" },
            { "Samantha", "en-AU-Wavenet-A" },
            { "Steve",    "en-AU-Wavenet-B" },
            { "Courtney", "en-AU-Wavenet-C" },
            { "Jayden",   "en-AU-Wavenet-D" },
            { "Ashleigh", "en-AU-Standard-C" },
            { "Daniel",   "en-AU-Standard-D" },
            { "Layla",    "en-GB-Standard-A" },
            { "Ali",      "en-GB-Standard-B" },
            { "Scarlett", "en-GB-Standard-C" },
            { "Oliver",   "en-GB-Standard-D" },
            { "Bella",    "en-GB-Wavenet-A" },
            { "John",     "en-GB-Wavenet-B" },
            { "Victoria", "en-GB-Wavenet-C" },
            { "Ron",      "en-GB-Wavenet-D" },
            { "Anushri",  "en-IN-Wavenet-A" },
            { "Sundar",   "en-IN-Wavenet-B" },
            { "Satya",    "en-IN-Wavenet-C" },
            { "Carter",   "en-US-Wavenet-A" },
            { "Paul",     "en-US-Wavenet-B" },
            { "Evelyn",   "en-US-Wavenet-C" },
            { "Liam",     "en-US-Wavenet-D" },
            { "Jasmine",  "en-US-Wavenet-E" },
            { "Madison",  "en-US-Wavenet-F" },
            { "Mark",     "en-US-Standard-B" },
            { "Vanessa",  "en-US-Standard-C" },
            { "Zachary",  "en-US-Standard-D" },
            { "Audrey",   "en-US-Standard-E" }
        };

        public override List<string> AvailableVoices => voices.Keys.ToList();

        public override string CurrentVoice => SettingsManager.Instance.GoogleVoice;

        protected override string VoiceID => voices[CurrentVoice];

        private GoogleSpeechManager() { }

        private static GoogleSpeechManager instance = new();
        public static GoogleSpeechManager Instance => instance;
    }
}
