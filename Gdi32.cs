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
using System.Drawing;
using System.Runtime.InteropServices;

namespace AeroShot
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct BitmapInfo
    {
        public Int32 biSize;
        public Int32 biWidth;
        public Int32 biHeight;
        public Int16 biPlanes;
        public Int16 biBitCount;
        public Int32 biCompression;
        public Int32 biSizeImage;
        public Int32 biXPelsPerMeter;
        public Int32 biYPelsPerMeter;
        public Int32 biClrUsed;
        public Int32 biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IconInfoStruct
    {
        [MarshalAs(UnmanagedType.Bool)]
        internal bool fIcon;
        internal Int32 xHotspot;
        internal Int32 yHotspot;
        internal IntPtr hbmMask;
        internal IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CursorInfoStruct
    {
        internal Int32 cbSize;
        internal Int32 flags;
        internal IntPtr hCursor;
        internal PointStruct ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PointStruct
    {
        internal int X;
        internal int Y;
    }


    internal static class Gdi
    {
        public static IntPtr CreateDisplayDC()
        {
            return NativeMethods.CreateDC("DISPLAY", null, null, IntPtr.Zero);
        }

        public static IntPtr SelectBitmap(IntPtr hdc, IntPtr hbmp)
        {
            return NativeMethods.SelectObject(hdc, hbmp);
        }

        public static void DeleteBitmap(IntPtr hBitmap)
        {
            // TODO check return value
            NativeMethods.DeleteObject(hBitmap);
        }

        private static class NativeMethods
        {
            [DllImport("gdi32.dll")]
            internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern int DeleteObject(IntPtr hDc);

            [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr CreateDC(string lpszDriver,
                                       string lpszDevice,
                                       string lpszOutput, IntPtr lpInitData);
        }
    }

    internal static partial class NativeMethods
    {
        [DllImport("gdi32.dll")]
        internal static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest,
                                           int wDest, int hDest,
                                           IntPtr hdcSource, int xSrc, int ySrc,
                                           CopyPixelOperation rop);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern int DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll")]
        internal static extern int SaveDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateDIBSection(IntPtr hdc,
                                                       [In] ref BitmapInfo pbmi,
                                                       uint pila,
                                                       out IntPtr ppvBits,
                                                       IntPtr hSection,
                                                       uint dwOffset);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc,
                                                             int nWidth,
                                                             int nHeight);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    }

}