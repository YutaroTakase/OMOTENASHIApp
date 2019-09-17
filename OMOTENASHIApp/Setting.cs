using System.Collections.Generic;
using System.Linq;

namespace OMOTENASHIApp
{
    public class Setting
    {
        public string Token { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();

        public class Message
        {
            public string User { get; set; }
            public List<string> Messages { get; set; } = new List<string>();
        }

        public Dictionary<string, List<string>> GetMessages() => Messages.ToDictionary(x => x.User, x => x.Messages);
    }
}
