using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;

namespace RZScreenSaver;

class TemporaryPictureSource : IPictureSource{
    readonly IPictureSource mainSource;
    IPictureSource? tempSource;
    readonly SlideMode mode;
    readonly int delayTime;

    public TemporaryPictureSource(IReadOnlyList<FolderCollection> picturePaths, int? pictureSetSelected, SlideMode slideMode, int delayTime)
        : this(new PictureSource(picturePaths, pictureSetSelected, slideMode, delayTime), slideMode, delayTime){
    }

    public TemporaryPictureSource(IPictureSource mainSource, SlideMode slideMode, int delayTime){
        this.mainSource = mainSource;
        PictureChanged = mainSource.PictureChanged;
        mode = slideMode;
        this.delayTime = delayTime;
    }
    public bool TempMode => tempSource is not null;

    public void SwitchToCurrentFolder(){
        var currentFolder = Path.GetDirectoryName(CurrentPictureFile)!;
        if (TempMode)
            tempSource!.Stop();
        else if (!mainSource.IsPaused)
            mainSource.Pause();
        tempSource = new PictureSource([new() { {currentFolder, InclusionMode.Recursive} }], 0, mode, delayTime);
        PictureChanged = tempSource.PictureChanged.Merge(mainSource.PictureChanged);
        tempSource.Start();
    }
    public void RevertToMainSet(){
        if (tempSource != null){
            tempSource.Stop();
            tempSource = null;
        }
        if (mainSource.IsPaused)
            mainSource.Resume();
    }

    IPictureSource Source => tempSource ?? mainSource;

    #region Implementation of IPictureSource

    public ImageSource? CurrentPicture => Source.CurrentPicture;
    public string CurrentPictureFile => Source.CurrentPictureFile;
    public int PictureIndex => mainSource.PictureIndex;

    public bool IsPaused => Source.IsPaused;

    public void Start(){
        Source.Start();
    }
    public void Pause(){
        Source.Pause();
    }
    public void Resume(){
        Source.Resume();
    }
    public void Stop(){
        Source.Stop();
    }
    public void SwitchToSet(int setIndex){
        Source.SwitchToSet(setIndex);
    }
    public void RestorePicturePosition(int numberToDiscard){
        Source.RestorePicturePosition(numberToDiscard);
    }
    public bool DeleteCurrentPicture(){
        return Source.DeleteCurrentPicture();
    }
    public bool MoveCurrentPictureTo(string targetFolder){
        return Source.MoveCurrentPictureTo(targetFolder);
    }
    public IObservable<Unit> PictureSetChanged => mainSource.PictureSetChanged;
    public IObservable<PictureChangedEventArgs> PictureChanged { get; private set; }

    #endregion
}