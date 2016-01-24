﻿/*  AeroShot - Transparent screenshot utility for Windows
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
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AeroShot {
    internal struct ScreenshotTask {
        public enum BackgroundType {
            Transparent,
            Checkerboard,
            SolidColour
        }

        public BackgroundType Background;
        public Color BackgroundColour;
        public bool CaptureMouse;
        public int CheckerboardSize;
        public bool ClipboardNotDisk;
        public string DiskSaveDirectory;
        public bool DoResize;
        public int ResizeX;
        public int ResizeY;
        public IntPtr WindowHandle;

        public ScreenshotTask(IntPtr window, bool clipboard, string file,
                              bool resize, int resizeX, int resizeY,
                              BackgroundType backType, Color backColour,
                              int checkerSize, bool mouse) {
            WindowHandle = window;
            ClipboardNotDisk = clipboard;
            DiskSaveDirectory = file;
            DoResize = resize;
            ResizeX = resizeX;
            ResizeY = resizeY;
            Background = backType;
            BackgroundColour = backColour;
            CheckerboardSize = checkerSize;
            CaptureMouse = mouse;
        }
    }

    internal static class Screenshot {
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int GWL_STYLE = -16;
        private const long WS_SIZEBOX = 0x00040000L;
        private const uint SWP_SHOWWINDOW = 0x0040;

        internal static void CaptureWindow(ref ScreenshotTask data) {
            IntPtr start = NativeMethods.FindWindow("Button", "Start");
            IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (data.ClipboardNotDisk ||
                Directory.Exists(data.DiskSaveDirectory)) {
                try {
                    // Hide the taskbar, just incase it gets in the way
                    if (data.WindowHandle != start &&
                        data.WindowHandle != taskbar) {
                        NativeMethods.ShowWindow(start, 0);
                        NativeMethods.ShowWindow(taskbar, 0);
                        Application.DoEvents();
                    }
                    if (NativeMethods.IsIconic(data.WindowHandle)) {
                        NativeMethods.ShowWindow(data.WindowHandle, 1);
                        Thread.Sleep(300); // Wait for window to be restored
                    } else {
                        NativeMethods.ShowWindow(data.WindowHandle, 5);
                        Thread.Sleep(100);
                    }
                    NativeMethods.SetForegroundWindow(data.WindowHandle);

                    var r = new WindowsRect(0);
                    if (data.DoResize) {
                        SmartResizeWindow(ref data, out r);
                        Thread.Sleep(100);
                    }

                    int length =
                        NativeMethods.GetWindowTextLength(data.WindowHandle);
                    var sb = new StringBuilder(length + 1);
                    NativeMethods.GetWindowText(data.WindowHandle, sb, sb.Capacity);

                    string name = sb.ToString();

                    foreach (char inv in Path.GetInvalidFileNameChars())
                        name = name.Replace(inv.ToString(), string.Empty);

                    Bitmap s = CaptureCompositeScreenshot(ref data);

                    // Show the taskbar again
                    if (data.WindowHandle != start &&
                        data.WindowHandle != taskbar) {
                        NativeMethods.ShowWindow(start, 1);
                        NativeMethods.ShowWindow(taskbar, 1);
                    }

                    if (s == null) {
                        MessageBox.Show(
                            "The screenshot taken was blank, it will not be saved.",
                            "Warning", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    } else {
                        if (data.ClipboardNotDisk &&
                            data.Background !=
                            ScreenshotTask.BackgroundType.Transparent)
                            // Screenshot is already opaque, don't need to modify it
                            Clipboard.SetImage(s);
                        else if (data.ClipboardNotDisk) {
                            var whiteS = new Bitmap(s.Width, s.Height,
                                                    PixelFormat.Format24bppRgb);
                            using (
                                Graphics graphics = Graphics.FromImage(whiteS)) {
                                graphics.Clear(Color.White);
                                graphics.DrawImage(s, 0, 0, s.Width, s.Height);
                            }
                            using (var stream = new MemoryStream()) {
                                // Save screenshot in clipboard as PNG which some applications support (eg. Microsoft Office)
                                s.Save(stream, ImageFormat.Png);
                                var pngClipboardData = new DataObject("PNG",
                                                                      stream);

                                // Add fallback for applications that don't support PNG from clipboard (eg. Photoshop or Paint)
                                pngClipboardData.SetData(DataFormats.Bitmap,
                                                         whiteS);
                                Clipboard.Clear();
                                Clipboard.SetDataObject(pngClipboardData, true);
                            }
                            whiteS.Dispose();
                        } else {
                            name = name.Trim();
                            if (name == string.Empty)
                                name = "AeroShot";
                            string path = Path.Combine(data.DiskSaveDirectory,
                                                       name + ".png");

                            if (File.Exists(path)) {
                                for (int i = 1; i < 9999; i++) {
                                    path = Path.Combine(data.DiskSaveDirectory,
                                                        name + " " + i + ".png");
                                    if (!File.Exists(path))
                                        break;
                                }
                            }
                            s.Save(path, ImageFormat.Png);
                        }
                        s.Dispose();
                    }

                    if (data.DoResize) {
                        if (
                            (NativeMethods.GetWindowLong(data.WindowHandle,
                                                      GWL_STYLE) & WS_SIZEBOX) ==
                            WS_SIZEBOX) {
                            NativeMethods.SetWindowPos(data.WindowHandle,
                                                    (IntPtr) 0, r.Left, r.Top,
                                                    r.Right - r.Left,
                                                    r.Bottom - r.Top,
                                                    SWP_SHOWWINDOW);
                        }
                    }
                } catch (Exception) {
                    if (data.WindowHandle != start &&
                        data.WindowHandle != taskbar) {
                        NativeMethods.ShowWindow(start, 1);
                        NativeMethods.ShowWindow(taskbar, 1);
                    }
                    MessageBox.Show(
                        "An error occurred while trying to take a screenshot.\r\n\r\nPlease make sure you have selected a valid window.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } else {
                MessageBox.Show("Invalid directory chosen.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void SmartResizeWindow(ref ScreenshotTask data,
                                              out WindowsRect oldWindowSize) {
            oldWindowSize = new WindowsRect(0);
            if ((NativeMethods.GetWindowLong(data.WindowHandle, GWL_STYLE) &
                 WS_SIZEBOX) != WS_SIZEBOX)
                return;

            var r = new WindowsRect();
            NativeMethods.GetWindowRect(data.WindowHandle, ref r);
            oldWindowSize = r;

            Bitmap f = CaptureCompositeScreenshot(ref data);
            if (f != null) {
                NativeMethods.SetWindowPos(data.WindowHandle, (IntPtr) 0, r.Left,
                                        r.Top,
                                        data.ResizeX -
                                        (f.Width - (r.Right - r.Left)),
                                        data.ResizeY -
                                        (f.Height - (r.Bottom - r.Top)),
                                        SWP_SHOWWINDOW);
                f.Dispose();
            } else {
                NativeMethods.SetWindowPos(data.WindowHandle, (IntPtr) 0, r.Left,
                                        r.Top, data.ResizeX, data.ResizeY,
                                        SWP_SHOWWINDOW);
            }
        }

        private static unsafe Bitmap CaptureCompositeScreenshot(
            ref ScreenshotTask data) {
            Color tmpColour = data.BackgroundColour;
            if (data.Background == ScreenshotTask.BackgroundType.Transparent ||
                data.Background == ScreenshotTask.BackgroundType.Checkerboard)
                tmpColour = Color.White;
            var backdrop = new Form {
                BackColor = tmpColour,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                Opacity = 0
            };

            // Generate a rectangle with the size of all monitors combined
            Rectangle totalSize = Rectangle.Empty;
            foreach (Screen s in Screen.AllScreens)
                totalSize = Rectangle.Union(totalSize, s.Bounds);

            var rct = new WindowsRect();

            if (
                NativeMethods.DwmGetWindowAttribute(data.WindowHandle,
                                                 DwmWindowAttribute
                                                     .ExtendedFrameBounds,
                                                 ref rct, sizeof (WindowsRect)) !=
                0)
                // DwmGetWindowAttribute() failed, usually means Aero is disabled so we fall back to GetWindowRect()
                NativeMethods.GetWindowRect(data.WindowHandle, ref rct);
            else {
                // DwmGetWindowAttribute() succeeded
                // Add a 100px margin for window shadows. Excess transparency is trimmed out later
                rct.Left -= 100;
                rct.Right += 100;
                rct.Top -= 100;
                rct.Bottom += 100;
            }

            // These next 4 checks handle if the window is outside of the visible screen
            if (rct.Left < totalSize.Left)
                rct.Left = totalSize.Left;
            if (rct.Right > totalSize.Right)
                rct.Right = totalSize.Right;
            if (rct.Top < totalSize.Top)
                rct.Top = totalSize.Top;
            if (rct.Bottom > totalSize.Bottom)
                rct.Bottom = totalSize.Bottom;

            NativeMethods.ShowWindow(backdrop.Handle, 4);
            NativeMethods.SetWindowPos(backdrop.Handle, data.WindowHandle, rct.Left,
                                    rct.Top, rct.Right - rct.Left,
                                    rct.Bottom - rct.Top, SWP_NOACTIVATE);
            backdrop.Opacity = 1;
            Application.DoEvents();

            // Capture screenshot with white background
            Bitmap whiteShot =
                CaptureScreenRegion(new Rectangle(rct.Left, rct.Top,
                                                  rct.Right - rct.Left,
                                                  rct.Bottom - rct.Top));

            if (data.Background == ScreenshotTask.BackgroundType.SolidColour) {
                backdrop.Dispose();
                if (data.CaptureMouse)
                    DrawCursorToBitmap(whiteShot, new Point(rct.Left, rct.Top));
                Bitmap final = CropEmptyEdges(whiteShot, tmpColour);
                whiteShot.Dispose();
                return final;
            }

            backdrop.BackColor = Color.Black;
            Application.DoEvents();

            // Capture screenshot with black background
            Bitmap blackShot =
                CaptureScreenRegion(new Rectangle(rct.Left, rct.Top,
                                                  rct.Right - rct.Left,
                                                  rct.Bottom - rct.Top));

            backdrop.Dispose();

            Bitmap transparentImage = DifferentiateAlpha(whiteShot, blackShot);
            if (data.CaptureMouse)
                DrawCursorToBitmap(transparentImage,
                                   new Point(rct.Left, rct.Top));
            transparentImage = CropEmptyEdges(transparentImage,
                                              Color.FromArgb(0, 0, 0, 0));

            whiteShot.Dispose();
            blackShot.Dispose();

            if (data.Background == ScreenshotTask.BackgroundType.Checkerboard) {
                var final = new Bitmap(transparentImage.Width,
                                       transparentImage.Height,
                                       PixelFormat.Format24bppRgb);
                Graphics finalGraphics = Graphics.FromImage(final);
                var brush =
                    new TextureBrush(GenerateChecker(data.CheckerboardSize));
                finalGraphics.FillRectangle(brush, finalGraphics.ClipBounds);
                finalGraphics.DrawImageUnscaled(transparentImage, 0, 0);
                finalGraphics.Dispose();
                transparentImage.Dispose();
                return final;
            }
            // Returns a bitmap with transparency, calculated by differentiating the white and black screenshots
            return transparentImage;
        }

        private static void DrawCursorToBitmap(Bitmap windowImage,
                                               Point offsetLocation) {
            var ci = new CursorInfoStruct();
            ci.cbSize = Marshal.SizeOf(ci);
            if (NativeMethods.GetCursorInfo(out ci)) {
                if (ci.flags == 1) {
                    IntPtr hicon = NativeMethods.CopyIcon(ci.hCursor);
                    IconInfoStruct icInfo;
                    if (NativeMethods.GetIconInfo(hicon, out icInfo)) {
                        var loc =
                            new Point(
                                ci.ptScreenPos.X - offsetLocation.X -
                                icInfo.xHotspot,
                                ci.ptScreenPos.Y - offsetLocation.Y -
                                icInfo.yHotspot);
                        Icon ic = Icon.FromHandle(hicon);
                        Bitmap bmp = ic.ToBitmap();

                        Graphics g = Graphics.FromImage(windowImage);
                        g.DrawImage(bmp, new Rectangle(loc, bmp.Size));
                        g.Dispose();
                        NativeMethods.DestroyIcon(hicon);
                        bmp.Dispose();
                    }
                }
            }
        }

        private static Bitmap CaptureScreenRegion(Rectangle crop) {
            Rectangle totalSize = Rectangle.Empty;

            foreach (Screen s in Screen.AllScreens)
                totalSize = Rectangle.Union(totalSize, s.Bounds);

            IntPtr hSrc = NativeMethods.CreateDC("DISPLAY", null, null, IntPtr.Zero);
            IntPtr hDest = NativeMethods.CreateCompatibleDC(hSrc);
            IntPtr hBmp = NativeMethods.CreateCompatibleBitmap(hSrc,
                                                            crop.Right -
                                                            crop.Left,
                                                            crop.Bottom -
                                                            crop.Top);
            IntPtr hOldBmp = NativeMethods.SelectObject(hDest, hBmp);
            NativeMethods.BitBlt(hDest, 0, 0, crop.Right - crop.Left,
                              crop.Bottom - crop.Top, hSrc, crop.Left, crop.Top,
                              CopyPixelOperation.SourceCopy |
                              CopyPixelOperation.CaptureBlt);
            Bitmap bmp = Image.FromHbitmap(hBmp);
            NativeMethods.SelectObject(hDest, hOldBmp);
            NativeMethods.DeleteObject(hBmp);
            NativeMethods.DeleteDC(hDest);
            NativeMethods.DeleteDC(hSrc);

            return bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height),
                             PixelFormat.Format24bppRgb);
        }

        private static unsafe Bitmap GenerateChecker(int s) {
            var b1 = new Bitmap(s*2, s*2, PixelFormat.Format24bppRgb);
            var b = new UnsafeBitmap(b1);
            b.LockImage();
            PixelData* pixel;
            for (int x = 0, y = 0; x < s*2 && y < s*2;) {
                pixel = b.GetPixel(x, y);
                if ((x >= 0 && x <= s - 1) && (y >= 0 && y <= s - 1))
                    pixel->SetAll(255);
                if ((x >= s && x <= s*2 - 1) && (y >= 0 && y <= s - 1))
                    pixel->SetAll(200);
                if ((x >= 0 && x <= s - 1) && (y >= s && y <= s*2 - 1))
                    pixel->SetAll(200);
                if ((x >= s && x <= s*2 - 1) && (y >= s && y <= s*2 - 1))
                    pixel->SetAll(255);
                if (x == s*2 - 1) {
                    y++;
                    x = 0;
                    continue;
                }
                x++;
            }
            b.UnlockImage();
            return b1;
        }

        private static unsafe Bitmap CropEmptyEdges(Bitmap b1, Color trimColour) {
            if (b1 == null)
                return null;

            int sizeX = b1.Width;
            int sizeY = b1.Height;
            var b = new UnsafeBitmap(b1);
            b.LockImage();

            int left = -1;
            int top = -1;
            int right = -1;
            int bottom = -1;

            PixelData* pixel;

            for (int x = 0, y = 0;;) {
                pixel = b.GetPixel(x, y);
                if (left == -1) {
                    if ((trimColour.A == 0 && pixel->Alpha != 0) ||
                        (trimColour.R != pixel->Red &
                         trimColour.G != pixel->Green &
                         trimColour.B != pixel->Blue)) {
                        left = x;
                        x = 0;
                        y = 0;
                        continue;
                    }
                    if (y == sizeY - 1) {
                        x++;
                        y = 0;
                    } else
                        y++;

                    continue;
                }
                if (top == -1) {
                    if ((trimColour.A == 0 && pixel->Alpha != 0) ||
                        (trimColour.R != pixel->Red &
                         trimColour.G != pixel->Green &
                         trimColour.B != pixel->Blue)) {
                        top = y;
                        x = sizeX - 1;
                        y = 0;
                        continue;
                    }
                    if (x == sizeX - 1) {
                        y++;
                        x = 0;
                    } else
                        x++;

                    continue;
                }
                if (right == -1) {
                    if ((trimColour.A == 0 && pixel->Alpha != 0) ||
                        (trimColour.R != pixel->Red &
                         trimColour.G != pixel->Green &
                         trimColour.B != pixel->Blue)) {
                        right = x + 1;
                        x = 0;
                        y = sizeY - 1;
                        continue;
                    }
                    if (y == sizeY - 1) {
                        x--;
                        y = 0;
                    } else
                        y++;

                    continue;
                }
                if (bottom == -1) {
                    if ((trimColour.A == 0 && pixel->Alpha != 0) ||
                        (trimColour.R != pixel->Red &
                         trimColour.G != pixel->Green &
                         trimColour.B != pixel->Blue)) {
                        bottom = y + 1;
                        break;
                    }
                    if (x == sizeX - 1) {
                        y--;
                        x = 0;
                    } else
                        x++;

                    continue;
                }
            }
            b.UnlockImage();
            if (left >= right || top >= bottom)
                return null;

            Bitmap final =
                b1.Clone(new Rectangle(left, top, right - left, bottom - top),
                         b1.PixelFormat);
            b1.Dispose();
            return final;
        }

        private static unsafe Bitmap DifferentiateAlpha(Bitmap whiteBitmap,
                                                        Bitmap blackBitmap) {
            if (whiteBitmap == null || blackBitmap == null ||
                whiteBitmap.Width != blackBitmap.Width ||
                whiteBitmap.Height != blackBitmap.Height)
                return null;
            int sizeX = whiteBitmap.Width;
            int sizeY = whiteBitmap.Height;
            var final = new Bitmap(sizeX, sizeY, PixelFormat.Format32bppArgb);
            var a = new UnsafeBitmap(whiteBitmap);
            var b = new UnsafeBitmap(blackBitmap);
            var f = new UnsafeBitmap(final);
            a.LockImage();
            b.LockImage();
            f.LockImage();

            bool empty = true;

            for (int x = 0, y = 0; x < sizeX && y < sizeY;) {
                PixelData* pixelA = a.GetPixel(x, y);
                PixelData* pixelB = b.GetPixel(x, y);
                PixelData* pixelF = f.GetPixel(x, y);

                pixelF->Alpha =
                    ToByte((pixelB->Red - pixelA->Red + 255 + pixelB->Green -
                            pixelA->Green + 255 + pixelB->Blue - pixelA->Blue +
                            255)/3);
                if (pixelF->Alpha > 0) {
                    // Following math creates an image optimized to be displayed on a black background
                    pixelF->Red = ToByte(255*pixelB->Red/pixelF->Alpha);
                    pixelF->Green = ToByte(255*pixelB->Green/pixelF->Alpha);
                    pixelF->Blue = ToByte(255*pixelB->Blue/pixelF->Alpha);

                    // Following math creates an image optimized to be displayed on a white background
                    /*pixelF->Red =
                        ToByte(255*(pixelA->Red + pixelF->Alpha - 255)/
                               pixelF->Alpha);
                    pixelF->Green =
                        ToByte(255*(pixelA->Green + pixelF->Alpha - 255)/
                               pixelF->Alpha);
                    pixelF->Blue =
                        ToByte(255*(pixelA->Blue + pixelF->Alpha - 255)/
                               pixelF->Alpha);*/
                }
                if (empty && pixelF->Alpha > 0)
                    empty = false;

                if (x == sizeX - 1) {
                    y++;
                    x = 0;
                    continue;
                }
                x++;
            }

            a.UnlockImage();
            b.UnlockImage();
            f.UnlockImage();
            return empty ? null : final;
        }

        private static byte ToByte(int i) {
            return (byte) (i > 255 ? 255 : (i < 0 ? 0 : i));
        }
    }
}