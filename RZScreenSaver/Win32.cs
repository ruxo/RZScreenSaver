using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace RZScreenSaver{
    static class Win32{
        public const int WM_DESTROY = 2;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;

        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const uint GW_HWNDNEXT = 2;

        public const uint SPI_GETSCREENSAVERRUNNING = 0x0072;

        static public readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll")]
        static public extern IntPtr GetWindow(IntPtr hwnd, uint wCmd);
        [DllImport("user32.dll")]
        static public extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")]
        static public extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        static public extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static public extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll", SetLastError = true)]
        static public extern int SetWindowLong(IntPtr hwnd, int index, int value);
        [DllImport("user32.dll", SetLastError = true)]
        static public extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", PreserveSig = false)]
        static public extern void GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")]
        static public extern bool SystemParametersInfo(uint uiAction, uint uiParam, out bool boolValue, uint fwInit);
        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS{
            public MARGINS(Thickness t){
                Left = (int) t.Left;
                Right = (int) t.Right;
                Top = (int) t.Top;
                Bottom = (int) t.Bottom;
            }
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
            public int Width{
                get { return Right - Left; }
            }
            public int Height{
                get { return Bottom - Top; }
            }
            public override string ToString() {
                return string.Format("MARGINS:{{left={0},top={1},right={2},bottom={3}}}", Left.ToString(), Top.ToString(), Right.ToString(), Bottom.ToString());
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT{
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width{
                get { return Right - Left; }
            }
            public int Height{
                get { return Bottom - Top; }
            }
            public override string ToString() {
                return string.Format("RECT:{{left={0},top={1},right={2},bottom={3}}}", Left.ToString(), Top.ToString(), Right.ToString(), Bottom.ToString());
            }
        }
        #endregion
    }
}