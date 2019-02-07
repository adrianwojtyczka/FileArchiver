using FileArchiver.Archive;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace FileArchiver
{
    public class ZipArchiveSettings : ArchiveSettings
    {
        public CompressionLevel CompressionLevel { get; set; }

        public bool UseFile { get; set; }
    }
}
