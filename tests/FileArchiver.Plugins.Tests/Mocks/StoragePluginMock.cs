using FileArchiver.Plugin;
using FileArchiver.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver.Plugins.Tests.Mocks
{
    [Plugin("StorageMock")]
    public class StoragePluginMock : IStorage
    {
        public void Store(Stream stream, DateTime startDate, DateTime endDate)
        { }
    }
}
