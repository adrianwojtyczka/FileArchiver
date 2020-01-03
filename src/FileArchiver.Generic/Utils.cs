using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FileArchiver.Generic
{
    public static class Utils
    {
        #region Enums

        private enum DateTimeScope
        {
            Year,
            Month,
            Day,
            Hour,
            Minute,
            Second,
            Millisecond
        }

        private enum DateTimeOperation
        {
            None,
            SetValue,
            Add
        }

        public enum Month
        {
            January = 1,
            February = 2,
            March = 3,
            April = 4,
            May = 5,
            June = 6,
            July = 7,
            August = 8,
            September = 9,
            October = 10,
            November = 11,
            December = 12
        }

        #endregion

        #region Constants

        private const string First = "First";
        private const string Last = "Last";

        private const string Next = "Next";
        private const string NextOf = "Next{0}";
        private const string Previous = "Previous";
        private const string PreviousOf = "Previous{0}";

        private static readonly int FirstYear = DateTime.MinValue.Year;
        private static readonly int LastYear = DateTime.MaxValue.Year;

        private const int FirstMonth = 1;
        private const int LastMonth = 12;

        private const int FirstDay = 1;

        private const int FirstHour = 0;
        private const int LastHour = 23;

        private const int FirstMinute = 0;
        private const int LastMinute = 59;

        private const int FirstSecond = 0;
        private const int LastSecond = 59;

        private const int FirstMillisecond = 0;
        private const int LastMillisecond = 999;

        private const int MonthsInYear = 12;
        private const int DaysInWeek = 7;


        private const string FileNameSuffix = " ({0})";

        #endregion

        #region Placeholders

        /// <summary>
        /// Delegate for evaluating single placeholder
        /// </summary>
        /// <param name="placeholder">Placeholder to evaluate</param>
        /// <param name="name">Placeholder name</param>
        /// <param name="format">Placeholder format. Null if the format was not provided</param>
        /// <returns>Result of the evaluation</returns>
        public delegate string EvaluatePlaceholder(string placeholder, string name, string format);

        /// <summary>
        /// Evaluate the string and call the placeholder evaluate function for every placeholder found
        /// </summary>
        /// <param name="stringToEvaluate">String to parse</param>
        /// <param name="placeholderEvaluateFunction">Placeholder evaluate function</param>
        /// <returns>Return parsed string</returns>
        public static string EvaluateString(string stringToEvaluate, EvaluatePlaceholder placeholderEvaluateFunction)
        {
            if (placeholderEvaluateFunction == null)
                throw new ArgumentException($"{nameof(placeholderEvaluateFunction)} cannot be null.");

            // Get all defined parameters
            const string placeholderRegexPattern = "({[a-zA-Z0-9]+(:[^}]*)?})";
            var matches = Regex.Matches(stringToEvaluate, placeholderRegexPattern);

            // For each parameter found...
            foreach (Match parameter in matches)
            {
                // Split the placeholder
                var nameFormat = parameter.Value
                    .Trim('{', '}')
                    .Split(':');

                // Retrieve the name and format
                string name = nameFormat[0];
                string format = nameFormat.Length >= 2 ? nameFormat[1] : null;

                // Evaluate placeholder and replace it with the evaluated value
                stringToEvaluate = stringToEvaluate.Replace(parameter.Value, placeholderEvaluateFunction(parameter.Value, name, format));
            }

            // Return evaluated string
            return stringToEvaluate;
        }

        #endregion

        /// <summary>
        /// Get first non-existing file name
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>first non-existing file name</returns>
        public static string GetFirstNonExistingFileName(string fileName)
        {
            if (!File.Exists(fileName))
                return fileName;


            // Get file info and file name without extension
            var fileInfo = new FileInfo(fileName);
            string fileNameWithoutExtension = fileInfo.Name.Replace(fileInfo.Extension, "");

            string newFileName;
            int fileNameCount = 1;
            do
            {
                // Generate new file name
                newFileName = fileNameWithoutExtension + string.Format(FileNameSuffix, fileNameCount++) + fileInfo.Extension;
                newFileName = Path.Combine(fileInfo.DirectoryName, newFileName);

            } while (File.Exists(newFileName));

            // Return file name that doesn't exists yet
            return newFileName;
        }

        #region DateTime methods

        /// <summary>
        /// Calculate date time given the parameters
        /// </summary>
        /// <param name="dateTime">Starting date time</param>
        /// <param name="parameters">Parameters to apply</param>
        /// <returns>Returns date time transformed with parameters</returns>
        public static DateTime CalculateDateTime(DateTime dateTime, DateTimeParameters parameters)
        {
            dateTime = UpdateYear(dateTime, parameters.Year, parameters.FirstDayOfWeek);
            dateTime = UpdateMonth(dateTime, parameters.Month, parameters.FirstDayOfWeek);
            dateTime = UpdateDay(dateTime, parameters.Day, parameters.FirstDayOfWeek);
            dateTime = UpdateHour(dateTime, parameters.Hour, parameters.FirstDayOfWeek);
            dateTime = UpdateMinute(dateTime, parameters.Minute, parameters.FirstDayOfWeek);
            dateTime = UpdateSecond(dateTime, parameters.Second, parameters.FirstDayOfWeek);
            dateTime = UpdateMillisecond(dateTime, parameters.Millisecond, parameters.FirstDayOfWeek);

            return dateTime;
        }

        /// <summary>
        /// Get the difference between destination month and current month in date time
        /// </summary>
        /// <param name="destinationMonth">Destination month</param>
        /// <param name="currentDateTime">Date time to compare</param>
        /// <returns>Returns the difference between destination month and current date time</returns>
        public static int GetMonthDifference(Month destinationMonth, DateTime currentDateTime)
        {
            return GetMonthDifference(destinationMonth, (Month)currentDateTime.Month);
        }

        /// <summary>
        /// Get the difference between destination month and current month
        /// </summary>
        /// <param name="destinationMonth">Destination month</param>
        /// <param name="currentMonth">Month to compare</param>
        /// <returns>Returns the difference between destination month and current month</returns>
        public static int GetMonthDifference(Month destinationMonth, Month currentMonth)
        {
            return (int)destinationMonth - (int)currentMonth;
        }

        /// <summary>
        /// Get the difference between the next destination month and current month in date time
        /// </summary>
        /// <param name="destinationMonth">Next destination month</param>
        /// <param name="currentDateTime">Date time to compare</param>
        /// <returns>Returns the difference between next destination month and current date time</returns>
        public static int GetNextMonthDifference(Month destinationMonth, DateTime currentDateTime)
        {
            return GetNextMonthDifference(destinationMonth, (Month)currentDateTime.Month);
        }

        /// <summary>
        /// Get the difference between the next destination month and current month
        /// </summary>
        /// <param name="destinationMonth">Next destination month</param>
        /// <param name="currentMonth">Month to compare</param>
        /// <returns>Returns the difference between next destination month and current month</returns>
        public static int GetNextMonthDifference(Month destinationMonth, Month currentMonth)
        {
            int difference = GetMonthDifference(destinationMonth, currentMonth);
            if (difference <= 0)
                difference += MonthsInYear;

            return difference;
        }

        /// <summary>
        /// Get the difference between the previous destination month and current month in date time
        /// </summary>
        /// <param name="destinationMonth">Previous destination month</param>
        /// <param name="currentDateTime">Month to compare</param>
        /// <returns>Returns the difference between previous destination month and current date time</returns>
        public static int GetPreviousMonthDifference(Month destinationMonth, DateTime currentDateTime)
        {
            return GetPreviousMonthDifference(destinationMonth, (Month)currentDateTime.Month);
        }

        /// <summary>
        /// Get the difference between the previous destination month and current month
        /// </summary>
        /// <param name="destinationMonth">Previous destination month</param>
        /// <param name="currentMonth">Month to compare</param>
        /// <returns>Returns the difference between previous destination month and current month</returns>
        public static int GetPreviousMonthDifference(Month destinationMonth, Month currentMonth)
        {
            int difference = GetNextMonthDifference(destinationMonth, currentMonth);
            if (difference >= 0)
                difference -= MonthsInYear;

            return difference;
        }

        /// <summary>
        /// Get the difference between destination day of week and current day of week in date time
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDateTime">Current date time</param>
        /// <returns>Returns the difference between destination day of week and current day of week in date time</returns>
        public static int GetDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DateTime currentDateTime)
        {
            return GetDayOfWeekDifference(destinationDayOfWeek, currentDateTime.DayOfWeek);
        }

        /// <summary>
        /// Get the difference between destination day of week and current day of week in date time accordingly to the first day of week
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDateTime">Current date time</param>
        /// <param name="firstDayOfWeek">First day of week</param>
        /// <returns>Returns the difference between destination day of week and current day of week in date time</returns>
        public static int GetDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DateTime currentDateTime, DayOfWeek firstDayOfWeek)
        {
            return GetDayOfWeekDifference(destinationDayOfWeek, currentDateTime.DayOfWeek, firstDayOfWeek);
        }

        /// <summary>
        /// Get the difference between destination day of week and current day of week
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDayOfWeek">Current day of week</param>
        /// <returns>Returns the difference between destination day of week and current day of week</returns>
        public static int GetDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DayOfWeek currentDayOfWeek)
        {
            return (int)destinationDayOfWeek - (int)currentDayOfWeek;
        }

        /// <summary>
        /// Get the difference between destination day of week and current day of week accordingly to the first day of week
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDayOfWeek">Current day of week</param>
        /// <param name="firstDayOfWeek">First day of week</param>
        /// <returns>Returns the difference between destination day of week and current day of week</returns>
        public static int GetDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DayOfWeek currentDayOfWeek, DayOfWeek firstDayOfWeek)
        {
            int difference = (int)destinationDayOfWeek - (int)currentDayOfWeek;
            if ((int)firstDayOfWeek > (int)currentDayOfWeek)
                difference += DaysInWeek;

            return difference;
        }

        /// <summary>
        /// Get the difference between the next destination day of week and current day of week in date time
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDateTime">Current date time</param>
        /// <returns>Returns the difference between the next destination day of week and current day of week in date time</returns>
        public static int GetNextDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DateTime currentDateTime)
        {
            return GetNextDayOfWeekDifference(destinationDayOfWeek, currentDateTime.DayOfWeek);
        }

        /// <summary>
        /// Get the difference between the next destination day of week and current day of week
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDayOfWeek">Current date time</param>
        /// <returns>Returns the difference between the next destination day of week and current day of week</returns>
        public static int GetNextDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DayOfWeek currentDayOfWeek)
        {
            int difference = GetDayOfWeekDifference(destinationDayOfWeek, currentDayOfWeek);
            if (difference <= 0)
                difference += DaysInWeek;

            return difference;
        }

        /// <summary>
        /// Get the difference between the previous destination day of week and current day of week in date time
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDateTime">Current date time</param>
        /// <returns>Returns the difference between the previous destination day of week and current day of week</returns>
        public static int GetPreviousDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DateTime currentDateTime)
        {
            return GetPreviousDayOfWeekDifference(destinationDayOfWeek, currentDateTime.DayOfWeek);
        }

        /// <summary>
        /// Get the difference between the previous destination day of week and current day of week
        /// </summary>
        /// <param name="destinationDayOfWeek">Destination day of week</param>
        /// <param name="currentDayOfWeek">Current day of week</param>
        /// <returns>Returns the difference between the previous destination day of week and current day of week</returns>
        public static int GetPreviousDayOfWeekDifference(DayOfWeek destinationDayOfWeek, DayOfWeek currentDayOfWeek)
        {
            int difference = GetDayOfWeekDifference(destinationDayOfWeek, currentDayOfWeek);
            if (difference > 0)
                difference -= DaysInWeek;

            return difference;
        }

        #region DateTime update methods

        private static DateTime UpdateYear(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Year, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(operationAndValue.Item2, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddYears(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateMonth(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Month, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, operationAndValue.Item2, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddMonths(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateDay(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Day, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, dateTime.Month, operationAndValue.Item2, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddDays(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateHour(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Hour, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, operationAndValue.Item2, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddHours(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateMinute(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Minute, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, operationAndValue.Item2, dateTime.Second, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddMinutes(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateSecond(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Second, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, operationAndValue.Item2, dateTime.Millisecond);

                case DateTimeOperation.Add:
                    return dateTime.AddSeconds(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static DateTime UpdateMillisecond(DateTime dateTime, string parameter, DayOfWeek firstDayOfWeek)
        {
            var operationAndValue = GetDateTimeOperation(parameter, DateTimeScope.Millisecond, firstDayOfWeek, dateTime);
            switch (operationAndValue.Item1)
            {
                case DateTimeOperation.SetValue:
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, operationAndValue.Item2);

                case DateTimeOperation.Add:
                    return dateTime.AddMilliseconds(operationAndValue.Item2);

                case DateTimeOperation.None:
                    return dateTime;

                default:
                    throw new ArgumentException($"Date time operation {parameter} not supported.");
            }
        }

        private static (DateTimeOperation, int) GetDateTimeOperation(string parameter, DateTimeScope scope, DayOfWeek firstDayOfWeek, DateTime referenceDate)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return (DateTimeOperation.None, 0);

            if (parameter[0] == '+' || parameter[0] == '-')
            {
                if (!int.TryParse(parameter, out int result))
                    throw new ArgumentException($"The value following the sign must be an integer. {parameter} given.");

                return (DateTimeOperation.Add, result);
            }

            if (int.TryParse(parameter, out int result2))
                return (DateTimeOperation.SetValue, result2);


            if (parameter.Equals(Previous, StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, -1);

            if (parameter.Equals(Next, StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, 1);

            if (parameter.Equals(First, StringComparison.OrdinalIgnoreCase))
            {
                switch (scope)
                {
                    case DateTimeScope.Year:
                        return (DateTimeOperation.SetValue, FirstYear);

                    case DateTimeScope.Month:
                        return (DateTimeOperation.SetValue, FirstMonth);

                    case DateTimeScope.Day:
                        return (DateTimeOperation.SetValue, FirstDay);

                    case DateTimeScope.Hour:
                        return (DateTimeOperation.SetValue, FirstHour);

                    case DateTimeScope.Minute:
                        return (DateTimeOperation.SetValue, FirstMinute);

                    case DateTimeScope.Second:
                        return (DateTimeOperation.SetValue, FirstSecond);

                    case DateTimeScope.Millisecond:
                        return (DateTimeOperation.SetValue, FirstMillisecond);
                }
            }

            if (parameter.Equals(Last, StringComparison.OrdinalIgnoreCase))
            {
                switch (scope)
                {
                    case DateTimeScope.Year:
                        return (DateTimeOperation.SetValue, LastYear);

                    case DateTimeScope.Month:
                        return (DateTimeOperation.SetValue, LastMonth);

                    case DateTimeScope.Day:
                        return (DateTimeOperation.SetValue, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));

                    case DateTimeScope.Hour:
                        return (DateTimeOperation.SetValue, LastHour);

                    case DateTimeScope.Minute:
                        return (DateTimeOperation.SetValue, LastMinute);

                    case DateTimeScope.Second:
                        return (DateTimeOperation.SetValue, LastSecond);

                    case DateTimeScope.Millisecond:
                        return (DateTimeOperation.SetValue, LastMillisecond);
                }
            }


            switch(scope)
            {
                case DateTimeScope.Month:
                    return GetDateTimeMonthOperation(parameter, referenceDate);

                case DateTimeScope.Day:
                    return GetDateTimeDayOperation(parameter, firstDayOfWeek, referenceDate);
            }

            throw new ArgumentException($"Date time {scope} operation '{parameter}' is not supported.");
        }

        private static (DateTimeOperation, int) GetDateTimeMonthOperation(string parameter, DateTime referenceDate)
        {
            if (parameter.Equals(Month.January.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.January);

            if (parameter.Equals(string.Format(NextOf, Month.January.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.January, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.January.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.January, referenceDate));


            if (parameter.Equals(Month.February.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.February);

            if (parameter.Equals(string.Format(NextOf, Month.February.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.February, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.February.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.February, referenceDate));


            if (parameter.Equals(Month.March.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.March);

            if (parameter.Equals(string.Format(NextOf, Month.March.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.March, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.March.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.March, referenceDate));


            if (parameter.Equals(Month.April.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.April);

            if (parameter.Equals(string.Format(NextOf, Month.April.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.April, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.April.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.April, referenceDate));


            if (parameter.Equals(Month.May.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.May);

            if (parameter.Equals(string.Format(NextOf, Month.May.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.May, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.May.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.May, referenceDate));


            if (parameter.Equals(Month.June.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.June);

            if (parameter.Equals(string.Format(NextOf, Month.June.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.June, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.June.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.June, referenceDate));


            if (parameter.Equals(Month.July.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.July);

            if (parameter.Equals(string.Format(NextOf, Month.July.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.July, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.July.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.July, referenceDate));


            if (parameter.Equals(Month.August.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.August);

            if (parameter.Equals(string.Format(NextOf, Month.August.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.August, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.August.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.August, referenceDate));


            if (parameter.Equals(Month.September.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.September);

            if (parameter.Equals(string.Format(NextOf, Month.September.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.September, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.September.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.September, referenceDate));


            if (parameter.Equals(Month.October.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.October);

            if (parameter.Equals(string.Format(NextOf, Month.October.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.October, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.October.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.October, referenceDate));


            if (parameter.Equals(Month.November.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.November);

            if (parameter.Equals(string.Format(NextOf, Month.November.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.November, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.November.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.November, referenceDate));


            if (parameter.Equals(Month.December.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.SetValue, (int)Month.December);

            if (parameter.Equals(string.Format(NextOf, Month.December.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextMonthDifference(Month.December, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, Month.December.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousMonthDifference(Month.December, referenceDate));


            throw new ArgumentException($"Date time {DateTimeScope.Month} operation '{parameter}' is not supported.");
        }

        private static (DateTimeOperation, int) GetDateTimeDayOperation(string parameter, DayOfWeek firstDayOfWeek, DateTime referenceDate)
        {
            if (parameter.Equals(DayOfWeek.Monday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Monday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Monday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Monday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Monday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Monday, referenceDate));


            if (parameter.Equals(DayOfWeek.Tuesday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Tuesday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Tuesday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Tuesday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Tuesday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Tuesday, referenceDate));


            if (parameter.Equals(DayOfWeek.Wednesday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Wednesday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Wednesday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Wednesday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Wednesday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Wednesday, referenceDate));


            if (parameter.Equals(DayOfWeek.Thursday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Thursday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Thursday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Thursday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Thursday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Thursday, referenceDate));


            if (parameter.Equals(DayOfWeek.Friday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Friday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Friday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Friday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Friday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Friday, referenceDate));


            if (parameter.Equals(DayOfWeek.Saturday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Saturday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Saturday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Saturday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Saturday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Saturday, referenceDate));


            if (parameter.Equals(DayOfWeek.Sunday.ToString(), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetDayOfWeekDifference(DayOfWeek.Sunday, referenceDate, firstDayOfWeek));

            if (parameter.Equals(string.Format(NextOf, DayOfWeek.Sunday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetNextDayOfWeekDifference(DayOfWeek.Sunday, referenceDate));

            if (parameter.Equals(string.Format(PreviousOf, DayOfWeek.Sunday.ToString()), StringComparison.OrdinalIgnoreCase))
                return (DateTimeOperation.Add, GetPreviousDayOfWeekDifference(DayOfWeek.Sunday, referenceDate));


            throw new ArgumentException($"Date time {DateTimeScope.Day} operation '{parameter}' is not supported.");
        }

        #endregion

        #endregion
    }
}
