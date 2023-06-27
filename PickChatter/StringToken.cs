using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickChatter
{
    internal class StringToken : IMessageToken
    {
        private string content;

        public object ToJsonObject()
        {
            return new { type = "string", content };
        }

        public StringToken(string content)
        {
            this.content = content;
        }
    }
}
