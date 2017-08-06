using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using Apex.Win32;
using System.Runtime.InteropServices;

namespace Apex.IO
{
    /// <summary>
    /// Provides functions dealing with directories with support for full ~32000 character NTFS path length.
    /// </summary>
    public static class UnicodeDirectory
    {
        /// <summary>
        /// Creates a directory and returns the DirectoryInfo object associated with it.
        /// </summary>
        /// <param name="Path">Path to the directory to create.</param>
        /// <returns>DirectoryInfo object for the created directory.</returns>
        public static DirectoryInfo CreateDirectory(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            InternalCreateDirectory(Path);

            return new DirectoryInfo(Path);
        }

        internal static void InternalCreateDirectory(string Path)
        {
            Path = UnicodePath.NormalisePath(Path, true);
            var len = Path.Length;
            var rootLen = UnicodePath.GetRootLength(Path);

            if (Exists(Path))
            {
                return;
            }

            var dirStack = new Stack<string>();

            if (len > rootLen)
            {
                var pos = len - 1;
                while (len > rootLen)
                {
                    var dir = Path.Substring(0, pos + 1);

                    if (Exists(dir))
                    {
                        break;
                    }
                    else
                    {
                        dirStack.Push(dir);
                    }

                    while (pos > rootLen && Path[pos] != UnicodePath.DirectorySeparatorChar && Path[pos] != UnicodePath.AltDirectorySeparatorChar)
                    {
                        pos--;
                    }
                    pos--;
                }
            }

            if (dirStack.Count > 0)
            {
                var dirList = dirStack.ToArray();
                for (int i = 0; i < dirList.Length; i++)
                {
#pragma warning disable CC0039 // Don't concatenate strings in loops
                    dirList[i] += @"\.";
#pragma warning restore CC0039 // Don't concatenate strings in loops
                }

                UnicodeStatic.DemandIOPermission(FileIOPermissionAccess.Write, dirList, false, false);
            }

            var success = true;
            var firstError = 0;
            var errorString = Path;
            while (dirStack.Count > 0)
            {
                var dir = dirStack.Pop();
                success = Win32Wrapper.CreateDirectory(dir, null);
                if (!success && firstError == 0)
                {
                    var currentError = Marshal.GetLastWin32Error();

                    if (currentError != Win32Wrapper.ERROR_ALREADY_EXISTS)
                    {
                        firstError = currentError;
                    }
                    else
                    {
                        if (UnicodeFile.Exists(dir) || (!Exists(dir) && currentError == Win32Wrapper.ERROR_ACCESS_DENIED))
                        {
                            firstError = currentError;
                            errorString = dir;
                        }
                    }
                }
            }

            if (!success && firstError != 0)
            {
                __Error.WinIOError(firstError, errorString);
            }
        }

        /// <summary>
        /// Deletes the specified directory. Throws if the directory is not empty.
        /// </summary>
        /// <param name="Path">Path to the directory to delete.</param>
        public static void Delete(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            Delete(Path, false);
        }

        /// <summary>
        /// Deletes the specfied directory.
        /// </summary>
        /// <param name="Path">Path to the directory to delete.</param>
        /// <param name="Recursive">Whether to recurse.</param>
        public static void Delete(string Path, bool Recursive)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            var dataInitialised = UnicodeFile.FillAttributeInfo(Path, ref data, true);
            if (dataInitialised != 0)
            {
                if (dataInitialised == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    dataInitialised = Win32Wrapper.ERROR_PATH_NOT_FOUND;
                }
                __Error.WinIOError(dataInitialised, Path);
            }

            // Don't recurse over reparse points.
            if ((data.fileAttributes & (int)FileAttributes.ReparsePoint) != 0)
            {
                Recursive = false;
            }

            DeleteHelper(Path, Recursive, true);
        }

        private static void DeleteHelper(string Path, bool Recursive, bool ThrowOnTopLevelDirectoryNotFound)
        {
            bool success;
            int error;
            Exception ex = null;

            if (Recursive)
            {
                var data = new Win32Wrapper.WIN32_FIND_DATA();

                using (var handle = Win32Wrapper.FindFirstFile($"{Path}{UnicodePath.DirectorySeparatorChar}*", data))
                {
                    if (handle.IsInvalid)
                    {
                        __Error.WinIOError(Marshal.GetLastWin32Error(), Path);
                    }

                    do
                    {
                        if ((data.dwFileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) != 0)
                        {
                            // Skip current and parent directory.
                            if (data.cFileName.Equals(".") || data.cFileName.Equals(".."))
                            {
                                continue;
                            }

                            // Delete symbolic links but do not recurse into them or mount points.
                            if ((data.dwFileAttributes & (int)FileAttributes.ReparsePoint) != 0)
                            {
                                var fullPath = System.IO.Path.Combine(Path, data.cFileName);

                                try
                                {
                                    DeleteHelper(fullPath, Recursive, false);
                                }
                                catch (Exception e)
                                {
                                    if (ex == null)
                                    {
                                        ex = e;
                                    }
                                }
                            }
                            else
                            {
                                // Unmount mount points.
                                if (data.dwReserved0 == Win32Wrapper.IO_REPARSE_TAG_MOUNT_POINT)
                                {
                                    var mountPath = System.IO.Path.Combine(Path, $"{data.cFileName}{UnicodePath.DirectorySeparatorChar}");
                                    success = Win32Wrapper.DeleteVolumeMountPoint(mountPath);
                                    if (success)
                                    {
                                        error = Marshal.GetLastWin32Error();
                                        if (error != Win32Wrapper.ERROR_PATH_NOT_FOUND)
                                        {
                                            try
                                            {
                                                __Error.WinIOError(error, data.cFileName);
                                            }
                                            catch (Exception e)
                                            {
                                                if (ex == null)
                                                {
                                                    ex = e;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Remove symbolic links.
                                var linkPoint = System.IO.Path.Combine(Path, data.cFileName);
                                success = Win32Wrapper.RemoveDirectory(linkPoint);
                                if (!success)
                                {
                                    error = Marshal.GetLastWin32Error();
                                    if (error != Win32Wrapper.ERROR_PATH_NOT_FOUND)
                                    {
                                        try
                                        {
                                            __Error.WinIOError(error, data.cFileName);
                                        }
                                        catch (Exception e)
                                        {
                                            if (ex == null)
                                            {
                                                ex = e;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var fileName = System.IO.Path.Combine(Path, data.cFileName);
                            success = Win32Wrapper.DeleteFile(fileName);
                            if (!success)
                            {
                                error = Marshal.GetLastWin32Error();
                                if (error != Win32Wrapper.ERROR_FILE_NOT_FOUND)
                                {
                                    try
                                    {
                                        __Error.WinIOError(error, data.cFileName);
                                    }
                                    catch (Exception e)
                                    {
                                        if (ex == null)
                                        {
                                            ex = e;
                                        }
                                    }
                                }
                            }
                        }
                    } while (Win32Wrapper.FindNextFile(handle, data));

                    error = Marshal.GetLastWin32Error();
                }

                // Throw tantrum.
                if (ex != null)
                {
                    throw ex;
                }

                if (error != 0 && error != Win32Wrapper.ERROR_NO_MORE_FILES)
                {
                    __Error.WinIOError(error, Path);
                }
            }

            success = Win32Wrapper.RemoveDirectory(Path);

            if (!success)
            {
                error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    error = Win32Wrapper.ERROR_PATH_NOT_FOUND;
                }

                if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                {
                    throw new IOException(OpenEnvironment.GetResourceString("UnauthorizedAccess_IODenied_Path", Path));
                }

                if (error == Win32Wrapper.ERROR_PATH_NOT_FOUND && !ThrowOnTopLevelDirectoryNotFound)
                {
                    return;
                }

                __Error.WinIOError(error, Path);
            }
        }

        /// <summary>
        /// Enumerates directories in the specified path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <returns>IEnumerable of directory names.</returns>
        public static IEnumerable<string> EnumerateDirectories(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateDirectories(Path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates directories in the specified path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">Search pattern to use when enumerating.</param>
        /// <returns>IEnumerable of directory names.</returns>
        public static IEnumerable<string> EnumerateDirectories(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates directories in the specified path using the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">Search pattern to use.</param>
        /// <param name="SearchOption">Search option to use.</param>
        /// <returns>IEnumerable of directory names.</returns>
        public static IEnumerable<string> EnumerateDirectories(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileNameIterator(Path, SearchPattern, false, true, SearchOption);
        }

        /// <summary>
        /// Enumerates files in the specified path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <returns>IEnumerable of file names.</returns>
        public static IEnumerable<string> EnumerateFiles(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateFiles(Path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates files in the specified path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>IEnumerable of file names.</returns>
        public static IEnumerable<string> EnumerateFiles(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates files in the specified path with the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>IEnumerable of file names.</returns>
        public static IEnumerable<string> EnumerateFiles(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileNameIterator(Path, SearchPattern, true, false, SearchOption);
        }

        /// <summary>
        /// Enumerates file system entries in the specifed path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">The path to the directory to enumerate.</param>
        /// <returns>IEnumerable of file system entry names.</returns>
        public static IEnumerable<string> EnumerateFileSystemEntries(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateFileSystemEntries(Path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates file system entries in the specified path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">The path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>IEnumerable of file system entry names.</returns>
        public static IEnumerable<string> EnumerateFileSystemEntries(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemEntries(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Enumerates file system entries in the specified path with the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">The path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>IEnumerable of file system entry names.</returns>
        public static IEnumerable<string> EnumerateFileSystemEntries(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return UnicodeFileSystemEnumerableFactory.CreateFileNameIterator(Path, SearchPattern, true, true, SearchOption);
        }

        /// <summary>
        /// Checks whether the specified folder exists on disk. Returns false for files.
        /// </summary>
        /// <param name="Path">The path to check.</param>
        /// <returns>True if the directory exists, false otherwise.</returns>
        public static bool Exists(string Path)
        {
            if (Path == null)
            {
                return false;
            }
            if (Path.Length == 0)
            {
                return false;
            }

            var data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            var error = UnicodeFile.FillAttributeInfo(Path, ref data, true);

            return (error == 0 && data.fileAttributes != -1 && (data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        /// <summary>
        /// Gets the creation time of the specified directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Creation time in local time.</returns>
        public static DateTime GetCreationTime(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return GetCreationTimeUtc(Path).ToLocalTime();
        }

        /// <summary>
        /// Gets the creation time of the specified directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Creation time in UTC.</returns>
        public static DateTime GetCreationTimeUtc(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return UnicodeFile.GetCreationTimeUtc(Path);
        }

        /// <summary>
        /// Gets the current working directory for the application.
        /// </summary>
        /// <returns>Current working directory.</returns>
        public static string GetCurrentDirectory()
        {
            var sb = new StringBuilder(UnicodePath.MAX_PATH + 1);
            if (Win32Wrapper.GetCurrentDirectory(sb.Capacity, sb) == 0)
            {
                __Error.WinIOError();
            }
            var currentDirectory = sb.ToString();

            // Handle short file name mode.
            if (currentDirectory.IndexOf('~') >= 0)
            {
                var len = Win32Wrapper.GetLongPathName(currentDirectory, sb, sb.Capacity);
                if (len == 0 || len >= UnicodePath.MAX_PATH)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (len > UnicodePath.MAX_PATH)
                    {
                        error = Win32Wrapper.ERROR_FILENAME_EXCED_RANGE;
                    }

                    if (error != Win32Wrapper.ERROR_FILE_NOT_FOUND &&
                        error != Win32Wrapper.ERROR_PATH_NOT_FOUND &&
                        error != Win32Wrapper.ERROR_INVALID_FUNCTION &&
                        error != Win32Wrapper.ERROR_ACCESS_DENIED)
                    {
                        __Error.WinIOError(error, string.Empty);
                    }
                }
                currentDirectory = sb.ToString();
            }

            return currentDirectory;
        }

        /// <summary>
        /// Gets all the directories in the specified path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <returns>Array of directory names.</returns>
        public static string[] GetDirectories(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateDirectories(Path).ToArray();
        }

        /// <summary>
        /// Gets all the directories in the specified path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">Search pattern to use.</param>
        /// <returns>Array of directory names.</returns>
        public static string[] GetDirectories(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(Path, SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all the directories in the specified path with the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of directory names.</returns>
        public static string[] GetDirectories(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateDirectories(Path, SearchPattern, SearchOption).ToArray();
        }

        /// <summary>
        /// Get's the directory root for the specified directory.
        /// </summary>
        /// <param name="Path">Path to the directory.</param>
        /// <returns>Directory root.</returns>
        public static string GetDirectoryRoot(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var fullPath = UnicodePath.NormalisePath(Path, true);
            var root = fullPath.Substring(0, UnicodePath.GetRootLength(fullPath));

            return root;
        }

        /// <summary>
        /// Gets all the files in the specified path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <returns>Array of file names.</returns>
        public static string[] GetFiles(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateFiles(Path).ToArray();
        }

        /// <summary>
        /// Gets all the files in the specifed path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>Array of files names.</returns>
        public static string[] GetFiles(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(Path, SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all the files in the specified path using the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of file names.</returns>
        public static string[] GetFiles(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFiles(Path, SearchPattern, SearchOption).ToArray();
        }

        /// <summary>
        /// Gets all the file system entries in the specified path without descending into subdirectories.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <returns>Array of file system entry names.</returns>
        public static string[] GetFileSystemEntries(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return EnumerateFileSystemEntries(Path).ToArray();
        }

        /// <summary>
        /// Gets all the file system entries in the specified path without descending into subdirectories using the specified search pattern.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <returns>Array of file system entry names.</returns>
        public static string[] GetFileSystemEntries(string Path, string SearchPattern)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemEntries(Path, SearchPattern).ToArray();
        }

        /// <summary>
        /// Gets all the file system entries in the specified path using the specified search pattern and search option.
        /// </summary>
        /// <param name="Path">Path to the directory to enumerate.</param>
        /// <param name="SearchPattern">The search pattern to use.</param>
        /// <param name="SearchOption">The search option to use.</param>
        /// <returns>Array of file system entry names.</returns>
        public static string[] GetFileSystemEntries(string Path, string SearchPattern, SearchOption SearchOption)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (SearchPattern == null)
            {
                throw new ArgumentNullException(nameof(SearchPattern));
            }

            return EnumerateFileSystemEntries(Path, SearchPattern, SearchOption).ToArray();
        }

        /// <summary>
        /// Gets the last access time for the specified directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Last access time in local time.</returns>
        public static DateTime GetLastAccessTime(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return GetLastAccessTimeUtc(Path).ToLocalTime();
        }

        /// <summary>
        /// Gets the last access time for the specifed directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Last access time in UTC></returns>
        public static DateTime GetLastAccessTimeUtc(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return UnicodeFile.GetLastAccessTimeUtc(Path);
        }

        /// <summary>
        /// Gets the last write time for the specified directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Last write time in local time.</returns>
        public static DateTime GetLastWriteTime(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return GetLastWriteTimeUtc(Path).ToLocalTime();
        }

        /// <summary>
        /// Gets the last write time for the specified directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directory to check.</param>
        /// <returns>Last write time in UTC.</returns>
        public static DateTime GetLastWriteTimeUtc(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return UnicodeFile.GetLastWriteTimeUtc(Path);
        }

        /// <summary>
        /// Gets the logical drives on the machine.
        /// </summary>
        /// <returns>Array of drive roots.</returns>
        public static string[] GetLogicalDrives()
        {
            return Directory.GetLogicalDrives();
        }

        /// <summary>
        /// Gets the DirectoryInfo for the parent directory.
        /// </summary>
        /// <param name="Path">Path to the directory to get the parent of.</param>
        /// <returns>DirectoryInfo of the parent directory.</returns>
        public static DirectoryInfo GetParent(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Path.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_PathEmpty"), nameof(Path));
            }

            var fullPath = UnicodePath.NormalisePath(Path, true);

            var parent = UnicodePath.GetDirectoryName(fullPath);

            return parent == null ? null : new DirectoryInfo(parent);
        }

        /// <summary>
        /// Moves the specified source folder to a new location. Folders cannot be copied across device boundaries.
        /// </summary>
        /// <param name="SourceDirectory">Path to the directory to move.</param>
        /// <param name="DestinationDirectory">Path to the new directory location.</param>
        public static void Move(string SourceDirectory, string DestinationDirectory)
        {
            if (SourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(SourceDirectory));
            }
            if (DestinationDirectory == null)
            {
                throw new ArgumentNullException(nameof(DestinationDirectory));
            }

            if (SourceDirectory.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(SourceDirectory));
            }
            if (DestinationDirectory.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(DestinationDirectory));
            }

            var fullSource = UnicodePath.NormalisePath(SourceDirectory, true);
            var fullDestination = UnicodePath.NormalisePath(DestinationDirectory, true);

            if (fullSource.Length > UnicodePath.MAX_PATH)
            {
                throw new PathTooLongException(OpenEnvironment.GetResourceString("IO.PathTooLong"));
            }
            if (fullDestination.Length > UnicodePath.MAX_PATH)
            {
                throw new PathTooLongException(OpenEnvironment.GetResourceString("IO.PathTooLong"));
            }

            if (string.Compare(fullSource, fullDestination, StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new IOException(OpenEnvironment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));
            }

            // Can't move files over device or partition boundaries (C:\ -> D:\ etc).
            if (string.Compare(UnicodePath.GetPathRoot(fullSource), UnicodePath.GetPathRoot(fullDestination), StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new IOException(OpenEnvironment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
            }

            if (!Win32Wrapper.MoveFile(fullSource, fullDestination))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    error = Win32Wrapper.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(error, fullSource);
                }

                // Dat Win95 compatability.
                if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                {
                    throw new IOException(OpenEnvironment.GetResourceString("UnauthorizedAccess_IODenied_Path", fullSource), Win32Wrapper.MakeHRFromErrorCode(error));
                }

                __Error.WinIOError(error, string.Empty);
            }
        }

        /// <summary>
        /// Sets the creation time of the specfied directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to change.</param>
        /// <param name="CreationTime">Creation time in local time.</param>
        public static void SetCreationTime(string Path, DateTime CreationTime)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (CreationTime == null)
            {
                throw new ArgumentNullException(nameof(CreationTime));
            }

            SetCreationTimeUtc(Path, CreationTime.ToUniversalTime());
        }

        /// <summary>
        /// Sets the creation time of the specified directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directroy to change.</param>
        /// <param name="CreationTimeUtc">Creation time in UTC.</param>
        public static void SetCreationTimeUtc(string Path, DateTime CreationTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (CreationTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(CreationTimeUtc));
            }

            UnicodeFile.SetCreationTimeUtc(Path, CreationTimeUtc);
        }

        /// <summary>
        /// Sets the current working directory for the application.
        /// </summary>
        /// <param name="Path">Path to set as the current working directory.</param>
        public static void SetCurrentDirectory(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Path.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_PathEmpty"));
            }
            if (Path.Length > UnicodePath.MAX_PATH)
            {
                throw new PathTooLongException(OpenEnvironment.GetResourceString("IO.PathTooLong"));
            }

            if (!Win32Wrapper.SetCurrentDirectory(UnicodePath.NormalisePath(Path, true)))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    error = Win32Wrapper.ERROR_PATH_NOT_FOUND;
                }
                __Error.WinIOError(error, UnicodePath.NormalisePath(Path, true));
            }
        }

        /// <summary>
        /// Sets the last access time for the specified directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to change.</param>
        /// <param name="LastAccessTime">Last access time in local time.</param>
        public static void SetLastAccessTime(string Path, DateTime LastAccessTime)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastAccessTime == null)
            {
                throw new ArgumentNullException(nameof(LastAccessTime));
            }

            SetLastAccessTimeUtc(Path, LastAccessTime.ToUniversalTime());
        }

        /// <summary>
        /// Sets the last access time for the specified directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directory to change.</param>
        /// <param name="LastAccessTimeUtc">Last access time in UTC.</param>
        public static void SetLastAccessTimeUtc(string Path, DateTime LastAccessTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastAccessTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(LastAccessTimeUtc));
            }

            UnicodeFile.SetLastAccessTimeUtc(Path, LastAccessTimeUtc);
        }

        /// <summary>
        /// Sets the last write time for the specified directory in local time.
        /// </summary>
        /// <param name="Path">Path to the directory to change.</param>
        /// <param name="LastWriteTime">Last write time in local time.</param>
        public static void SetLastWriteTime(string Path, DateTime LastWriteTime)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastWriteTime == null)
            {
                throw new ArgumentNullException(nameof(LastWriteTime));
            }

            SetLastWriteTimeUtc(Path, LastWriteTime.ToUniversalTime());
        }

        /// <summary>
        /// Sets the last write time for the specified directory in UTC.
        /// </summary>
        /// <param name="Path">Path to the directory to change.</param>
        /// <param name="LastWriteTimeUtc">Last write time in UTC.</param>
        public static void SetLastWriteTimeUtc(string Path, DateTime LastWriteTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastWriteTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(LastWriteTimeUtc));
            }

            UnicodeFile.SetLastWriteTimeUtc(Path, LastWriteTimeUtc);
        }
    }
}
