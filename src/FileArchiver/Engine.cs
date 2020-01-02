using FileArchiver.Generic;
using FileArchiver.Plugins;
using FileArchiver.Settings;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileArchiver
{
    public class Engine
    {
        #region Constants

        private const string Last = "Last";
        private const string Previous = "Previous";

        #endregion

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
        public Engine(IConfiguration configuration, ILogger logger)
        {
            _pluginFactory = new PluginFactory(logger);
            _configuration = configuration;
            _logger = logger;
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
                    CheckSettings(archiveSettings);

                    // Archive and store files
                    ArchiveAndStoreFiles(archiveSection, archiveSettings);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                }
            }

            _logger.Information("End archiving files.");
        }

        private void ArchiveAndStoreFiles(IConfigurationSection archiveSection, ArchiveSettings archiveSettings)
        {
            DateTime startDateTime = GetInitialDate(archiveSettings.RetentionDateParameters);
            DateTime endDateTime;
            bool hasOtherFiles;

            do
            {
                // Get start and end date for archive
                (startDateTime, endDateTime) = GetNextFileDate(startDateTime, archiveSettings.Strategy, archiveSettings.FirstDayOfWeek);

                // Get files to archive
                var fileNames = GetFilesToArchive(startDateTime, endDateTime, archiveSettings);
                if (fileNames.Any())
                {
                    try
                    {
                        // Archive and store files
                        var archiveStream = ArchiveFiles(archiveSettings.Archive.Name, archiveSection.GetSection("Archive"), fileNames);
                        StoreArchive(archiveSettings.Storage.Name, archiveSection.GetSection("Storage"), archiveStream, startDateTime, endDateTime);

                        // Delete files, if needed
                        if (archiveSettings.DeleteArchivedFiles)
                            DeleteArchivedFiles(fileNames);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, ex.Message);
                    }

                    hasOtherFiles = true;
                }
                else
                    hasOtherFiles = HasOtherFiles(startDateTime.AddMilliseconds(-1), archiveSettings);

            } while (hasOtherFiles);
        }

        /// <summary>
        /// Check settings
        /// </summary>
        /// <param name="settings">Settings to check</param>
        private void CheckSettings(ArchiveSettings settings)
        {
            _logger.Debug("Checking configuration...");

            if (string.IsNullOrWhiteSpace(settings.Path))
                throw new ConfigurationErrorsException("Path to archive is empty.");

            if (!Directory.Exists(settings.Path))
                throw new ConfigurationErrorsException($"Directory {settings.Path} doesn't exists or is unreachable.");

            if (settings.Archive == null)
                throw new ConfigurationErrorsException("Archive configuration is not specified.");

            if (string.IsNullOrWhiteSpace(settings.Archive.Name))
                throw new ConfigurationErrorsException("Archive plugin name is not specified.");

            if (settings.Storage == null)
                throw new ConfigurationErrorsException("Storage configuration is not specified.");

            if (string.IsNullOrWhiteSpace(settings.Storage.Name))
                throw new ConfigurationErrorsException("Storage plugin name is not specified.");

            if (settings.Strategy == ArchiveStrategy.Unknown)
                throw new ConfigurationErrorsException("Strategy is not specified.");
        }

        /// <summary>
        /// Get files to archive
        /// </summary>
        /// <param name="settings">Settings</param>
        /// <returns>Returns collection of files to archive</returns>
        private IEnumerable<string> GetFilesToArchive(DateTime startDateTime, DateTime endDateTime, ArchiveSettings settings)
        {
            _logger.Debug($"Searching files to archive for period {startDateTime.ToShortDateString()} - {endDateTime.ToShortDateString()} ...");

            // Get all files to archive
            var fileNames = GetFilteredFileNames(startDateTime, endDateTime, settings);

            // Add log
            _logger.Information($"Found {fileNames.Count()} files to archive.");

            // Return files list to store
            return fileNames;
        }

        private IEnumerable<string> GetFilteredFileNames(DateTime startDateTime, DateTime endDateTime, ArchiveSettings settings)
        {
            // Get all files in provided directory
            var fileNames = Directory.EnumerateFiles(settings.Path, settings.FilePattern ?? string.Empty);

            // Filter files with RegEx expression
            if (settings.FileRegEx != null)
                fileNames = fileNames.Where(fileName => settings.FileRegEx.IsMatch(fileName));

            // Filter files by creation date
            return fileNames.Where(fileName =>
            {
                var fileInfo = new FileInfo(fileName);
                return fileInfo.CreationTime >= startDateTime && fileInfo.CreationTime <= endDateTime;
            });
        }

        /// <summary>
        /// Check if there are files older than date time parameter
        /// </summary>
        private bool HasOtherFiles(DateTime dateTime, ArchiveSettings settings)
        {
            return GetFilteredFileNames(DateTime.MinValue, dateTime, settings).Any();
        }

        /// <summary>
        /// Archive files using plugin
        /// </summary>
        /// <param name="archivePluginName">Archive plugin name to use</param>
        /// <param name="archiveSection">Archive settings section</param>
        /// <param name="fileNames">File names to archive</param>
        /// <returns>Returns archive stream</returns>
        private Stream ArchiveFiles(string archivePluginName, IConfiguration archiveSection, IEnumerable<string> fileNames)
        {
            _logger.Information($"Archiving files using '{archivePluginName}' archive plugin...");

            // Archive files
            var archivePlugin = _pluginFactory.GetArchive(archivePluginName, archiveSection);
            var archiveStream = archivePlugin.Archive(fileNames);

            // Check if stream was created
            if (archiveStream == null)
                throw new NullReferenceException("Archive stream cannot be null.");

            // Set the pointer at beginning of the stream
            if (archiveStream.CanSeek)
                archiveStream.Seek(0, SeekOrigin.Begin);

            // Return archive stream
            return archiveStream;
        }

        /// <summary>
        /// Store archive
        /// </summary>
        /// <param name="storagePluginName">Storage plugin name to use</param>
        /// <param name="storeSection">Store settings section</param>
        /// <param name="archiveStream">Archive stream to store</param>
        private void StoreArchive(string storagePluginName, IConfiguration storeSection, Stream archiveStream, DateTime startDate, DateTime endDate)
        {
            _logger.Information($"Storing archive using '{storagePluginName}' storage plugin...");

            // Store archive stream
            var storage = _pluginFactory.GetStorage(storagePluginName, storeSection);
            storage.Store(archiveStream, startDate, endDate);
        }

        /// <summary>
        /// Delete archived files
        /// </summary>
        /// <param name="filesToDelete">File names to delete</param>
        private void DeleteArchivedFiles(IEnumerable<string> filesToDelete)
        {
            _logger.Information("Deleting archived files...");

            // Delete each archived file
            foreach (var fileName in filesToDelete)
            {
                _logger.Debug($"Deleting file {fileName}.");

                try
                {
                    File.Delete(fileName);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, $"Unable to delete file {fileName}: {ex.Message}");
                }
            }
        }

        private (DateTime, DateTime) GetNextFileDate(DateTime dateTime, ArchiveStrategy archiveStrategy, DayOfWeek firstDayOfWeek)
        {
            DateTime startDateTime;
            DateTime endDateTime;

            _logger.Debug($"Calculating date using {archiveStrategy} strategy...");

            switch (archiveStrategy)
            {
                // Get files older than a day
                case ArchiveStrategy.Daily:
                    endDateTime = dateTime.AddDays(-1);
                    startDateTime = endDateTime;
                    break;

                // Get files older than a week
                case ArchiveStrategy.Weekly:
                    endDateTime = dateTime.AddDays(Utils.GetPreviousDayOfWeekDifference(firstDayOfWeek, dateTime) - 1);
                    startDateTime = endDateTime.AddDays(-7);
                    break;

                // Get files older than a month
                case ArchiveStrategy.Monthly:
                    endDateTime = new DateTime(dateTime.Year, dateTime.Month - 1, DateTime.DaysInMonth(dateTime.Year, dateTime.Month - 1));
                    startDateTime = new DateTime(endDateTime.Year, endDateTime.Month, 1);
                    break;

                // Get files older than a year
                case ArchiveStrategy.Yearly:
                    endDateTime = new DateTime(dateTime.Year - 1, 12, 31);
                    startDateTime = new DateTime(endDateTime.Year, 1, 1);
                    break;

                default:
                    throw new ApplicationException($"Archive strategy '{archiveStrategy}' is not a valid value.");
            }

            // Set the end of the calculated day
            startDateTime = new DateTime(startDateTime.Year, startDateTime.Month, startDateTime.Day, 0, 0, 0, 0);
            endDateTime = new DateTime(endDateTime.Year, endDateTime.Month, endDateTime.Day, 23, 59, 59, 999);

            _logger.Debug($"Date calculated: {startDateTime.ToString(System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat)} - {endDateTime.ToString(System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat)}.");

            // Return calculated date
            return (startDateTime, endDateTime);
        }

        private DateTime GetInitialDate(DateTimeParameters retentionDateTimeParameters)
        {
            var dateTime = DateTime.Today;
            if (retentionDateTimeParameters != null)
            {
                dateTime = dateTime.AddYears(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Year));
                dateTime = dateTime.AddMonths(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Month));
                dateTime = dateTime.AddDays(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Day));

                dateTime = dateTime.AddHours(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Hour));
                dateTime = dateTime.AddMinutes(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Minute));
                dateTime = dateTime.AddSeconds(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Second));
                dateTime = dateTime.AddMilliseconds(GetRetentionDateTimeValueToAdd(retentionDateTimeParameters.Millisecond));
            }

            _logger.Debug($"Initial date calculated: {dateTime.ToString(System.Threading.Thread.CurrentThread.CurrentUICulture)}.");

            return dateTime;
        }

        private int GetRetentionDateTimeValueToAdd(string parameter)
        {
            // Add nothing if the parameter is empty
            if (string.IsNullOrEmpty(parameter))
                return 0;

            // If the parameter is an integer...
            if (int.TryParse(parameter, out int result))
            {
                if (result < 0)
                    throw new ArgumentException($"Retention date time parameter value cannot be negative.");

                // Subtract this value
                return -result;
            }

            // If the parameter is "Last" or "Previous", subtract 1
            if (parameter.Equals(Last, StringComparison.OrdinalIgnoreCase) || parameter.Equals(Previous, StringComparison.OrdinalIgnoreCase))
                return -1;

            throw new ArgumentException($"Retention date time parameter '{parameter}' is not supported.");
        }

        #endregion
    }
}
