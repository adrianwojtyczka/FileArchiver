﻿using FileArchiver.Generic;
using System;
using System.Text.RegularExpressions;

namespace FileArchiver.Settings
{
    public enum ArchiveStrategy
    {
        Unknown = 0,
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public class ArchiveSettings
    {
        private Regex _fileRegEx;


        public string Path { get; set; }

        public string NormalizedPath => Path.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar).Trim(System.IO.Path.DirectorySeparatorChar);

        public bool IncludeSubfolders { get; set; }

        public string SubfolderPattern { get; set; }

        public bool DeleteEmptySubfolders { get; set; }

        public string FilePattern { get; set; }

        public string FileRegExPattern { get; set; }

        public ArchiveStrategy Strategy { get; set; }

        public DayOfWeek FirstDayOfWeek { get; set; }

        public DateTimeParameters RetentionDateParameters { get; set; }

        public PluginSettings Archive { get; set; }

        public PluginSettings Storage { get; set; }

        public bool DeleteArchivedFiles { get; set; }

        /// <summary>
        /// Return compiled file regular expression
        /// </summary>
        public Regex FileRegEx
        {
            get
            {
                if (_fileRegEx == null && !string.IsNullOrWhiteSpace(FileRegExPattern))
                    _fileRegEx = new Regex(FileRegExPattern, RegexOptions.Compiled);

                return _fileRegEx;
            }
        }
    }
}
