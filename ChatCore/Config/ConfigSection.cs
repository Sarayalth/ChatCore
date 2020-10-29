using System;

namespace ChatCore.Config
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigSection : Attribute
    {
        public string Name;
        public ConfigSection(string name)
        {
            Name = name;
        }
    }
}
