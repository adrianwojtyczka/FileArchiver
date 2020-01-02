using System;
using Xunit;
using static FileArchiver.Generic.Utils;

namespace FileArchiver.Generic.Tests
{
    public class UtilsTest
    {
        #region Evaluate string function

        [Fact]
        public void EvaluateString_WithoutEvaluateFunction_ShouldThrowArgumentException()
        {
            const string testString = "Test string without evaluate function.";

            var exception = Record.Exception(() => Utils.EvaluateString(testString, null));

            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void EvaluateString_WithoutPlaceholders_ShouldBeTheSame()
        {
            const string testString = "Test string without placeholders.";

            var evaluatedString = Utils.EvaluateString(testString, PlaceholderEvaluateFunction);

            Assert.Equal(testString, evaluatedString);
        }

        [Fact]
        public void EvaluateString_WithDatePlaceholderWithoutFormat_PlaceholderShouldBeReplacedWithCorrectValue()
        {
            const string testString = "Test with {date} placeholder.";
            var expectedString = testString.Replace("{date}", System.DateTime.Today.ToString());

            var evaluatedString = Utils.EvaluateString(testString, PlaceholderEvaluateFunction);

            Assert.Equal(expectedString, evaluatedString);
        }

        [Fact]
        public void EvaluateString_WithDatePlaceholderWithFormat_PlaceholderShouldBeReplacedWithCorrectValueInGivenFormat()
        {
            const string testString = "Test with {date:yyyyMMdd} placeholder.";
            var expectedString = testString.Replace("{date:yyyyMMdd}", System.DateTime.Today.ToString("yyyyMMdd"));

            var evaluatedString = Utils.EvaluateString(testString, PlaceholderEvaluateFunction);

            Assert.Equal(expectedString, evaluatedString);
        }

        [Fact]
        public void EvaluateString_WithUnknownPlaceholder_PlaceholderShouldBeReplacedWithEmptyString()
        {
            const string testString = "Test with {unknown} placeholder.";
            var expectedString = testString.Replace("{unknown}", string.Empty);

            var evaluatedString = Utils.EvaluateString(testString, PlaceholderEvaluateFunction);

            Assert.Equal(expectedString, evaluatedString);
        }

        [Fact]
        public void EvaluateString_WithDateAndUnknownPlaceholder_PlaceholderShouldBeReplacedCorrectly()
        {
            const string testString = "Test with {date} and {unknown} placeholder.";
            var expectedString = testString
                .Replace("{date}", DateTime.Today.ToString())
                .Replace("{unknown}", string.Empty);

            var evaluatedString = Utils.EvaluateString(testString, PlaceholderEvaluateFunction);

            Assert.Equal(expectedString, evaluatedString);
        }

        private string PlaceholderEvaluateFunction(string placeholder, string name, string format)
        {
            switch (name)
            {
                case "date":
                    return DateTime.Today.ToString(format);

                default:
                    return string.Empty;
            }
        }

        #endregion

        #region Date time functions

        [Theory]
        [InlineData(Month.January, "2019-01-01", 0)]
        [InlineData(Month.January, "2019-12-01", 12)]
        public void GetMonthDifference(Month month, DateTime dateTime, int result)
        {
            var difference = Utils.GetMonthDifference(month, dateTime);
        }

        #endregion
    }
}
