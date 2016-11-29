using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Apex.Win32;
using System.Runtime.InteropServices;

namespace Apex.IO
{
    /// <summary>
    /// Provides information about a directory.
    /// </summary>
    public class UnicodeDirectoryInfo : UnicodeFileSystemInfo
    {
        /// <summary>
        /// Gets the directory info for the parent directory.
        /// </summary>
        public UnicodeDirectoryInfo Parent
        {
            get
            {
                var parent = path;
                if (parent.Length > 3 && parent[parent.Length - 1] == Path.DirectorySeparatorChar)
                {
                    parent = parent.Substring(0, parent.Length - 1);
                }
                parent = UnicodePath.GetDirectoryName(parent);

                if (parent == null)
                {
                    return null;
                }

                return new UnicodeDirectoryInfo(parent);
            }
        }

        /// <summary>
        /// Gets the directory info for the root.
        /// </summary>
        public UnicodeDirectoryInfo Root
        {
            get
            {
                var root = UnicodePath.GetRootLength(path);

                return new UnicodeDirectoryInfo(path.Substring(0, root));
            }
        }

        /// <summary>
        /// Checks whether the directory specifed by this directory info exists on disk.
        /// </summary>
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

        /// <summary>
        /// The name of the directory.
        /// </summary>
        public override string Name
        {
            get
            {
                return GetDirName(path);
            }
        }

        /// <summary>
        /// Creates a new directory info object from the specified path.
        /// </summary>
        /// <param name="Path">Path to the directory.</param>
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

        /// <summary>
        /// Creates the directory pointed to by this directory info.
        /// </summary>
        public void Create()
        {
            UnicodeDirectory.CreateDirectory(path);
        }

        /// <summary>
        /// Creates a subdirectory of this directory info.
        /// </summary>
        /// <param name="Subdirectory">The subdirectory to create.</param>
        /// <returns>The directory info of the new directory.</returns>
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

        /// <summary>
        /// Deletes the current directory if it's empty.
        /// </summary>
        public override void Delete()
        {
            Delete(false);
        }

        /// <summary>
        /// Deletes the current directory and optionally recursing.
        /// </summary>
        /// <param name="Recursive">Whether to delete directory contents too.</param>
        public void Delete(bool Recursive)
        {
            UnicodeDirectory.Delete(path, Recursive);
        }

        /// <summary>
        /// Enumerates all directories in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>IEnumerable of directory info.</returns>
        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories()
        {
            return EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all directories in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>IEnumerable of directory info.</returns>
        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all directories in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>IEnumerable of directory info.</returns>
        public IEnumerable<UnicodeDirectoryInfo> EnumerateDirectories(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateDirectoryInfoIterator(path, SearchPattern, SearchOption);
        }

        /// <summary>
        /// Enumerates all files in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>IEnumerable of file info.</returns>
        public IEnumerable<UnicodeFileInfo> EnumerateFiles()
        {
            return EnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all files in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>IEnumerable of file info.</returns>
        public IEnumerable<UnicodeFileInfo> EnumerateFiles(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all files in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>IEnumerable of file info.</returns>
        public IEnumerable<UnicodeFileInfo> EnumerateFiles(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileInfoIterator(path, SearchPattern, SearchOption);
        }

        /// <summary>
        /// Enumerates all file system info in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>IEnumerable of file system info.</returns>
        public IEnumerable<UnicodeFileSystemInfo> EnumerateFileSystemInfos()
        {
            return EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all file system info in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>IEnumerable of file system info.</returns>
        public IEnumerable<UnicodeFileSystemInfo> EnumerateFileSystemInfos(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemInfos(SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates all file system info in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>IEnumerable of file system info.</returns>
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

        /// <summary>
        /// Gets all directories in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>Array of directory info.</returns>
        public UnicodeDirectoryInfo[] GetDirectories()
        {
            return EnumerateDirectories().ToArray();
        }

        /// <summary>
        /// Gets all directories in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>Array of directory info.</returns>
        public UnicodeDirectoryInfo[] GetDirectories(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all directories in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of directory info.</returns>
        public UnicodeDirectoryInfo[] GetDirectories(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(SearchPattern, SearchOption).ToArray();
        }

        /// <summary>
        /// Gets all files in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>Array of file info.</returns>
        public UnicodeFileInfo[] GetFiles()
        {
            return EnumerateFiles().ToArray();
        }

        /// <summary>
        /// Gets all files in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>Array of file info.</returns>
        public UnicodeFileInfo[] GetFiles(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all files in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of file info.</returns>
        public UnicodeFileInfo[] GetFiles(string SearchPattern, SearchOption SearchOption)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(SearchPattern, SearchOption).ToArray();
        }

        /// <summary>
        /// Gets all file system entries in this directory without descending into subdirectories.
        /// </summary>
        /// <returns>Array of file system info.</returns>
        public UnicodeFileSystemInfo[] GetFileSystemInfos()
        {
            return EnumerateFileSystemInfos().ToArray();
        }

        /// <summary>
        /// Gets all the file system entries in this directory without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>Array of file system info.</returns>
        public UnicodeFileSystemInfo[] GetFileSystemInfos(string SearchPattern)
        {
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemInfos(SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all the file system entries in this directory using the specified search pattern and search option.
        /// </summary>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of file system info.</returns>
        public UnicodeFileSystemInfo[] GetFileSystemInfos(string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateFileSystemInfos(SearchPattern, SearchOption).ToArray();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Moves this directory to a new location. Cannot be moved across file system boundaries.
        /// </summary>
        /// <param name="NewPath">The path to move this directory to.</param>
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
