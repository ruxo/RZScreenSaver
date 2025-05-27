using System;
using System.Collections.Generic;
using System.IO;
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
        mainSource.PictureChanged += pictureChangedDelegates;
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
        tempSource.PictureChanged += pictureChangedDelegates;
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
    public event EventHandler PictureSetChanged{
        add{
            mainSource.PictureSetChanged += value;
            pictureSetChangedDelegates += value;
        }
        remove{
            mainSource.PictureSetChanged -= value;
            pictureSetChangedDelegates -= value;
        }
    }
    public event EventHandler<PictureChangedEventArgs> PictureChanged{
        add{
            mainSource.PictureChanged += value;
            pictureChangedDelegates += value;
        }
        remove{
            mainSource.PictureChanged -= value;
            pictureChangedDelegates -= value;
        }
    }
    EventHandler pictureSetChangedDelegates;
    EventHandler<PictureChangedEventArgs> pictureChangedDelegates;

    #endregion
}