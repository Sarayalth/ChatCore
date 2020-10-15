using ChatCore.Interfaces;
using ChatCore.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using ChatCore.Utilities;

namespace ChatCore.Services.Twitch
{
	public class TwitchBadgeProvider : IChatResourceProvider<ChatResourceData>
	{
		private readonly ILogger _logger;
		private readonly HttpClient _httpClient;

		public TwitchBadgeProvider(ILogger<TwitchBadgeProvider> logger, HttpClient httpClient)
		{
			_logger = logger;
			_httpClient = httpClient;
		}

		public ConcurrentDictionary<string, ChatResourceData?> Resources { get; } = new ConcurrentDictionary<string, ChatResourceData?>();

		public async Task<bool> TryRequestResources(string category)
		{
			var isGlobal = string.IsNullOrEmpty(category);
			try
			{
				_logger.LogDebug($"Requesting Twitch {(isGlobal ? "global " : "")}badges{(isGlobal ? "." : $" for channel {category}")}.");
				using var msg = new HttpRequestMessage(HttpMethod.Get,
					isGlobal ? $"https://badges.twitch.tv/v1/badges/global/display" : $"https://badges.twitch.tv/v1/badges/channels/{category}/display");
				var resp = await _httpClient.SendAsync(msg);
				if (!resp.IsSuccessStatusCode)
				{
					_logger.LogError($"Unsuccessful status code when requesting Twitch {(isGlobal ? "global " : "")}badges{(isGlobal ? "." : " for channel " + category)}. {resp.ReasonPhrase}");
					return false;
				}

				var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
				if (!json["badge_sets"].IsObject)
				{
					_logger.LogError("badge_sets was not an object.");
					return false;
				}

				var count = 0;
				foreach (var kvp in json["badge_sets"])
				{
					var badgeName = kvp.Key;
					foreach (var version in kvp.Value.AsObject["versions"].AsObject)
					{
						var badgeVersion = version.Key;
						var finalName = $"{badgeName}{badgeVersion}";
						var uri = version.Value.AsObject["image_url_4x"].Value;
						//_logger.LogInformation($"Global Badge: {finalName}, URI: {uri}");
						var identifier = isGlobal ? finalName : $"{category}_{finalName}";
						Resources[identifier] = new ChatResourceData() {Uri = uri, IsAnimated = false, Type = isGlobal ? "TwitchGlobalBadge" : "TwitchChannelBadge"};
						count++;
					}
				}

				_logger.LogDebug($"Success caching {count} Twitch {(isGlobal ? "global " : "")}badges{(isGlobal ? "." : " for channel " + category)}.");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An error occurred while requesting Twitch {(isGlobal ? "global " : "")}badges{(isGlobal ? "." : " for channel " + category)}.");
			}

			return false;
		}

		public bool TryGetResource(string identifier, string category, out ChatResourceData? data)
		{
			if (!string.IsNullOrEmpty(category) && Resources.TryGetValue($"{category}_{identifier}", out data))
			{
				return true;
			}

			if (Resources.TryGetValue(identifier, out data))
			{
				return true;
			}

			data = null;
			return false;
		}
	}
}