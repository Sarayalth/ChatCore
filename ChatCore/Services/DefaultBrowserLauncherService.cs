using ChatCore.Interfaces;
using System.Diagnostics;

namespace ChatCore.Services
{
    public class ProcessDotStartBrowserLauncherService : IDefaultBrowserLauncherService
    {
	    public void Launch(string uri)
        {
            Process.Start(uri);
        }
    }
}
