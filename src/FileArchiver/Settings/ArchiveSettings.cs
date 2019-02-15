using System;

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

        public PluginSettings Archive { get; set; }

        public PluginSettings Storage { get; set; }
    }
}
