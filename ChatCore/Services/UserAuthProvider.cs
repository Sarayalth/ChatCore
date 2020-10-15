using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models;
using System;
using System.IO;
using ChatCore.Config;
using System.Threading.Tasks;

namespace ChatCore.Services
{
	internal class OldStreamCoreConfig
    {
        public string? TwitchChannelName { get; set; }
        public string? TwitchUsername { get; set; }
        public string? TwitchOAuthToken { get; set; }
    }

    public class UserAuthProvider : IUserAuthProvider
    {
	    private readonly string _credentialsPath;
	    private readonly ObjectSerializer _credentialSerializer;

        public event Action<LoginCredentials>? OnCredentialsUpdated;

        public LoginCredentials Credentials { get; } = new LoginCredentials();

        // If this is set, old StreamCore config data will be read in from this file.
        internal static string OldConfigPath = null!;

        public UserAuthProvider(ILogger<UserAuthProvider> logger, IPathProvider pathProvider)
        {
	        _credentialsPath = Path.Combine(pathProvider.GetDataPath(), "auth.ini");
            _credentialSerializer = new ObjectSerializer();
            _credentialSerializer.Load(Credentials, _credentialsPath);

            Task.Delay(1000).ContinueWith(_ =>
            {
                if (!string.IsNullOrEmpty(OldConfigPath) && File.Exists(OldConfigPath))
                {
	                logger.LogInformation($"Trying to convert old StreamCore config at path {OldConfigPath}");
                    var old = new OldStreamCoreConfig();
                    _credentialSerializer.Load(old, OldConfigPath);

                    if (!string.IsNullOrEmpty(old.TwitchChannelName))
                    {
                        var oldName = old.TwitchChannelName?.ToLower().Replace(" ", "");
                        if (oldName != null && !Credentials.Twitch_Channels.Contains(oldName))
                        {
                            Credentials.Twitch_Channels.Add(oldName);
                            logger.LogInformation($"Added channel {oldName} from old StreamCore config.");
                        }
                    }

                    if (!string.IsNullOrEmpty(old.TwitchOAuthToken))
                    {
                        Credentials.Twitch_OAuthToken = old.TwitchOAuthToken!;
                        logger.LogInformation($"Pulled in old Twitch auth info from StreamCore config.");
                    }

                    var convertedPath = OldConfigPath + ".converted";
                    try
                    {
                        if (!File.Exists(convertedPath))
                        {
                            File.Move(OldConfigPath, convertedPath);
                        }
                        else
                        {
                            File.Delete(OldConfigPath);
                        }
                    }
                    catch (Exception ex)
                    {
	                    logger.LogWarning(ex, "An exception occurred while trying to yeet old StreamCore config!");
                    }
                }
            });
        }

        public void Save(bool callback = true)
        {
            _credentialSerializer.Save(Credentials, _credentialsPath);
            if (callback)
            {
                OnCredentialsUpdated?.Invoke(Credentials);
            }
        }
    }
}
