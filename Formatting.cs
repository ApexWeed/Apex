using System.IO;
using System.Text.RegularExpressions;

namespace Apex
{
    public static class Formatting
    {
        /// <summary>
        /// Formats bytes into base 2 units (B, KiB, MiB...).
        /// </summary>
        /// <param name="Bytes">Number of bytes to format.</param>
        /// <returns>Bytes formatted.</returns>
        public static string FormatBytes(long Bytes)
        {
            var Suffixes = new string[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "YiB" };
            var Multiplier = 0;
            double Value = Bytes;
            while (Value > 1024)
            {
                Value /= 1024;
                Multiplier++;
            }

            return $"{Value:N2} {Suffixes[Multiplier]}";
        }

        static char[] illegalChars = Path.GetInvalidFileNameChars();
        static string illegalPattern = string.Format("[{0}]", Regex.Escape(string.Join("", illegalChars)));
        /// <summary>
        /// Strips out any characters that are forbidden from filenames.
        /// </summary>
        /// <param name="Filename">The filename to check.</param>
        /// <returns>Filtered filename.</returns>
        public static string FormatFilename(string Filename)
        {
            return Regex.Replace(Filename, illegalPattern, "");
        }
    }
}
