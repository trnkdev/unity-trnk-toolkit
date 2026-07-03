using TMPro;
using UnityEngine;

namespace TRnK.Extensions
{
    public static class TMPTextExtensions
    {
        private const int SecondsPerMinute = 60;
        private const int MinutesPerHour = 60;
        private const int HoursPerDay = 24;

        private const string ClockFormat = "{0:00}:{1:00}:{2:00}";
        private const string ShortClockFormat = "{0:00}:{1:00}";
        private const string DayHourMinuteSpacedFormat = "{0}d {1}h {2}m";
        private const string DayHourMinuteFormat = "{0}d{1}h{2}m";
        private const string HourMinuteSecondSpacedFormat = "{0}h {1}m {2}s";
        private const string HourMinuteSecondFormat = "{0}h{1}m{2}s";
        private const string MinuteSecondSpacedFormat = "{0}m {1}s";
        private const string MinuteSecondFormat = "{0}m{1}s";
        private const string SecondsOnlyFormat = "{0}s";

        /// <summary>Sets text to "HH:MM:SS" from seconds.</summary>
        public static void SetClock(this TMP_Text text, int totalSeconds)
        {
            if (text == null) return;

            if (totalSeconds < 0) totalSeconds = 0;

            int totalMinutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds - (totalMinutes * SecondsPerMinute);

            int totalHours = totalMinutes / MinutesPerHour;
            int minutes = totalMinutes - (totalHours * MinutesPerHour);

            int hours = totalHours % HoursPerDay;

            text.SetText(ClockFormat, hours, minutes, seconds);
        }

        /// <summary>Sets text to "HH:MM:SS" from seconds.</summary>
        public static void SetClock(this TMP_Text text, float totalSeconds, bool useCeiling = true)
        {
            SetClock(text, useCeiling ? Mathf.CeilToInt(totalSeconds) : Mathf.FloorToInt(totalSeconds));
        }

        /// <summary>Sets text to "MM:SS" from seconds.</summary>
        public static void SetShortClock(this TMP_Text text, int totalSeconds)
        {
            if (text == null) return;

            if (totalSeconds < 0) totalSeconds = 0;

            int totalMinutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds - (totalMinutes * SecondsPerMinute);

            int minutes = totalMinutes % MinutesPerHour;

            text.SetText(ShortClockFormat, minutes, seconds);
        }

        /// <summary>Sets text to "MM:SS" from seconds.</summary>
        public static void SetShortClock(this TMP_Text text, float totalSeconds, bool useCeiling = true)
        {
            SetShortClock(text, useCeiling ? Mathf.CeilToInt(totalSeconds) : Mathf.FloorToInt(totalSeconds));
        }

        /// <summary>Sets text to readable duration like "2d 3h 45m" (spacing optional).</summary>
        public static void SetReadableTime(this TMP_Text text, int totalSeconds, bool useSpacing = true)
        {
            if (text == null) return;

            if (totalSeconds < 0) totalSeconds = 0;

            int totalMinutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds - (totalMinutes * SecondsPerMinute);

            int totalHours = totalMinutes / MinutesPerHour;
            int minutes = totalMinutes - (totalHours * MinutesPerHour);

            int totalDays = totalHours / HoursPerDay;
            int hours = totalHours - (totalDays * HoursPerDay);

            if (totalDays >= 1)
            {
                text.SetText(useSpacing ? DayHourMinuteSpacedFormat : DayHourMinuteFormat, totalDays, hours, minutes);
                return;
            }

            if (totalHours >= 1)
            {
                text.SetText(useSpacing ? HourMinuteSecondSpacedFormat : HourMinuteSecondFormat, hours, minutes, seconds);
                return;
            }

            if (totalMinutes >= 1)
            {
                text.SetText(useSpacing ? MinuteSecondSpacedFormat : MinuteSecondFormat, minutes, seconds);
                return;
            }

            text.SetText(SecondsOnlyFormat, seconds);
        }

        /// <summary>Sets text to readable duration like "2d 3h 45m" (spacing optional).</summary>
        public static void SetReadableTime(this TMP_Text text, float totalSeconds, bool useSpacing = true, bool useCeiling = true)
        {
            SetReadableTime(text, useCeiling ? Mathf.CeilToInt(totalSeconds) : Mathf.FloorToInt(totalSeconds), useSpacing);
        }
    }
}
