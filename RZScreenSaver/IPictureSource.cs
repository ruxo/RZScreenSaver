using System;
using System.Windows.Media;

namespace RZScreenSaver{
    public interface IPictureSource{
        ImageSource CurrentPicture { get; }
        string CurrentPictureFile { get; }
        int PictureIndex { get; }
        bool IsPaused { get; }

        void Start();
        void Pause();
        void Resume();
        void Stop();
        void SwitchToSet(int setIndex);

        void RestorePicturePosition(int lastPosition);
        bool DeleteCurrentPicture();
        bool MoveCurrentPictureTo(string targetFileAndFolder);

        event EventHandler PictureSetChanged;
        event EventHandler<PictureChangedEventArgs> PictureChanged;
    }
}