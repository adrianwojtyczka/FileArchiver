using FileArchiver.Archive;
using FileArchiver.Plugin;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace FileArchiver.ZipArchive
{
    [Plugin("FileArchiver.ZipArchive", typeof(ZipArchiveSettings))]
    public class ZipArchive : IArchive
    {
        private readonly ZipArchiveSettings _settings;

        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Settings</param>
        public ZipArchive(ZipArchiveSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Archive and zip files
        /// </summary>
        /// <param name="fileNames">Files to archive</param>
        /// <returns>Returns archive stream</returns>
        public Stream Archive(IDictionary<string, string> fileNames)
        {
            if (fileNames == null)
                throw new ArgumentException($"{nameof(fileNames)} cannot be null");

            Stream stream;

            if (_settings.UseFile)
            {
                // Create temporary file
                stream = File.Create(Path.GetTempFileName());
            }
            else
            {
                // Create memory stream
                stream = new MemoryStream();
            }

            // Create new zip archive
            using (var zipArchive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, !_settings.UseFile))
            {
                // For each file to archive...
                foreach (var fileName in fileNames)
                {
                    _logger.Debug($"Adding file {fileName.Value}.");

                    // Create new entry and write file stream to it
                    var zipEntry = zipArchive.CreateEntry(fileName.Key, _settings.CompressionLevel);
                    using (var fileStream = File.OpenRead(fileName.Value))
                    using (var entryStream = zipEntry.Open())
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }

            // Returns zip archive stream
            return stream;
        }
    }
}
