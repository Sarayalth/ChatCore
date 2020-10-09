using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ChatCore.Utilities;

namespace ChatCore.Services.Twitch
{
    public class TwitchCheermoteProvider : IChatResourceProvider<TwitchCheermoteData>
    {
        public ConcurrentDictionary<string, TwitchCheermoteData> Resources { get; } = new ConcurrentDictionary<string, TwitchCheermoteData>();
        public TwitchCheermoteProvider(ILogger<TwitchCheermoteProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private ILogger _logger;
        private HttpClient _httpClient;

        public async Task<bool> TryRequestResources(string category)
        {
            var isGlobal = string.IsNullOrEmpty(category);
            try
            {
                _logger.LogDebug($"Requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : $" for channel {category}")}.");
                using (var msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/v5/bits/actions?client_id={TwitchDataProvider.TWITCH_CLIENT_ID}&channel_id={category}&include_sponsored=1"))
                {
                    var resp = await _httpClient.SendAsync(msg);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Unsuccessful status code when requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}. {resp.ReasonPhrase}");
                        return false;
                    }
                    var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (!json["actions"].IsArray)
                    {
                        _logger.LogError("badge_sets was not an object.");
                        return false;
                    }
                    var count = 0;
                    foreach (var node in json["actions"].AsArray.Values)
                    {
                        //_logger.LogInformation($"Cheermote: {node.ToString()}");
                        var cheermote = new TwitchCheermoteData();
                        var prefix = node["prefix"].Value.ToLower();
                        foreach (var tier in node["tiers"].Values)
                        {
                            var newTier = new CheermoteTier();
                            newTier.MinBits = tier["min_bits"].AsInt;
                            newTier.Color = tier["color"].Value;
                            newTier.CanCheer = tier["can_cheer"].AsBool;
                            newTier.Uri = tier["images"]["dark"]["animated"]["4"].Value;
                            //_logger.LogInformation($"Cheermote: {prefix}{newTier.MinBits}, URI: {newTier.Uri}");
                            cheermote.Tiers.Add(newTier);
                        }
                        cheermote.Prefix = prefix;
                        cheermote.Tiers = cheermote.Tiers.OrderBy(t => t.MinBits).ToList();

                        var identifier = isGlobal ? prefix : $"{category}_{prefix}";
                        Resources[identifier] = cheermote;
                        count++;
                    }
                    _logger.LogDebug($"Success caching {count} Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}.");
            }
            return false;
        }

        public bool TryGetResource(string identifier, string category, out TwitchCheermoteData data)
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
