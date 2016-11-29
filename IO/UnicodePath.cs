using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apex.Win32;
using System.IO;
using System.Reflection;

namespace Apex.IO
{
    public class UnicodePath
    {
        internal const int MAX_PATH = 32000;

        private static BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static MethodInfo NormalizePathMethod;
        private static MethodInfo GetRootLengthMethod;
        private static MethodInfo CheckSearchPatternMethod;
        private static MethodInfo IsDirectorySeparatorMethod;

        public static char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
        public static char VolumeSeparatorChar = Path.VolumeSeparatorChar;
        
        /// <summary>
        /// Formats a path to the unicode extension, or if the Windows 10 path extension is enabled, leaves it as is so that
        /// relative links continue to function.
        /// </summary>
        /// <param name="Path">Path to format.</param>
        /// <returns>Formatted path.</returns>
        public static string FormatPath(string Path)
        {
            if (UnicodeStatic.Win10PathExtensionEnabled)
            {
                if (Path.StartsWith(@"\\?\"))
                {
                    return Path.Substring(4);
                }
                else
                {
                    return Path;
                }
            }
            else
            {
                if (Path.StartsWith(@"\\?\"))
                {
                    return Path;
                }
                else
                {
                    return Path.Length > 260 ? $@"\\?\{Path}" : Path;
                }
            }
        }

        public static string GetFileName(string FileName)
        {
            if (FileName == null)
            {
                return null;
            }

            var trimmed = FileName.TrimEnd();

            if (trimmed.LastIndexOf(DirectorySeparatorChar) > -1)
            {
                // Return past last slash.
                return trimmed.Substring(trimmed.LastIndexOf(DirectorySeparatorChar) + 1);
            }
            else if (trimmed.LastIndexOf(AltDirectorySeparatorChar) > -1)
            {
                // Return past last slash.
                return trimmed.Substring(trimmed.LastIndexOf(AltDirectorySeparatorChar) + 1);
            }
            else if (trimmed.LastIndexOf(VolumeSeparatorChar) > -1)
            {
                // Return past semicolon.
                return trimmed.Substring(trimmed.LastIndexOf(VolumeSeparatorChar) + 1);
            }

            return FileName;
        }

        public static string GetFileNameWithoutExtension(string FileName)
        {
            FileName = GetFileName(FileName);
            if (FileName == null)
            {
                return null;
            }

            var index = FileName.LastIndexOf('.');
            return index == -1 ? FileName : FileName.Substring(0, index);
        }

        public static string GetDirectoryName(string FileName)
        {
            if (FileName != null)
            {
                FileName = NormalisePath(FileName, true);

                var root = GetRootLength(FileName);
                var len = FileName.Length;
                if (len > root)
                {
                    while (len > root && FileName[--len] != DirectorySeparatorChar && FileName[len] != AltDirectorySeparatorChar)
                        ;

                    return FileName.Substring(0, len);
                }
            }

            return null;
        }

        public static string GetPathRoot(string FileName)
        {
            if (FileName == null)
            {
                return null;
            }

            FileName = NormalisePath(FileName, true);
            return FileName.Substring(0, GetRootLength(FileName));
        }

        internal static string NormalisePath(string Path, bool FullCheck)
        {
            // Just roughly. 
            return NormalisePath(Path, FullCheck, MAX_PATH, true);
        }

        internal static string NormalisePath(string Path, bool FullCheck, bool ExpandShortPaths)
        {
            // Just roughly.
            return NormalisePath(Path, FullCheck, MAX_PATH, ExpandShortPaths);
        }

        internal static string NormalisePath(string Path, bool FullCheck, int MaxPathLength)
        {
            return NormalisePath(Path, FullCheck, MaxPathLength, true);
        }

        internal static string NormalisePath(string Path, bool FullCheck, int MaxPathLength, bool ExpandShortPaths)
        {
            if (NormalizePathMethod == null)
            {
                NormalizePathMethod = typeof(Path).GetMethod("NormalizePath", flags, null, new Type[] { typeof(string), typeof(bool), typeof(int), typeof(bool) }, null);
            }

            return (string)NormalizePathMethod.Invoke(null, new object[] { Path, FullCheck, MaxPathLength, ExpandShortPaths });
        }

        internal static int GetRootLength(string FileName)
        {
            if (GetRootLengthMethod == null)
            {
                GetRootLengthMethod = typeof(Path).GetMethod(nameof(GetRootLength), flags, null, new Type[] { typeof(string) }, null);
            }

            return (int)GetRootLengthMethod.Invoke(null, new object[] { FileName });
        }

        internal static void CheckSearchPattern(string SearchPattern)
        {
            if (CheckSearchPatternMethod == null)
            {
                CheckSearchPatternMethod = typeof(Path).GetMethod(nameof(CheckSearchPattern), flags, null, new Type[] { typeof(string) }, null);
            }

            CheckSearchPatternMethod.Invoke(null, new object[] { SearchPattern });
        }

        internal static bool IsDirectorySeparator(char Char)
        {
            if (IsDirectorySeparatorMethod == null)
            {
                IsDirectorySeparatorMethod = typeof(Path).GetMethod(nameof(IsDirectorySeparator), flags, null, new Type[] { typeof(char) }, null);
            }

            return (bool)IsDirectorySeparatorMethod.Invoke(null, new object[] { Char });
        }
    }
}
