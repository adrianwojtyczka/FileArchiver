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
        private string _tempPathName;

        public DiskStorageTest()
        {
            // Delete all remaining test files
            foreach (var fileName in Directory.EnumerateFiles(".", "DiskStorageTest*.txt"))
                File.Delete(fileName);

            _tempPathName = Path.GetTempPath();
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
            var exception = Record.Exception(() => diskStorage.Store(null, DateTime.MinValue, DateTime.MinValue));

            // Assert
            Assert.IsType<ConfigurationErrorsException>(exception);
            Assert.Equal($"The setting {nameof(settings.FileName)} is not defined.", exception.Message);
        }

        [Fact]
        public void StoreFile_FileAlreadyExists_ShouldCreateFileWithProgressiveName()
        {
            // Arrange
            var settings = new DiskStorageSettings
            {
                FileName = Path.Combine(_tempPathName, "DiskStorageTest_AlreadyExistFileName.txt")
            };
            var fileName = Path.Combine(_tempPathName, "DiskStorageTest_AlreadyExistFileName (1).txt");

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);

            var fileStream = File.Create(settings.FileName);

            // Act
            diskStorage.Store(fileStream, DateTime.MinValue, DateTime.MinValue);

            // Assert
            Assert.True(File.Exists(fileName));

            // Cleanup
            fileStream.Dispose();
            File.Delete(settings.FileName);
            File.Delete(fileName);
        }

        [Fact]
        public void StoreFile_MoveFile_MovedFileShouldNotExistsAndSettingFileShouldExists()
        {
            // Arrange
            var settings = new DiskStorageSettings
            {
                FileName = Path.Combine(_tempPathName, "DiskStorageTest_MovedFile.txt")
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);

            // Create file
            var fileToMoveFileName = Path.Combine(_tempPathName, "DiskStorageTest_FileToMove.txt");
            var fileStream = File.Create(fileToMoveFileName);

            // Write string to file
            const string testString = "Test string\r\nIn 2 lines.";
            fileStream.Write(Encoding.UTF8.GetBytes(testString));
            fileStream.Seek(0, SeekOrigin.Begin);

            // Act
            diskStorage.Store(fileStream, DateTime.MinValue, DateTime.MinValue);

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
                FileName = Path.Combine(_tempPathName, "DiskStorageTest_NewFile.txt")
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);


            var stream = new MemoryStream();

            const string testString = "Test string\r\nIn 2 lines.";
            stream.Write(Encoding.UTF8.GetBytes(testString));
            stream.Seek(0, SeekOrigin.Begin);

            // Act
            diskStorage.Store(stream, DateTime.MinValue, DateTime.MinValue);

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
            const string startDatePlaceholder = "{StartDate:yyyyMMdd}";
            const string endDatePlaceholder = "{EndDate:yyyyMMdd}";
            const string datePlaceHolder = "{date:yyyyMMdd}";
            const string timestampPlaceHolder = "{timestamp:yyyyMMdd}";
            var settings = new DiskStorageSettings
            {
                FileName = Path.Combine(_tempPathName, $"DiskStorageTest_NewFile_{startDatePlaceholder}_{endDatePlaceholder}_{datePlaceHolder}_{timestampPlaceHolder}.txt")
            };

            var logger = new Mock<ILogger>();

            var diskStorage = new DiskStorage(settings, logger.Object);


            var stream = new MemoryStream();

            // Act
            diskStorage.Store(stream, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(-1));

            // Assert
            var correctFileName = settings.FileName
                .Replace(startDatePlaceholder, DateTime.Today.AddDays(-2).ToString("yyyyMMdd"))
                .Replace(endDatePlaceholder, DateTime.Today.AddDays(-1).ToString("yyyyMMdd"))
                .Replace(datePlaceHolder, DateTime.Now.ToString("yyyyMMdd"))
                .Replace(timestampPlaceHolder, DateTime.Now.ToString("yyyyMMdd"));

            Assert.True(File.Exists(correctFileName));

            // Cleanup
            File.Delete(correctFileName);
        }
    }
}
