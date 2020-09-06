using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RZScreenSaver{
    public class GlassHelper{
        readonly Window window;
        readonly Win32.MARGINS margin;

        public GlassHelper(Window window) : this(window, new Thickness(-1)) {}
        public GlassHelper(Window window, Thickness margin){
            this.window = window;
            this.margin = new Win32.MARGINS(margin);
        }
        /// <summary>
        /// Repeat call may cause unexpected behavior!
        /// </summary>
        /// <returns></returns>
        public bool ExtendGlassFrame(){
            var isVistable = Environment.OSVersion.Version.Major >= 6;
            if (!isVistable || !DwmIsCompositionEnabled())
                return false;
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("The Window must be shown before extending glass.");
            window.Background = Brushes.Transparent;

            var hwndSource = HwndSource.FromHwnd(hwnd);
            Debug.Assert(hwndSource != null);
            hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

            var marginParam = margin;
            DwmExtendFrameIntoClientArea(hwnd, ref marginParam);

            hwndSource.AddHook(wndProc);

            return true;
        }
        IntPtr wndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled){
            if (msg == DwmCompositionChanged){
                ExtendGlassFrame();
                handled = true;
            }
            return IntPtr.Zero;
        }
        const int DwmCompositionChanged = 0x031E;
        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Win32.MARGINS pMarInset);
        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern bool DwmIsCompositionEnabled();
    }
}