using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Storage
{
    public class ArchiveException : Exception
    {
        public ArchiveException()
        { }

        public ArchiveException(string message)
            : base(message)
        { }

        public ArchiveException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
