using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    class ChatterSettings
    {
        public string ID { get; set; } = "";
        public VoiceSettings VoiceSettings { get; set; } = new();
    }
}
