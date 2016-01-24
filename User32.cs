/*  AeroShot - Transparent screenshot utility for Windows
    Copyright (C) 2012 Caleb Joseph

    AeroShot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AeroShot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>. */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AeroShot
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowsRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal WindowsRect(int x)
        {
            Left = x;
            Top = x;
            Right = x;
            Bottom = x;
        }
    }

    internal static class User32
    {
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        }

        public static int GetWindowStyle(IntPtr hWnd)
        {
            return NativeMethods.GetWindowLong(hWnd, GWL_STYLE);
        }

        public static int GetWindowExStyle(IntPtr hWnd)
        {
            return NativeMethods.GetWindowLong(hWnd, GWL_EXSTYLE);
        }
    }

    internal static partial class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName,
                                                 string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd,
                                                 IntPtr hWndInsertAfter, int x,
                                                 int y, int width, int height,
                                                 uint uFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd,
                                                  ref WindowsRect rect);

        internal delegate bool CallBackPtr(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(CallBackPtr lpEnumFunc,
                                                IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd,
                                                 StringBuilder lpString,
                                                 int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfoStruct pci);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon,
                                              out IconInfoStruct piconinfo);

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id,
                                                   int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}