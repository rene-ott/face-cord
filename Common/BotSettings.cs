using System.Collections.Generic;

namespace FaceCord.Common
{
    public class BotSettings
    {
        public string Name { get; set; }
        public IList<string> Channels { get; set; } = new List<string>();
        public FacebookSettings Facebook { get; set; }
        public DiscordSettings Discord { get; set; }

        public class FacebookSettings
        {
            public string Login { get; set; }
            public string Password { get; set; }
            public string User { get; set; }
        }

        public class DiscordSettings
        {
            public string Token { get; set; }
        }
    }
}
