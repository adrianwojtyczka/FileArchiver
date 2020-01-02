using FileArchiver.Generic;
using System;
using System.Collections.Generic;

namespace FileArchiver.DiskStorage
{
    public class DiskStorageSettings
    {
        public string FileName { get; set; }

        public List<DateTimeParameters> DateParameters { get; set; }
    }
}
