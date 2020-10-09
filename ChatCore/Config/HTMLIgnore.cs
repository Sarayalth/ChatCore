using System;

namespace ChatCore.Config
{
    [AttributeUsage(AttributeTargets.Field)]
    public class HTMLIgnore : Attribute
    {
        public HTMLIgnore()
        {
        }
    }
}
