using System;

namespace Apex.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Parses a string into an enum.
        /// </summary>
        /// <param name="Value">The string to convert.</param>
        /// <param name="IgnoreCase">Whether to ignore case.</param>
        /// <returns></returns>
        public static T ToEnum<T>(this string Value, bool IgnoreCase = false)
        {
            return (T)Enum.Parse(typeof(T), Value, IgnoreCase);
        }

        public static T ToEnum<T>(this string Value, T DefaultValue, bool IgnoreCase = false) where T : struct
        {
            T parsedValue;
            return Enum.TryParse(Value, IgnoreCase, out parsedValue) ? parsedValue : DefaultValue;
        }
    }
}
