using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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
    readonly Dispatcher myDispatcher;

    List<ImagePath> pictureList = new();
    ConcurrentQueue<int> slideOrder = new();

    int currentPictureIndex;
    ImageSource? currentPicture;
    int? pictureSetSelected;
    IDisposable pictureSetLoader = Disposable.Empty;

    #region ctors

    public PictureSource(IReadOnlyList<FolderCollection> paths, int? selectedPictureSet, SlideMode slideMode, int slideDelay) {
        picturePaths = paths;
        pictureSetSelected = selectedPictureSet;
        this.slideMode = slideMode;

        myDispatcher = Dispatcher.CurrentDispatcher;
        PictureSetChanged = pictureSetChanged.ObserveOn(myDispatcher);
        PictureChanged = pictureChanged.ObserveOn(myDispatcher);

        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(slideDelay) };
        timer.Tick += ChangePictureEvent;

        if (pictureSetSelected is not null)
            pictureSetLoader = LoadPictureSet(picturePaths[pictureSetSelected.Value]);
    }

    bool isDisposed;
    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;
        pictureSetLoader.Dispose();
        timer.Stop();
        pictureChanged.OnCompleted();
        pictureSetChanged.OnCompleted();
    }

    #endregion

    #region Engine Control

    public int PictureIndex => pictureList.Count - slideOrder.Count;
    public bool IsPaused => !IsStarted && pauseCall > 0;
    public bool IsStarted => timer.IsEnabled;

    public void Start(){
        timer.Start();
        myDispatcher.InvokeAsync(NotifyNextImage);
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
                slideOrder = new(GenerateImageOrder(pictureList));
            else
                for (var discardCount = 0; discardCount < lastPosition; ++discardCount)
                    slideOrder.TryDequeue(out _);
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
            pictureList[currentPictureIndex] = pictureList[currentPictureIndex] with { Path = newPath };
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
        if (setIndex >= picturePaths.Count)
            return;

        pictureSetSelected = setIndex;
        pictureList = [];
        slideOrder = new();
        pictureSetLoader.Dispose();
        pictureSetLoader = LoadPictureSet(picturePaths[setIndex]);

        if (IsStarted){
            pictureSetChanged.OnNext(Unit.Default);
            myDispatcher.InvokeAsync(NotifyNextImage);
        }
    }

    public IObservable<Unit> PictureSetChanged { get; }
    public IObservable<PictureChangedEventArgs> PictureChanged { get; }

    IEnumerable<int> GenerateImageOrder(IReadOnlyList<ImagePath> list)
        => slideMode switch {
            SlideMode.Sequence => Enumerable.Range(0, list.Count),
            SlideMode.SortedByFilenamePerFolder => from x in list
                                                   group x by Path.GetDirectoryName(x.Path) into g
                                                   from p in g
                                                   orderby p.Path
                                                   select p.Index,
            SlideMode.SortedByFilenameAllFolders => from x in list
                                                    orderby Path.GetFileName(x.Path)
                                                    select x.Index,
            SlideMode.SortedByDatePerFolder => from x in list
                                               group x by Path.GetDirectoryName(x.Path) into g
                                               from p in g
                                               orderby p.FileDate
                                               select p.Index,
            SlideMode.SortedByDateAllFolders => from x in list
                                                orderby x.FileDate
                                                select x.Index,
            SlideMode.Random => GenerateRandomSequence(list.Count),

            _ => throw new NotSupportedException($"Unknown slide mode {slideMode}")
        };

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
        for(var i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Shuffle(pos1, pos2, ref output);
            Swap(ref sequence, ref output);
        }
        return sequence;
    }
    static void ShuffleItemByItem(int count, int[] sequence){
        for(var i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Swap(pos1, pos2);
        }
    }
    static void Swap<T>(ref T a, ref T b){
        (a, b) = (b, a);
    }
    void NotifyNextImage() {
        if (FetchNextPicture(out var fileName, out var fileDate) is not { } image) return;

        currentPicture = image;
        pictureChanged.OnNext(new PictureChangedEventArgs(fileName, fileDate, image));
    }
    ImageSource? FetchNextPicture(out string fileName, out DateTime fileDate){
        fileName = String.Empty;
        fileDate = DateTime.MinValue;

        var pictureFile = String.Empty;
        try{
            if (slideOrder.Count == 0)
                slideOrder = new(GenerateImageOrder(pictureList));

            if (!slideOrder.TryDequeue(out currentPictureIndex))
                return null;

            fileName = pictureFile = pictureList[currentPictureIndex].Path;
            fileDate = pictureList[currentPictureIndex].FileDate;

            using var s = File.OpenRead(pictureFile);
            // use stream so file won't be locked.
            var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return decoder.Frames[0];
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
            var newSlide = slideOrder.Select(i => i > currentPictureIndex ? i - 1 : i);
            slideOrder = new ConcurrentQueue<int>(newSlide);
        }
        return null;
    }

    IDisposable LoadPictureSet(FolderCollection fc, int startId = 0) {
        var timing = Observable.Interval(TimeSpan.FromSeconds(2)).StartWith(0).Select(_ => new List<ImagePath>());

        List<ImagePath>? lastList = null;
        var source = RegeneratePictureList(fc, startId).Append(ImagePath.Empty).ToObservable(ThreadPoolScheduler.Instance);

        var ended = false;
        var disposables = Disposable.Empty;
        disposables = timing.CombineLatest(source, (list, image) => {
                                 if (image == ImagePath.Empty){
                                     ended = true;
                                     return [];
                                 }
                                 list.Add(image);
                                 return list;
                             })
                            .DistinctUntilChanged()
                            .Select(list => {
                                 var result = lastList;
                                 lastList = list;
                                 return result;
                             })
                            .Where(x => x?.Count > 0)
                            .Subscribe(list => {
                                 Debug.WriteLine($"Picture list changed. {list!.Count} pictures.");
                                 pictureList.AddRange(list);

                                 foreach (var i in GenerateImageOrder(list))
                                     slideOrder.Enqueue(i);

                                 if (ended)
                                     // ReSharper disable once AccessToModifiedClosure
                                     disposables.Dispose();
                             });
        return disposables;
    }

    [Pure]
    static IEnumerable<ImagePath> RegeneratePictureList(FolderCollection folderList, int startId = 0) {
        var excludedFolders = (from folder in folderList
                               where folder.Inclusion == InclusionMode.Exclude
                               select folder.Path
                              ).ToArray();
        var isExcluded = IsExcluded(excludedFolders);

        var id = startId;
        return from folder in folderList
               let fileList = folder.Inclusion switch {
                   InclusionMode.Recursive => isExcluded(folder.Path) ? [] : GetImageFileRecursive(isExcluded, folder.Path),
                   InclusionMode.Single    => GetImageFileSingle(folder.Path),
                   InclusionMode.Exclude   => [],

                   _ => throw new NotSupportedException($"Unknown inclusion mode {folder.Inclusion}")
               }
               from filePath in fileList
               select new ImagePath(id++, filePath, File.GetCreationTime(filePath));
    }

    [Pure]
    static IEnumerable<string> GetImageFileSingle(string path)
        => from file in Directory.GetFiles(path)
           where SupportedImage.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)
           select file;

    [Pure]
    static IEnumerable<string> GetImageFileRecursive(Func<string, bool> isExcluded, string path){
        var subImageFiles = from dir in Directory.GetDirectories(path)
                            where !isExcluded(dir)
                            from file in GetImageFileRecursive(isExcluded, dir)
                            select file;
        return GetImageFileSingle(path).Concat(subImageFiles);
    }

    [Pure]
    static Func<string, bool> IsExcluded(string[] excludedFolders)
        => path => Array.FindIndex(excludedFolders, folder => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) != -1;

    readonly record struct ImagePath(int Index, string Path, DateTime FileDate)
    {
        public static readonly ImagePath Empty = new(-1, string.Empty, DateTime.MinValue);
    }
}