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
using RZScreenSaver.Properties;

namespace RZScreenSaver.SlidePages{
    /// <summary>
    /// Interaction logic for PhotoCollagePage.xaml
    /// </summary>
    public partial class PhotoCollagePage{
        #region ctors
        static PhotoCollagePage(){
            // TODO: Decouple this class from Settings!
            ViewAngle = Settings.Default.PhotoCollageAngle;
            SquareCardSize = new Range
                             {Min = Settings.Default.MinSquareCardSize, Max = Settings.Default.MaxSquareCardSize};
        }
        public PhotoCollagePage() {
            InitializeComponent();
            try{
                background = new BitmapImage(new Uri(Settings.Default.BackgroundPicturePath, UriKind.RelativeOrAbsolute));
            }
            catch (FileNotFoundException e){
                Trace.Write("Background image is invalid: ");
                Trace.WriteLine(e);
            }
        }
        #endregion

        static readonly int ViewAngle;
        const int AnglePrecision = 100;
        static readonly Range SquareCardSize;

        public override bool ShowTitle{
            get { return imagePathText.Visibility == Visibility.Visible; }
            set{ imagePathText.Visibility = value? Visibility.Visible : Visibility.Collapsed; }
        }
        protected override Size ArrangeOverride(Size arrangeBounds) {
            var result = base.ArrangeOverride(arrangeBounds);

            // there may be better place for this code... (changing child in this method cause this method to be called again :(
            pageWidth = (int)result.Width;
            pageHeight = (int) result.Height;
            if (viewBitmap == null || viewBitmap.Width != pageWidth || viewBitmap.Height != pageHeight){
                resetViewBitmaps(result);
            }
            return result;
        }
        protected override void OnPictureSetChanged(object sender, EventArgs arg){
            viewBitmap.Clear();
            rebuildSurfaceBackground();
        }
        protected override void OnShowPicture(object sender, PictureChangedEventArgs arg){
            var picture = arg.Picture;
            var angle = (double) arg.Random(0, 2 *ViewAngle *AnglePrecision) / AnglePrecision -ViewAngle;
            var r3 = (double) arg.Random(1, int.MaxValue)/int.MaxValue;
            var orientation = GetImageOrientation(picture);
            var newPictureSize = getNiceSize(picture, r3, orientation);

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

            var rawBitmap = desaturate(viewBitmap);
            currentViewBitmap.WritePixels(new Int32Rect(0, 0, rawBitmap.Width, rawBitmap.Height), rawBitmap.Data,
                                          rawBitmap.Stride, 0);

            var animation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(2))){AccelerationRatio = 0.8};
            animation.Completed += onFadeInCompleted;

            postcard.Visibility = Visibility.Visible;
            postcard.BeginAnimation(OpacityProperty, animation);
        }
        void onFadeInCompleted(object sender, EventArgs e){
            // NOTE: current implementation sometimes makes noticable darker shadown at the end of animation before the postcard is hidden.
            viewBitmap.Render(pageSurface);
            viewBitmap.Render(postcard);
        }
        static RawBitmap desaturate(BitmapSource bitmap){
            var raw = new RawBitmap(bitmap);
            var hsl = Hsla32.FromPbra32(raw.Data, raw.Width).Desaturate(0.87F, 1e-2F);
            return raw.CloneWithData(Rgba32.FromHsl(hsl));
        }
        Size getNiceSize(ImageSource image, double sizeScale, int? orientation){
            Size imageSize;
            GetImageSizeByOrientation(image, orientation, out imageSize);
            
            var expectedArea = minCardArea + (maxCardArea - minCardArea)*sizeScale;
            return imageSize.ScaleToArea(expectedArea);
        }
        void rebuildSurfaceBackground(){
            if (background != null){
                var imageRenderer = new Image{Source = background, Stretch = Stretch.UniformToFill};
                imageRenderer.Measure(new Size(viewBitmap.Width,viewBitmap.Height));
                imageRenderer.Arrange(new Rect(new Point(0,0), imageRenderer.DesiredSize));
                viewBitmap.Render(imageRenderer);
            }
            pageSurface.Source = currentViewBitmap = new WriteableBitmap(viewBitmap);
        }
        void resetViewBitmaps(Size result) {
            viewBitmap = new RenderTargetBitmap(pageWidth, pageHeight, 96, 96, PixelFormats.Default);
            rebuildSurfaceBackground();

            var area = result.Width*result.Height;
            minCardArea = area*SquareCardSize.Min*SquareCardSize.Min;
            maxCardArea = area*SquareCardSize.Max*SquareCardSize.Max;
            Debug.Write("Min/Max Size = ");
            Debug.Write(minCardArea); Debug.Write(','); Debug.WriteLine(maxCardArea);
        }
        struct Range{
            public double Min, Max;
        }
        int pageWidth, pageHeight;
        double minCardArea, maxCardArea;
        RenderTargetBitmap viewBitmap;
        WriteableBitmap currentViewBitmap;
        BitmapSource background;
    }
}