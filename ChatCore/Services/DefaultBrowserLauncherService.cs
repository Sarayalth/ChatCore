using ChatCore.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChatCore.Services
{
    public class ProcessDotStartBrowserLauncherService : IDefaultBrowserLauncherService
    {
        public ProcessDotStartBrowserLauncherService(ILogger<ProcessDotStartBrowserLauncherService> logger)
        {
            _logger = logger;
        }
        private ILogger _logger;

        public void Launch(string uri)
        {
            Process.Start(uri);
        }
    }
}
