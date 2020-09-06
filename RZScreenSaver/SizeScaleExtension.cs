using System;
using System.Windows;
using System.Windows.Media;

namespace RZScreenSaver{
    static class SizeScaleExtension{
        static public bool IsPortrait(this Size size){
            return size.Height > size.Width;
        }
        static public Size ScaleToArea(this Size size, double newArea){
            var width = Math.Sqrt(newArea*size.Width/size.Height);
            var height = newArea/width;
            return new Size(width,height);
        }
    }
}