using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal sealed class AmazonSpeechManager : StreamElementsSpeechManager
    {
        public override List<string> AvailableVoices => new() {
            "Russell",
            "Nicole",
            "Linda",
            "Heather",
            "Emma",
            "Brian",
            "Amy",
            "Geraint",
            "Sean",
            "Raveena",
            "Aditi",
            "Salli",
            "Matthew",
            "Kimberly",
            "Kendra",
            "Justin",
            "Joey",
            "Joanna",
            "Ivy"
        };

        public override string CurrentVoice => SettingsManager.Instance.AmazonVoice;

        protected override string VoiceID => CurrentVoice;

        private AmazonSpeechManager() { }

        private static AmazonSpeechManager instance = new();
        public static AmazonSpeechManager Instance => instance;
    }
}
