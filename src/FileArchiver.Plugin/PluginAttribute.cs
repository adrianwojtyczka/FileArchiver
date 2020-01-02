using System;

namespace FileArchiver.Plugin
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class PluginAttribute : Attribute
    {
        public PluginAttribute(string name, Type settingsType = null)
        {
            Name = name;
            SettingsType = settingsType;
        }

        public string Name { get; }

        public Type SettingsType { get; }
    }
}
