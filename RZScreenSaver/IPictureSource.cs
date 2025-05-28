using System;
using System.Reactive;
using System.Windows.Media;

namespace RZScreenSaver;

public interface IPictureSource : IDisposable {
    ImageSource? CurrentPicture { get; }
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

    IObservable<Unit> PictureSetChanged { get; }
    IObservable<PictureChangedEventArgs> PictureChanged { get; }
}