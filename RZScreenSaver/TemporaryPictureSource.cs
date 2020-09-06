using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace RZScreenSaver{
    class TemporaryPictureSource : IPictureSource{
        public TemporaryPictureSource(FolderCollectionSet folderSet, SlideMode slideMode, int delayTime)
            : this(new PictureSource(folderSet, slideMode, delayTime), slideMode, delayTime){
        }
        public TemporaryPictureSource(IPictureSource mainSource, SlideMode slideMode, int delayTime){
            Debug.Assert(mainSource != null);
            this.mainSource = mainSource;
            mode = slideMode;
            this.delayTime = delayTime;
        }
        public bool TempMode{
            get { return tempSource != null; }
        }
        public void SwitchToCurrentFolder(){
            var currentFolder = Path.GetDirectoryName(CurrentPictureFile);
            if (TempMode)
                tempSource.Stop();
            else if (!mainSource.IsPaused)
                    mainSource.Pause();
            tempSource =
                new PictureSource(
                    new FolderCollectionSet{
                                               new FolderCollection{
                                                                       new FolderInclusion(currentFolder, InclusionMode.Recursive)
                                                                   }
                                           },
                                           mode, delayTime);
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
        #region Implementation of IPictureSource

        public ImageSource CurrentPicture{
            get { return Source.CurrentPicture; }
        }
        public string CurrentPictureFile{
            get { return Source.CurrentPictureFile; }
        }
        public int PictureIndex{
            get { return mainSource.PictureIndex; }
        }
        public bool IsPaused{
            get { return Source.IsPaused; }
        }
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
        IPictureSource Source{
            get { return tempSource ?? mainSource; }
        }

        readonly IPictureSource mainSource;
        IPictureSource tempSource;
        readonly SlideMode mode;
        readonly int delayTime;
    }
}