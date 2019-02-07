using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Plugins
{
    public enum PluginType
    {
        Unknown = 0,
        Archive,
        Storage
    }

    public class PluginSettings
    {
        public PluginType Type { get; set; }

        public string Name { get; set; }

        public string AssemblyName { get; set; }

        public string ClassName { get; set; }
    }
}
