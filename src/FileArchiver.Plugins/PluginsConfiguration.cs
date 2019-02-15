using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using FileArchiver.Archive;
using FileArchiver.Plugin;
using FileArchiver.Storage;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FileArchiver.Plugins
{
    internal class PluginsConfiguration
    {
        #region Constants

        private const string PluginsFolder = "Plugins";

        #endregion

        #region Private members

        private readonly Dictionary<Type, Dictionary<string, PluginSettings>> _pluginsSettings;

        private readonly List<Type> _pluginTypeInterfaces;

        private readonly ILogger _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        public PluginsConfiguration(ILogger logger)
        {
            _logger = logger;

            _pluginTypeInterfaces = GetPluginsTypeInterface();
            _pluginsSettings = GetPluginsFromAssemblies();
        }

        #endregion

        #region Public methods

        public PluginSettings GetPluginSettings<T>(string name)
        {
            return GetPluginSettings(typeof(T), name);
        }

        /// <summary>
        /// Get plugin settings
        /// </summary>
        /// <param name="type">Plugin type</param>
        /// <param name="name">Plugin name</param>
        /// <returns>Returns plugin settings</returns>
        public PluginSettings GetPluginSettings(Type type, string name)
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
        /// Get all plugins type and the relative interface type
        /// </summary>
        /// <returns></returns>
        private List<Type> GetPluginsTypeInterface()
        {
            return new List<Type>
            {
                typeof(IArchive),
                typeof(IStorage)
            };
        }

        /// <summary>
        /// Get plugins from assemblies in plugins folder
        /// </summary>
        /// <param name="pluginsFolder">Plugins folder</param>
        /// <returns>Returns plugins settings dictionary</returns>
        private Dictionary<Type, Dictionary<string, PluginSettings>> GetPluginsFromAssemblies()
        {
            _logger.Debug("Loading plugins...");

            // Check if the path exists
            if (!Directory.Exists(PluginsFolderFullName))
                throw new PluginException($"Plugin folder {PluginsFolderFullName} does not exists or is unreachable.");


            // Initialize plugins settings dictionary
            var pluginsSettings = new Dictionary<Type, Dictionary<string, PluginSettings>>();
            _pluginTypeInterfaces.ForEach(type => pluginsSettings.Add(type, new Dictionary<string, PluginSettings>()));


            // For each .dll file found...
            foreach (var assemblyFullFileName in Directory.EnumerateFiles(PluginsFolderFullName, "*.dll"))
            {
                var assemblyFileName = new FileInfo(assemblyFullFileName).Name;

                _logger.Debug($"Loading plugins from {assemblyFileName} file...");
                LoadPluginsFromAssembly(assemblyFullFileName, pluginsSettings);
            }

            return pluginsSettings;
        }

        /// <summary>
        /// Load plugins settings from assembly
        /// </summary>
        /// <param name="assemblyFullFileName">Assembly file name to load</param>
        /// <param name="pluginsSettings">Plugins settings dictionary to which add plugins settings</param>
        private void LoadPluginsFromAssembly(string assemblyFullFileName, Dictionary<Type, Dictionary<string, PluginSettings>> pluginsSettings)
        {
            // Load the assembly and get exported types
            var assembly = Assembly.LoadFrom(assemblyFullFileName);
            var exportedTypes = assembly.GetExportedTypes();

            // For each plugin type to load...
            _pluginTypeInterfaces.ForEach(pluginInterfaceType =>
            {
                var pluginsSettingsType = pluginsSettings[pluginInterfaceType];

                // For each eligible plugin type...
                var pluginTypes = exportedTypes.Where(type => PluginTypeFilter(type, pluginInterfaceType));
                foreach (var pluginType in pluginTypes)
                {
                    // Get settings from type
                    var pluginSettings = GetPluginSettingsFromType(pluginType);
                    if (pluginsSettingsType.ContainsKey(pluginSettings.Name))
                        throw new PluginException($"Plugin with name {pluginSettings.Name} was already defined.");

                    // Add the settings to the plugins settings
                    pluginsSettingsType.Add(pluginSettings.Name, pluginSettings);
                }
            });
        }

        /// <summary>
        /// Create plugin settings from its type
        /// </summary>
        /// <param name="pluginType">Plugin type</param>
        /// <param name="type">Type</param>
        /// <returns>Returns plugin settings</returns>
        private static PluginSettings GetPluginSettingsFromType(Type type)
        {
            // Get the PluginAttribute
            var pluginAttribute = type.GetCustomAttribute<PluginAttribute>();
            if (pluginAttribute == null)
                throw new PluginException($"The type {type} doesn't have {nameof(PluginAttribute)} attribute.");

            // Check if the plugin name is defined
            if (string.IsNullOrWhiteSpace(pluginAttribute.Name))
                throw new PluginException($"The name of plugin {type} is empty.");


            // Create and add the plugin settings
            return new PluginSettings
            {
                Name = pluginAttribute.Name,
                Type = type,
                SettingsType = pluginAttribute.SettingsType
            };
        }

        /// <summary>
        /// Filter plugin types
        /// </summary>
        /// <param name="pluginType">Plugin type</param>
        /// <param name="pluginInterfaceType">Plugin interface type</param>
        /// <returns>Return true if the plugin can be used. Otherwise returns false</returns>
        private bool PluginTypeFilter(Type pluginType, Type pluginInterfaceType)
        {
            return !pluginType.IsAbstract &&
                !pluginType.IsInterface &&
                pluginInterfaceType.IsAssignableFrom(pluginType);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns full name for the plugins folder
        /// </summary>
        public string PluginsFolderFullName => Path.Combine(AppContext.BaseDirectory, PluginsFolder);

        #endregion

        #region Assembly resolution events

        /// <summary>
        /// Resolve plugin assembly
        /// </summary>
        /// <returns>Returns plugin assembly, if exists.</returns>
        private Assembly PluginFactory_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyFullPathName = Path.Combine(PluginsFolderFullName, new AssemblyName(args.Name).Name);
            if (!File.Exists(assemblyFullPathName))
                return null;

            return Assembly.LoadFrom(assemblyFullPathName);
        }

        #endregion
    }
}
