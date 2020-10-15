using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ChatCore.Services
{
    public class ChatServiceManager : IChatServiceManager, IDisposable
    {

	    private readonly ILogger _logger;
	    private readonly IList<IChatServiceManager> _streamServiceManagers;
	    private readonly IChatService _streamingService;

        public bool IsRunning { get; } = false;

        public HashSet<Assembly> RegisteredAssemblies
        {
            get
            {
                var assemblies = new HashSet<Assembly>();
                foreach(var service in _streamServiceManagers)
                {
                    assemblies.UnionWith(service.RegisteredAssemblies);
                }
                return assemblies;
            }
        }

        public ChatServiceManager(ILogger<ChatServiceManager> logger, IChatService streamingService, IList<IChatServiceManager> streamServiceManagers)
        {
	        _logger = logger;
	        _streamingService = streamingService;
	        _streamServiceManagers = streamServiceManagers;
        }

        public void Start(Assembly assembly)
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Start(assembly);
            }
            _logger.LogInformation($"Streaming services have been started");
        }

        public void Stop(Assembly assembly)
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Stop(assembly);
            }
            _logger.LogInformation($"Streaming services have been stopped");
        }

        public void Dispose()
        {
            foreach(var service in _streamServiceManagers)
            {
                service.Stop(null!);
            }
            _logger.LogInformation("Disposed");
        }

        public IChatService GetService()
        {
            return _streamingService;
        }
    }
}
