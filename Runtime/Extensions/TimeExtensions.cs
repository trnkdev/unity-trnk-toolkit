using System;

namespace TRnK.Extensions
{
    public static class TimeExtensions
    {
        internal const int MaxUnits = 3;
        internal const int DefaultUnits = MaxUnits;

        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = 3600;
        private const int SecondsPerDay = 86400;
        private const int ClockDigits = 2;
        private const int NumberBase = 10;
        private const int BufferSize = 32;
        private const char ZeroDigit = '0';
        private const char ClockSeparator = ':';
        private const char UnitSpacing = ' ';
        private const string UnitSuffixes = "dhms";

        // Shared with TMPTextExtensions so both print the same text; one per thread so worker threads never mix digits
        [ThreadStatic] private static char[] t_formatBuffer;

        internal static char[] FormatBuffer => t_formatBuffer ??= new char[BufferSize];

        #region Time to String Formatting

        /// <summary>Converts seconds to "HH:MM:SS"; hours keep counting past a day ("27:25:10").</summary>
        public static string ToClock(this float time, bool useCeiling = true) => ToWholeSeconds(time, useCeiling).ToClock();

        /// <summary>Converts seconds to "HH:MM:SS"; hours keep counting past a day ("27:25:10").</summary>
        public static string ToClock(this double time, bool useCeiling = true) => ToWholeSeconds(time, useCeiling).ToClock();

        /// <summary>Converts seconds to "HH:MM:SS"; hours keep counting past a day ("27:25:10").</summary>
        public static string ToClock(this int time)
        {
            char[] buffer = FormatBuffer;
            return new string(buffer, 0, WriteClock(time, buffer));
        }

        /// <summary>Converts seconds to "MM:SS"; minutes keep counting past an hour ("125:30").</summary>
        public static string ToShortClock(this float time, bool useCeiling = true) => ToWholeSeconds(time, useCeiling).ToShortClock();

        /// <summary>Converts seconds to "MM:SS"; minutes keep counting past an hour ("125:30").</summary>
        public static string ToShortClock(this double time, bool useCeiling = true) => ToWholeSeconds(time, useCeiling).ToShortClock();

        /// <summary>Converts seconds to "MM:SS"; minutes keep counting past an hour ("125:30").</summary>
        public static string ToShortClock(this int time)
        {
            char[] buffer = FormatBuffer;
            return new string(buffer, 0, WriteShortClock(time, buffer));
        }

        /// <summary>Converts a duration to text from its largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static string ToReadableFormat(this TimeSpan timeSpan, bool useSpacing = true, int units = DefaultUnits) =>
            ToWholeSeconds(timeSpan.TotalSeconds, false).ToReadableFormat(useSpacing, units);

        /// <summary>Converts seconds to text from the largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static string ToReadableFormat(this double seconds, bool useSpacing = true, bool useCeiling = true,
                                              int units = DefaultUnits) =>
            ToWholeSeconds(seconds, useCeiling).ToReadableFormat(useSpacing, units);

        /// <summary>Converts seconds to text from the largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static string ToReadableFormat(this float seconds, bool useSpacing = true, bool useCeiling = true,
                                              int units = DefaultUnits) =>
            ToWholeSeconds(seconds, useCeiling).ToReadableFormat(useSpacing, units);

        /// <summary>Converts seconds to text from the largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static string ToReadableFormat(this int seconds, bool useSpacing = true, int units = DefaultUnits)
        {
            char[] buffer = FormatBuffer;
            return new string(buffer, 0, WriteReadable(seconds, useSpacing, units, buffer));
        }

        internal static int ToWholeSeconds(float seconds, bool useCeiling) => ToWholeSeconds((double)seconds, useCeiling);

        /// <summary>Rounds to whole seconds, clamped to 0..int.MaxValue (NaN gives 0).</summary>
        internal static int ToWholeSeconds(double seconds, bool useCeiling)
        {
            // Casting NaN or out-of-range values to int differs between x64 and ARM, so clamp first
            if (!(seconds > 0d)) return 0;
            double whole = useCeiling ? Math.Ceiling(seconds) : Math.Floor(seconds);
            return whole >= int.MaxValue ? int.MaxValue : (int)whole;
        }

        /// <summary>Writes "HH:MM:SS" with total hours into the buffer and returns its length.</summary>
        internal static int WriteClock(int totalSeconds, char[] buffer)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            int length = WriteNumber(buffer, 0, totalSeconds / SecondsPerHour, ClockDigits);
            buffer[length++] = ClockSeparator;
            length = WriteNumber(buffer, length, totalSeconds % SecondsPerHour / SecondsPerMinute, ClockDigits);
            buffer[length++] = ClockSeparator;
            return WriteNumber(buffer, length, totalSeconds % SecondsPerMinute, ClockDigits);
        }

        /// <summary>Writes "MM:SS" with total minutes into the buffer and returns its length.</summary>
        internal static int WriteShortClock(int totalSeconds, char[] buffer)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            int length = WriteNumber(buffer, 0, totalSeconds / SecondsPerMinute, ClockDigits);
            buffer[length++] = ClockSeparator;
            return WriteNumber(buffer, length, totalSeconds % SecondsPerMinute, ClockDigits);
        }

        /// <summary>Writes the largest non-zero unit and up to units - 1 after it ("1d 3h 25m") and returns the length.</summary>
        internal static int WriteReadable(int totalSeconds, bool useSpacing, int units, char[] buffer)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            int first = totalSeconds >= SecondsPerDay ? 0
                : totalSeconds >= SecondsPerHour ? 1
                : totalSeconds >= SecondsPerMinute ? 2 : 3;
            int end = Math.Min(first + Math.Clamp(units, 1, MaxUnits), UnitSuffixes.Length);

            int length = 0;
            for (int unit = first; unit < end; unit++)
            {
                if (useSpacing && unit > first) buffer[length++] = UnitSpacing;
                length = WriteNumber(buffer, length, UnitValue(totalSeconds, unit), 1);
                buffer[length++] = UnitSuffixes[unit];
            }

            return length;
        }

        // Unit order matches UnitSuffixes: days, hours, minutes, seconds
        private static int UnitValue(int totalSeconds, int unit) => unit switch
        {
            0 => totalSeconds / SecondsPerDay,
            1 => totalSeconds % SecondsPerDay / SecondsPerHour,
            2 => totalSeconds % SecondsPerHour / SecondsPerMinute,
            _ => totalSeconds % SecondsPerMinute,
        };

        // Writes a non-negative number, zero-padded to minDigits, without allocating
        private static int WriteNumber(char[] buffer, int index, int value, int minDigits)
        {
            int digits = 1;
            for (int rest = value / NumberBase; rest > 0; rest /= NumberBase) digits++;
            for (int pad = digits; pad < minDigits; pad++) buffer[index++] = ZeroDigit;

            int end = index + digits;
            for (int i = end - 1; i >= index; i--, value /= NumberBase) buffer[i] = (char)(ZeroDigit + value % NumberBase);
            return end;
        }

        #endregion

        #region DateTime Manipulation

        /// <summary>Returns a new DateTime with modified date components (day clamps to the new month's max).</summary>
        public static DateTime WithDate(this DateTime dt, int? year = null, int? month = null, int? day = null)
        {
            int newYear = year ?? dt.Year;
            int newMonth = month ?? dt.Month;
            int newDay = day ?? dt.Day;

            int daysInMonth = DateTime.DaysInMonth(newYear, newMonth);
            newDay = Math.Min(newDay, daysInMonth);

            return new DateTime(newYear, newMonth, newDay, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        /// <summary>Creates a new DateTime with modified time components.</summary>
        public static DateTime WithTime(this DateTime dt, int? hour = null, int? minute = null, int? second = null, int? millisecond = null)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day,
                hour ?? dt.Hour,
                minute ?? dt.Minute,
                second ?? dt.Second,
                millisecond ?? dt.Millisecond);
        }

        #endregion

        #region DateTime Period Checks

        /// <summary>Checks if the DateTime is at the start of the day (00:00:00).</summary>
        public static bool IsStartOfDay(this DateTime time)
        {
            return time.TimeOfDay == TimeSpan.Zero;
        }

        /// <summary>Checks if the DateTime is at the start of the week (00:00:00 on the given first day; defaults to Monday).</summary>
        public static bool IsStartOfWeek(this DateTime time, DayOfWeek firstDay = DayOfWeek.Monday)
        {
            return time.DayOfWeek == firstDay && time.IsStartOfDay();
        }

        /// <summary>Checks if the DateTime is at the start of the month (1st day 00:00:00).</summary>
        public static bool IsStartOfMonth(this DateTime time)
        {
            return time.Day == 1 && time.IsStartOfDay();
        }

        #endregion
    }
}
