using Moq;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using Xunit;

namespace FileArchiver.DiskStorage.Test
{
    public class DiskStorageTest
    {
        public DiskStorageTest()
        {
            // Delete all remaining test files
            foreach (var fileName in Directory.EnumerateFiles(".", "DiskStorageTest*.txt"))
                File.Delete(fileName);
        }

        [Fact]
        public void StoreFile_EmptyFileNameSetting_ShouldThrowException()
        {
            // Arrange
            var settings = new DiskStorageSettings
            {
                FileName = string.Empty
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);

            // Act
            var exception = Record.Exception(() => diskStorage.Store(null));

            // Assert
            Assert.IsType<ConfigurationErrorsException>(exception);
            Assert.Equal($"The setting {nameof(settings.FileName)} is not defined.", exception.Message);
        }

        [Fact]
        public void StoreFile_FileAlreadyExists_ShouldThrowException()
        {
            // Arrange
            var settings = new DiskStorageSettings
            {
                FileName = "DiskStorageTest_AlreadyExistFileName.txt"
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);

            var fileStream = File.Create(settings.FileName);

            // Act
            var exception = Record.Exception(() => diskStorage.Store(fileStream));

            // Assert
            Assert.IsType<DiskStorageException>(exception);
            Assert.Equal($"File {settings.FileName} already exists.", exception.Message);


            // Cleanup
            fileStream.Dispose();
            File.Delete(settings.FileName);
        }

        [Fact]
        public void StoreFile_MoveFile_MovedFileShouldNotExistsAndSettingFileShouldExists()
        {
            // Arrange
            
            var settings = new DiskStorageSettings
            {
                FileName = "DiskStorageTest_MovedFile.txt"
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);

            // Create file
            var fileToMoveFileName = "DiskStorageTest_FileToMove.txt";
            var fileStream = File.Create(fileToMoveFileName);

            // Write string to file
            const string testString = "Test string\r\nIn 2 lines.";
            fileStream.Write(Encoding.UTF8.GetBytes(testString));
            fileStream.Seek(0, SeekOrigin.Begin);

            // Act
            diskStorage.Store(fileStream);

            // Assert
            Assert.False(File.Exists(fileToMoveFileName));
            Assert.True(File.Exists(settings.FileName));
            Assert.Equal(testString, File.ReadAllText(settings.FileName));


            // Cleanup
            File.Delete(settings.FileName);
        }

        [Fact]
        public void StoreFile_CreateFileFromStream_FileShouldExists()
        {
            // Arrange
            var settings = new DiskStorageSettings
            {
                FileName = "DiskStorageTest_NewFile.txt"
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);


            var stream = new MemoryStream();

            const string testString = "Test string\r\nIn 2 lines.";
            stream.Write(Encoding.UTF8.GetBytes(testString));
            stream.Seek(0, SeekOrigin.Begin);

            // Act
            diskStorage.Store(stream);

            // Assert
            Assert.True(File.Exists(settings.FileName));
            Assert.Equal(testString, File.ReadAllText(settings.FileName));


            // Cleanup
            File.Delete(settings.FileName);
        }

        [Fact]
        public void StoreFile_CreateFileWithDateAndTimestampPlaceholders_FileNameShouldMatch()
        {
            // Arrange
            const string datePlaceHolder = "{date:yyyyMMdd}";
            const string timestampPlaceHolder = "{timestamp:yyyyMMdd}";
            var settings = new DiskStorageSettings
            {
                FileName = $"DiskStorageTest_NewFile_{datePlaceHolder}_{timestampPlaceHolder}.txt",
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);


            var stream = new MemoryStream();

            // Act
            diskStorage.Store(stream);

            // Assert
            var correctFileName = settings.FileName
                .Replace(datePlaceHolder, DateTime.Now.ToString("yyyyMMdd"))
                .Replace(timestampPlaceHolder, DateTime.Now.ToString("yyyyMMdd"));

            Assert.True(File.Exists(correctFileName));

            // Cleanup
            File.Delete(correctFileName);
        }
    }
}