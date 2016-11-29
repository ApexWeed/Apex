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
    public class UnicodeFileInfo : UnicodeFileSystemInfo
    {
        private string name;

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

        public UnicodeDirectoryInfo Directory
        {
            get
            {
                return DirectoryName == null ? null : new UnicodeDirectoryInfo(DirectoryName);
            }
        }

        public string DirectoryName
        {
            get
            {
                return UnicodePath.GetDirectoryName(path);
            }
        }


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

        public bool IsReadOnly
        {
            get
            {
                return (Attributes & System.IO.FileAttributes.ReadOnly) != 0;
            }
            set
            {
                if (value)
                {
                    Attributes |= System.IO.FileAttributes.ReadOnly;
                }
                else
                {
                    Attributes &= ~System.IO.FileAttributes.ReadOnly;
                }
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public StreamWriter AppendText()
        {
            return UnicodeFile.AppendText(path);
        }

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

        public FileStream Create()
        {
            return File.Create(path);
        }

        public StreamWriter CreateText()
        {
            return File.CreateText(path);
        }

        public void Decrypt()
        {
            File.Decrypt(path);
        }

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

        public void Encrypt()
        {
            File.Encrypt(path);
        }

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

        public FileStream Open(FileMode FileMode)
        {
            return UnicodeFile.Open(path, FileMode);
        }

        public FileStream Open(FileMode FileMode, FileAccess FileAccess)
        {
            return UnicodeFile.Open(path, FileMode, FileAccess);
        }

        public FileStream Open(FileMode FileMode, FileAccess FileAccess, FileShare FileShare)
        {
            return UnicodeFile.Open(path, FileMode, FileAccess, FileShare);
        }

        public FileStream OpenRead()
        {
            return UnicodeFile.OpenRead(path);
        }

        public StreamReader OpenText()
        {
            return UnicodeFile.OpenText(path);
        }

        public FileStream OpenWrite()
        {
            return UnicodeFile.OpenWrite(path);
        }

        public UnicodeFileInfo Replace(string NewFileName, string BackupFilename)
        {
            return Replace(NewFileName, BackupFilename, false);
        }

        public UnicodeFileInfo Replace(string NewFileNAme, string BackupFilename, bool IgnoreMetadataErrors)
        {
            UnicodeFile.Replace(path, NewFileNAme, BackupFilename, IgnoreMetadataErrors);
            return new UnicodeFileInfo(NewFileNAme);
        }
    }
}
