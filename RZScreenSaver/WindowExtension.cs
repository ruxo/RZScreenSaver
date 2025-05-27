using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RZScreenSaver{
    static class WindowExtension{
        public static void ActivateNextNotBottomMost(this Window w){
            var hwnd = new WindowInteropHelper(w).Handle;
            Debug.Assert(hwnd != IntPtr.Zero);

            var nextHwnd = Win32.GetWindow(hwnd, Win32.GW_HWNDNEXT);
            while(nextHwnd != hwnd){
                if (Win32.IsWindowVisible(nextHwnd) && (Win32.GetWindowLong(nextHwnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_NOACTIVATE) == 0){
                    var canSet = Win32.SetForegroundWindow(nextHwnd);
                    Debug.Assert(canSet);
                    break;
                }
                nextHwnd = Win32.GetWindow(nextHwnd, Win32.GW_HWNDNEXT);
            }
        }
        public static void SetBottomMost(this Window w){
            var hwnd = new WindowInteropHelper(w).Handle;
            Debug.Assert(hwnd != IntPtr.Zero);
            Win32.SetWindowPos(hwnd, Win32.HWND_BOTTOM, 0, 0, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | Win32.SWP_NOACTIVATE);
        }
        public static void SetNoActivate(this Window w){
            var hwnd = new WindowInteropHelper(w).Handle;
            Debug.Assert(hwnd != IntPtr.Zero);
            var newValue = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE)|Win32.WS_EX_NOACTIVATE;
            var lastError = Marshal.GetLastWin32Error();
            Debug.Assert(lastError == 0, "GetWindowLong error " + lastError);
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, newValue);
            lastError = Marshal.GetLastWin32Error();
            Debug.Assert(lastError == 0, "SetWindowLong error " + lastError);
#if DEBUG
            var confirm = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            Debug.Assert(newValue == confirm);
#endif
        }
    }
}