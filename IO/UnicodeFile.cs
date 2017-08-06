using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Apex.Win32;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Security.AccessControl;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace Apex.IO
{
    /// <summary>
    /// Provides methods for manipulating files with support for full ~32000 NTFS path length.
    /// </summary>
    public static class UnicodeFile
    {
        /// <summary>
        /// Opens the specified file, appends all lines of text, and then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to append text to.</param>
        /// <param name="Contents">Enumerable of strings to append to the file.</param>
        public static void AppendAllLines(string Path, IEnumerable<string> Contents)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            AppendAllLines(Path, Contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Opens the specified file, appends all lines of text in the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to append text to.</param>
        /// <param name="Contents">Enumerable of strings to append to the file.</param>
        /// <param name="Encoding">Text encoding to use to write to the file.</param>
        public static void AppendAllLines(string Path, IEnumerable<string> Contents, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }
            if (Path.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyPath"));
            }

            using (var fs = Open(Path, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.End);
                using (var w = new StreamWriter(fs, Encoding))
                {
                    foreach (var line in Contents)
                    {
                        w.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>
        /// Opens the specified file, appends all text to it, and then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to append text to.</param>
        /// <param name="Contents">Text to append to the file.</param>
        public static void AppendAllText(string Path, string Contents)
        {
            AppendAllText(Path, Contents, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the specified file, appends all text to it in the specified encoding, and then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to append text to.</param>
        /// <param name="Contents">Text to append to the file.</param>
        /// <param name="Encoding">Text encoding to use to write to the file.</param>
        public static void AppendAllText(string Path, string Contents, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }
            if (Path.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyPath"));
            }

            using (var fs = Open(Path, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.End);
                using (var w = new StreamWriter(fs, Encoding))
                {
                    w.Write(Contents);
                }
            }
        }

        /// <summary>
        /// Opens the specified file for append text, and returns a stream writer.
        /// </summary>
        /// <param name="Path">Path to the file to append text to.</param>
        /// <returns>Stream writer to append text to the file.</returns>
        public static StreamWriter AppendText(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Path.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyPath"));
            }

            var w = new StreamWriter(Open(Path, FileMode.Open, FileAccess.Write));
            w.BaseStream.Seek(0, SeekOrigin.End);

            return w;
        }

        /// <summary>
        /// Copies the specified file to a new file name.
        /// </summary>
        /// <param name="SourceFilename">The path to the file to copy.</param>
        /// <param name="DestinationFilename">The new path to copy the file to.</param>
        public static void Copy(string SourceFilename, string DestinationFilename)
        {
            if (SourceFilename == null)
            {
                throw new ArgumentNullException(nameof(SourceFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (DestinationFilename == null)
            {
                throw new ArgumentNullException(nameof(DestinationFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (SourceFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(SourceFilename));
            }
            if (DestinationFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(DestinationFilename));
            }

            Copy(SourceFilename, DestinationFilename, false);
        }

        /// <summary>
        /// Copies the specified file to a new file name, optionally overwriting if the destination exists.
        /// </summary>
        /// <param name="SourceFilename">The path to the file to copy.</param>
        /// <param name="DestinationFilename">The new path to copy the file to.</param>
        /// <param name="Overwrite">Whether to overwrite the destination file if it exists.</param>
        public static void Copy(string SourceFilename, string DestinationFilename, bool Overwrite)
        {
            if (SourceFilename == null)
            {
                throw new ArgumentNullException(nameof(SourceFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (DestinationFilename == null)
            {
                throw new ArgumentNullException(nameof(DestinationFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (SourceFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(SourceFilename));
            }
            if (DestinationFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(DestinationFilename));
            }

            InternalCopy(SourceFilename, DestinationFilename, Overwrite);
        }

        internal static string InternalCopy(string SourceFileName, string DestinationFileName, bool Overwrite)
        {
            var source = UnicodePath.FormatPath(SourceFileName);
            var dest = UnicodePath.FormatPath(DestinationFileName);
            var success = Win32Wrapper.CopyFile(source, dest, !Overwrite);

            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                var filename = DestinationFileName;

                if (error != Win32Wrapper.ERROR_FILE_EXISTS)
                {
                    // Need to find if the error is with source or destination.
                    using (var handle = Win32Wrapper.CreateFile(source, Win32Wrapper.GENERIC_READ, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid)
                        {
                            filename = SourceFileName;
                        }
                    }

                    if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                    {
                        // TODO: Directory methods.
                    }
                }

                __Error.WinIOError(error, filename);
            }

            return UnicodePath.NormalisePath(dest, true);
        }

        /// <summary>
        /// Creates the specified file and opens a file stream.
        /// </summary>
        /// <param name="Path">Path to the file to create.</param>
        /// <returns>File stream.</returns>
        public static FileStream Create(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Create(Path, 4096, FileOptions.None);
        }

        /// <summary>
        /// Creates the specified file and opens a file stream with the specified buffer size.
        /// </summary>
        /// <param name="Path">Path to the file to create.</param>
        /// <param name="BufferSize">Buffersize for the file stream.</param>
        /// <returns>File stream.</returns>
        public static FileStream Create(string Path, int BufferSize)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Create(Path, BufferSize, FileOptions.None);
        }

        /// <summary>
        /// Creates the specified file with the specified file options and opens a file stream with the specified buffer size.
        /// </summary>
        /// <param name="Path">Path to the file to create.</param>
        /// <param name="BufferSize">Buffersize for the file stream.</param>
        /// <param name="Options">File stream.</param>
        /// <returns></returns>
        public static FileStream Create(string Path, int BufferSize, FileOptions Options)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var handle = Win32Wrapper.SafeCreateFile(UnicodePath.FormatPath(Path), Win32Wrapper.GENERIC_READ | Win32Wrapper.GENERIC_WRITE, FileShare.None, new Win32Wrapper.SECURITY_ATTRIBUTES(), FileMode.Create, (int)Options, IntPtr.Zero);
            return new FileStream(handle, FileAccess.ReadWrite, BufferSize);
        }

        /// <summary>
        /// Creates the specified file and opens it for writing text.
        /// </summary>
        /// <param name="Path">Path to the file to create.</param>
        /// <returns>Streamwriter for the file.</returns>
        public static StreamWriter CreateText(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return new StreamWriter(Create(Path), new UTF8Encoding(false));
        }

        /// <summary>
        /// Decrypts the specified file using the user's account credentials.
        /// </summary>
        /// <param name="Path">Path to the file to decrypt.</param>
        public static void Decrypt(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var success = Win32Wrapper.DecryptFile(UnicodePath.FormatPath(Path));
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                {
                    var di = new DriveInfo(System.IO.Path.GetPathRoot(Path));
                    if (!string.Equals("NTFS", di.DriveFormat))
                    {
                        throw new NotSupportedException(OpenEnvironment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                    }
                }
                __Error.WinIOError(error, Path);
            }
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="Path">Path to the file to delete.</param>
        public static void Delete(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            Win32Wrapper.DeleteFile(UnicodePath.FormatPath(Path));
        }

        /// <summary>
        /// Encrypts the specified file using the user's account credentials.
        /// </summary>
        /// <param name="Path">Path to the file to encrypt.</param>
        public static void Encrypt(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var success = Win32Wrapper.EncryptFile(UnicodePath.FormatPath(Path));
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                {
                    var di = new DriveInfo(System.IO.Path.GetPathRoot(Path));
                    if (!string.Equals("NTFS", di.DriveFormat))
                    {
                        throw new NotSupportedException(OpenEnvironment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                    }
                }
                __Error.WinIOError(error, Path);
            }
        }

        /// <summary>
        /// Checks if the specified file exists on disk. Returns false for directories.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public static bool Exists(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Path.Length == 0)
            {
                return false;
            }

            var data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            var dataInitialised = FillAttributeInfo(UnicodePath.FormatPath(Path), ref data, true);

            return dataInitialised == 0 && data.fileAttributes != -1 && (data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) == 0;
        }

        /// <summary>
        /// Gets the attributes of the specified file.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
        /// <returns>File attributes for the file.</returns>
        public static FileAttributes GetAttributes(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var data = GetAttributeInfo(Path);

            return (FileAttributes)data.fileAttributes;
        }

        /// <summary>
        /// Gets the creation time of the specified file in local time.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
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
        /// Gets the creation time of the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
        /// <returns>Creation time in UTC.</returns>
        public static DateTime GetCreationTimeUtc(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftCreationTimeHigh) << 32) | data.ftCreationTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// Gets the last access time of the specified file in local time.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
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
        /// Gets the last access time of the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
        /// <returns>Last access time in UTC.</returns>
        public static DateTime GetLastAccessTimeUtc(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftLastAccessTimeHigh) << 32) | data.ftLastAccessTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// Gets the last write time of the specified file in local time.
        /// </summary>
        /// <param name="Path">Path of the file to check.</param>
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
        /// Gets the last write time fo the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to check.</param>
        /// <returns>Last write time in UTC.</returns>
        public static DateTime GetLastWriteTimeUtc(string Path)
        {
            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftLastWriteTimeHigh) << 32) | data.ftLastWriteTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// Move the specified file to a new location.
        /// </summary>
        /// <param name="SourceFilename">Path to the file to move.</param>
        /// <param name="DestinationFilename">Path to the new location for the file.</param>
        public static void Move(string SourceFilename, string DestinationFilename)
        {
            if (SourceFilename == null)
            {
                throw new ArgumentNullException(nameof(SourceFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (DestinationFilename == null)
            {
                throw new ArgumentNullException(nameof(DestinationFilename), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (SourceFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(SourceFilename));
            }
            if (DestinationFilename.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(DestinationFilename));
            }

            var source = UnicodePath.FormatPath(SourceFilename);
            var dest = UnicodePath.FormatPath(DestinationFilename);

            if (!Exists(source))
            {
                __Error.WinIOError(Win32Wrapper.ERROR_FILE_NOT_FOUND, SourceFilename);
            }

            if (!Win32Wrapper.MoveFile(source, dest))
            {
                __Error.WinIOError();
            }
        }

        /// <summary>
        /// Opens the specified file for reading with the specified file mode.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Mode">Mode to open the file in.</param>
        /// <returns>File stream to the file.</returns>
        public static FileStream Open(string Path, FileMode Mode)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Open(Path, Mode, (Mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        /// <summary>
        /// Opens the specified file for reading with the specified file mode and file access.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Mode">Mode to open the file in.</param>
        /// <param name="Access">Access to open the file with.</param>
        /// <returns>File stream to the file.</returns>
        public static FileStream Open(string Path, FileMode Mode, FileAccess Access)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Open(Path, Mode, Access, FileShare.None);
        }

        /// <summary>
        /// Opens the specified file for reading with the specified file mode, file access, and file share options.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Mode">Mode to open the file in.</param>
        /// <param name="Access">Access to open the file with.</param>
        /// <param name="Share">Sharemode to open the file with.</param>
        /// <returns>File stream to the file.</returns>
        public static FileStream Open(string Path, FileMode Mode, FileAccess Access, FileShare Share)
        {
            var fAccess = Access == FileAccess.Read ? Win32Wrapper.GENERIC_READ : (Access == FileAccess.Write ? Win32Wrapper.GENERIC_WRITE : Win32Wrapper.GENERIC_READ | Win32Wrapper.GENERIC_WRITE);
            var securityAttributes = UnicodeStatic.GetSecurityAttributes(Share);
            
            // Stop the floppy drives opening.
            var oldMode = Win32Wrapper.SetErrorMode(Win32Wrapper.SEM_FAILCRITICALERRORS);
            SafeFileHandle handle;
            try
            {
                handle = Win32Wrapper.SafeCreateFile(UnicodePath.FormatPath(Path), fAccess, Share, securityAttributes, Mode, 0, IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error();
                    __Error.WinIOError(error, Path);
                }
            }
            finally
            {
                Win32Wrapper.SetErrorMode(oldMode);
            }

            // Don't allow opening of pipes and consoles and what not.
            var fileType = Win32Wrapper.GetFileType(handle);
            if (fileType != Win32Wrapper.FILE_TYPE_DISK)
            {
                handle.Close();
                throw new NotSupportedException(OpenEnvironment.GetResourceString("NotSupported_FileStreamOnNonFiles"));
            }

            return new FileStream(handle, Access);
        }

        private static SafeFileHandle OpenFile(string Path, FileAccess Access)
        {
            var fAccess = Access == FileAccess.Read ? Win32Wrapper.GENERIC_READ : (Access == FileAccess.Write ? Win32Wrapper.GENERIC_WRITE : Win32Wrapper.GENERIC_READ | Win32Wrapper.GENERIC_WRITE);
            // Stop the floppy drives opening.
            var oldMode = Win32Wrapper.SetErrorMode(Win32Wrapper.SEM_FAILCRITICALERRORS);
            SafeFileHandle handle;
            try
            {
                handle = Win32Wrapper.SafeCreateFile(UnicodePath.FormatPath(Path), fAccess, FileShare.None, new Win32Wrapper.SECURITY_ATTRIBUTES(), FileMode.Open, 0, IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error();
                    __Error.WinIOError(error, Path);
                }
            }
            finally
            {
                Win32Wrapper.SetErrorMode(oldMode);
            }

            return handle;
        }

        /// <summary>
        /// Opens the specified file for reading.
        /// </summary>
        /// <param name="Path">Path to the file to open for reading.</param>
        /// <returns>File stream to the file.</returns>
        public static FileStream OpenRead(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Opens the specified file for reading UTF-8 text.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <returns>Stream reader to the file.</returns>
        public static StreamReader OpenText(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return OpenText(Path, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the specified file for reading with the specified encoding.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Encoding">The encoding to use to decode the text.</param>
        /// <returns>Stream reader to the file.</returns>
        public static StreamReader OpenText(string Path, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var fs = OpenRead(Path);
            return new StreamReader(fs, Encoding);
        }

        /// <summary>
        /// Opens or creates the specified file for writing.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <returns>File stream to the file.</returns>
        public static FileStream OpenWrite(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return Open(Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Opens the specified file, reads all the contents into an array of bytes, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <returns>File contents as a byte array.</returns>
        public static byte[] ReadAllBytes(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            byte[] bytes;
            using (var fs = OpenRead(Path))
            {
                if (fs.Length > int.MaxValue)
                {
                    throw new IOException(OpenEnvironment.GetResourceString("IO.IO_FileTooLong2GB"));
                }

                var count = (int)fs.Length;
                bytes = new byte[count];
                var index = 0;

                while (count > 0)
                {
                    var read = fs.Read(bytes, index, count);
                    if (read == 0)
                    {
                        __Error.EndOfFile();
                    }
                    index += read;
                    count -= read;
                }
            }
            return bytes;
        }

        /// <summary>
        /// Opens the specified file as text, reads each line, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to read.</param>
        /// <returns>List of each line in the file.</returns>
        public static List<string> ReadAllLines(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return ReadAllLines(Path, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the specified file as text using the specified encoding, reads each line, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Encoding">Encoding to use to decode the text.</param>
        /// <returns>List of each line in the file.</returns>
        public static List<string> ReadAllLines(string Path, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            using (var read = OpenText(Path, Encoding))
            {
                var lines = new List<string>();

                while (!read.EndOfStream)
                {
                    lines.Add(read.ReadLine());
                }

                return lines;
            }
        }

        /// <summary>
        /// Opens the specified file, reads all content as text, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <returns>File contents as a string.</returns>
        public static string ReadAllText(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return ReadAllText(Path, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the specified file, reads all content as text using the specified encoding, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to open.</param>
        /// <param name="Encoding">Encoding to use to decode the text.</param>
        /// <returns>File contents as a string.</returns>
        public static string ReadAllText(string Path, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            using (var read = OpenText(Path, Encoding))
            {
                return read.ReadToEnd();
            }
        }

        /// <summary>
        /// Reads all lines of the specified file as text.
        /// </summary>
        /// <param name="Path">Path to the file to read.</param>
        /// <returns>IEnumerable of all lines of text in the file.</returns>
        public static IEnumerable<string> ReadLines(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return ReadLines(Path, Encoding.UTF8);
        }

        /// <summary>
        /// Reads all lines of the specified file as text using the specified encoding.
        /// </summary>
        /// <param name="Path">Path to the file to read.</param>
        /// <param name="Encoding">The encoding to use to read the file.</param>
        /// <returns>IEnumerable of all lines of text in the file.</returns>
        public static IEnumerable<string> ReadLines(string Path, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            return UnicodeReadLinesIterator.CreateIterator(Path, Encoding);
        }

        /// <summary>
        /// Replaces the specified file with another file and optionally moves the destination file to a backup location.
        /// </summary>
        /// <param name="SourceFilename">Source path to move.</param>
        /// <param name="DestinationFilename">Destination path to replace.</param>
        /// <param name="DestinationBackupFilename">Backup path to move the destination file to.</param>
        public static void Replace(string SourceFilename, string DestinationFilename, string DestinationBackupFilename)
        {
            if (SourceFilename == null)
            {
                throw new ArgumentNullException(nameof(SourceFilename));
            }
            if (DestinationFilename == null)
            {
                throw new ArgumentNullException(nameof(DestinationFilename));
            }

            Replace(SourceFilename, DestinationFilename, DestinationBackupFilename, false);
        }

        /// <summary>
        /// Replaces the specified file with another file and optionally moves the destination file to a backup location, optionally ignoring metadata errors.
        /// </summary>
        /// <param name="SourceFilename">Source path to move.</param>
        /// <param name="DestinationFilename">Destination path to replace.</param>
        /// <param name="DestinationBackupFilename">Backup path to move the destination file to.</param>
        /// <param name="IgnoreMetadataErrors">Whether to ignore metadata errors.</param>
        public static void Replace(string SourceFilename, string DestinationFilename, string DestinationBackupFilename, bool IgnoreMetadataErrors)
        {
            if (SourceFilename == null)
            {
                throw new ArgumentNullException(nameof(SourceFilename));
            }
            if (DestinationFilename == null)
            {
                throw new ArgumentNullException(nameof(DestinationFilename));
            }

            var flags = Win32Wrapper.REPLACEFILE_WRITE_THROUGH;
            if (IgnoreMetadataErrors)
            {
                flags |= Win32Wrapper.REPLACEFILE_IGNORE_MERGE_ERRORS;
            }

            var success = Win32Wrapper.ReplaceFile(UnicodePath.FormatPath(DestinationFilename), UnicodePath.FormatPath(SourceFilename), UnicodePath.FormatPath(DestinationBackupFilename), flags, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Sets file attributes.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
        /// <param name="Attributes">Attributes to set.</param>
        public static void SetAttributes(string Path, FileAttributes Attributes)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var success = Win32Wrapper.SetFileAttributes(UnicodePath.FormatPath(Path), (int)Attributes);
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_INVALID_PARAMETER)
                {
                    throw new ArgumentException(OpenEnvironment.GetResourceString("Arg_InvalidFileAttrs"));
                }
                __Error.WinIOError(error, Path);
            }
        }

        /// <summary>
        /// Sets the creation time for the specified file in local time.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
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
        /// Sets the creation time of the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
        /// <param name="CreationTimeUtc">Creation time in UTC.</param>
        public unsafe static void SetCreationTimeUtc(string Path, DateTime CreationTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (CreationTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(CreationTimeUtc));
            }

            using (var handle = OpenFile(Path, FileAccess.Write))
            {
                var fileTime = new Win32Wrapper.FILE_TIME(CreationTimeUtc.ToFileTimeUtc());
                var success = Win32Wrapper.SetFileTime(handle, &fileTime, null, null);
                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();
                    __Error.WinIOError(error, Path);
                }
            }
        }

        /// <summary>
        /// Sets the last access time of the specified file in local time.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
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
        /// Sets the last access time of the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
        /// <param name="LastAccessTimeUtc">Last access time in UTC.</param>
        public unsafe static void SetLastAccessTimeUtc(string Path, DateTime LastAccessTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastAccessTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(LastAccessTimeUtc));
            }

            using (var handle = OpenFile(Path, FileAccess.Write))
            {
                var fileTime = new Win32Wrapper.FILE_TIME(LastAccessTimeUtc.ToFileTimeUtc());
                var success = Win32Wrapper.SetFileTime(handle, null, &fileTime, null);
                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();
                    __Error.WinIOError(error, Path);
                }
            }
        }

        /// <summary>
        /// Sets the last write time of the specified file in local time.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
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
        /// Sets the last write time of the specified file in UTC.
        /// </summary>
        /// <param name="Path">Path to the file to change.</param>
        /// <param name="LastWriteTimeUtc">Last write time in UTC.</param>
        public unsafe static void SetLastWriteTimeUtc(string Path, DateTime LastWriteTimeUtc)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (LastWriteTimeUtc == null)
            {
                throw new ArgumentNullException(nameof(LastWriteTimeUtc));
            }

            using (var handle = OpenFile(Path, FileAccess.Write))
            {
                var fileTime = new Win32Wrapper.FILE_TIME(LastWriteTimeUtc.ToFileTimeUtc());
                var success = Win32Wrapper.SetFileTime(handle, null, null, &fileTime);
                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();
                    __Error.WinIOError(error, Path);
                }
            }
        }

        /// <summary>
        /// Opens the specified file, writes the contents of the specified array, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Bytes">Bytes to write to the file.</param>
        public static void WriteAllBytes(string Path, byte[] Bytes)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Bytes == null)
            {
                throw new ArgumentNullException(nameof(Bytes));
            }

            using (var fs = Create(Path))
            {
                fs.Write(Bytes, 0, Bytes.Length);
            }
        }

        /// <summary>
        /// Opens the specified file, writes all lines to it, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write to the file.</param>
        public static void WriteAllLines(string Path, IEnumerable<string> Contents)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            WriteAllLines(Path, Contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Opens the specified file, writes all lines in the specified encoding, the closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write to the file.</param>
        /// <param name="Encoding">The encoding to use.</param>
        public static void WriteAllLines(string Path, IEnumerable<string> Contents, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            using (var fs = Create(Path))
            {
                using (var w = new StreamWriter(fs, Encoding))
                {
                    foreach (var line in Contents)
                    {
                        w.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>
        /// Opens the specified file, writes all lines to it, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write.</param>
        public static void WriteAllLines(string Path, string[] Contents)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            WriteAllLines(Path, Contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Opens the specified file, writes all lines in the specified encoding, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write.</param>
        /// <param name="Encoding">The encoding to use.</param>
        public static void WriteAllLines(string Path, string[] Contents, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            using (var fs = Create(Path))
            {
                using (var w = new StreamWriter(fs, Encoding))
                {
                    foreach (var line in Contents)
                    {
                        w.WriteLine(line);
                    }
                }
            }
        }

        /// <summary>
        /// Opens the specified file, writes the specified string to it, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write.</param>
        public static void WriteAllText(string Path, string Contents)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            WriteAllText(Path, Contents, new UTF8Encoding(false));
        }

        /// <summary>
        /// Opens the specified file, writes the specified string using the specified encoding, then closes the file.
        /// </summary>
        /// <param name="Path">Path to the file to write.</param>
        /// <param name="Contents">Contents to write.</param>
        /// <param name="Encoding">The encoding to use.</param>
        public static void WriteAllText(string Path, string Contents, Encoding Encoding)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }
            if (Contents == null)
            {
                throw new ArgumentNullException(nameof(Contents));
            }

            using (var fs = Create(Path))
            {
                using (var w = new StreamWriter(fs, Encoding))
                {
                    w.WriteLine(Contents);
                }
            }
        }
        
        internal static int FillAttributeInfo(string Path, ref Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA Data, bool ReturnErrorOnNotFound)
        {
            var dataInitialised = 0;
            bool success;
            // Stop the floppy drives opening.
            var oldMode = Win32Wrapper.SetErrorMode(Win32Wrapper.SEM_FAILCRITICALERRORS);
            try
            {
                success = Win32Wrapper.GetFileAttributesEx(Path, Win32Wrapper.GET_FILE_EX_INFO_STANDARD, ref Data);
            }
            finally
            {
                Win32Wrapper.SetErrorMode(oldMode);
            }

            if (!success)
            {
                dataInitialised = Marshal.GetLastWin32Error();
                if (dataInitialised == Win32Wrapper.ERROR_FILE_NOT_FOUND || dataInitialised == Win32Wrapper.ERROR_PATH_NOT_FOUND || dataInitialised == Win32Wrapper.ERROR_NOT_READY)
                {
                    if (!ReturnErrorOnNotFound)
                    {
                        dataInitialised = 0;
                        Data.fileAttributes = -1;
                    }
                }
                else
                {
                    // Something else has it open.
                    var findData = new Win32Wrapper.WIN32_FIND_DATA();
                    oldMode = Win32Wrapper.SetErrorMode(Win32Wrapper.SEM_FAILCRITICALERRORS);
                    try
                    {
                        // FindFirstFile doesn't like having a trailing slash.
                        var handle = Win32Wrapper.FindFirstFile(Path.TrimEnd(new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }), findData);
                        try
                        {
                            if (handle.IsInvalid)
                            {
                                dataInitialised = Marshal.GetLastWin32Error();

                                if (dataInitialised == Win32Wrapper.ERROR_FILE_NOT_FOUND || dataInitialised == Win32Wrapper.ERROR_PATH_NOT_FOUND || dataInitialised == Win32Wrapper.ERROR_NOT_READY)
                                {
                                    if (!ReturnErrorOnNotFound)
                                    {
                                        dataInitialised = 0;
                                        Data.fileAttributes = -1;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            handle.Close();
                        }
                    }
                    finally
                    {
                        Win32Wrapper.SetErrorMode(oldMode);
                    }
                }
            }

            return dataInitialised;
        }

        private static Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA GetAttributeInfo(string Path)
        {
            var data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            var dataInitialised = FillAttributeInfo(UnicodePath.FormatPath(Path), ref data, true);
            if (dataInitialised != 0)
            {
                __Error.WinIOError(dataInitialised, Path);
            }

            return data;
        }
    }
}
