using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Apex.Win32;
using System.Runtime.InteropServices;

namespace Apex.IO
{
    public class UnicodeDirectoryInfo : UnicodeFileSystemInfo
    {
        public string Parent { get; }
        public string Root { get; }

        public override bool Exists
        {
            get
            {
                try
                {
                    if (dataInitialised == -1)
                    {
                        Refresh();
                    }
                    if (dataInitialised != 0)
                    {
                        return false;
                    }

                    return data.fileAttributes != -1 && (data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) != 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override string Name
        {
            get
            {
                return GetDirName(path);
            }
        }

        public UnicodeDirectoryInfo(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            Init(Path);
        }

        private void Init(string Path)
        {
            // For some reason <DriveLetter>: points to . instead.
            path = (Path.Length == 2 && Path[1] == ':') ? "." : Path;
        }

        public void Create()
        {
            UnicodeDirectory.CreateDirectory(path);
        }

        public UnicodeDirectoryInfo CreateSubdirectory(string Subdirectory)
        {
            if (Subdirectory == null)
            {
                throw new ArgumentNullException(nameof(Subdirectory));
            }

            var fullPath = UnicodePath.NormalisePath(Path.Combine(path, Subdirectory), true);
            UnicodeDirectory.CreateDirectory(fullPath);

            return new UnicodeDirectoryInfo(fullPath);
        }

        public override void Delete()
        {
            Delete(false);
        }

        public void Delete(bool Recursive)
        {
            UnicodeDirectory.Delete(path, Recursive);
        }

        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories()
        {
            return EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateDirectoryInfoIterator(path, SearchPattern, SearchOption);
        }

        public IEnumerable<UnicodeFileInfo> EnumerateFiles()
        {
            return EnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeFileInfo> EnumerateFiles(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeFileInfo> EnumerateFiles(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileInfoIterator(path, SearchPattern, SearchOption);
        }

        public IEnumerable<UnicodeFileSystemInfo> EnumerateFileSystemInfos()
        {
            return EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeFileSystemInfo> EnumerateFileSystemInfos(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemInfos(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<UnicodeFileSystemInfo> EnumerateFileSystemInfos(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileSystemInfoIterator(path, SearchPattern, SearchOption);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public UnicodeDirectoryInfo[] GetDirectories()
        {
            return EnumerateDirectories().ToArray();
        }

        public UnicodeDirectoryInfo[] GetDirectories(string SearchPattern)
        {
            return EnumerateDirectories(SearchPattern).ToArray();
        }

        public UnicodeDirectoryInfo[] GetDirectories(string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateDirectories(SearchPattern, SearchOption).ToArray();
        }

        public UnicodeFileInfo[] GetFiles()
        {
            return EnumerateFiles().ToArray();
        }

        public UnicodeFileInfo[] GetFiles(string SearchPattern)
        {
            return EnumerateFiles(SearchPattern).ToArray();
        }

        public UnicodeFileInfo[] GetFiles(string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateFiles(SearchPattern, SearchOption).ToArray();
        }

        public UnicodeFileSystemInfo[] GetFileSystemInfos()
        {
            return EnumerateFileSystemInfos().ToArray();
        }

        public UnicodeFileSystemInfo[] GetFileSystemInfos(string SearchPattern)
        {
            return EnumerateFileSystemInfos(SearchPattern).ToArray();
        }

        public UnicodeFileSystemInfo[] GetFileSystemInfos(string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateFileSystemInfos(SearchPattern, SearchOption).ToArray();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void MoveTo(string NewPath)
        {
            if (NewPath == null)
            {
                throw new ArgumentNullException(nameof(NewPath));
            }
            if (NewPath.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(NewPath));
            }

            var fullNewPath = UnicodePath.NormalisePath(NewPath, true);

            if (fullNewPath[fullNewPath.Length - 1] != Path.DirectorySeparatorChar)
            {
                fullNewPath = fullNewPath + Path.DirectorySeparatorChar;
            }

            var fullOldPath = UnicodePath.NormalisePath(path, true);

            if (fullOldPath[fullOldPath.Length - 1] != Path.DirectorySeparatorChar)
            {
                fullOldPath = fullOldPath + Path.DirectorySeparatorChar;
            }

            if (fullOldPath.Length > UnicodePath.MAX_PATH)
            {
                throw new PathTooLongException(OpenEnvironment.GetResourceString("IO.PathTooLong"));
            }
            if (fullNewPath.Length > UnicodePath.MAX_PATH)
            {
                throw new PathTooLongException(OpenEnvironment.GetResourceString("IO.PathTooLong"));
            }

            if (string.Compare(fullOldPath, fullNewPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new IOException(OpenEnvironment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));
            }

            // Can't move files over device or partition boundaries (C:\ -> D:\ etc).
            if (string.Compare(UnicodePath.GetPathRoot(fullOldPath), UnicodePath.GetPathRoot(fullNewPath), StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new IOException(OpenEnvironment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
            }

            if (!Win32Wrapper.MoveFile(fullOldPath, fullNewPath))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    error = Win32Wrapper.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(error, fullOldPath);
                }

                // Dat Win95 compatability.
                if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                {
                    throw new IOException(OpenEnvironment.GetResourceString("UnauthorizedAccess_IODenied_Path", fullOldPath), Win32Wrapper.MakeHRFromErrorCode(error));
                }

                __Error.WinIOError(error, string.Empty);
            }

            path = NewPath;
            dataInitialised = -1;
        }

        private static string GetDirName(string Path)
        {
            if (Path.Length > 3)
            {
                if (Path[Path.Length - 1] == UnicodePath.DirectorySeparatorChar || Path[Path.Length - 1] == UnicodePath.AltDirectorySeparatorChar)
                {
                    return UnicodePath.GetFileName(Path.Substring(0, Path.Length - 1));
                }
                else
                {
                    return UnicodePath.GetFileName(Path);
                }
            }
            else
            {
                return Path;
            }
        }
    }
}
