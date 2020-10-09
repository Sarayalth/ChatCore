using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Config
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigHeader : Attribute
    {
        public string[] Comment;
        public ConfigHeader(params string[] comment)
        {
            Comment = comment;
        }
    }
}
