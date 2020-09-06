namespace RZScreenSaver.SlidePages{
    public interface ISlidePage{
        DisplayMode DisplayMode { get; set; }
        bool ShowTitle { get; set; }

        void OnPictureSetChanged(object sender, System.EventArgs arg);
        void OnShowPicture(object sender, PictureChangedEventArgs arg);
    }
}