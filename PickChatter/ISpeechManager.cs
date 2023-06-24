using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal interface ISpeechManager
    {
        public bool SpeechSpeaking { get; }

        public List<string> AvailableVoices { get; }

        public string CurrentVoice { get; }

        public void Speak(string message);

        public void Stop();

        public event EventHandler<EventArgs>? StateChanged;
    }
}
