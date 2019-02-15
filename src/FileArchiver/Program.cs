﻿using FileArchiver.Settings;
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
            var config = LoadConfiguration();
            
            // Create logger
            var logger = CreateLogger(config);

            try
            {
                // Create and run engine
                var engine = new Engine(config.GetSection("FileArchiver"), logger);
                engine.Run();
            }
            catch(Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
        }

        private static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("FileArchiver.json", false)
#if DEBUG
                .AddJsonFile("FileArchiver.Development.json", true)
#endif
                .Build();
        }

        /// <summary>
        /// Create logger from configuration
        /// </summary>
        /// <param name="config">Configuration to appy</param>
        /// <returns>Returns logger created</returns>
        private static ILogger CreateLogger(IConfiguration config)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }
    }
}
