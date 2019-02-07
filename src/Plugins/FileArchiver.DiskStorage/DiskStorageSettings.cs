using FileArchiver.Generic;
using FileArchiver.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver
{
    public class DiskStorageSettings : StorageSettings
    {
        public string FileName { get; set; }

        public List<DateTimeParameters> DateParameters { get; set; }
    }
}
