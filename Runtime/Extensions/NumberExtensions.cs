using System;
using TRnK.Logger;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TRnK.Extensions
{
    public static class NumberExtensions
    {
        /// <summary>Returns the percentage of the current value relative to the total.</summary>
        public static float PercentageOf(this int current, int total)
        {
            if (total == 0)
                return 0;

            return Mathf.Clamp01((float)current / total);
        }

        /// <summary>Returns the percentage of the current value relative to the total.</summary>
        public static float PercentageOf(this float current, float total)
        {
            if (total == 0)
                return 0;

            return Mathf.Clamp01(current / total);
        }

        /// <summary>Returns true with the given probability (boundary-exact at min and max).</summary>
        public static bool IsSuccessfulRoll(this float probability, float min = 0f, float max = 1f)
        {
            if (probability < min || probability > max)
            {
                throw new ArgumentException($"probability '{probability}' must be in range [{min}, {max}]");
            }

            if (probability <= min) return false;
            if (probability >= max) return true;

            return Random.Range(min, max) <= probability;
        }

        /// <summary>Returns true if a random roll succeeds based on the percentage rate.</summary>
        public static bool IsSuccessfulRoll(this int probability, int min = 0, int max = 100)
        {
            if (probability < min || probability > max)
            {
                throw new ArgumentException($"percentage '{probability}' must be in range [{min}, {max}]");
            }

            // Random.Range(int, int) is [min, maxExclusive), so Range(min, max) yields [min, max-1].
            // Comparing with < probability gives exact boundary behaviour:
            //   probability == min  → 0%  (roll can never be < min)
            //   probability == max  → 100% (roll is always < max)
            var randomRoll = Random.Range(min, max);
            return randomRoll < probability;
        }

        /// <summary>Converts an integer to the specified enum type. Throws if value is not a defined enum member.</summary>
        public static T ToEnum<T>(this int value) where T : struct, Enum
        {
            if (!TryToDefinedEnum(value, out T result))
                throw new ArgumentException($"Value {value} is not defined in enum {typeof(T).Name}");

            return result;
        }

        /// <summary>Converts to enum T, or returns the default if the value is not defined.</summary>
        public static T ToEnumOrDefault<T>(this int value, T defaultValue = default) where T : struct, Enum
        {
            if (!TryToDefinedEnum(value, out T result))
            {
                Log.Warn($"Value {value} is not defined in enum {typeof(T).Name}, using default: {defaultValue}");
                return defaultValue;
            }

            return result;
        }

        /// <summary>Converts through the enum's underlying type, so enums not stored as int work too.</summary>
        private static bool TryToDefinedEnum<T>(int value, out T result) where T : struct, Enum
        {
            result = default;
            object underlying;
            try
            {
                underlying = Convert.ChangeType(value, Enum.GetUnderlyingType(typeof(T)));
            }
            catch (OverflowException)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(T), underlying))
                return false;

            result = (T)Enum.ToObject(typeof(T), underlying);
            return true;
        }
    }
}
