using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace RZScreenSaver.SlidePages;

/// <summary>
/// Interaction logic for SimpleSlide.xaml
/// </summary>
public partial class SimpleSlide{
    public SimpleSlide() {
        InitializeComponent();
        imagePathText.Visibility = AppDeps.Settings.Value.ShowTitle ? Visibility.Visible : Visibility.Collapsed;
    }
    public override bool ShowTitle{
        get => imagePathText.Visibility == Visibility.Visible;
        set => imagePathText.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }
    public override void OnPictureSetChanged(){
        slideShowImage.Source = null;
    }
    public override void OnShowPicture(PictureChangedEventArgs arg){
        var image = arg.Picture;
        var orientation = GetImageOrientation(image);

        imagePathText.Content = FormatImageDescription(image, arg.Path, arg.FileDate, orientation);

        switch (DisplayMode){
            case DisplayMode.OriginalOrFit:
                slideShowImage.Stretch = canImageFitScreen(image, orientation)? Stretch.None : Stretch.Uniform;
                break;
            case DisplayMode.OriginalOrFillCrop:
                slideShowImage.Stretch = canImageFitScreen(image, orientation) ? Stretch.None : Stretch.UniformToFill;
                break;
            // default just ignore.
        }
        slideShowImage.Source = image;
        imageOrientation.Angle = -(orientation ?? 0);
        if (!IsLandscape(orientation)){
            var parentWindow = (FrameworkElement) Parent;
            slideShowImage.Width = parentWindow.Height;
            slideShowImage.Height = parentWindow.Width;
        } else{
            slideShowImage.Width = slideShowImage.Height = double.NaN;
        }
    }
    protected override void OnDisplayModeChanged() {
        base.OnDisplayModeChanged();
        switch (DisplayMode){
            case DisplayMode.Original:
                slideShowImage.Stretch = Stretch.None;
                break;
            case DisplayMode.Stretch:
                slideShowImage.Stretch = Stretch.Fill;
                break;
            case DisplayMode.Fit:
                slideShowImage.Stretch = Stretch.Uniform;
                break;
            case DisplayMode.FillCrop:
                slideShowImage.Stretch = Stretch.UniformToFill;
                break;
            case DisplayMode.OriginalOrFit:
            case DisplayMode.OriginalOrFillCrop:
                // effect takes on changing image.
                break;
            default:
                Trace.WriteLine("Unhandled mode: " + DisplayMode);
                break;
        }
    }
    bool canImageFitScreen(ImageSource image, int? orientation){
        Size imageSize;
        GetImageSizeByOrientation(image, orientation, out imageSize);

        var parentWindow = Parent as FrameworkElement;
        return parentWindow != null && imageSize.Width <= parentWindow.Width && imageSize.Height <= parentWindow.Height;
    }
}