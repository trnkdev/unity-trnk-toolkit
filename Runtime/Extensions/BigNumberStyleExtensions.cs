using System;
using System.Globalization;

namespace TRnK.Extensions
{
    /// <summary>Display style for big-number formatting.</summary>
    public enum BigNumberStyle
    {
        /// <summary>K/M/B/T up to 1e12, then doubled-letter suffixes (aa, bb, cc, ...). Covers up to ~1e90.</summary>
        Compact,

        /// <summary>Single alphabetic suffixes from 1e3 onward (a, b, c, ..., z). Covers up to ~1e78.</summary>
        Alphabetical,
    }

    public static class BigNumberStyleExtensions
    {
        private const int MaxRoundingDecimals = 28;

        private static readonly string[] s_compactSuffixes =
        {
            "K", "M", "B", "T",
            "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj",
            "kk", "ll", "mm", "nn", "oo", "pp", "qq", "rr", "ss", "tt",
            "uu", "vv", "ww", "xx", "yy", "zz",
        };

        private static readonly string[] s_alphaSuffixes =
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
            "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        };

        /// <summary>Formats the value as a big-number display string in the chosen style.</summary>
        public static string ToBigNumber(this int value, BigNumberStyle style, int decimalPlaces = 1)
            => ToBigNumber((decimal)value, style, decimalPlaces);

        /// <summary>Formats the value as a big-number display string in the chosen style.</summary>
        public static string ToBigNumber(this long value, BigNumberStyle style, int decimalPlaces = 1)
            => ToBigNumber((decimal)value, style, decimalPlaces);

        /// <summary>Formats the value as a big-number display string in the chosen style.</summary>
        public static string ToBigNumber(this decimal value, BigNumberStyle style, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "decimalPlaces cannot be negative.");

            bool negative = value < 0m;
            if (negative) value = -value;

            if (value < 1000m)
                return (negative ? "-" : string.Empty) + FormatSmall(value);

            string[] suffixes = style == BigNumberStyle.Alphabetical ? s_alphaSuffixes : s_compactSuffixes;

            int tier = 0;
            // Saturate at the largest suffix: stop dividing once we'd exceed the table.
            while (value >= 1000m && tier < suffixes.Length)
            {
                value /= 1000m;
                tier++;
            }

            // Rounding can carry into the next tier: 999.96K would print "1000K" instead of "1M"
            decimal rounded = Math.Round(value, Math.Min(decimalPlaces, MaxRoundingDecimals), MidpointRounding.AwayFromZero);
            if (rounded >= 1000m && tier < suffixes.Length)
            {
                value /= 1000m;
                tier++;
            }

            string suffix = suffixes[tier - 1];
            return (negative ? "-" : string.Empty) + TrimTrailing(value, decimalPlaces) + suffix;
        }

        /// <summary>Formats sub-1000 values with trailing zeros trimmed.</summary>
        private static string FormatSmall(decimal value)
        {
            return value == Math.Floor(value)
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        /// <summary>Formats with the given decimal places and trims trailing zeros.</summary>
        private static string TrimTrailing(decimal value, int decimalPlaces)
        {
            if (decimalPlaces == 0)
                return value.ToString("F0", CultureInfo.InvariantCulture);

            string s = value.ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
            if (s.IndexOf('.') >= 0) s = s.TrimEnd('0').TrimEnd('.');
            return s;
        }
    }
}
