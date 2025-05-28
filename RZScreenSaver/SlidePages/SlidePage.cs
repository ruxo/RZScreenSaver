using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RZScreenSaver.SlidePages;

/// <summary>
/// Interaction logic for SlidePage.xaml
/// </summary>
public class SlidePage : Page, ISlidePage
{
    #region Implementation of ISlidePage

    public DisplayMode DisplayMode
    {
        get;
        set
        {
            if (field != value){
                field = value;
                OnDisplayModeChanged();
            }
        }
    } = DisplayMode.Fit;

    public virtual bool ShowTitle
    {
        get => false;
        set { }
    }

    #endregion

    protected virtual void OnDisplayModeChanged() { }

    public virtual void OnPictureSetChanged() { }

    public virtual void OnShowPicture(PictureChangedEventArgs arg) { }

    protected static string FormatImageDescription(ImageSource image, string path, DateTime fileDate, int? orientation) {
        if (orientation == null || orientation.Value == 0)
            return string.Format("[{0}x{1}] {2} {3}", image.Width.ToString("F0"),
                                 image.Height.ToString("F0"),
                                 fileDate.ToString("G"), path);
        else
            return string.Format("[{0}x{1} {2}\u00B0] {3} {4}",
                                 image.Width.ToString("F0"), image.Height.ToString("F0"),
                                 orientation,
                                 fileDate.ToString("G"), path);
    }

    protected static int? GetImageOrientation(ImageSource image) {
        const string exifOrientation = "System.Photo.Orientation";
        var bitmapMeta = image.Metadata as BitmapMetadata;
        if (bitmapMeta == null || !bitmapMeta.ContainsQuery(exifOrientation))
            return null;
        var orientation = Convert.ToInt32(bitmapMeta.GetQuery(exifOrientation));
        // refer to http://www.impulseadventure.com/photo/exif-orientation.html
        // all mirror cases are treated as normal cases.
        switch (orientation){
            case 1:
            case 2: // mirror
                return 0;
            case 7: // mirror
            case 8:
                return 90 /* degree clockwise */;
            case 3:
            case 4: // mirror
                return 180 /* degree clockwise */;
            case 5: // mirror
            case 6:
                return 270 /* degree clockwise */;
            default:
                Trace.WriteLine("Unrecognized EXIF orientation: " + orientation.ToString());
                return null;
        }
    }

    protected static void GetImageSizeByOrientation(ImageSource image, int? orientation, out Size imageSize) {
        imageSize = IsLandscape(orientation) ? new Size(image.Width, image.Height) : new Size(image.Height, image.Width);
    }

    protected static bool IsLandscape(int? orientation) {
        return orientation == null || orientation.Value == 0 || orientation.Value == 180;
    }
}