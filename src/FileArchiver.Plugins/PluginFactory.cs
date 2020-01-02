using FileArchiver.Archive;
using FileArchiver.Storage;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
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
        /// Constructor
        /// </summary>
        /// <param name="baseFolder">Base application folder</param>
        /// <param name="pluginsFolder">Plugin folder</param>
        public PluginFactory(ILogger logger)
        {
            _pluginsConfiguration = new PluginsConfiguration(logger);
            _logger = logger;
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
            return GetPlugin<IArchive>(name, configuration);
        }

        /// <summary>
        /// Get storage plugin
        /// </summary>
        /// <param name="name">Plugin name to instance</param>
        /// <param name="configuration">Configuration of the plugin</param>
        /// <returns>Returns instance of the required Storage plugin</returns>
        public IStorage GetStorage(string name, IConfiguration configurationSection)
        {
            return GetPlugin<IStorage>(name, configurationSection);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get plugin
        /// </summary>
        /// <typeparam name="T">Plugin type</typeparam>
        /// <param name="type">Plugin type to instance</param>
        /// <param name="name">Plugin name to instance</param>
        /// <param name="settingsType">Type of the settings required</param>
        /// <param name="configuration">Plugin configuration</param>
        /// <returns>Returns instance of the required plugin</returns>
        private T GetPlugin<T>(string name, IConfiguration configuration)
        {
            // Get plugin settings
            var pluginSettings = _pluginsConfiguration.GetPluginSettings<T>(name);

            // Get plugin constructor
            var pluginCtor = GetPluginConstructor(pluginSettings);

            // Create plugin with settings
            if (pluginSettings.SettingsType != null)
            {
                var settings = CreateSettingsObject(pluginSettings.SettingsType, configuration);
                return CreatePlugin<T>(pluginCtor, settings);
            }

            // Create plugin without settings
            return CreatePlugin<T>(pluginCtor);
        }

        /// <summary>
        /// Search for suitable plugin constructor
        /// </summary>
        /// <param name="pluginSettings">Plugin settings</param>
        /// <returns>Return plugin constructor</returns>
        private ConstructorInfo GetPluginConstructor(PluginSettings pluginSettings)
        {
            // Get plugin constructors
            var pluginCtors = pluginSettings.Type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (pluginCtors.Length == 0)
                throw new PluginException($"Plugin {pluginSettings.Type} has no public constructor.");

            // Get the logger type and its position
            var loggerType = typeof(ILogger);
            var loggerTypePosition = pluginSettings != null ? 2 : 1;

            // Search for suitable constructor
            foreach (var pluginCtor in pluginCtors.OrderByDescending(ctor => ctor.GetParameters().Length))
            {
                // Get constructor parameters
                var pluginCtorParameters = pluginCtor.GetParameters();
                if (pluginCtorParameters.Length > loggerTypePosition)
                    continue;

                // The empty constructor can only exists if the plugin doesn't have any setting class
                if (pluginSettings == null)
                {
                    // If the constructor have no parameters, we found it
                    if (pluginCtorParameters.Length == 0)
                        return pluginCtor;
                }

                if (pluginSettings != null)
                {
                    // First parameter must be a setting class
                    if (!pluginSettings.SettingsType.IsAssignableFrom(pluginCtorParameters[0].ParameterType))
                        continue;

                    // If the constructor have 1 parameter (settings), we found it
                    if (pluginCtorParameters.Length == 1)
                        return pluginCtor;
                }

                // Second optional parameter must be a logger class
                if (pluginCtorParameters.Length >= loggerTypePosition && !loggerType.IsAssignableFrom(pluginCtorParameters[1].ParameterType))
                    continue;

                // If the constructor have 2 parameters (settings and logger), we found it
                if (pluginCtorParameters.Length == loggerTypePosition)
                    return pluginCtor;
            }

            if (pluginSettings.SettingsType != null)
                throw new PluginException($"Plugin {pluginSettings.Type} need a constructor with first required parameter of type {pluginSettings.SettingsType} and second optional parameter of type {loggerType}.");

            throw new PluginException($"Plugin {pluginSettings.Type} need a constructor with first optional parameter of type {loggerType}.");
        }

        /// <summary>
        /// Create plugin with settings object
        /// </summary>
        /// <typeparam name="T">Plugin type</typeparam>
        /// <param name="pluginCtor">Plugin constructor</param>
        /// <param name="settings">Plugin settings</param>
        /// <returns>Returns plugin instance</returns>
        private T CreatePlugin<T>(ConstructorInfo pluginCtor, object settings)
        {
            List<object> ctorParameters = new List<object> { settings };
            if (pluginCtor.GetParameters().Length >= 2)
                ctorParameters.Add(_logger);

            return (T)pluginCtor.Invoke(ctorParameters.ToArray());
        }

        /// <summary>
        /// Create plugin
        /// </summary>
        /// <typeparam name="T">Plugin type</typeparam>
        /// <param name="pluginCtor">Plugin constructor</param>
        /// <returns>Returns plugin instance</returns>
        private T CreatePlugin<T>(ConstructorInfo pluginCtor)
        {
            List<object> ctorParameters = new List<object>();
            if (pluginCtor.GetParameters().Length >= 1)
                ctorParameters.Add(_logger);

            return (T)pluginCtor.Invoke(ctorParameters.ToArray());
        }

        /// <summary>
        /// Create settings object and bind the configuration
        /// </summary>
        /// <param name="settingsType">Settings type to instance</param>
        /// <param name="configuration">Configuration to bind</param>
        /// <returns>Returns the settings object</returns>
        private object CreateSettingsObject(Type settingsType, IConfiguration configuration)
        {
            // Create instance of plugin settings
            var pluginCtorParameterTypeCtor = settingsType.GetConstructor(Array.Empty<Type>());
            if (pluginCtorParameterTypeCtor == null)
                throw new PluginException($"{settingsType} must have a default constructor.");

            // Create and bind settings
            var settings = pluginCtorParameterTypeCtor.Invoke(Array.Empty<object>());
            configuration.Bind(settings);

            return settings;
        }

        #endregion
    }
}
