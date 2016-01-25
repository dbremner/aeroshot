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
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AeroShot {
    public sealed partial class MainForm : Form {
        private const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_EX_APPWINDOW = 0x00040000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const uint GW_OWNER = 4;
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private readonly List<IntPtr> _handleList = new List<IntPtr>();
        private readonly RegistryKey _registryKey;
        private readonly int _windowId;
        private NativeMethods.CallBackPtr _callBackPtr;
        private bool _dwmComposited;
        private Image _ssButtonImage;
        private Thread _worker;

        public MainForm() {
            DoubleBuffered = true;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            InitializeComponent();

            _dwmComposited = Dwm.IsCompositionEnabled;
            if (_dwmComposited) {
                ssButton.Location = new Point(ssButton.Location.X, 310);
                var margin = new WindowsMargins(0, 0, 0, 35);
                NativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margin);
            }

            _windowId = GetHashCode();
            NativeMethods.RegisterHotKey(Handle, _windowId, MOD_ALT,
                                      (int) Keys.PrintScreen);

            object value;
            _registryKey =
                Registry.CurrentUser.CreateSubKey(@"Software\AeroShot");
            if ((value = _registryKey.GetValue("LastPath")) != null &&
                value.GetType() == (typeof (string))) {
                if (((string) value).Substring(0, 1) == "*") {
                    folderTextBox.Text = ((string) value).Substring(1);
                    clipboardButton.Checked = true;
                } else {
                    folderTextBox.Text = (string) value;
                    diskButton.Checked = true;
                }
            } else {
                folderTextBox.Text =
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if ((value = _registryKey.GetValue("WindowSize")) != null &&
                value.GetType() == (typeof (long))) {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte) (((long) value >> (i*8)) & 0xff);
                resizeCheckbox.Checked = (b[0] & 1) == 1;
                windowWidth.Value = b[1] << 16 | b[2] << 8 | b[3];
                windowHeight.Value = b[4] << 16 | b[5] << 8 | b[6];
            }

            if ((value = _registryKey.GetValue("Opaque")) != null &&
                value.GetType() == (typeof (long))) {
                var b = new byte[8];
                for (int i = 0; i < 8; i++)
                    b[i] = (byte) (((long) value >> (i*8)) & 0xff);
                opaqueCheckbox.Checked = (b[0] & 1) == 1;
                if ((b[0] & 2) == 2)
                    opaqueType.SelectedIndex = 0;
                if ((b[0] & 4) == 4)
                    opaqueType.SelectedIndex = 1;

                checkerValue.Value = b[1] + 2;

                var hex = new StringBuilder(6);
                hex.AppendFormat("{0:X2}", b[2]);
                hex.AppendFormat("{0:X2}", b[3]);
                hex.AppendFormat("{0:X2}", b[4]);
                colourHexBox.Text = hex.ToString();
            } else
                opaqueType.SelectedIndex = 0;
            if ((value = _registryKey.GetValue("CapturePointer")) != null &&
                value.GetType() == (typeof (int)))
                mouseCheckbox.Checked = ((int) value & 1) == 1;

            groupBox1.Enabled = resizeCheckbox.Checked;
            groupBox2.Enabled = opaqueCheckbox.Checked;
            groupBox3.Enabled = mouseCheckbox.Checked;

            _ssButtonImage = Resources.capture;
        }

        private void ScreenshotButtonPlaceholderMouseEnter(object sender,
                                                           EventArgs e) {
            _ssButtonImage = Resources.capture_hover;
            Invalidate();
            Update();
        }

        private void ScreenshotButtonPlaceholderMouseDown(object sender,
                                                          MouseEventArgs e) {
            _ssButtonImage = Resources.capture_press;
            Invalidate();
            Update();
        }

        private void ScreenshotButtonPlaceholderMouseLeave(object sender,
                                                           EventArgs e) {
            _ssButtonImage = Resources.capture;
            Invalidate();
            Update();
        }

        private void ScreenshotButtonPlaceholderMouseUp(object sender,
                                                        MouseEventArgs e) {
            if (e != null &&
                (e.X < 0 || e.Y < 0 || e.X > ssButton.Size.Width ||
                 e.Y > ssButton.Size.Height)) {
                _ssButtonImage = Resources.capture;
                Invalidate();
                Update();
                return;
            }
            _ssButtonImage = Resources.capture_hover;
            Invalidate();
            Update();

            ScreenshotTask info = GetParamteresFromUI(false);
            _worker = new Thread(() => Screenshot.CaptureWindow(ref info)) {
                IsBackground = true
            };
            _worker.SetApartmentState(ApartmentState.STA);
            _worker.Start();
        }

        private void ScreenshotButtonPlaceholderKeyDown(object sender,
                                                        KeyEventArgs e) {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                ScreenshotButtonPlaceholderMouseDown(null, null);
        }

        private void ScreenshotButtonPlaceholderKeyUp(object sender,
                                                      KeyEventArgs e) {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                ScreenshotButtonPlaceholderMouseUp(null, null);
        }

        private void ScreenshotButtonPlaceholderEnter(object sender, EventArgs e) {
            ScreenshotButtonPlaceholderMouseEnter(null, null);
        }

        private void ScreenshotButtonPlaceholderLeave(object sender, EventArgs e) {
            ScreenshotButtonPlaceholderMouseLeave(null, null);
        }

        private void RefreshButtonClick(object sender, EventArgs e) {
            _handleList.Clear();
            windowList.Items.Clear();

            _callBackPtr = ListWindows;
            NativeMethods.EnumWindows(_callBackPtr, (IntPtr) 0);
            windowList.SelectedIndex = 0;
        }

        private void BrowseButtonClick(object sender, EventArgs e) {
            if (folderSelection.ShowDialog() == DialogResult.OK)
                folderTextBox.Text = folderSelection.SelectedPath;
        }

        private void ColourDisplayClick(object sender, EventArgs e) {
            if (colourDialog.ShowDialog() == DialogResult.OK) {
                colourDisplay.Color = colourDialog.Color;

                var hex = new StringBuilder(6);
                hex.AppendFormat("{0:X2}", colourDisplay.Color.R);
                hex.AppendFormat("{0:X2}", colourDisplay.Color.G);
                hex.AppendFormat("{0:X2}", colourDisplay.Color.B);
                colourHexBox.Text = hex.ToString();
            }
        }

        private void ResizeCheckboxStateChange(object sender, EventArgs e) {
            groupBox1.Enabled = resizeCheckbox.Checked;
        }

        private void OpaqueCheckboxStateChange(object sender, EventArgs e) {
            groupBox2.Enabled = opaqueCheckbox.Checked;
        }

        private void MouseCheckboxStateChange(object sender, EventArgs e) {
            groupBox3.Enabled = mouseCheckbox.Checked;
        }

        private void ClipboardButtonStateChange(object sender, EventArgs e) {
            if (!clipboardButton.Checked)
                return;
            diskButton.Checked = false;
            folderTextBox.Enabled = false;
            bButton.Enabled = false;
        }

        private void DiskButtonStateChange(object sender, EventArgs e) {
            if (!diskButton.Checked)
                return;
            clipboardButton.Checked = false;
            folderTextBox.Enabled = true;
            bButton.Enabled = true;
        }

        private void OpaqueTypeItemChange(object sender, EventArgs e) {
            if (opaqueType.SelectedIndex == 0) {
                label4.Text = "Checker size:";
                checkerValue.Enabled = true;
                checkerValue.Visible = true;
                label5.Visible = true;

                colourDisplay.Enabled = false;
                colourDisplay.Visible = false;
                colourHexBox.Enabled = false;
                colourHexBox.Visible = false;
                labelHash.Visible = false;
            }
            if (opaqueType.SelectedIndex == 1) {
                label4.Text = "Colour:";
                colourDisplay.Enabled = true;
                colourDisplay.Visible = true;
                colourHexBox.Enabled = true;
                colourHexBox.Visible = true;
                labelHash.Visible = true;

                checkerValue.Enabled = false;
                checkerValue.Visible = false;
                label5.Visible = false;
            }
        }

        private void ColourTextboxTextChange(object sender, EventArgs e) {
            var c = new[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
                'D', 'E', 'F', 'a', 'b', 'c', 'd', 'e', 'f'
            };
            foreach (char v in colourHexBox.Text) {
                bool b = false;
                foreach (char v1 in c) {
                    if (v == v1) {
                        b = true;
                        break;
                    }
                }
                if (!b)
                    colourHexBox.Text = colourHexBox.Text.Replace(v.ToString(),
                                                                  string.Empty);
            }

            if (colourHexBox.TextLength != 6)
                return;

            colourDisplay.Color =
                Color.FromArgb(Convert.ToInt32("FF" + colourHexBox.Text, 16));
            colourDialog.Color =
                Color.FromArgb(Convert.ToInt32("FF" + colourHexBox.Text, 16));
        }

        private void FormShown(object sender, EventArgs e) {
            RefreshButtonClick(null, null);
        }

        private void FormClose(object sender, FormClosingEventArgs e) {
            NativeMethods.UnregisterHotKey(Handle, _windowId);
            if (clipboardButton.Checked)
                _registryKey.SetValue("LastPath", "*" + folderTextBox.Text);
            else
                _registryKey.SetValue("LastPath", folderTextBox.Text);

            // Save resizing settings in an 8-byte long
            var b = new byte[8];
            b[0] = (byte) (resizeCheckbox.Checked ? 1 : 0);

            b[3] = (byte) ((int) windowWidth.Value & 0xff);
            b[2] = (byte) (((int) windowWidth.Value >> 8) & 0xff);
            b[1] = (byte) (((int) windowWidth.Value >> 16) & 0xff);

            b[6] = (byte) ((int) windowHeight.Value & 0xff);
            b[5] = (byte) (((int) windowHeight.Value >> 8) & 0xff);
            b[4] = (byte) (((int) windowHeight.Value >> 16) & 0xff);

            long data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("WindowSize", data, RegistryValueKind.QWord);

            // Save background colour settings in an 8-byte long
            b = new byte[8];
            b[0] = (byte) (opaqueCheckbox.Checked ? 1 : 0);
            b[0] += (byte) Math.Pow(2, opaqueType.SelectedIndex + 1);

            b[1] = (byte) (checkerValue.Value - 2);

            b[2] = colourDialog.Color.R;
            b[3] = colourDialog.Color.G;
            b[4] = colourDialog.Color.B;

            data = BitConverter.ToInt64(b, 0);
            _registryKey.SetValue("Opaque", data, RegistryValueKind.QWord);

            _registryKey.SetValue("CapturePointer",
                                  mouseCheckbox.Checked ? 1 : 0,
                                  RegistryValueKind.DWord);
        }

        private void FormSizeChange(object sender, EventArgs e) {
            if (WindowState == FormWindowState.Normal) {
                Invalidate();
                Update();
            }
        }

        private void OnPaint(object sender, PaintEventArgs e) {
            if (_dwmComposited) {
                var rc = new Rectangle(0, ClientSize.Height - 35,
                                       ClientSize.Width, 35);
                IntPtr destdc = e.Graphics.GetHdc();
                IntPtr memdc = NativeMethods.CreateCompatibleDC(destdc);
                IntPtr bitmapOld = IntPtr.Zero;
                var dib = new BitmapInfo {
                    biHeight = -(rc.Bottom - rc.Top),
                    biWidth = rc.Right - rc.Left,
                    biPlanes = 1,
                    biSize = Marshal.SizeOf(typeof (BitmapInfo)),
                    biBitCount = 32,
                    biCompression = 0
                };
                if (NativeMethods.SaveDC(memdc) != 0) {
                    IntPtr tmp;
                    IntPtr bitmap = NativeMethods.CreateDIBSection(memdc, ref dib,
                                                                0, out tmp,
                                                                IntPtr.Zero, 0);
                    if (!(bitmap == IntPtr.Zero)) {
                        bitmapOld = Gdi.SelectBitmap(memdc, bitmap);
                        NativeMethods.BitBlt(destdc, rc.Left, rc.Top,
                                          rc.Right - rc.Left, rc.Bottom - rc.Top,
                                          memdc, 0, 0,
                                          CopyPixelOperation.SourceCopy);
                    }
                    Gdi.SelectBitmap(memdc, bitmapOld);
                    Gdi.DeleteBitmap(bitmap);
                    NativeMethods.DeleteDC(memdc);
                }
                e.Graphics.ReleaseHdc(destdc);
            }
            e.Graphics.DrawImage(_ssButtonImage,
                                 new Rectangle(ssButton.Location, ssButton.Size));
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY) {
                ScreenshotTask info = GetParamteresFromUI(true);
                _worker = new Thread(() => Screenshot.CaptureWindow(ref info)) {
                    IsBackground = true
                };
                _worker.SetApartmentState(ApartmentState.STA);
                _worker.Start();
            }
            if (m.Msg == WM_DWMCOMPOSITIONCHANGED) {
                _dwmComposited = Dwm.IsCompositionEnabled;

                if (_dwmComposited) {
                    ssButton.Location = new Point(ssButton.Location.X, 310);
                    var margin = new WindowsMargins(0, 0, 0, 35);
                    NativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margin);
                } else
                    ssButton.Location = new Point(ssButton.Location.X, 305);
            }
        }

        private ScreenshotTask GetParamteresFromUI(bool useForegroundWindow) {
            var type = ScreenshotTask.BackgroundType.Transparent;
            if (opaqueCheckbox.Checked && opaqueType.SelectedIndex == 0)
                type = ScreenshotTask.BackgroundType.Checkerboard;
            else if (opaqueCheckbox.Checked && opaqueType.SelectedIndex == 1)
                type = ScreenshotTask.BackgroundType.SolidColour;

            return
                new ScreenshotTask(
                    useForegroundWindow
                        ? NativeMethods.GetForegroundWindow()
                        : _handleList[windowList.SelectedIndex],
                    clipboardButton.Checked, folderTextBox.Text,
                    resizeCheckbox.Checked, (int) windowWidth.Value,
                    (int) windowHeight.Value, type, colourDialog.Color,
                    (int) checkerValue.Value,
                    useForegroundWindow && mouseCheckbox.Checked);
        }

        private bool ListWindows(IntPtr hWnd, int lParam) {
            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;
            if ((User32.GetWindowStyle(hWnd) & WS_EX_APPWINDOW) !=
                WS_EX_APPWINDOW) {
                if (NativeMethods.GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
                    return true;
                if ((User32.GetWindowExStyle(hWnd) &
                     WS_EX_TOOLWINDOW) == WS_EX_TOOLWINDOW)
                    return true;
                if ((User32.GetWindowStyle(hWnd) & WS_CHILD) ==
                    WS_CHILD)
                    return true;
            }

            int length = NativeMethods.GetWindowTextLength(hWnd);
            var sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);

            _handleList.Add(hWnd);
            windowList.Items.Add(sb.ToString());
            return true;
        }
    }

    public class ColourDisplay : UserControl {
        private readonly SolidBrush _border =
            new SolidBrush(SystemColors.ControlDark);

        private SolidBrush _brush;
        private Color _colour = Color.Black;

        public Color Color {
            get { return _colour; }
            set {
                _colour = value;
                Invalidate();
                Update();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) {}

        protected override void OnPaint(PaintEventArgs e) {
            Rectangle rect = e.ClipRectangle;
            rect.X = 2;
            rect.Y = 2;
            rect.Width -= 4;
            rect.Height -= 4;

            if (Enabled)
                _brush = new SolidBrush(_colour);
            else {
                var grayScale =
                    (byte)
                    (((_colour.R*.3) + (_colour.G*.59) + (_colour.B*.11)));
                _brush =
                    new SolidBrush(
                        ControlPaint.Light(Color.FromArgb(grayScale, grayScale,
                                                          grayScale)));
            }
            e.Graphics.FillRectangle(_border, e.ClipRectangle);
            e.Graphics.FillRectangle(_brush, rect);
        }
    }

    public class Placeholder : Control {
        public Placeholder() {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }
    }
}