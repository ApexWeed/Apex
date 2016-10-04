using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex
{
    public static class FileUtil
    {
        public static List<string> GetFiles(string Directory)
        {
            var files = new List<string>();

            files.AddRange(System.IO.Directory.GetFiles(Directory));

            foreach (var subDir in System.IO.Directory.GetDirectories(Directory))
            {
                files.AddRange(GetFiles(subDir));
            }

            return files;
        }
    }
}
