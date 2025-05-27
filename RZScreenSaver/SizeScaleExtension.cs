using System;
using System.Windows;

namespace RZScreenSaver{
    static class SizeScaleExtension{
        public static bool IsPortrait(this Size size){
            return size.Height > size.Width;
        }
        public static Size ScaleToArea(this Size size, double newArea){
            var width = Math.Sqrt(newArea*size.Width/size.Height);
            var height = newArea/width;
            return new Size(width,height);
        }
    }
}