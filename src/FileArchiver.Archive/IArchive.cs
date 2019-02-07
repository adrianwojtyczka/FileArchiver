using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver.Archive
{
    public interface IArchive
    {
        /// <summary>
        /// Archive files
        /// </summary>
        /// <param name="fileNames">Files to archive</param>
        /// <returns>Returns archive stream</returns>
        Stream Archive(IEnumerable<string> fileNames);
    }
}
