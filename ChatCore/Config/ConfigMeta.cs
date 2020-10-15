using System;

namespace ChatCore.Config
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigMeta : Attribute
    {
        public string Comment;
        public ConfigMeta()
        {
            Comment = null!;
        }
    }
}
