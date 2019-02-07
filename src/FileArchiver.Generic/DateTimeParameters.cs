using System;

namespace FileArchiver.Generic
{
    public class DateTimeParameters
    {
        public string Year { get; set; }
        public string Month { get; set; }
        public string Day { get; set; }

        public string Hour { get; set; }
        public string Minute { get; set; }
        public string Second { get; set; }
        public string Millisecond { get; set; }

        public DayOfWeek FirstDayOfWeek { get; set; }
    }
}
