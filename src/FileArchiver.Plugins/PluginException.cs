using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Plugins
{
    public class PluginException : Exception
    {
        public PluginException()
        { }
        
        public PluginException(string message)
            : base(message)
        { }
        
        public PluginException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
