using FileArchiver.Archive;
using FileArchiver.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver.Plugins.Tests.Mocks
{
    [Plugin("ArchiveMock")]
    public class ArchivePluginMock : IArchive
    {
        public Stream Archive(IDictionary<string, string> fileNames)
        {
            return new MemoryStream();
        }
    }
}
