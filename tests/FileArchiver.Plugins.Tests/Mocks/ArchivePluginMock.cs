using FileArchiver.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver.Plugins.Tests.Mocks
{
    [Plugin.Plugin("ArchiveMock")]
    public class ArchivePluginMock : IArchive
    {
        public Stream Archive(IEnumerable<string> fileNames)
        {
            return new MemoryStream();
        }
    }
}
