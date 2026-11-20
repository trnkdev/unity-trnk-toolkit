using TMPro;

namespace TRnK.Extensions
{
    /// <summary>Zero-alloc time setters: digits are written into a reused buffer and copied by TMP_Text.SetText(char[]).</summary>
    public static class TMPTextExtensions
    {
        /// <summary>Sets text to "HH:MM:SS" from seconds; hours keep counting past a day ("27:25:10").</summary>
        public static void SetClock(this TMP_Text text, int totalSeconds)
        {
            if (text == null) return;

            char[] buffer = TimeExtensions.FormatBuffer;
            text.SetText(buffer, 0, TimeExtensions.WriteClock(totalSeconds, buffer));
        }

        /// <summary>Sets text to "HH:MM:SS" from seconds; hours keep counting past a day ("27:25:10").</summary>
        public static void SetClock(this TMP_Text text, float totalSeconds, bool useCeiling = true)
        {
            SetClock(text, TimeExtensions.ToWholeSeconds(totalSeconds, useCeiling));
        }

        /// <summary>Sets text to "MM:SS" from seconds; minutes keep counting past an hour ("125:30").</summary>
        public static void SetShortClock(this TMP_Text text, int totalSeconds)
        {
            if (text == null) return;

            char[] buffer = TimeExtensions.FormatBuffer;
            text.SetText(buffer, 0, TimeExtensions.WriteShortClock(totalSeconds, buffer));
        }

        /// <summary>Sets text to "MM:SS" from seconds; minutes keep counting past an hour ("125:30").</summary>
        public static void SetShortClock(this TMP_Text text, float totalSeconds, bool useCeiling = true)
        {
            SetShortClock(text, TimeExtensions.ToWholeSeconds(totalSeconds, useCeiling));
        }

        /// <summary>Sets text to a duration from its largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static void SetReadableTime(this TMP_Text text, int totalSeconds, bool useSpacing = true,
                                           int units = TimeExtensions.DefaultUnits)
        {
            if (text == null) return;

            char[] buffer = TimeExtensions.FormatBuffer;
            text.SetText(buffer, 0, TimeExtensions.WriteReadable(totalSeconds, useSpacing, units, buffer));
        }

        /// <summary>Sets text to a duration from its largest unit, showing up to <paramref name="units"/> units ("1d 14h 20m", or "1d 14h" with 2).</summary>
        public static void SetReadableTime(this TMP_Text text, float totalSeconds, bool useSpacing = true,
                                           bool useCeiling = true, int units = TimeExtensions.DefaultUnits)
        {
            SetReadableTime(text, TimeExtensions.ToWholeSeconds(totalSeconds, useCeiling), useSpacing, units);
        }
    }
}
