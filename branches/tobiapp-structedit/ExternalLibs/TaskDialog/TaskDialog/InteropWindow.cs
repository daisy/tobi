using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;

namespace Sid.Windows.Controls
{
    public enum InteropWindowZOrder
    {
        Top, TopMost, NoTopMost, Bottom
    }
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left_, int top_, int right_, int bottom_)
        {
            Left = left_;
            Top = top_;
            Right = right_;
            Bottom = bottom_;
        }

        public int Height { get { return Bottom - Top; } }
        public int Width { get { return Right - Left; } }
        public Size Size { get { return new Size(Width, Height); } }

        public Point Location { get { return new Point(Left, Top); } }

        public override int GetHashCode()
        {
            return Left ^ ((Top << 13) | (Top >> 0x13))
              ^ ((Width << 0x1a) | (Width >> 6))
              ^ ((Height << 7) | (Height >> 0x19));
        }
    }


    public class InteropWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();

        [DllImport("User32", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        private const uint MF_BYCOMMAND = 0x00;
        private const uint MF_ENABLED = 0x00;
        private const uint MF_GRAYED = 0x01;
        private const uint SC_CLOSE = 0xF060;
        private const int WM_SHOWWINDOW = 0x18;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        private InteropWindowZOrder _zorder = InteropWindowZOrder.Top;
        private HwndSource _hwndSource;
        private readonly WindowInteropHelper _interopHelper;
        private bool _centered;

        public InteropWindow()
        {
            _interopHelper = new WindowInteropHelper(this);
            AutoCenterOnResize = true;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _hwndSource = HwndSource.FromHwnd(_interopHelper.Handle);
            if (_hwndSource == null) return;

            // set the parent of the window
            IntPtr parent = ParentWindowHandle == IntPtr.Zero ? GetActiveWindow() : ParentWindowHandle;
            SetWindowLong(new HandleRef(this, _interopHelper.Handle), -8, parent.ToInt32());

            // add windows message hook
            _hwndSource.AddHook(HwndSourceHook);

            // listen to the resize event
            _hwndSource.AutoResized +=
                delegate(object sender, AutoResizedEventArgs e)
                {
                    // set minimum size
                    Size sz = e.Size;
                    sz.Width = Math.Max(sz.Width, 300);
                    sz.Height = Math.Max(sz.Height, 150);

                    Rect parentRect;
                    int x;
                    int y;

                    if (!AutoCenterOnResize && _centered)
                        return;

                    _centered = true;

                    // Center in the Parent
                    if (ParentWindow != null)
                    {
                        parentRect = new Rect(ParentWindow.Left, ParentWindow.Top, ParentWindow.ActualWidth, ParentWindow.ActualHeight);
                        if (parentRect.Width < sz.Width)
                            x = (int) (parentRect.Left - (int) ((sz.Width - parentRect.Width)/2));
                        else
                            x = (int) (parentRect.Left + (int) ((parentRect.Width - sz.Width)/2));

                        if (parentRect.Height < sz.Height)
                            y = (int)(parentRect.Top - (int)((sz.Height - parentRect.Height) / 2));
                        else
                            y = (int)(parentRect.Top + (int)((parentRect.Height - sz.Height) / 2));
                    }
                    else
                    {
                        // center in the active monitor
                        parentRect = InteropHelper.GetMonitorWorkArea(_interopHelper.Handle);
                        x = (int)(parentRect.Left + (int)((parentRect.Width - sz.Width) / 2));
                        y = (int)(parentRect.Top + (int)((parentRect.Height - sz.Height) / 2));
                    }


                    SetWindowTopMost(_zorder, new Rect(x, y, sz.Width, sz.Height));
                };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_hwndSource != null)
                _hwndSource.Dispose();
        }

        /// <summary>
        ///     Listen to all window messages
        /// </summary>
        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                // set the initial state of the close button
                if (!IsCloseButtonEnabled)
                    DisableCloseButton();

                // call the OnShowWindow override
                OnShowWindow();
            }
            return IntPtr.Zero;
        }

        /// <summary>
        ///     Disable the close button
        /// </summary>
        private void DisableCloseButton()
        {
            if (_interopHelper.Handle == IntPtr.Zero)
                return;

            IntPtr hMenu = GetSystemMenu(_interopHelper.Handle, false);
            if(hMenu != IntPtr.Zero)
                EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
        }

        /// <summary>
        ///     Enable the close button
        /// </summary>
        private void EnableCloseButton()
        {
            if (_interopHelper.Handle == IntPtr.Zero)
                return;

            IntPtr hMenu = GetSystemMenu(_interopHelper.Handle, false);
            if(hMenu != IntPtr.Zero)
                EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
        }
        
        /// <summary>
        ///     Set the TopMost order of the TaskDialogWindow
        /// </summary>
        private bool SetWindowTopMost(InteropWindowZOrder zorder, Rect rect)
        {
            _zorder = zorder;
            if (_interopHelper.Handle == IntPtr.Zero)
                return false;

            IntPtr p;
            switch (zorder)
            {
                default:
                    p = HWND_TOP;
                    break;
                case InteropWindowZOrder.TopMost:
                    p = HWND_TOPMOST;
                    break;
                case InteropWindowZOrder.NoTopMost:
                    p = HWND_NOTOPMOST;
                    break;
                case InteropWindowZOrder.Bottom:
                    p = HWND_BOTTOM;
                    break;
            }

            return SetWindowPos(_interopHelper.Handle, p, (int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height, 0);
        }
        /// <summary>
        ///     Set the TopMost order of the TaskDialogWindow
        /// </summary>
        /// <param name="zorder"></param>
        public bool SetWindowTopMost(InteropWindowZOrder zorder)
        {
            return SetWindowTopMost(zorder, new Rect(this.Left, this.Top, this.Width, this.Height));
        }

        /// <summary>
        ///     called when the window is about to be shown
        /// </summary>
        protected virtual void OnShowWindow()
        {
        }

        /// <summary>
        ///     Get or Set the enabled state of the close button
        /// </summary>
        public bool IsCloseButtonEnabled
        {
            get { return isCloseButtonEnabled; }
            set
            {
                if (isCloseButtonEnabled != value)
                {
                    isCloseButtonEnabled = value;
                    if (isCloseButtonEnabled)
                        EnableCloseButton();
                    else
                        DisableCloseButton();
                }
            }
        }
        private bool isCloseButtonEnabled = true;

        /// <summary>
        ///     Get or Set the AutoCenterOnResize property (defaults to true)
        /// </summary>
        public bool AutoCenterOnResize { get; set; }
        /// <summary>
        ///     Get or Set the IsModal property
        /// </summary>
        public bool IsModal { get; set; }

        public IntPtr ParentWindowHandle { get; internal set; }
        public Window ParentWindow { get; internal set; }
    }
}
