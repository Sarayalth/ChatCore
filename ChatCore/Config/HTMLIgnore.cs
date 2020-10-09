using System;
using System.Collections.Generic;
using System.Text;

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
