using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TRnK.Logger;

namespace TRnK.Extensions
{
    public static class StringExtensions
    {
        /// <summary>Parses a string with comma as decimal separator to float.</summary>
        public static float ParseFloatWithComma(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input string cannot be null or empty.", nameof(input));
            }

            string trimmedInput = input.Trim();

            string standardizedInput = trimmedInput.Replace(",", ".");

            if (float.TryParse(standardizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }
            else
            {
                throw new FormatException($"Invalid input string: \"{input}\".  Could not parse to float.");
            }
        }

        /// <summary>Non-throwing version of ParseFloatWithComma.</summary>
        public static bool TryParseFloatWithComma(this string input, out float result)
        {
            result = 0f;

            if (string.IsNullOrEmpty(input))
                return false;

            string trimmedInput = input.Trim();

            string standardizedInput = trimmedInput.Replace(",", ".");

            return float.TryParse(standardizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>Formats a number directly as percentage (25 → 25%).</summary>
        public static string AsExactPercent(this float value, int decimalPlaces = 0)
        {
            var format = decimalPlaces > 0 ? "0." + new string('#', decimalPlaces) : "0";
            return value.ToString(format, CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>Converts floating value to percentage string (0.5f → 50%).</summary>
        public static string AsPercent(this float value, int decimalPlaces = 0)
        {
            var format = decimalPlaces > 0 ? "0." + new string('#', decimalPlaces) : "0";
            return (value * 100).ToString(format, CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>Removes all spaces from string. Null-safe.</summary>
        public static string WithoutSpaces(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            return str.Replace(" ", string.Empty);
        }

        private static readonly Regex s_uppercaseSplitRegex = new(@"(?<!^)(?=[A-Z])", RegexOptions.Compiled);

        /// <summary>Splits camelCase/PascalCase by inserting spaces before uppercase letters.</summary>
        public static string SplitCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            var split = s_uppercaseSplitRegex.Split(str);
            return string.Join(" ", split);
        }

        /// <summary>Converts a string to the specified enum type.</summary>
        public static T ToEnum<T>(this string value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, out var result))
                return result;

            throw new ArgumentException($"Unable to parse '{value}' as {typeof(T).Name}");
        }

        /// <summary>Converts a string to the specified enum type, or returns a default value if the conversion fails.</summary>
        public static T ToEnumOrDefault<T>(this string value, T defaultValue = default) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Log.Warn($"Null or empty string passed for enum {typeof(T).Name}, using default: {defaultValue}");
                return defaultValue;
            }

            if (Enum.TryParse<T>(value, out var result))
                return result;

            Log.Warn($"Failed to parse '{value}' to enum {typeof(T).Name}, using default: {defaultValue}");
            return defaultValue;
        }
    }
}
