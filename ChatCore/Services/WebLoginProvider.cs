﻿using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ChatCore.Services
{
    public class WebLoginProvider : IWebLoginProvider
    {
	    private readonly ILogger _logger;
	    private readonly IUserAuthProvider _authManager;
	    private readonly MainSettingsProvider _settings;

	    private HttpListener? _listener;
	    private CancellationTokenSource? _cancellationToken;
	    private static string? _pageData;

	    private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

	    public WebLoginProvider(ILogger<WebLoginProvider> logger, IUserAuthProvider authManager, MainSettingsProvider settings)
        {
            _logger = logger;
            _authManager = authManager;
            _settings = settings;
        }

	    public void Start()
        {
            if (_pageData == null)
            {
	            using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ChatCore.Resources.Web.index.html")!);
	            _pageData = reader.ReadToEnd();
	            //_logger.LogInformation($"PageData: {pageData}");
            }

            if (_listener != null)
            {
	            return;
            }

            _cancellationToken = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_settings.WebAppPort}/");
            Task.Run(() =>
            {
	            _listener.Start();
	            _listener.BeginGetContext(OnContext, null);
            });
        }

        private async void OnContext(IAsyncResult res)
        {
            var ctx = _listener!.EndGetContext(res);
            _listener.BeginGetContext(OnContext, null);

            _logger.LogWarning("Request received");

            await _requestLock.WaitAsync();
            try
            {
                var req = ctx.Request;
                var resp = ctx.Response;

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/submit")
                {
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        var postStr = await reader.ReadToEndAsync();
                        var twitchChannels = new List<string>();

                        var postDict = new Dictionary<string, string>();
                        foreach (var postData in postStr.Split('&'))
                        {
                            try
                            {
                                var split = postData.Split('=');
                                postDict[split[0]] = split[1];

                                switch (split[0])
                                {
                                    case "twitch_oauthtoken":
                                        var twitchOauthToken = HttpUtility.UrlDecode(split[1]);
                                        _authManager.Credentials.Twitch_OAuthToken = twitchOauthToken.StartsWith("oauth:") ? twitchOauthToken : !string.IsNullOrEmpty(twitchOauthToken) ? $"oauth:{twitchOauthToken}" : "";
                                        break;
                                    case "twitch_channel":
                                        var twitchChannel = split[1].ToLower();
                                        if (!string.IsNullOrWhiteSpace(twitchChannel) && !_authManager.Credentials.Twitch_Channels.Contains(twitchChannel))
                                        {
                                            _authManager.Credentials.Twitch_Channels.Add(twitchChannel);
                                        }
                                        _logger.LogInformation($"TwitchChannel: {twitchChannel}");
                                        twitchChannels.Add(twitchChannel);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An exception occurred in OnLoginDataUpdated callback");
                            }
                        }
                        foreach (var channel in _authManager.Credentials.Twitch_Channels.ToArray())
                        {
                            // Remove any channels that weren't present in the post data
                            if (!twitchChannels.Contains(channel))
                            {
                                _authManager.Credentials.Twitch_Channels.Remove(channel);
                            }
                        }

                        _authManager.Save();
                        _settings.SetFromDictionary(postDict);
                        _settings.Save();
                    }
                    resp.Redirect(req.UrlReferrer.OriginalString);
                    resp.Close();
                    return;
                }

                var pageBuilder = new StringBuilder(_pageData);
                var twitchChannelHtmlString = new StringBuilder();
                for (var i = 0; i < _authManager.Credentials.Twitch_Channels.Count; i++)
                {
                    var channel = _authManager.Credentials.Twitch_Channels[i];
                    twitchChannelHtmlString.Append($"<span id=\"twitch_channel_{i}\" class=\"chip \"><div style=\"overflow: hidden;text-overflow: ellipsis;\">{channel}</div><input type=\"text\" class=\"form-input\" name=\"twitch_channel\" style=\"display: none; \" value=\"{channel}\" /><button type=\"button\" onclick=\"removeTwitchChannel('twitch_channel_{i}')\" class=\"btn btn-clear\" aria-label=\"Close\" role=\"button\"></button></span>");
                }

                var sectionHtml = _settings.GetSettingsAsHtml();
                pageBuilder.Replace("{WebAppSettingsHTML}", sectionHtml["WebApp"]);
                pageBuilder.Replace("{GlobalSettingsHTML}", sectionHtml["Global"]);
                pageBuilder.Replace("{TwitchSettingsHTML}", sectionHtml["Twitch"]);
                pageBuilder.Replace("{TwitchChannelHtml}", twitchChannelHtmlString.ToString());
                pageBuilder.Replace("{TwitchOAuthToken}", _authManager.Credentials.Twitch_OAuthToken);

                var data = Encoding.UTF8.GetBytes(pageBuilder.ToString());
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during webapp request.");
            }
            finally
            {
                _requestLock.Release();
            }
        }
        public void Stop()
        {
	        if (_cancellationToken is null)
	        {
		        return;
	        }

	        _cancellationToken.Cancel();
	        _logger.LogInformation("Stopped");
        }
    }
}
