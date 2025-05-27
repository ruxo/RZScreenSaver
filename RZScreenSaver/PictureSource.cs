using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RZScreenSaver;

public class PictureChangedEventArgs : EventArgs{
    public PictureChangedEventArgs(string path, DateTime fileDate, ImageSource source){
        Path = path;
        Picture = source;
        FileDate = fileDate;

        for(var count=0; count < ProvidedRandomValue; ++count)
            randomValues[count] = MainApp.Rand();
    }
    public int Random(int slot, int maxValue){
        return (int) (maxValue*(long)randomValues[slot]/int.MaxValue);
    }
    const int ProvidedRandomValue = 4;
    public DateTime FileDate { get; private set; }
    public string Path { get; private set; }
    public ImageSource Picture { get; private set; }
    readonly int[] randomValues = new int[ProvidedRandomValue];
}

public class PictureSource : IPictureSource{
    static readonly string[] SupportedImage = [".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tiff", ".ico"];

    readonly DispatcherTimer timer;
    readonly SlideMode slideMode;
    readonly IReadOnlyList<FolderCollection> picturePaths;

    readonly Subject<Unit> pictureSetChanged = new();
    readonly Subject<PictureChangedEventArgs> pictureChanged = new();

    List<ImagePath> pictureList = new();
    Queue<int> slideOrder = new();
    int currentPictureIndex;
    ImageSource? currentPicture;
    int? pictureSetSelected;

    #region ctors

    public PictureSource(IReadOnlyList<FolderCollection> paths, int? selectedPictureSet, SlideMode slideMode, int slideDelay){
        picturePaths = paths;
        pictureSetSelected = selectedPictureSet;
        this.slideMode = slideMode;

        PictureSetChanged = pictureSetChanged.ObserveOn(Dispatcher.CurrentDispatcher);
        PictureChanged = pictureChanged.ObserveOn(Dispatcher.CurrentDispatcher);

        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(slideDelay) };
        timer.Tick += ChangePictureEvent;

        if (pictureSetSelected is not null)
            RegeneratePictureList(picturePaths[pictureSetSelected!.Value]);

        ApplySlideMode();
    }

    #endregion

    #region Engine Control

    public int PictureIndex => pictureList.Count - slideOrder.Count;
    public bool IsPaused => !IsStarted && pauseCall > 0;
    public bool IsStarted => timer.IsEnabled;

    public void Start(){
        if (!IsStarted && pictureList.Count > 0){
            timer.Start();
            NotifyNextImage();
        }
        pauseCall = 0;
    }
    public void Stop(){
        pauseCall = 1;  // assume pause state
        timer.Stop();
    }
    int pauseCall;
    public void Pause(){
        if (pauseCall++ == 0)
            Stop();
    }
    public void Resume(){
        if (--pauseCall == 0)
            Start();
        Debug.Assert(pauseCall >= 0);
    }
    #endregion

    public string CurrentPictureFile
        => currentPictureIndex == -1 ? String.Empty : pictureList[currentPictureIndex].Path;

    public ImageSource? CurrentPicture
        => currentPicture;

    public void RestorePicturePosition(int lastPosition){
        if (slideMode != SlideMode.Random)
            // random order doesn't make sense to be reponsitioned.
            if (slideOrder.Count < lastPosition)
                ApplySlideMode();
            else
                for(int discardCount=0; discardCount < lastPosition; ++discardCount)
                    slideOrder.Dequeue();
    }
    /// <summary>
    /// Delete current picture from physical disk storage!
    /// </summary>
    /// <returns>true - if picture can be deleted.</returns>
    public bool DeleteCurrentPicture(){
        if (currentPictureIndex == -1){
            // in case there is no picture in the collection and user sends this command.
            return true;
        }
        var filePath = pictureList[currentPictureIndex].Path;
        bool success;
        try{
            File.Delete(filePath);
            success = true;
            Debug.WriteLine("Deleted " + filePath);
        }
        catch (IOException e){
            Trace.WriteLine("ERROR: File " + filePath + " is in used.\n" + e.Message);
            success = false;
        }
        pictureList.RemoveAt(currentPictureIndex);
        return success;
    }
    public bool MoveCurrentPictureTo(string targetFileAndFolder){
        if (currentPictureIndex == -1){
            // in case there is no picture in the collection and user sends this command.
            return true;
        }
        var targetFile = CurrentPictureFile;
        try{
            var newPath = targetFileAndFolder;
            File.Move(targetFile, newPath);
            var currentImagePath = pictureList[currentPictureIndex];
            currentImagePath.Path = newPath;
            pictureList[currentPictureIndex] = currentImagePath;
            Debug.Write("File ");
            Debug.Write(targetFile);
            Debug.Write(" is moved to ");
            Debug.WriteLine(newPath);
            return true;
        }
        catch(UnauthorizedAccessException e){
            Trace.WriteLine("Unauthorized to access " + targetFileAndFolder);
            Debug.WriteLine(e);
        }
        catch(PathTooLongException e){
            Trace.WriteLine("Path too long: " + targetFileAndFolder);
            Debug.WriteLine(e);
        }
        return false;
    }
    public void SwitchToSet(int setIndex) {
        if (pictureSetSelected is null) return;

        pictureSetSelected = setIndex;
        RegeneratePictureList(picturePaths[setIndex]);
        ApplySlideMode();

        if (IsStarted){
            pictureSetChanged.OnNext(Unit.Default);
            NotifyNextImage();
        }
    }

    public IObservable<Unit> PictureSetChanged { get; }
    public IObservable<PictureChangedEventArgs> PictureChanged { get; }

    void ApplySlideMode(){
        if (pictureList.Count == 0)
            return;

        slideOrder = new Queue<int>(pictureList.Count);
        IEnumerable<int> order;
        switch (slideMode){
            case SlideMode.Sequence:
                order = Enumerable.Range(0, pictureList.Count);
                break;
            case SlideMode.SortedByFilenamePerFolder:
                order = from path in pictureList
                        group path by Path.GetDirectoryName(path.Path) into g
                        from p in g
                        orderby p.Path select p.ID;
                break;
            case SlideMode.SortedByFilenameAllFolders:
                order = from path in pictureList orderby Path.GetFileName(path.Path) select path.ID;
                break;
            case SlideMode.SortedByDatePerFolder:
                order = from path in pictureList
                        group path by Path.GetDirectoryName(path.Path) into g
                        from p in g
                        orderby p.FileDate select p.ID;
                break;
            case SlideMode.SortedByDateAllFolders:
                order = from path in pictureList orderby path.FileDate select path.ID;
                break;
            case SlideMode.Random:
                order = GenerateRandomSequence(pictureList.Count);
                break;
            default:
                Trace.WriteLine("Unhandled slide mode " + slideMode);
                order = Enumerable.Range(0, pictureList.Count);
                break;
        }
        foreach (var i in order){
            slideOrder.Enqueue(i);
        }
    }
    void ChangePictureEvent(object sender, EventArgs e){
        NotifyNextImage();
    }
    static IEnumerable<int> GenerateRandomSequence(int count){
        var sequence = Enumerable.Range(0, count).ToArray();
        ShuffleItemByItem(count, sequence);
        sequence = ShuffleSequenceDeck(count, sequence);
        ShuffleItemByItem(count, sequence);
        return sequence;
    }
    static int[] ShuffleSequenceDeck(int count, int[] sequence) {
        var output = new int[sequence.Length];
        for(int i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Shuffle(pos1, pos2, ref output);
            Swap(ref sequence, ref output);
        }
        return sequence;
    }
    static void ShuffleItemByItem(int count, int[] sequence){
        for(int i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Swap(pos1, pos2);
        }
    }
    static void Swap<T>(ref T a, ref T b){
        (a, b) = (b, a);
    }
    void NotifyNextImage() {
        ImageSource? image;
        string fileName;
        DateTime fileDate;
        do{
            image = FetchNextPicture(out fileName, out fileDate);
        } while (image == null && pictureList.Count > 0);
        if (image != null){
            currentPicture = image;
            pictureChanged.OnNext(new PictureChangedEventArgs(fileName, fileDate, image));
        }else{
            Debug.Assert(pictureList.Count == 0, "Picture list supposes to be all removed because the invalid image file.");
            timer.Stop();
        }
    }
    ImageSource? FetchNextPicture(out string fileName, out DateTime fileDate){
        string pictureFile = String.Empty;
        try{
            currentPictureIndex = slideOrder.Dequeue();
            if (slideOrder.Count == 0)
                ApplySlideMode();
            fileName = pictureFile = pictureList[currentPictureIndex].Path;
            fileDate = pictureList[currentPictureIndex].FileDate;
            using(var s = File.OpenRead(pictureFile)){
                // use stream so file won't be locked.
                var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return decoder.Frames[0];
            }
        }
        catch (ArgumentException){
            // strange exception thrown from the BitmapDecoder sometimes, dunno why yet.
            Trace.Write("Cannot process file (.Net error): ");
            Trace.WriteLine(pictureFile);
        }
        catch (NotSupportedException){
            Trace.WriteLine(pictureFile + " is not a recognized image format!");
            pictureList.RemoveAt(currentPictureIndex);
            currentPictureIndex = -1;
        }
        fileName = String.Empty;
        fileDate = DateTime.MinValue;
        return null;
    }
    void RegeneratePictureList(FolderCollection folderList){
        if (folderList.Count == 0){
            pictureList = new List<ImagePath>();
            return;
        }
        pictureList = new List<ImagePath>(folderList.Count * 100 /* expected files in sub-folders */);
        var excludedFolders =
            (from folder in folderList
             where folder.Inclusion == InclusionMode.Exclude
             select folder.Path).ToArray();
        var id = 0;
        foreach (var folder in folderList){
            IEnumerable<string> fileList;
            switch (folder.Inclusion){
                case InclusionMode.Recursive:
                    fileList = GetImageFileRecursive(folder.Path, excludedFolders);
                    break;
                case InclusionMode.Single:
                    fileList = GetImageFileSingle(folder.Path);
                    break;
                case InclusionMode.Exclude:
                    continue;
                default:
                    Debug.WriteLine("Unhandled folder inclusion " + folder.Inclusion);
                    continue;
            }
            foreach (var filePath in fileList){
                pictureList.Add(new ImagePath { ID = id, Path = filePath, FileDate = File.GetCreationTime(filePath)});
                ++id;
            }
        }
    }

    static IEnumerable<string> GetImageFileSingle(string path)
        => from file in Directory.GetFiles(path)
           where SupportedImage.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)
           select file;

    static IEnumerable<string> GetImageFileRecursive(string path, string[] excludedFolders){
        if (Array.FindIndex(excludedFolders, folder => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) != -1){
            Debug.WriteLine("Exclude: " + path);
            return [];
        }
        var subImageFiles = from dir in Directory.GetDirectories(path)
                            from file in GetImageFileRecursive(dir, excludedFolders)
                            select file;
        return GetImageFileSingle(path).Union(subImageFiles);
    }
    #region Structures
    struct ImagePath{
        public int ID;
        public string Path;
        public DateTime FileDate;
    }
    #endregion
}