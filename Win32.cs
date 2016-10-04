using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Apex
{
    public class Win32
    {
        // Offset of window style value.
        public const int GWL_STYLE = -16;

        // Window style constants for scrollbars.
        public const int WS_VSCROLL = 0x00200000;
        public const int WS_HSCROLL = 0x00100000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
    }
}
