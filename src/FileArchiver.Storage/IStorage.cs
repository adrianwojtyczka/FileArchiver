using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver.Storage
{
    public interface IStorage
    {
        /// <summary>
        /// Store stream
        /// </summary>
        /// <param name="stream">Stream to store</param>
        void Store(Stream stream, DateTime startDate, DateTime endDate);
    }
}
