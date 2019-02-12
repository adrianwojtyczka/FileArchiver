using FileArchiver.Archive;
using FileArchiver.Storage;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FileArchiver.Plugins
{
    public class PluginFactory
    {
        #region Private members

        /// <summary>
        /// Plugins configuration class
        /// </summary>
        private readonly PluginsConfiguration _pluginsConfiguration;

        private readonly ILogger _logger;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Cnstructor
        /// </summary>
        /// <param name="baseFolder">Base application folder</param>
        /// <param name="pluginsFolder">Plugin folder</param>
        public PluginFactory(string baseFolder, string pluginsFolder, ILogger logger)
        {
            _pluginsConfiguration = new PluginsConfiguration(baseFolder, pluginsFolder, logger);
            _logger = logger;
            
            // Attach to the event for assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += PluginFactory_AssemblyResolve;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get archive plugin
        /// </summary>
        /// <param name="name">Plugin name to instance</param>
        /// <param name="configuration">Configuration of the plugin</param>
        /// <returns>Returns instance of the required Archive plugin</returns>
        public IArchive GetArchive(string name, IConfiguration configuration)
        {
            return (IArchive)GetPlugin(PluginType.Archive, name, typeof(IArchive), typeof(ArchiveSettings), configuration);
        }

        /// <summary>
        /// Get storage plugin
        /// </summary>
        /// <param name="name">Plugin name to instance</param>
        /// <param name="configuration">Configuration of the plugin</param>
        /// <returns>Returns instance of the required Storage plugin</returns>
        public IStorage GetStorage(string name, IConfiguration configurationSection)
        {
            return (IStorage)GetPlugin(PluginType.Storage, name, typeof(IStorage), typeof(StorageSettings), configurationSection);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get plugin
        /// </summary>
        /// <param name="type">Plugin type to instance</param>
        /// <param name="name">Plugin name to instance</param>
        /// <param name="interfaceType">Type of the interface required</param>
        /// <param name="settingsType">Type of the settings required</param>
        /// <param name="configuration">Plugin configuration</param>
        /// <returns>Returns instance of the required plugin</returns>
        private object GetPlugin(PluginType type, string name, Type interfaceType, Type settingsType, IConfiguration configuration)
        {
            // Get plugin settings
            var pluginSettings = _pluginsConfiguration.GetPluginSettings(type, name);

            // Get plugin type
            var pluginType = GetPluginType(interfaceType, pluginSettings);

            // Get plugin constructor
            var pluginCtor = GetPluginConstructor(pluginType, interfaceType, settingsType, pluginSettings);

            // Create settings object
            var settings = CreateSettingsObject(pluginCtor, configuration);

            // Set all necessary parameters
            List<object> ctorParameters = new List<object>();
            if (pluginCtor.GetParameters().Length >= 1)
                ctorParameters.Add(settings);
            if (pluginCtor.GetParameters().Length >= 2)
                ctorParameters.Add(_logger);

            // Create plugin object
            return pluginCtor.Invoke(ctorParameters.ToArray());
        }

        private ConstructorInfo GetPluginConstructor(Type pluginType, Type interfaceType, Type settingsType, PluginSettings pluginSettings)
        {
            // Get plugin constructors
            var pluginCtors = pluginType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (pluginCtors.Length == 0)
                throw new PluginException($"Plugin {pluginSettings.ClassName}, {pluginSettings.AssemblyName} has no public constructor.");

            // Get the logger type
            var loggerType = typeof(ILogger);

            // Search for suitable constructor
            foreach (var pluginCtor in pluginCtors.OrderByDescending(ctor => ctor.GetParameters().Length))
            {
                // Get constructor parameters
                var pluginCtorParameters = pluginCtor.GetParameters();
                if (pluginCtorParameters.Length > 2)
                    continue;

                // If the constructor have no parameters, we found it
                if (pluginCtorParameters.Length == 0)
                    return pluginCtor;

                // First parameter must be a setting class
                if (!pluginCtorParameters[0].ParameterType.IsSubclassOf(settingsType))
                    continue;

                // If the constructor have 1 parameter (settings), we found it
                if (pluginCtorParameters.Length == 1)
                    return pluginCtor;

                // Second optional parameter must be a logger class
                if (pluginCtorParameters.Length >= 2 && !loggerType.IsAssignableFrom(pluginCtorParameters[1].ParameterType))
                    continue;

                // If the constructor have 2 parameters (settings and logger), we found it
                if (pluginCtorParameters.Length == 2)
                    return pluginCtor;
            }

            throw new PluginException($"Plugin {pluginSettings.ClassName}, {pluginSettings.AssemblyName} need a constructor with first optional parameter of type {settingsType.Name} and second optional parameter of type {loggerType}.");
        }

        private Type GetPluginType(Type interfaceType, PluginSettings pluginSettings)
        {
            // Load plugin assembly and get plugin type
            var pluginAssembly = Assembly.Load(pluginSettings.AssemblyName);
            var pluginType = pluginAssembly.GetType(pluginSettings.ClassName);

            // Check if the type meets the required interface
            if (!interfaceType.IsAssignableFrom(pluginType))
                throw new PluginException($"Plugin {pluginSettings.ClassName}, {pluginSettings.AssemblyName} must implements {interfaceType.Name} interface.");

            // Check if the type is not abstract or interface
            if (pluginType.IsAbstract || pluginType.IsInterface)
                throw new PluginException($"Plugin {pluginSettings.ClassName}, {pluginSettings.AssemblyName} cannot be abstract or interface.");

            return pluginType;
        }

        private object CreateSettingsObject(ConstructorInfo pluginCtor, IConfiguration configuration)
        {
            // Create instance of plugin settings
            var pluginCtorParameter = pluginCtor.GetParameters()[0];
            var pluginCtorParameterType = pluginCtorParameter.ParameterType;
            var pluginCtorParameterTypeCtor = pluginCtorParameterType.GetConstructor(new Type[] { });
            var settings = pluginCtorParameterTypeCtor.Invoke(new object[] { });

            // Bind settings to concrete object
            configuration.Bind(settings);
            return settings;
        }

        #endregion

        #region Events

        /// <summary>
        /// Resolve plugin assembly
        /// </summary>
        /// <returns>Returns plugin assembly, if exists.</returns>
        private Assembly PluginFactory_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(_pluginsConfiguration.PluginsFolder))
                return null;

            var assemblyFullPathName = Path.Combine(_pluginsConfiguration.PluginsFolder, new AssemblyName(args.Name).Name);
            if (!File.Exists(assemblyFullPathName))
                return null;

            return Assembly.LoadFrom(assemblyFullPathName);
        }

        #endregion
    }
}
