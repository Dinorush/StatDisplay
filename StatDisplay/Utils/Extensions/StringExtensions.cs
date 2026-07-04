using System;

namespace StatDisplay.Utils.Extensions
{
    internal static class StringExtensions
    {
        public static T ToEnum<T>(this string? value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;

            return Enum.TryParse(value.Replace(" ", null), true, out T result) ? result : defaultValue;
        }

        public static bool TryToEnum<T>(this string? value, out T enumValue) where T : struct
        {
            enumValue = default;
            if (string.IsNullOrEmpty(value)) return false;

            return Enum.TryParse(value.Replace(" ", null), true, out enumValue);
        }
    }
}
