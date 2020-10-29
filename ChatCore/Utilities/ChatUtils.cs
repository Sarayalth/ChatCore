using System;
using System.Collections.Concurrent;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using ChatCore.Services.Twitch;

namespace ChatCore.Utilities
{
	public static class ChatUtils
	{
		private static readonly ConcurrentDictionary<int, string> UserColors = new ConcurrentDictionary<int, string>();

		public static TwitchService? AsTwitchService(this IChatService svc)
		{
			return svc as TwitchService;
		}

		public static TwitchMessage? AsTwitchMessage(this IChatMessage msg)
		{
			return msg as TwitchMessage;
		}

		public static TwitchChannel? AsTwitchChannel(this IChatChannel channel)
		{
			return channel as TwitchChannel;
		}

		public static TwitchUser? AsTwitchUser(this IChatUser user)
		{
			return user as TwitchUser;
		}

		public static TwitchBadge? AsTwitchBadge(this IChatBadge badge)
		{
			return badge as TwitchBadge;
		}

		public static TwitchEmote? AsTwitchEmote(this IChatEmote emote)
		{
			return emote as TwitchEmote;
		}

		public static string GetNameColor(string name)
		{
			var nameHash = name.GetHashCode();
			if (UserColors.TryGetValue(nameHash, out var nameColor))
			{
				return nameColor;
			}

			// Generate a psuedo-random color based on the users display name
			var rand = new Random(nameHash);
			var argb = (rand.Next(255) << 16) + (rand.Next(255) << 8) + rand.Next(255);
			var colorString = $"#{argb:X6}FF";
			UserColors.TryAdd(nameHash, colorString);
			nameColor = colorString;
			return nameColor;
		}
	}
}