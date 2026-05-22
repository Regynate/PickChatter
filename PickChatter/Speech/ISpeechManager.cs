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

        public string CurrentVoice { get; set; }

        public void Speak(string id, string message);

        public void Stop(string id);

        public event EventHandler<EventArgs>? StateChanged;
    }
}
