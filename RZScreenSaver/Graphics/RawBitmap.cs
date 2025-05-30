using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RZScreenSaver.Graphics.ColorSpaces;

namespace RZScreenSaver.Graphics;

public class RawBitmap{
    public RawBitmap(BitmapSource bitmap){
        if (bitmap.Format != PixelFormats.Pbgra32)
            throw new NotSupportedException("Current supported pixel format is Pbgra32");

        var bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        stride = bitmap.PixelWidth * bytesPerPixel;

        var length = bitmap.PixelHeight*stride;
        data = new byte[length];
        bitmap.CopyPixels(data, stride, 0);

        width = bitmap.PixelWidth;
        height = bitmap.PixelHeight;
        dpiX = bitmap.DpiX;
        dpiY = bitmap.DpiY;
        format = bitmap.Format;
        palette = bitmap.Palette;
    }
    public byte[] Data => data;

    public int Stride => stride;

    public int Height => height;

    public int Width => width;

    public RawBitmap CloneWithData(Rgba32 rgbData){
        Debug.Assert(format == PixelFormats.Pbgra32);
        Debug.Assert(rgbData.Width == width && rgbData.Height == height);
        var result = (RawBitmap) MemberwiseClone();
        result.data = rgbData.Data;
        return result;
    }
    public BitmapSource ToBitmap(){
        var result = BitmapSource.Create(width, height, dpiX, dpiY, format, palette, data, stride);
        Debug.Assert(!result.IsFrozen);
        Debug.Assert(result.CanFreeze);
        result.Freeze();
        return result;
    }

    byte[] data;
    readonly int stride;
    readonly int width, height;
    readonly double dpiX, dpiY;
    readonly PixelFormat format;
    readonly BitmapPalette palette;
}