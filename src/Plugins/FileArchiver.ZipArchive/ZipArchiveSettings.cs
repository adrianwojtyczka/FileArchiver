using System;
using System.IO.Compression;

namespace FileArchiver.ZipArchive
{
    public class ZipArchiveSettings
    {
        public CompressionLevel CompressionLevel { get; set; }

        public bool UseFile { get; set; }
    }
}
