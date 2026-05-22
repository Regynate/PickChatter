using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Speech.AudioFormat;
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
            set => synthesizer.SelectVoice(value);
        }

        public void Speak(string id, string message)
        {
            synthesizer.SpeakAsync(message);
        }

        public void Stop(string id)
        {
            synthesizer.SpeakAsyncCancelAll();
        }

        public event EventHandler<EventArgs>? StateChanged;

        public MicrosoftSpeechManager()
        {
            synthesizer.StateChanged += (_, args) =>
            {
                SpeechSpeaking = args.State == SynthesizerState.Speaking;
                StateChanged?.Invoke(this, new());
            };

            synthesizer.SetOutputToDefaultAudioDevice();
        }

        private static readonly MicrosoftSpeechManager instance = new();
        public static MicrosoftSpeechManager Instance => instance;
    }
}
