using ChatCore.Interfaces;
using System.Collections.Generic;

namespace ChatCore.Models.Twitch
{
    public class CheermoteTier : IChatResourceData
    {
	    public string Uri { get; internal set; } = null!;
	    public int MinBits { get; internal set; }
	    public string Color { get; internal set; } = null!;
        public bool CanCheer { get; internal set; }
        public bool IsAnimated { get; internal set; } = true;
        public string Type { get; internal set; } = "TwitchCheermote";
    }

    public class TwitchCheermoteData
    {
        public string? Prefix;
        public List<CheermoteTier> Tiers = new List<CheermoteTier>();

        public CheermoteTier GetTier(int numBits)
        {
            for (var i = 1; i < Tiers.Count; i++)
            {
                if (numBits < Tiers[i].MinBits)
                {
	                return Tiers[i - 1];
                }
            }
            return Tiers[0];
        }
    }
}
