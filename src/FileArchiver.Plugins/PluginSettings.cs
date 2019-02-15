using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Plugins
{
    public class PluginSettings
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public Type SettingsType { get; set; }
    }
}
