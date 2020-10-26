using ChatCore.Config;
using System.Collections.Generic;

namespace ChatCore.Models
{
    public class LoginCredentials
    {
        [ConfigSection("Twitch")]
        [ConfigMeta(Comment = "The OAuth token associated with your Twitch account. Grab it from https://twitchapps.com/tmi/")]
        // ReSharper disable InconsistentNaming
        public string Twitch_OAuthToken = "";
        public readonly List<string> Twitch_Channels = new List<string>();
        // ReSharper restore InconsistentNaming
    }
}
