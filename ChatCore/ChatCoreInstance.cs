﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChatCore.Exceptions;
using ChatCore.Interfaces;
using ChatCore.Services;
using ChatCore.Services.Twitch;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Http;
using ChatCore.Config;
using ChatCore.Logging;

namespace ChatCore
{
    public class ChatCoreInstance
    {
        private static readonly object CreateLock = new object();

        private readonly object _runLock = new object();

        private static ChatCoreInstance? _instance;
        private static ServiceProvider? _serviceProvider;

        private ChatCoreInstance() { }

        public event Action<CustomLogLevel, string, string>? OnLogReceived;


        internal void OnLogReceivedInternal(CustomLogLevel level, string category, string message)
        {
            OnLogReceived?.Invoke(level, category, message);
        }

        public static ChatCoreInstance Create()
        {
            lock (CreateLock)
            {
	            if (_instance is not null)
	            {
		            return _instance;
	            }

	            _instance = new ChatCoreInstance();
                var serviceCollection = new ServiceCollection();
                serviceCollection
	                .AddLogging(builder =>
	                {
#if DEBUG
		                builder.AddConsole();
#endif
		                builder.AddProvider(new CustomSinkProvider(_instance));
	                })
	                .AddSingleton<Random>()
	                .AddSingleton<HttpClient>()
	                .AddSingleton<ObjectSerializer>()
	                .AddSingleton<MainSettingsProvider>()
	                .AddSingleton<TwitchService>()
	                .AddSingleton<TwitchServiceManager>()
	                .AddSingleton<TwitchMessageParser>()
	                .AddSingleton<TwitchDataProvider>()
	                .AddSingleton<TwitchCheermoteProvider>()
	                .AddSingleton<TwitchBadgeProvider>()
	                .AddSingleton<BTTVDataProvider>()
	                .AddSingleton<FFZDataProvider>()
	                .AddSingleton<IChatService>(x =>
		                new ChatServiceMultiplexer(
			                x.GetService<ILogger<ChatServiceMultiplexer>>(),
			                new List<IChatService>
			                {
				                x.GetService<TwitchService>()
			                }
		                )
	                )
	                .AddSingleton<IChatServiceManager>(x =>
		                new ChatServiceManager(
			                x.GetService<ILogger<ChatServiceManager>>(),
			                x.GetService<IChatService>(),
			                new List<IChatServiceManager>
			                {
				                x.GetService<TwitchServiceManager>()
			                }
		                )
	                )
	                .AddSingleton<IPathProvider, PathProvider>()
	                .AddSingleton<IUserAuthProvider, UserAuthProvider>()
	                .AddSingleton<IWebLoginProvider, WebLoginProvider>()
	                .AddSingleton<IEmojiParser, FrwTwemojiParser>()
	                .AddSingleton<IDefaultBrowserLauncherService, ProcessDotStartBrowserLauncherService>()
	                .AddTransient<IWebSocketService, WebSocket4NetServiceProvider>();
                _serviceProvider = serviceCollection.BuildServiceProvider();

                var settings = _serviceProvider.GetService<MainSettingsProvider>();
                if (settings.DisableWebApp)
                {
	                return _instance;
                }

                _serviceProvider.GetService<IWebLoginProvider>().Start();
                if (settings.LaunchWebAppOnStartup)
                {
	                _serviceProvider.GetService<IDefaultBrowserLauncherService>().Launch($"http://localhost:{_serviceProvider.GetService<MainSettingsProvider>().WebAppPort}");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Starts all services if they haven't been already.
        /// </summary>
        /// <returns>A reference to the generic service multiplexer</returns>
        public ChatServiceMultiplexer RunAllServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new ChatCoreNotInitializedException("Make sure to call ChatCoreInstance.Create() to initialize ChatCore!");
                }

                var services = _serviceProvider.GetService<IChatServiceManager>();
                services.Start(Assembly.GetCallingAssembly());
                return (ChatServiceMultiplexer) services.GetService();
            }
        }

        /// <summary>
        /// Stops all services as long as no references remain. Make sure to unregister any callbacks first!
        /// </summary>
        public void StopAllServices()
        {
            lock (_runLock)
            {
                _serviceProvider.GetService<IChatServiceManager>().Stop(Assembly.GetCallingAssembly());
            }
        }

        /// <summary>
        /// Starts the Twitch services if they haven't been already.
        /// </summary>
        /// <returns>A reference to the Twitch service</returns>
        public TwitchService RunTwitchServices()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new ChatCoreNotInitializedException("Make sure to call ChatCoreInstance.Create() to initialize ChatCore!");
                }

                var twitch = _serviceProvider.GetService<TwitchServiceManager>();
                twitch.Start(Assembly.GetCallingAssembly());
                return (TwitchService) twitch.GetService();
            }
        }

        /// <summary>
        /// Stops the Twitch services as long as no references remain. Make sure to unregister any callbacks first!
        /// </summary>
        public void StopTwitchServices()
        {
            lock (_runLock)
            {
                _serviceProvider.GetService<TwitchServiceManager>().Stop(Assembly.GetCallingAssembly());
            }
        }

        /// <summary>
        /// Launches the settings WebApp in the users default browser.
        /// </summary>
        public void LaunchWebApp()
        {
            lock (_runLock)
            {
                if (_serviceProvider == null)
                {
                    throw new ChatCoreNotInitializedException("Make sure to call ChatCoreInstance.Create() to initialize ChatCore!");
                }

                _serviceProvider.GetService<IDefaultBrowserLauncherService>().Launch($"http://localhost:{_serviceProvider.GetService<MainSettingsProvider>().WebAppPort}");
            }
        }
    }
}
