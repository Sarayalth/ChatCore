using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Config
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigMeta : Attribute
    {
        public string Comment;
        public ConfigMeta()
        {
            Comment = null;
        }
    }
}
