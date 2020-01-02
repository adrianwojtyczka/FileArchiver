using System;

namespace FileArchiver.Plugins
{
    internal class PluginSettings
    {
        /// <summary>
        /// Plugin name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Plugin type
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Plugin settings type
        /// </summary>
        public Type SettingsType { get; set; }
    }
}
