using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Chat.Emotes;

namespace PickChatter
{
    internal class EmoteToken : IMessageToken
    {
        private string url;

        public object ToJsonObject()
        {
            return new { type = "emote", content = url };
        }

        public EmoteToken(string url)
        {
            this.url = url;
        }
    }
}
