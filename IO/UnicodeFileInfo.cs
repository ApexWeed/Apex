using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Apex.Win32;

namespace Apex.IO
{
    /// <summary>
    /// Provides information about a file.
    /// </summary>
    public class UnicodeFileInfo : UnicodeFileSystemInfo
    {
        private string name;

        /// <summary>
        /// Creates a new file info from the specified path.
        /// </summary>
        /// <param name="Filename">Path to the file.</param>
        public UnicodeFileInfo(string Filename)
        {
            if (Filename == null)
            {
                throw new ArgumentNullException(nameof(Filename));
            }

            Init(Filename);
        }

        private void Init(string Filename)
        {
            name = UnicodePath.GetFileName(Filename);
            path = Filename;
        }

        /// <summary>
        /// Returns the directory info for the file's parent.
        /// </summary>
        public UnicodeDirectoryInfo Directory
        {
            get
            {
                return DirectoryName == null ? null : new UnicodeDirectoryInfo(DirectoryName);
            }
        }

        /// <summary>
        /// Returns the name of the file's parent.
        /// </summary>
        public string DirectoryName
        {
            get
            {
                return UnicodePath.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// Checks whether this file exists.
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

                    return (data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the size of this file in bytes.
        /// </summary>
        public long Length
        {
            get
            {
                EnsureDataLoaded();

                // Imagine actually trying to load a directory as a file.
                if ((data.fileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                    __Error.WinIOError(Win32Wrapper.ERROR_FILE_NOT_FOUND, path);
                }

                return ((long)data.fileSizeHigh) << 32 | (data.fileSizeLow & 0xFFFFFFFFL);
            }
        }

        /// <summary>
        /// Checks whether this file is readonly.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }
            set
            {
                if (value)
                {
                    Attributes |= FileAttributes.ReadOnly;
                }
                else
                {
                    Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }

        /// <summary>
        /// Returns the name of the file.
        /// </summary>
        public override string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Opens this file to append text and returns a stream writer.
        /// </summary>
        /// <returns>Stream writer to this file.</returns>
        public StreamWriter AppendText()
        {
            return UnicodeFile.AppendText(path);
        }

        /// <summary>
        /// Copies this file to a new location.
        /// </summary>
        /// <param name="NewFileName">The new filename.</param>
        /// <returns>The file info of the new file.</returns>
        public UnicodeFileInfo CopyTo(string NewFileName)
        {
            if (NewFileName == null)
            {
                throw new ArgumentNullException(nameof(NewFileName), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (NewFileName.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(NewFileName));
            }

            return CopyTo(NewFileName, false);
        }

        /// <summary>
        /// Copies this file to a new location and optionally overwrites if the file exists.
        /// </summary>
        /// <param name="NewFileName">The new filename.</param>
        /// <param name="Overwrite">Whether to overwrite if the file exists.</param>
        /// <returns>The file info of the new file.</returns>
        public UnicodeFileInfo CopyTo(string NewFileName, bool Overwrite)
        {
            if (NewFileName == null)
            {
                throw new ArgumentNullException(nameof(NewFileName), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (NewFileName.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(NewFileName));
            }

            return new UnicodeFileInfo(UnicodeFile.InternalCopy(path, NewFileName, Overwrite));
        }

        /// <summary>
        /// Creates this file and opens a file stream.
        /// </summary>
        /// <returns>File stream to this file.</returns>
        public FileStream Create()
        {
            return File.Create(path);
        }

        /// <summary>
        /// Creates this file and opens it for writing text.
        /// </summary>
        /// <returns>Stream writer to this file.</returns>
        public StreamWriter CreateText()
        {
            return File.CreateText(path);
        }

        /// <summary>
        /// Decrypts this file using the user's account credentials.
        /// </summary>
        public void Decrypt()
        {
            File.Decrypt(path);
        }

        /// <summary>
        /// Deletes this file.
        /// </summary>
        public override void Delete()
        {
            var success = Win32Wrapper.DeleteFile(path);
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                {
                    return;
                }

                __Error.WinIOError(error, path);
            }
        }

        /// <summary>
        /// Encrypts this file using the user's account credentials.
        /// </summary>
        public void Encrypt()
        {
            File.Encrypt(path);
        }

        /// <summary>
        /// Moves this file to a new location.
        /// </summary>
        /// <param name="NewFileName">The new filename.</param>
        public void MoveTo(string NewFileName)
        {
            if (NewFileName == null)
            {
                throw new ArgumentNullException(nameof(NewFileName), OpenEnvironment.GetResourceString("ArgumentNull_FileName"));
            }
            if (NewFileName.Length == 0)
            {
                throw new ArgumentException(OpenEnvironment.GetResourceString("Argument_EmptyFileName"), nameof(NewFileName));
            }

            if (!Win32Wrapper.MoveFile(path, NewFileName))
            {
                __Error.WinIOError();
            }

            path = NewFileName;
            dataInitialised = -1;
        }

        /// <summary>
        /// Opens this file with the specified file mode.
        /// </summary>
        /// <param name="FileMode">The file mode to use.</param>
        /// <returns>File stream to this file.</returns>
        public FileStream Open(FileMode FileMode)
        {
            return UnicodeFile.Open(path, FileMode);
        }

        /// <summary>
        /// Opens this file with the specified file mode and file access.
        /// </summary>
        /// <param name="FileMode">The file mode to use.</param>
        /// <param name="FileAccess">The file access to use.</param>
        /// <returns>File stream to this file.</returns>
        public FileStream Open(FileMode FileMode, FileAccess FileAccess)
        {
            return UnicodeFile.Open(path, FileMode, FileAccess);
        }

        /// <summary>
        /// Opens this file with the specified file mode, file access, and file share options.
        /// </summary>
        /// <param name="FileMode">The file mode to use.</param>
        /// <param name="FileAccess">The file access to use.</param>
        /// <param name="FileShare">The file share to use.</param>
        /// <returns>File stream to this file.</returns>
        public FileStream Open(FileMode FileMode, FileAccess FileAccess, FileShare FileShare)
        {
            return UnicodeFile.Open(path, FileMode, FileAccess, FileShare);
        }

        /// <summary>
        /// Opens this file for reading.
        /// </summary>
        /// <returns>File stream to this file.</returns>
        public FileStream OpenRead()
        {
            return UnicodeFile.OpenRead(path);
        }

        /// <summary>
        /// Opens this file for reading text.
        /// </summary>
        /// <returns>Stream reader to this file.</returns>
        public StreamReader OpenText()
        {
            return UnicodeFile.OpenText(path);
        }

        /// <summary>
        /// Opens this file for writing.
        /// </summary>
        /// <returns>File strema to this file.</returns>
        public FileStream OpenWrite()
        {
            return UnicodeFile.OpenWrite(path);
        }

        /// <summary>
        /// Replaces another file with this one and optionally moves the old file to a backup.
        /// </summary>
        /// <param name="NewFileName">New filename.</param>
        /// <param name="BackupFilename">Backup filename.</param>
        /// <returns>File info of the new file.</returns>
        public UnicodeFileInfo Replace(string NewFileName, string BackupFilename)
        {
            return Replace(NewFileName, BackupFilename, false);
        }

        /// <summary>
        /// Replaces another file with this one and optionally moves the old file to a backup and optionally ignoring metadata errors.
        /// </summary>
        /// <param name="NewFileName">New filename.</param>
        /// <param name="BackupFilename">Backup filename.</param>
        /// <param name="IgnoreMetadataErrors">Whether to ignore metadata errors.</param>
        /// <returns></returns>
        public UnicodeFileInfo Replace(string NewFileName, string BackupFilename, bool IgnoreMetadataErrors)
        {
            UnicodeFile.Replace(path, NewFileName, BackupFilename, IgnoreMetadataErrors);
            return new UnicodeFileInfo(NewFileName);
        }
    }
}
