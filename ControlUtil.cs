﻿using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Apex.Win32;

namespace Apex
{
    public static class ControlUtil
    {
        public static bool ShowScrollBar(IntPtr WindowHandle, ScrollBarDirection Direction, bool Show)
        {
            return Win32Wrapper.ShowScrollBar(WindowHandle, (int)Direction, Show);
        }

        public enum ScrollBarDirection
        {
            Horizontal = 0,
            Vertical = 1,
            Control = 2,
            Both = 3
        }

        public static ScrollBars GetVisibleScrollbars(Control ctl)
        {
            var wndStyle = Win32Wrapper.GetWindowLong(ctl.Handle, Win32Wrapper.GWL_STYLE);
            var hsVisible = (wndStyle & Win32Wrapper.WS_HSCROLL) != 0;
            var vsVisible = (wndStyle & Win32Wrapper.WS_VSCROLL) != 0;

            if (hsVisible)
                return vsVisible ? ScrollBars.Both : ScrollBars.Horizontal;
            else
                return vsVisible ? ScrollBars.Vertical : ScrollBars.None;
        }
    }
}
