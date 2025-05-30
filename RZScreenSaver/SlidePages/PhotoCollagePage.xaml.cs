using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using RZScreenSaver.Graphics;
using RZScreenSaver.Graphics.ColorSpaces;

namespace RZScreenSaver.SlidePages;

/// <summary>
/// Interaction logic for PhotoCollagePage.xaml
/// </summary>
public partial class PhotoCollagePage{
    readonly record struct Range(double Min, double Max);

    readonly BitmapSource? background;

    int pageWidth, pageHeight;
    double minCardArea, maxCardArea;
    RenderTargetBitmap liveBackground;

    #region ctors

    static PhotoCollagePage(){
        // TODO: Decouple this class from Settings!
        ViewAngle = AppDeps.Settings.Value.PhotoCollageAngle;
        SquareCardSize = new Range(AppDeps.Settings.Value.MinSquareCardSize, AppDeps.Settings.Value.MaxSquareCardSize);

        var dpiScaleX = SystemParameters.PrimaryScreenWidth / SystemParameters.WorkArea.Width;
        var dpiScaleY = SystemParameters.PrimaryScreenHeight / SystemParameters.WorkArea.Height;

        DPI = new(96.0 * dpiScaleX, 96.0 * dpiScaleY);
    }
    public PhotoCollagePage() {
        InitializeComponent();

        liveBackground = new RenderTargetBitmap(1, 1, DPI.X, DPI.Y, PixelFormats.Default);
        pageSurface.Source = liveBackground;

        if (AppDeps.Settings.Value.BackgroundPicturePath is { } picturePath)
            background = LoadBackground(picturePath);
    }

    static BitmapImage? LoadBackground(string picturePath) {
        try{
            return new BitmapImage(new Uri(picturePath, UriKind.RelativeOrAbsolute));
        }
        catch (FileNotFoundException e){
            Trace.Write("Background image is invalid: ");
            Trace.WriteLine(e);
            return null;
        }
    }

    #endregion

    static readonly int ViewAngle;
    const int AnglePrecision = 100;
    static readonly Range SquareCardSize;
    static readonly Point DPI;

    public override bool ShowTitle{
        get => imagePathText.Visibility == Visibility.Visible;
        set => imagePathText.Visibility = value? Visibility.Visible : Visibility.Collapsed;
    }
    protected override Size ArrangeOverride(Size arrangeBounds) {
        var result = base.ArrangeOverride(arrangeBounds);

        // there may be better place for this code... (changing child in this method cause this method to be called again :(
        pageWidth = (int)result.Width;
        pageHeight = (int) result.Height;
        if (Math.Abs(liveBackground.Width - result.Width) > 1e-6 || Math.Abs(liveBackground.Height - result.Height) > 1e-6)
            ResetViewBitmaps(pageWidth, pageHeight);

        return result;
    }
    public override void OnPictureSetChanged(){
        liveBackground.Clear();
        RebuildSurfaceBackground(liveBackground);
    }
    public override void OnShowPicture(PictureChangedEventArgs arg){
        var picture = arg.Picture;
        var angle = (double) arg.Random(0, 2 *ViewAngle *AnglePrecision) / AnglePrecision -ViewAngle;
        var r3 = (double) arg.Random(1, int.MaxValue)/int.MaxValue;
        var orientation = GetImageOrientation(picture);
        var newPictureSize = GetNiceSize(picture, r3, orientation);

        // make point (newX,newY) becoming the center of the picture.
        var newX = arg.Random(2, pageWidth) -newPictureSize.Width /2;
        var newY = arg.Random(3, pageHeight) -newPictureSize.Height /2;

        imagePathText.Content = FormatImageDescription(picture, arg.Path, arg.FileDate, orientation);

        postcard.Opacity = 0;
        Canvas.SetLeft(postcard, newX);
        Canvas.SetTop(postcard, newY);
        postcard.Size = newPictureSize;
        postcard.Angle = angle -(orientation ?? 0);
        postcard.Source = picture;

        var animation = new DoubleAnimation(fromValue: 0, toValue: 1, new Duration(TimeSpan.FromSeconds(2))) {
            AccelerationRatio = 0.8
        };
        animation.Completed += (_,_) => {
            liveBackground.Render(postcard);
            // postcard.Visibility = Visibility.Collapsed;
        };

        postcard.Visibility = Visibility.Visible;
        postcard.BeginAnimation(OpacityProperty, animation);
    }

    /// Used to work with <see cref="WriteableBitmap"/>
    static RawBitmap Desaturate(RawBitmap raw){
        var hsl = Hsla32.FromPbra32(raw.Data, raw.Width).Desaturate(0.87F, 1e-2F);
        return raw.CloneWithData(Rgba32.FromHsl(hsl));
    }

    Size GetNiceSize(ImageSource image, double sizeScale, int? orientation){
        var imageSize = GetImageSizeByOrientation(image, orientation);

        var expectedArea = minCardArea + (maxCardArea - minCardArea)*sizeScale;
        return imageSize.ScaleToArea(expectedArea);
    }

    void RebuildSurfaceBackground(RenderTargetBitmap bitmap) {
        if (background is not null)
            bitmap.Render(RebuildSurfaceBackground(background, new(bitmap.Width, bitmap.Height)));
    }

    static Image RebuildSurfaceBackground(BitmapSource background, Size size){
        var imageRenderer = new Image{Source = background, Stretch = Stretch.UniformToFill};
        imageRenderer.Measure(size);
        imageRenderer.Arrange(new(new(0,0), imageRenderer.DesiredSize));
        return imageRenderer;
    }

    void ResetViewBitmaps(int width, int height) {
        pageSurface.Source = liveBackground = new RenderTargetBitmap(width, height, DPI.X, DPI.Y, PixelFormats.Default);
        RebuildSurfaceBackground(liveBackground);

        var area = width*height;
        minCardArea = area*SquareCardSize.Min*SquareCardSize.Min;
        maxCardArea = area*SquareCardSize.Max*SquareCardSize.Max;
        Debug.Write("Min/Max Size = ");
        Debug.Write(minCardArea); Debug.Write(','); Debug.WriteLine(maxCardArea);
    }
}