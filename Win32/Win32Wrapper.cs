using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Apex.Win32
{
    public static class Win32Wrapper
    {
        #region "Structs"
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        [BestFitMapping(false)]
        public class WIN32_FIND_DATA
        {
            public int  dwFileAttributes;
            // ftCreationTime was a by-value FILETIME structure
            public uint ftCreationTime_dwLowDateTime;
            public uint ftCreationTime_dwHighDateTime;
            // ftLastAccessTime was a by-value FILETIME structure
            public uint ftLastAccessTime_dwLowDateTime;
            public uint ftLastAccessTime_dwHighDateTime;
            // ftLastWriteTime was a by-value FILETIME structure
            public uint ftLastWriteTime_dwLowDateTime;
            public uint ftLastWriteTime_dwHighDateTime;
            public int  nFileSizeHigh;
            public int  nFileSizeLow;
            // If the file attributes' reparse point flag is set, then
            // dwReserved0 is the file tag (aka reparse tag) for the 
            // reparse point.  Use this to figure out whether something is
            // a volume mount point or a symbolic link.
            public int  dwReserved0;
            public int  dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
            public string cAlternateFileName;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public int fileAttributes;
            public uint ftCreationTimeLow;
            public uint ftCreationTimeHigh;
            public uint ftLastAccessTimeLow;
            public uint ftLastAccessTimeHigh;
            public uint ftLastWriteTimeLow;
            public uint ftLastWriteTimeHigh;
            public int fileSizeHigh;
            public int fileSizeLow;

            [System.Security.SecurityCritical]
            public void PopulateFrom(WIN32_FIND_DATA findData)
            {
                // Copy the information to data
                fileAttributes = findData.dwFileAttributes;
                ftCreationTimeLow = findData.ftCreationTime_dwLowDateTime;
                ftCreationTimeHigh = findData.ftCreationTime_dwHighDateTime;
                ftLastAccessTimeLow = findData.ftLastAccessTime_dwLowDateTime;
                ftLastAccessTimeHigh = findData.ftLastAccessTime_dwHighDateTime;
                ftLastWriteTimeLow = findData.ftLastWriteTime_dwLowDateTime;
                ftLastWriteTimeHigh = findData.ftLastWriteTime_dwHighDateTime;
                fileSizeHigh = findData.nFileSizeHigh;
                fileSizeLow = findData.nFileSizeLow;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SECURITY_ATTRIBUTES
        {
            public int nLength;
            // don't remove null, or this field will disappear in bcl.small
            public unsafe byte * pSecurityDescriptor = null;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILE_TIME
        {
            public FILE_TIME(long fileTime)
            {
                ftTimeLow = (uint)fileTime;
                ftTimeHigh = (uint)(fileTime >> 32);
            }

            public long ToTicks()
            {
                return ((long)ftTimeHigh << 32) + ftTimeLow;
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh;
        }
        #endregion

        #region "Constants"
        // Offset of window style value.
        public const int GWL_STYLE = -16;

        // Window style constants for scrollbars.
        public const int WS_VSCROLL = 0x00200000;
        public const int WS_HSCROLL = 0x00100000;

        // Note, these are #defines used to extract handles, and are NOT handles.
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;

        // From wincon.h
        public const int CTRL_C_EVENT = 0;
        public const int CTRL_BREAK_EVENT = 1;
        public const int CTRL_CLOSE_EVENT = 2;
        public const int CTRL_LOGOFF_EVENT = 5;
        public const int CTRL_SHUTDOWN_EVENT = 6;
        public const short KEY_EVENT = 1;

        // From WinBase.h
        public const int FILE_TYPE_DISK = 0x0001;
        public const int FILE_TYPE_CHAR = 0x0002;
        public const int FILE_TYPE_PIPE = 0x0003;

        public const int REPLACEFILE_WRITE_THROUGH = 0x1;
        public const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        public const uint FILE_MAP_WRITE = 0x0002;
        public const uint FILE_MAP_READ = 0x0004;

        // Constants from WinNT.h
        public const int FILE_ATTRIBUTE_READONLY      = 0x00000001;
        public const int FILE_ATTRIBUTE_DIRECTORY     = 0x00000010;
        public const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        public const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);

        public const int PAGE_READWRITE = 0x04;

        public const int MEM_COMMIT  =  0x1000;
        public const int MEM_RESERVE =  0x2000;
        public const int MEM_RELEASE =  0x8000;
        public const int MEM_FREE    = 0x10000;

        // Error codes from WinError.h
        public const int ERROR_SUCCESS = 0x0;
        public const int ERROR_INVALID_FUNCTION = 0x1;
        public const int ERROR_FILE_NOT_FOUND = 0x2;
        public const int ERROR_PATH_NOT_FOUND = 0x3;
        public const int ERROR_ACCESS_DENIED  = 0x5;
        public const int ERROR_INVALID_HANDLE = 0x6;
        public const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        public const int ERROR_INVALID_DATA = 0xd;
        public const int ERROR_INVALID_DRIVE = 0xf;
        public const int ERROR_NO_MORE_FILES = 0x12;
        public const int ERROR_NOT_READY = 0x15;
        public const int ERROR_BAD_LENGTH = 0x18;
        public const int ERROR_SHARING_VIOLATION = 0x20;
        public const int ERROR_NOT_SUPPORTED = 0x32;
        public const int ERROR_FILE_EXISTS = 0x50;
        public const int ERROR_INVALID_PARAMETER = 0x57;
        public const int ERROR_BROKEN_PIPE = 0x6D;
        public const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        public const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        public const int ERROR_INVALID_NAME = 0x7B;
        public const int ERROR_BAD_PATHNAME = 0xA1;
        public const int ERROR_ALREADY_EXISTS = 0xB7;
        public const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        public const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        public const int ERROR_NO_DATA = 0xE8;
        public const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        public const int ERROR_MORE_DATA = 0xEA;
        public const int ERROR_DIRECTORY = 0x10B;
        public const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        public const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation
        public const int ERROR_NO_TOKEN = 0x3f0;
        public const int ERROR_DLL_INIT_FAILED = 0x45A;
        public const int ERROR_NON_ACCOUNT_SID = 0x4E9;
        public const int ERROR_NOT_ALL_ASSIGNED = 0x514;
        public const int ERROR_UNKNOWN_REVISION = 0x519;
        public const int ERROR_INVALID_OWNER = 0x51B;
        public const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        public const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        public const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        public const int ERROR_NONE_MAPPED = 0x534;
        public const int ERROR_INVALID_ACL = 0x538;
        public const int ERROR_INVALID_SID = 0x539;
        public const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
        public const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        public const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
        public const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        public const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

        // Error codes from ntstatus.h
        public const uint STATUS_SUCCESS = 0x00000000;
        public const uint STATUS_SOME_NOT_MAPPED = 0x00000107;
        public const uint STATUS_NO_MEMORY = 0xC0000017;
        public const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        public const uint STATUS_NONE_MAPPED = 0xC0000073;
        public const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        public const uint STATUS_ACCESS_DENIED = 0xC0000022;

        public const int INVALID_FILE_SIZE     = -1;

        // From WinStatus.h
        public const int STATUS_ACCOUNT_RESTRICTION = unchecked((int) 0xC000006E);

        // From WinBase.h
        public const int SEM_FAILCRITICALERRORS = 1;

        // Windows API definitions, from winbase.h and others
        public const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const int FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        public const int FILE_FLAG_OVERLAPPED = 0x40000000;
        public const int GENERIC_READ = unchecked((int)0x80000000);
        public const int GENERIC_WRITE = 0x40000000;
        public const int CREATE_NEW = 1;
        public const int CREATE_ALWAYS = 2;
        public const int OPEN_EXISTING = 3;
        public const int OPEN_ALWAYS = 4;
        public const int TRUNCATE_EXISTING = 5;

        public const int FILE_BEGIN = 0;
        public const int FILE_CURRENT = 1;
        public const int FILE_END = 2;

        // Error codes (not HRESULTS), from winerror.h
        public const int ERROR_HANDLE_EOF = 38;
        public const int ERROR_IO_PENDING = 997;

        public const int GET_FILE_EX_INFO_STANDARD = 0;
        #endregion

        #region "Methods"
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveFile(string lpExistingFileName, string lpNewFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReplaceFile(string ReplacedFilename, string ReplacementFilename, string BackupFilename, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFindHandle FindFirstFile(string lpFileName, WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = false, EntryPoint = "SetErrorMode", ExactSpelling = true)]
        private static extern int SetErrorMode_VistaAndOlder(int newMode);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetThreadErrorMode")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetErrorMode_Win7AndNewer(int newMode, out int oldMode);

        // RTM versions of Win7 and Windows Server 2008 R2
        private static readonly Version ThreadErrorModeMinOsVersion = new Version(6, 1, 7600);

        // this method uses the thread-safe version of SetErrorMode on Windows 7 / Windows Server 2008 R2 operating systems.
        public static int SetErrorMode(int newMode)
        {
            if (Environment.OSVersion.Version >= ThreadErrorModeMinOsVersion)
            {
                int oldMode;
                SetErrorMode_Win7AndNewer(newMode, out oldMode);
                return oldMode;
            }
            return SetErrorMode_VistaAndOlder(newMode);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        public static extern bool GetFileAttributesEx(string name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        public static extern bool SetFileAttributes(string Name, int Attributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern bool SetFileTime(SafeFileHandle hFile, FILE_TIME* creationTime, FILE_TIME* lastAccessTime, FILE_TIME* lastWriteTime);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetFileType(SafeFileHandle handle);

        // Do not use these directly, use the safe or unsafe versions above.
        // The safe version does not support devices (aka if will only open
        // files on disk), while the unsafe version give you the full semantic
        // of the native version.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            SECURITY_ATTRIBUTES securityAttrs,
            System.IO.FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DecryptFile(string Path, int ReservedMustBeZero = 0);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool EncryptFile(string Path);

        // Disallow access to all non-file devices from methods that take
        // a String.  This disallows DOS devices like "con:", "com1:", 
        // "lpt1:", etc.  Use this to avoid security problems, like allowing
        // a web client asking a server for "http://server/com1.aspx" and
        // then causing a worker process to hang.
        internal static SafeFileHandle SafeCreateFile(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            SECURITY_ATTRIBUTES securityAttrs,
            System.IO.FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            var handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            if (!handle.IsInvalid)
            {
                var fileType = GetFileType(handle);
                if (fileType != FILE_TYPE_DISK)
                {
                    handle.Dispose();
                    throw new NotSupportedException();
                }
            }

            return handle;
        }
        #endregion
    }
}
