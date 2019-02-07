using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Settings
{
    public enum ArchiveStrategy
    {
        Unknown,
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public class ArchiveSettings
    {
        public string Path { get; set; }

        public ArchiveStrategy Strategy { get; set; }

        public DayOfWeek FirstDayOfWeek { get; set; }

        public string FilePattern { get; set; }

        public string FileRegExPattern { get; set; }

        public bool DeleteArchivedFiles { get; set; }

        public Archive.ArchiveSettings Archive { get; set; }

        public Storage.StorageSettings Storage { get; set; }
    }
}
