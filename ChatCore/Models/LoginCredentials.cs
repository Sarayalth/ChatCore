using ChatCore.Config;
using System.Collections.Generic;

namespace ChatCore.Models
{
    public class LoginCredentials
    {
        [ConfigSection("Twitch")]
        [ConfigMeta(Comment = "The OAuth token associated with your Twitch account. Grab it from https://twitchapps.com/tmi/")]
        public string Twitch_OAuthToken = "";
        public List<string> Twitch_Channels = new List<string>();
    }
}
