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

namespace AeroShot
{
    internal enum DwmWindowAttribute
    {
        NonClientRenderingEnabled = 1,
        NonClientRenderingPolicy,
        TransitionsForceDisabled,
        AllowNonClientPaint,
        CaptionButtonBounds,
        NonClientRtlLayout,
        ForceIconicRepresentation,
        Flip3DPolicy,
        ExtendedFrameBounds,
        HasIconicBitmap,
        DisallowPeek,
        ExcludedFromPeek,
        Last
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowsMargins
    {
        internal int LeftWidth;
        internal int RightWidth;
        internal int TopHeight;
        internal int BottomHeight;

        internal WindowsMargins(int left, int right, int top, int bottom)
        {
            LeftWidth = left;
            RightWidth = right;
            TopHeight = top;
            BottomHeight = bottom;
        }
    }

    internal static class Dwm
    {
        private static readonly bool _isCompositionEnabled;

        static Dwm()
        {
            // TODO check HRESULT properly
            bool succeeded = NativeMethods.DwmIsCompositionEnabled(ref _isCompositionEnabled) == 0;
            if (!succeeded)
                _isCompositionEnabled = false;
        }

        public static bool IsCompositionEnabled()
        {
            return _isCompositionEnabled;
        }

        private static class NativeMethods
        {
            [DllImport("dwmapi.dll", ExactSpelling = true)]
            internal static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] ref bool pfEnabled);
        }
    }

    internal static partial class NativeMethods
    {
        [DllImport("dwmapi.dll", ExactSpelling = true)]
        internal static extern int DwmGetWindowAttribute(IntPtr hWnd,
            DwmWindowAttribute dwAttribute,
            ref WindowsRect pvAttribute,
            int cbAttribute);

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd,
            ref WindowsMargins
                pMarInset);
    }
}