using System.Windows;
using System.Windows.Media;
using RZScreenSaver.SlidePages;

namespace RZScreenSaver{
    public class PageHost : Window{
        public PageHost(){
            Background = Brushes.Black;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
        }
        public PageHost(IPictureSource source, Rect displayArea) : this(){
            Left = (displayArea.Left);
            Top = (displayArea.Top);
            Width = (displayArea.Width);
            Height = (displayArea.Height);
            pictureSource = source;
        }
        public ISlidePage SlidePage{
            get { return slide; }
            set{
                if (slide != null){
                    pictureSource.PictureSetChanged -= slide.OnPictureSetChanged;
                    pictureSource.PictureChanged -= slide.OnShowPicture;
                }
                Content = slide = value;
                pictureSource.PictureChanged += slide.OnShowPicture;
                pictureSource.PictureSetChanged += slide.OnPictureSetChanged;
            }
        }
        public void SendToBottom(){
            this.SetNoActivate();
            this.SetBottomMost();
        }
        protected readonly IPictureSource pictureSource;
        protected ISlidePage slide;
    }
}