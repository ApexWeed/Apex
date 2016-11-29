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
    public abstract class UnicodeFileSystemInfo
    {
        protected Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA data;
        protected int dataInitialised = -1;

        protected string path;

        public FileAttributes Attributes
        {
            get
            {
                EnsureDataLoaded();

                return (FileAttributes)data.fileAttributes;
            }
            set
            {
                var success = Win32Wrapper.SetFileAttributes(UnicodePath.FormatPath(path), (int)value);

                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();

                    if (error == Win32Wrapper.ERROR_INVALID_PARAMETER)
                    {
                        throw new ArgumentException(OpenEnvironment.GetResourceString("Arg_InvalidFileAttrs"));
                    }

                    if (error == Win32Wrapper.ERROR_ACCESS_DENIED)
                    {
                        // Keeping that logical error compatability.
                        throw new ArgumentException(OpenEnvironment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
                    }

                    __Error.WinIOError(error, path);
                }

                dataInitialised = -1;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return CreationTimeUtc.ToLocalTime();
            }
            set
            {
                CreationTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                EnsureDataLoaded();

                var dt = ((long)(data.ftCreationTimeHigh) << 32) | data.ftCreationTimeLow;
                return DateTime.FromFileTimeUtc(dt);
            }
            set
            {
                if (this is UnicodeDirectoryInfo)
                {
                    UnicodeDirectory.SetCreationTimeUtc(path, value);
                }
                else
                {
                    UnicodeFile.SetCreationTimeUtc(path, value);
                }

                dataInitialised = -1;
            }
        }

        public string FullName
        {
            get { return path; }
        }

        public abstract bool Exists { get; }

        public DateTime LastAccessTime
        {
            get
            {
                return LastAccessTimeUtc.ToLocalTime();
            }
            set
            {
                LastAccessTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastAccessTimeUtc
        {
            get
            {
                EnsureDataLoaded();

                var dt = ((long)(data.ftLastAccessTimeHigh) << 32) | data.ftLastAccessTimeLow;
                return DateTime.FromFileTimeUtc(dt);
            }
            set
            {
                if (this is UnicodeDirectoryInfo)
                {
                    UnicodeDirectory.SetLastAccessTimeUtc(path, value);
                }
                else
                {
                    UnicodeFile.SetLastAccessTimeUtc(path, value);
                }

                dataInitialised = -1;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
            set
            {
                LastWriteTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                EnsureDataLoaded();

                var dt = ((long)(data.ftLastWriteTimeHigh) << 32) | data.ftLastWriteTimeLow;
                return DateTime.FromFileTimeUtc(dt);
            }
            set
            {
                if (this is UnicodeDirectoryInfo)
                {
                    UnicodeDirectory.SetLastWriteTimeUtc(path, value);
                }
                else
                {
                    UnicodeFile.SetLastWriteTimeUtc(path, value);
                }

                dataInitialised = -1;
            }
        }

        public abstract string Name { get; }

        protected UnicodeFileSystemInfo()
        { }

        internal void InitialiseFrom(Win32Wrapper.WIN32_FIND_DATA FindData)
        {
            data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
            data.PopulateFrom(FindData);
            dataInitialised = 0;
        }

        public abstract void Delete();

        protected void EnsureDataLoaded()
        {
            if (dataInitialised == -1)
            {
                data = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();
                Refresh();
            }

            if (dataInitialised != 0)
            {
                __Error.WinIOError(dataInitialised, path);
            }
        }

        public void Refresh()
        {
            dataInitialised = UnicodeFile.FillAttributeInfo(UnicodePath.FormatPath(path), ref data, false);
        }
    }
}
