using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apex.Win32;

namespace Apex.IO
{
    public class UnicodePath
    {
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
    }
}
