using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FileArchiver.Plugins
{
    internal class PluginsConfiguration
    {
        #region Private members
        
        private readonly Dictionary<PluginType, Dictionary<string, PluginSettings>> _pluginsSettings;

        private readonly ILogger _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseFolder">Application base path</param>
        /// <param name="pluginsFolder">Plugin folder path</param>
        /// <param name="logger">Logger</param>
        public PluginsConfiguration(string baseFolder, string pluginsFolder, ILogger logger)
        {
            BaseFolder = baseFolder;
            PluginsFolder = !string.IsNullOrEmpty(pluginsFolder) ? Path.GetFullPath(pluginsFolder) : null;
            _logger = logger;

            _pluginsSettings = LoadPluginsSettingsFromConfigFiles();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get plugin settings
        /// </summary>
        /// <param name="type">Plugin type</param>
        /// <param name="name">Plugin name</param>
        /// <returns>Returns plugin settings</returns>
        public PluginSettings GetPluginSettings(PluginType type, string name)
        {
            if (!_pluginsSettings.ContainsKey(type))
                throw new PluginException($"Plugins of type {type} doesn't exists.");

            var pluginSettings = _pluginsSettings[type];
            if (!pluginSettings.ContainsKey(name))
                throw new PluginException($"Plugin of type {type} with name {name} does not exists.");

            return pluginSettings[name];
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Load plugins settings from config files
        /// </summary>
        private Dictionary<PluginType, Dictionary<string, PluginSettings>> LoadPluginsSettingsFromConfigFiles()
        {
            _logger.Debug("Loading plugins...");

            // Initialize plugin settings dictionary
            var pluginsSettings = new Dictionary<PluginType, Dictionary<string, PluginSettings>>();

            // Add all paths in which search for plugins
            var searchFolders = new List<string> { BaseFolder };
            if (!string.IsNullOrEmpty(PluginsFolder))
                searchFolders.Add(PluginsFolder);

            // For each path to search...
            foreach (var folder in searchFolders)
            {
                _logger.Debug($"Loading plugins from directory {folder}...");
                if (!Directory.Exists(folder))
                {
                    _logger.Warning($"Plugin directory {folder} does not exists or is unreachable.");
                    continue;
                }

                // For each .json file found...
                foreach (var configFileName in Directory.EnumerateFiles(folder, "*.json"))
                {
                    _logger.Debug($"Found {configFileName} file.");

                    // For each plugin to register...
                    foreach (var pluginSettings in EnumeratePluginsSettingsConfiguration(configFileName))
                    {
                        // Check if plugin settings are valid
                        if (!IsPluginSettingsValid(pluginSettings))
                            throw new PluginException($"Plugins settings file {configFileName} is invalid.");

                        _logger.Debug($"Loading {pluginSettings.Type} plugin {pluginSettings.Name}...");

                        // Add or get plugin dictionary
                        Dictionary<string, PluginSettings> pluginSettingDictionary;
                        if (pluginsSettings.ContainsKey(pluginSettings.Type))
                        {
                            pluginSettingDictionary = pluginsSettings[pluginSettings.Type];
                        }
                        else
                        {
                            pluginSettingDictionary = new Dictionary<string, PluginSettings>();
                            pluginsSettings.Add(pluginSettings.Type, pluginSettingDictionary);
                        }

                        // Check if dictionary doesn't have more plugins with the same name
                        if (pluginSettingDictionary.ContainsKey(pluginSettings.Name))
                            throw new PluginException($"Plugin of type {pluginSettings.Type} with name {pluginSettings.Name} was already defined.");

                        // Add plugin to the dictionary
                        pluginSettingDictionary.Add(pluginSettings.Name, pluginSettings);
                    }
                }
            }

            _logger.Debug("Plugins loaded.");

            return pluginsSettings;
        }

        /// <summary>
        /// Check if plugin settings are valid
        /// </summary>
        /// <param name="pluginSettings">Plugin settings to check</param>
        /// <returns>Returns true if plugin settings are valid. Otherwise returns false.</returns>
        private bool IsPluginSettingsValid(PluginSettings pluginSettings)
        {
            return pluginSettings.Type != PluginType.Unknown &&
                !string.IsNullOrEmpty(pluginSettings.Name) &&
                !string.IsNullOrEmpty(pluginSettings.AssemblyName) &&
                !string.IsNullOrEmpty(pluginSettings.ClassName);
        }

        /// <summary>
        /// Enumerate plugins settings present in the configuration file
        /// </summary>
        /// <param name="configFileName">Configuration file name</param>
        private IEnumerable<PluginSettings> EnumeratePluginsSettingsConfiguration(string configFileName)
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile(configFileName)
                    .Build();

            foreach (var configSection in config.GetSection("Plugins").GetChildren())
            {
                var pluginSettings = new PluginSettings();
                configSection.Bind(pluginSettings);

                yield return pluginSettings;
            }
        }

        #endregion

        #region Properties

        public string BaseFolder { get; }

        public string PluginsFolder { get; }

        #endregion
    }
}
