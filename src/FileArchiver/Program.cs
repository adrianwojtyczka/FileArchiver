using FileArchiver.Settings;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace FileArchiver
{
    class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        static void Main()
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("FileArchiver.json", false)
#if DEBUG
                .AddJsonFile("FileArchiver.Development.json", true)
#endif
                .Build();
            
            // Get and bind FileArchiver config section
            var configSection = config.GetSection("FileArchiver");
            var settings = new FileArchiverSettings();
            configSection.Bind(settings);

            // Create logger
            var logger = CreateLogger(config);

            try
            {
                // Create and run engine
                var engine = new Engine(configSection, AppContext.BaseDirectory, settings.PluginsFolder, logger);
                engine.Run();
            }
            catch(Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
        }

        /// <summary>
        /// Create logger from configuration
        /// </summary>
        /// <param name="config">Configuration to appy</param>
        /// <returns>Returns logger created</returns>
        static ILogger CreateLogger(IConfiguration config)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }
    }
}
