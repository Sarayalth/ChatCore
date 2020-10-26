using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatCore.Models;
using System.Threading;

namespace ChatCore.Services.Twitch
{
	public class TwitchDataProvider
	{
		internal const string TWITCH_CLIENT_ID = "jg6ij5z8mf8jr8si22i5uq8tobnmde";

		private readonly ILogger _logger;
		private readonly TwitchBadgeProvider _twitchBadgeProvider;
		private readonly TwitchCheermoteProvider _twitchCheermoteProvider;
		private readonly BTTVDataProvider _bttvDataProvider;
		private readonly FFZDataProvider _ffzDataProvider;

		private readonly HashSet<string> _channelDataCached = new HashSet<string>();
		private readonly SemaphoreSlim _globalLock;
		private readonly SemaphoreSlim _channelLock;

		public TwitchDataProvider(ILogger<TwitchDataProvider> logger, TwitchBadgeProvider twitchBadgeProvider, TwitchCheermoteProvider twitchCheermoteProvider, BTTVDataProvider bttvDataProvider,
			FFZDataProvider ffzDataProvider)
		{
			_logger = logger;
			_twitchBadgeProvider = twitchBadgeProvider;
			_twitchCheermoteProvider = twitchCheermoteProvider;
			_bttvDataProvider = bttvDataProvider;
			_ffzDataProvider = ffzDataProvider;

			_globalLock = new SemaphoreSlim(1, 1);
			_channelLock = new SemaphoreSlim(1, 1);
		}

		public void TryRequestGlobalResources()
		{
			Task.Run(async () =>
			{
				await _globalLock.WaitAsync();
				try
				{
					await _twitchBadgeProvider.TryRequestResources(null!);
					await _bttvDataProvider.TryRequestResources(null!);
					await _ffzDataProvider.TryRequestResources(null!);
					//_logger.LogInformation("Finished caching global emotes/badges.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"An exception occurred while trying to request global Twitch resources.");
				}
				finally
				{
					_globalLock.Release();
				}
			});
		}


		public void TryRequestChannelResources(TwitchChannel channel, Action<Dictionary<string, IChatResourceData>> channelResourceDataCached)
		{
			Task.Run(async () =>
			{
				await _channelLock.WaitAsync();
				try
				{
					if (!_channelDataCached.Contains(channel.Id))
					{
						var roomId = channel.Roomstate.RoomId;
						await _twitchBadgeProvider.TryRequestResources(roomId);
						await _twitchCheermoteProvider.TryRequestResources(roomId);
						await _bttvDataProvider.TryRequestResources(channel.Id);
						await _ffzDataProvider.TryRequestResources(channel.Id);

						var ret = new Dictionary<string, IChatResourceData>();
						_twitchBadgeProvider.Resources.ToList().ForEach(x =>
						{
							var parts = x.Key.Split(new[]
							{
								'_'
							}, 2);
							ret[$"{x.Value.Type}_{(parts.Length > 1 ? parts[1] : parts[0])}"] = x.Value;
						});

						_twitchCheermoteProvider.Resources.ToList().ForEach(x =>
						{
							var parts = x.Key.Split(new[]
							{
								'_'
							}, 2);
							foreach (var tier in x.Value.Tiers)
							{
								ret[$"{tier.Type}_{(parts.Length > 1 ? parts[1] : parts[0])}{tier.MinBits}"] = tier;
							}
						});

						_bttvDataProvider.Resources.ToList().ForEach(x =>
						{
							var parts = x.Key.Split(new[]
							{
								'_'
							}, 2);
							ret[$"{x.Value.Type}_{(parts.Length > 1 ? parts[1] : parts[0])}"] = x.Value;
						});

						_ffzDataProvider.Resources.ToList().ForEach(x =>
						{
							var parts = x.Key.Split(new[]
							{
								'_'
							}, 2);
							ret[$"{x.Value.Type}_{(parts.Length > 1 ? parts[1] : parts[0])}"] = x.Value;
						});

						channelResourceDataCached?.Invoke(ret);
						_channelDataCached.Add(channel.Id);
						//_logger.LogInformation($"Finished caching emotes for channel {channel.Id}.");
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"An exception occurred while trying to request Twitch channel resources for {channel.Id}.");
				}
				finally
				{
					_channelLock.Release();
				}
			});
		}

		public async void TryReleaseChannelResources(IChatChannel channel)
		{
			await _channelLock.WaitAsync();
			try
			{
				// TODO: readd a way to actually clear channel resources
				_logger.LogInformation($"Releasing resources for channel {channel.Id}");
				_channelDataCached.Remove(channel.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"An exception occurred while trying to release Twitch channel resources for {channel.Id}.");
			}
			finally
			{
				_channelLock.Release();
			}
		}


		internal bool TryGetThirdPartyEmote(string word, string channel, out ChatResourceData data)
		{
			if (_bttvDataProvider.TryGetResource(word, channel, out data))
			{
				return true;
			}

			if (_ffzDataProvider.TryGetResource(word, channel, out data))
			{
				return true;
			}

			data = null!;
			return false;
		}

		internal bool TryGetCheermote(string word, string roomId, out TwitchCheermoteData data, out int numBits)
		{
			numBits = 0;
			data = null!;
			if (string.IsNullOrEmpty(roomId))
			{
				return false;
			}

			if (!char.IsLetter(word[0]) || !char.IsDigit(word[word.Length - 1]))
			{
				return false;
			}

			var prefixLength = -1;
			for (var i = word.Length - 1; i > 0; i--)
			{
				if (char.IsDigit(word[i]))
				{
					continue;
				}

				prefixLength = i + 1;
				break;
			}

			if (prefixLength == -1)
			{
				return false;
			}

			var prefix = word.Substring(0, prefixLength).ToLower();
			return _twitchCheermoteProvider.TryGetResource(prefix, roomId, out data) && int.TryParse(word.Substring(prefixLength), out numBits);
		}

		internal bool TryGetBadgeInfo(string badgeId, string roomId, out ChatResourceData badge)
		{
			if (_twitchBadgeProvider.TryGetResource(badgeId, roomId, out badge))
			{
				return true;
			}

			badge = null!;
			return false;
		}
	}
}