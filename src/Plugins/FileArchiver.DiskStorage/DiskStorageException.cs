using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.Storage
{
    public class DiskStorageException : Exception
    {
        public DiskStorageException()
        { }

        public DiskStorageException(string message)
            : base(message)
        { }

        public DiskStorageException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
