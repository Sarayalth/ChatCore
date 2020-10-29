using ChatCore.Interfaces;
using System;
using System.IO;

namespace ChatCore.Services
{
    public class PathProvider : IPathProvider
    {
        public string GetDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".chatcore");
        }

        public string GetResourcePath()
        {
            return Path.Combine(GetDataPath(), "resources");
        }
    }
}
