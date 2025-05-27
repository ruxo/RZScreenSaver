namespace RZScreenSaver.SlidePages{
    public interface ISlidePage{
        DisplayMode DisplayMode { get; set; }
        bool ShowTitle { get; set; }

        void OnPictureSetChanged();
        void OnShowPicture(PictureChangedEventArgs arg);
    }
}