using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Permissions;
using Apex.Win32;
using System.Runtime.InteropServices;

namespace Apex.IO
{
    public static class UnicodeDirectory
    {
        public static DirectoryInfo CreateDirectory(string Path)
        {
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

        public static void Delete(string Path)
        {
            Delete(Path, false);
        }

        public static void Delete(string Path, bool Recursive)
        {
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

        public static IEnumerable<string> EnumerateDirectories(string Path)
        {
            return EnumerateDirectories(Path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateDirectories(string Path, string SearchPattern)
        {
            return EnumerateDirectories(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

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

        public static IEnumerable<string> EnumerateFiles(string Path)
        {
            return EnumerateFiles(Path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFiles(string Path, string SearchPattern)
        {
            return EnumerateFiles(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

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

        public static IEnumerable<string> EnumerateFileSystemEntries(string Path)
        {
            return EnumerateFileSystemEntries(Path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string Path, string SearchPattern)
        {
            return EnumerateFileSystemEntries(Path, SearchPattern, SearchOption.TopDirectoryOnly);
        }

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

        public static DateTime GetCreationTime(string Path)
        {
            return GetCreationTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetCreationTimeUtc(string Path)
        {
            return UnicodeFile.GetCreationTimeUtc(Path);
        }

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

        public static string[] GetDirectories(string Path)
        {
            return EnumerateDirectories(Path).ToArray();
        }

        public static string[] GetDirectories(string Path, string SearchPattern)
        {
            return EnumerateDirectories(Path, SearchPattern).ToArray();
        }

        public static string[] GetDirectories(string Path, string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateDirectories(Path, SearchPattern, SearchOption).ToArray();
        }

        public static string GetDirectoryRoot(string Path)
        {
            var fullPath = UnicodePath.NormalisePath(Path, true);
            var root = fullPath.Substring(0, UnicodePath.GetRootLength(fullPath));

            return root;
        }

        public static string[] GetFiles(string Path)
        {
            return EnumerateFiles(Path).ToArray();
        }

        public static string[] GetFiles(string Path, string SearchPattern)
        {
            return EnumerateFiles(Path, SearchPattern).ToArray();
        }

        public static string[] GetFiles(string Path, string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateFiles(Path, SearchPattern, SearchOption).ToArray();
        }

        public static string[] GetFileSystemEntries(string Path)
        {
            return EnumerateFileSystemEntries(Path).ToArray();
        }

        public static string[] GetFileSystemEntries(string Path, string SearchPattern)
        {
            return EnumerateFileSystemEntries(Path, SearchPattern).ToArray();
        }

        public static string[] GetFileSystemEntries(string Path, string SearchPattern, SearchOption SearchOption)
        {
            return EnumerateFileSystemEntries(Path, SearchPattern, SearchOption).ToArray();
        }

        public static DateTime GetLastAccessTime(string Path)
        {
            return GetLastAccessTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetLastAccessTimeUtc(string Path)
        {
            return UnicodeFile.GetLastAccessTimeUtc(Path);
        }

        public static DateTime GetLastWriteTime(string Path)
        {
            return GetLastWriteTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetLastWriteTimeUtc(string Path)
        {
            return UnicodeFile.GetLastWriteTimeUtc(Path);
        }

        public static string[] GetLogicalDrives()
        {
            return Directory.GetLogicalDrives();
        }

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

        public static void SetCreationTime(string Path, DateTime CreationTime)
        {
            SetCreationTimeUtc(Path, CreationTime.ToUniversalTime());
        }

        public static void SetCreationTimeUtc(string Path, DateTime CreationTimeUtc)
        {
            UnicodeFile.SetCreationTimeUtc(Path, CreationTimeUtc);
        }

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

        public static void SetLastAccessTime(string Path, DateTime LastAccessTime)
        {
            SetLastAccessTimeUtc(Path, LastAccessTime.ToUniversalTime());
        }

        public static void SetLastAccessTimeUtc(string Path, DateTime LastAccessTimeUtc)
        {
            UnicodeFile.SetLastAccessTimeUtc(Path, LastAccessTimeUtc);
        }

        public static void SetLastWriteTime(string Path, DateTime LastWriteTime)
        {
            SetLastWriteTimeUtc(Path, LastWriteTime.ToUniversalTime());
        }

        public static void SetLastWriteTimeUtc(string Path, DateTime LastWriteTimeUtc)
        {
            UnicodeFile.SetLastWriteTimeUtc(Path, LastWriteTimeUtc);
        }
    }
}
