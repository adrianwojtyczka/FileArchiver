using FileArchiver.Generic;
using FileArchiver.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace FileArchiver
{
    public class DiskStorage : IStorage
    {
        #region Private members

        /// <summary>
        /// Disk storage settings
        /// </summary>
        private readonly DiskStorageSettings _settings;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Settings</param>
        public DiskStorage(DiskStorageSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Store stream on disk
        /// </summary>
        /// <param name="stream">Stream to store</param>
        public void Store(Stream stream)
        {
            // Check settings
            CheckSettings();

            // Parse file name
            var fileName = ParseFileName(_settings.FileName, _settings.DateParameters);

            // Check if file doesn't exists
            if (File.Exists(fileName))
                throw new DiskStorageException($"File {fileName} already exists.");

            // Write file to disk and check if it exists
            WriteFile(stream, fileName);
            if (!File.Exists(fileName))
                throw new DiskStorageException($"An error occurred while storing file {fileName}.");
        }

        /// <summary>
        /// Check settings
        /// </summary>
        private void CheckSettings()
        {
            // Check if the file name is not empty
            if (string.IsNullOrEmpty(_settings.FileName))
                throw new ConfigurationErrorsException($"The setting {nameof(_settings.FileName)} is not defined.");
        }

        /// <summary>
        /// Write file on disk
        /// </summary>
        /// <param name="stream">Stream to store</param>
        /// <param name="fileName">File name to assign to the file</param>
        private void WriteFile(Stream stream, string fileName)
        {
            // If the stream is FileStream...
            if (stream is FileStream)
            {
                _logger.Information($"Moving file to {fileName}.");

                // Ensure the stream is closed
                stream.Close();

                // Move it to the correct path
                string zipFileName = ((FileStream)stream).Name;
                File.Move(zipFileName, fileName);
            }
            // ... otherwise...
            else
            {
                _logger.Information($"Writing file {fileName}.");

                // Create new file and write stream to it
                using (var file = File.Create(fileName))
                {
                    stream.CopyTo(file);
                }
            }
        }

        /// <summary>
        /// Parse file name
        /// </summary>
        /// <param name="fileName">File name to parse</param>
        /// <param name="dateParameters">Optional date parameters to apply</param>
        /// <returns>Return parsed file name</returns>
        private string ParseFileName(string fileName, List<DateTimeParameters> dateParameters)
        {
            int dateParametersCount = 0;

            // Get all defined parameters
            const string placeholderRegexPattern = "({[a-zA-Z0-9]+(:[^}]*)?})";
            var matches = Regex.Matches(fileName, placeholderRegexPattern);

            // For each parameter found...
            foreach (Match parameter in matches)
            {
                // Split the placeholder
                var nameFormat = parameter.Value
                    .Trim('{', '}')
                    .Split(':');

                // Retrieve the name and format
                string name = nameFormat[0].Trim();
                string format = nameFormat.Length >= 2 ? nameFormat[1].Trim() : null;

                // Get formatted value for the current placeholder
                string value;
                switch (name.ToLower())
                {
                    case "date":
                        var dateTime = DateTime.Now;
                        if (dateParameters != null && dateParametersCount < dateParameters.Count)
                        {
                            dateTime = Utils.CalculateDateTime(dateTime, _settings.DateParameters[dateParametersCount]);
                            dateParametersCount++;
                        }
                        
                        value = dateTime.ToString(format);
                        break;

                    case "timestamp":
                        value = DateTime.Now.ToString(format);
                        break;

                    default:
                        value = string.Empty;
                        break;
                }

                // Replace placeholder
                fileName = fileName.Replace(parameter.Value, value);
            }

            // Return parsed file name
            return fileName;
        }

        #endregion
    }
}
