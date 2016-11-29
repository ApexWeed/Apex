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
    public static class UnicodeFile
    {
        public static void AppendAllLines(string Path, IEnumerable<string> Contents)
        {
            AppendAllLines(Path, Contents, new UTF8Encoding(false));
        }

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
                throw new ArgumentException();
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

        public static void AppendAllText(string Path, string Contents)
        {
            AppendAllText(Path, Contents, Encoding.UTF8);
        }

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
                throw new ArgumentException();
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

        public static StreamWriter AppendText(string Path)
        {
            if (Path == null)
            {
                throw new ArgumentNullException(nameof(Path));
            }

            var w = new StreamWriter(Open(Path, FileMode.Open, FileAccess.Write));
            w.BaseStream.Seek(0, SeekOrigin.End);

            return w;
        }

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

        public static FileStream Create(string Path)
        {
            return Create(Path, 4096, FileOptions.None);
        }

        public static FileStream Create(string Path, int BufferSize)
        {
            return Create(Path, BufferSize, FileOptions.None);
        }

        public static FileStream Create(string Path, int BufferSize, FileOptions Options)
        {
            var handle = Win32Wrapper.SafeCreateFile(UnicodePath.FormatPath(Path), Win32Wrapper.GENERIC_READ | Win32Wrapper.GENERIC_WRITE, FileShare.None, new Win32Wrapper.SECURITY_ATTRIBUTES(), FileMode.Create, (int)Options, IntPtr.Zero);
            return new FileStream(handle, FileAccess.ReadWrite, BufferSize);
        }

        public static StreamWriter CreateText(string Path)
        {
            return new StreamWriter(Create(Path), new UTF8Encoding(false));
        }

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

        public static void Delete(string Path)
        {
            Win32Wrapper.DeleteFile(UnicodePath.FormatPath(Path));
        }

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

        public static bool Exists(string Path)
        {
            var data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            var dataInitialised = FillAttributeInfo(UnicodePath.FormatPath(Path), ref data, true);

            return dataInitialised == 0 && data.fileAttributes != -1 && (data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) == 0;
        }

        public static FileAttributes GetAttributes(string Path)
        {
            var data = GetAttributeInfo(Path);

            return (FileAttributes)data.fileAttributes;
        }

        public static DateTime GetCreationTime(string Path)
        {
            return GetCreationTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetCreationTimeUtc(string Path)
        {
            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftCreationTimeHigh) << 32) | data.ftCreationTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

        public static DateTime GetLastAccessTime(string Path)
        {
            return GetLastAccessTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetLastAccessTimeUtc(string Path)
        {
            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftLastAccessTimeHigh) << 32) | data.ftLastAccessTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

        public static DateTime GetLastWriteTime(string Path)
        {
            return GetLastWriteTimeUtc(Path).ToLocalTime();
        }

        public static DateTime GetLastWriteTimeUtc(string Path)
        {
            var data = GetAttributeInfo(Path);

            var dt = ((long)(data.ftLastWriteTimeHigh) << 32) | data.ftLastWriteTimeLow;
            return DateTime.FromFileTimeUtc(dt);
        }

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

        public static FileStream Open(string Path, FileMode Mode)
        {
            return Open(Path, Mode, (Mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        public static FileStream Open(string Path, FileMode Mode, FileAccess Access)
        {
            return Open(Path, Mode, Access, FileShare.None);
        }

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

        public static FileStream OpenRead(string Path)
        {
            return Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static StreamReader OpenText(string Path)
        {
            return OpenText(Path, Encoding.UTF8);
        }

        public static StreamReader OpenText(string Path, Encoding Encoding)
        {
            var fs = OpenRead(Path);
            return new StreamReader(fs, Encoding);
        }

        public static FileStream OpenWrite(string Path)
        {
            return Open(Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        public static byte[] ReadAllBytes(string Path)
        {
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

        public static List<string> ReadAllLines(string Path)
        {
            return ReadAllLines(Path, Encoding.UTF8);
        }

        public static List<string> ReadAllLines(string Path, Encoding Encoding)
        {
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

        public static string ReadAllText(string Path)
        {
            return ReadAllText(Path, Encoding.UTF8);
        }

        public static string ReadAllText(string Path, Encoding Encoding)
        {
            using (var read = OpenText(Path, Encoding))
            {
                return read.ReadToEnd();
            }
        }

        public static IEnumerable<string> ReadLines(string Path)
        {
            return ReadLines(Path, Encoding.UTF8);
        }

        public static IEnumerable<string> ReadLines(string Path, Encoding Encoding)
        {
            return UnicodeReadLinesIterator.CreateIterator(Path, Encoding);
        }

        public static void Replace(string SourceFilename, string DestinationFilename, string DestinationBackupFilename)
        {
            Replace(SourceFilename, DestinationFilename, DestinationBackupFilename, false);
        }

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

        public static void SetAttributes(string Path, FileAttributes Attributes)
        {
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

        public static void SetCreationTime(string Path, DateTime CreationTime)
        {
            SetCreationTimeUtc(Path, CreationTime.ToUniversalTime());
        }

        public unsafe static void SetCreationTimeUtc(string Path, DateTime CreationTimeUtc)
        {
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

        public static void SetLastAccessTime(string Path, DateTime LastAccessTime)
        {
            SetLastAccessTimeUtc(Path, LastAccessTime.ToUniversalTime());
        }

        public unsafe static void SetLastAccessTimeUtc(string Path, DateTime LastAccessTimeUtc)
        {
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

        public static void SetLastWriteTime(string Path, DateTime LastWriteTime)
        {
            SetLastWriteTimeUtc(Path, LastWriteTime.ToUniversalTime());
        }

        public unsafe static void SetLastWriteTimeUtc(string Path, DateTime LastWriteTimeUtc)
        {
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

        public static void WriteAllBytes(string Path, byte[] Bytes)
        {
            using (var fs = Create(Path))
            {
                fs.Write(Bytes, 0, Bytes.Length);
            }
        }

        public static void WriteAllLines(string Path, IEnumerable<string> Contents)
        {
            WriteAllLines(Path, Contents, new UTF8Encoding(false));
        }

        public static void WriteAllLines(string Path, IEnumerable<string> Contents, Encoding Encoding)
        {
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

        public static void WriteAllLines(string Path, string[] Contents)
        {
            WriteAllLines(Path, Contents, new UTF8Encoding(false));
        }

        public static void WriteAllLines(string Path, string[] Contents, Encoding Encoding)
        {
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

        public static void WriteAllText(string Path, string Contents)
        {
            WriteAllText(Path, Contents, new UTF8Encoding(false));
        }

        public static void WriteAllText(string Path, string Contents, Encoding Encoding)
        {
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
