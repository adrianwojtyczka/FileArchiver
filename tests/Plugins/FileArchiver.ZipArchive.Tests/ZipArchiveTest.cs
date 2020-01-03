using Moq;
using Serilog;
using System;
using System.Linq;
using System.IO;
using Xunit;
using System.Collections.Generic;

namespace FileArchiver.ZipArchive.Tests
{
    public class ZipArchiveTest
    {
        private const string DataFolder = "Data";

        [Fact]
        public void ZipFiles_NullFileList_ShouldThrowArgumentException()
        {
            // Arrange
            var settings = new ZipArchiveSettings { };
            var logger = new Mock<ILogger>();
            var zipArchive = new ZipArchive(settings, logger.Object);

            // Act
            var exception = Record.Exception(() => zipArchive.Archive(null));

            // Assert
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void ZipFiles_EmptyFileList_ShouldCreateEmptyZipArchive()
        {
            // Arrange
            var settings = new ZipArchiveSettings { };
            var logger = new Mock<ILogger>();
            var zipArchive = new ZipArchive(settings, logger.Object);

            // Act
            using (var zipStream = zipArchive.Archive(new Dictionary<string, string>(0)))
            {
                // Assert
                var zip = new System.IO.Compression.ZipArchive(zipStream);
                Assert.True(zip.Entries.Count == 0);
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { "ZipArchive_TestFile_1.txt" } })]
        [InlineData(new object[] { new string[] { "ZipArchive_TestFile_1.txt", "ZipArchive_TestFile_2.txt" } })]
        [InlineData(new object[] { new string[] { "ZipArchive_TestFile_1.txt", "ZipArchive_TestFile_2.txt", "ZipArchive_TestFile_3.txt", "ZipArchive_TestFile_4.txt" } })]
        public void ZipFiles_FileList_ShouldContainsAllFilesFromFileList(string[] files)
        {
            // Arrange
            var fileList = new Dictionary<string, string>();
            foreach (var fileName in files)
                fileList.Add(fileName, Path.Combine(DataFolder, fileName));

            var fileProcessed = new Dictionary<string, bool>();
            foreach (var fileName in files)
                fileProcessed.Add(fileName, false);


            var settings = new ZipArchiveSettings { };
            var logger = new Mock<ILogger>();
            var zipArchive = new ZipArchive(settings, logger.Object);

            // Act
            using (var zipStream = zipArchive.Archive(fileList))
            {
                // Assert
                var zip = new System.IO.Compression.ZipArchive(zipStream);

                foreach (var entry in zip.Entries)
                {
                    // Check if the file is not already processed
                    Assert.True(fileProcessed.ContainsKey(entry.Name));
                    Assert.False(fileProcessed[entry.Name]);

                    // Check if the zip content is the same as original file
                    Assert.True(CheckZipEntryContent(entry, fileList[entry.Name]));

                    // Set file as processed
                    fileProcessed[entry.Name] = true;
                }
            }

            // Check if all files are included in zip entry
            Assert.True(fileProcessed.Values.All(processed => processed));
        }

        [Fact]
        public void ZipFiles_FileListWithUseFileSetting_ShouldCreateZipFileOnDiskContainingTestFile()
        {
            const string testFileName = "ZipArchive_TestFile_1.txt";
            string testFullFileName = Path.Combine(DataFolder, testFileName);
            var filesToArchive = new Dictionary<string, string>
            {
                { testFileName, testFullFileName }
            };

            // Arrange
            var settings = new ZipArchiveSettings { UseFile = true };
            var logger = new Mock<ILogger>();
            var zipArchive = new ZipArchive(settings, logger.Object);

            // Act
            var zipStream = zipArchive.Archive(filesToArchive);

            // Assert
            Assert.IsType<FileStream>(zipStream);

            string tempFileName = ((FileStream)zipStream).Name;
            Assert.True(File.Exists(tempFileName));

            using (var zip = System.IO.Compression.ZipFile.OpenRead(tempFileName))
            {
                Assert.True(zip.Entries.Count == 1);
                Assert.Equal(testFileName, zip.Entries[0].Name);
                Assert.True(CheckZipEntryContent(zip.Entries[0], testFullFileName));
            }

            // Cleanup
            File.Delete(tempFileName);
        }


        private bool CheckZipEntryContent(System.IO.Compression.ZipArchiveEntry entry, string fileName)
        {
            using (var entryStream = entry.Open())
            using (var fileStream = File.Open(fileName, FileMode.Open))
            {
                int entryStreamByte;
                int fileStreamByte;
                do
                {
                    entryStreamByte = entryStream.ReadByte();
                    fileStreamByte = fileStream.ReadByte();

                    if (entryStreamByte != fileStreamByte)
                        return false;

                } while (entryStreamByte != -1 && fileStreamByte != -1);
            }

            return true;
        }
    }
}
