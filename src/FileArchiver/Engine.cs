using FileArchiver.Generic;
using FileArchiver.Plugins;
using FileArchiver.Settings;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileArchiver
{
    public class Engine
    {
        #region Private members

        private readonly IConfiguration _configuration;
        private readonly PluginFactory _pluginFactory;

        private readonly ILogger _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="baseFolder">Base folder</param>
        /// <param name="pluginFolder">Plugin folder</param>
        /// <param name="firstDayOfWeek">First day of week</param>
        public Engine(IConfiguration configuration, string baseFolder, string pluginFolder, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;

            _pluginFactory = new PluginFactory(baseFolder, pluginFolder, logger);
        }

        #endregion

        #region Methods

        public void Run()
        {
            _logger.Information("Start archiving files.");

            // For each archive setting present...
            foreach (var archiveSection in _configuration.GetSection("ArchiveSettings").GetChildren())
            {
                try
                {
                    // Bind settings to concrete object
                    var archiveSettings = new ArchiveSettings();
                    archiveSection.Bind(archiveSettings);

                    // Check settings
                    _logger.Debug("Checking configuration...");
                    CheckSettings(archiveSettings);


                    // Get files to archive
                    _logger.Debug("Searching files to archive...");
                    var fileNames = GetFilesToArchive(archiveSettings);

                    // Archive files
                    _logger.Information($"Archiving files using '{archiveSettings.Archive.Name}' archive plugin.");
                    var archive = _pluginFactory.GetArchive(archiveSettings.Archive.Name, archiveSection.GetSection("Archive"));
                    var stream = archive.Archive(fileNames);

                    // Set the pointer at beginning of the stream
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);

                    // Store files
                    _logger.Information($"Storing archive using '{archiveSettings.Storage.Name}' storage plugin.");
                    var storage = _pluginFactory.GetStorage(archiveSettings.Storage.Name, archiveSection.GetSection("Storage"));
                    storage.Store(stream);

                    // Delete files, if needed
                    if (archiveSettings.DeleteArchivedFiles)
                    {
                        _logger.Information("Deleting archived files.");
                        foreach (var fileName in fileNames)
                        {
                            _logger.Debug($"Deleting file {fileName}");
                            try
                            {
                                File.Delete(fileName);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warning($"Unable to delete file {fileName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                }
            }

            _logger.Information("End archiving files.");
        }

        private void CheckSettings(ArchiveSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Path))
                throw new ConfigurationErrorsException("Path to archive is empty.");

            if (!Directory.Exists(settings.Path))
                throw new ConfigurationErrorsException($"Directory {settings.Path} doesn't exists or is unreachable.");

            if (settings.Archive == null)
                throw new ConfigurationErrorsException("Archive configuration is not specified.");

            if (string.IsNullOrEmpty(settings.Archive.Name))
                throw new ConfigurationErrorsException("Archive plugin name is not specified.");

            if (settings.Storage == null)
                throw new ConfigurationErrorsException("Storage configuration is not specified.");

            if (string.IsNullOrEmpty(settings.Storage.Name))
                throw new ConfigurationErrorsException("Storage plugin name is not specified.");

            if (settings.Strategy == ArchiveStrategy.Unknown)
                throw new ConfigurationErrorsException("Strategy is not specified.");
        }

        private System.Collections.Generic.IEnumerable<string> GetFilesToArchive(ArchiveSettings archiveGroupSettings)
        {
            // Get all files in provided directory
            var fileNames = Directory.EnumerateFiles(archiveGroupSettings.Path, archiveGroupSettings.FilePattern ?? string.Empty);

            // Filter files with RegEx expression
            if (!string.IsNullOrEmpty(archiveGroupSettings.FileRegExPattern))
            {
                var fileNameRegEx = new Regex(archiveGroupSettings.FileRegExPattern, RegexOptions.Compiled);
                fileNames = fileNames.Where(fileName => fileNameRegEx.IsMatch(fileName));
            }

            // Filter files by creation date
            var getMaxFileDate = GetMaxFileDate(archiveGroupSettings.Strategy, archiveGroupSettings.FirstDayOfWeek);
            fileNames = fileNames.Where(fileName => new FileInfo(fileName).CreationTime <= getMaxFileDate);

            // Add log
            _logger.Information($"Found {fileNames.Count()} files to archive");

            // Return files list to store
            return fileNames;
        }

        private DateTime GetMaxFileDate(ArchiveStrategy archiveStrategy, DayOfWeek firstDayOfWeek)
        {
            DateTime dateTime;
            DateTime today = DateTime.Today;

            _logger.Debug($"Calculating date using {archiveStrategy} strategy.");

            switch (archiveStrategy)
            {
                // Get files older than a day
                case ArchiveStrategy.Daily:
                    dateTime = today.AddDays(-1);
                    break;

                // Get files older than a week
                case ArchiveStrategy.Weekly:
                    dateTime = today.AddDays(Utils.GetPreviousDayOfWeekDifference(firstDayOfWeek, today) - 1);
                    break;

                // Get files older than a month
                case ArchiveStrategy.Monthly:
                    dateTime = new DateTime(today.Year, today.Month - 1, DateTime.DaysInMonth(today.Year, today.Month - 1));
                    break;

                // Get files older than a year
                case ArchiveStrategy.Yearly:
                    dateTime = new DateTime(today.Year - 1, 12, 31);
                    break;

                default:
                    throw new Exception($"Archive strategy '{archiveStrategy}' is not a valid value.");
            }
            
            // Set the end of the calculated day
            dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59);

            _logger.Debug($"Date calculated: {dateTime.ToString(System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat)}");

            // Return calculated date
            return dateTime;
        }

        #endregion
    }
}
