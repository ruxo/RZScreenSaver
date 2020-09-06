using System;
using System.Windows;
using System.Windows.Interop;

namespace RZScreenSaver{
    class Winform32Helper : System.Windows.Forms.IWin32Window{
        public Winform32Helper(Window window){
            Handle = new WindowInteropHelper(window).Handle;
        }
        public IntPtr Handle{ get; private set; }
    }
}