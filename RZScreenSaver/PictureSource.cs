using System;
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
    static readonly string[] SupportedImage = [".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tiff", ".ico", ".heic"];

    readonly DispatcherTimer timer;
    readonly IReadOnlyList<FolderCollection> picturePaths;

    readonly Subject<Unit> pictureSetChanged = new();
    readonly Subject<PictureChangedEventArgs> pictureChanged = new();
    readonly Dispatcher myDispatcher;

    List<ImagePath> pictureList = [];
    int currentPictureIndex;
    SlideMode pictureMode = SlideMode.Random;
    DateTimeOffset lastCached = DateTimeOffset.MinValue;

    ImageSource? currentPicture;
    int? pictureSetSelected;
    IDisposable pictureSetLoader = Disposable.Empty;

    #region ctors

    public PictureSource(IReadOnlyList<FolderCollection> paths, int? selectedPictureSet, int slideDelay) {
        picturePaths = paths;
        pictureSetSelected = selectedPictureSet;

        myDispatcher = Dispatcher.CurrentDispatcher;
        PictureSetChanged = pictureSetChanged.ObserveOn(myDispatcher);
        PictureChanged = pictureChanged.ObserveOn(myDispatcher);

        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(slideDelay) };
        timer.Tick += ChangePictureEvent;

        if (pictureSetSelected is not null)
            SwitchToSet(pictureSetSelected.Value);
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

    public int PictureIndex => currentPictureIndex;
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

    /// <summary>
    /// Delete current picture from physical disk storage!
    /// </summary>
    /// <returns>true - if picture can be deleted.</returns>
    public bool DeleteCurrentPicture(){
        if (currentPictureIndex >= pictureList.Count){
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
        AppDeps.Settings.SavePictureSet(pictureSetSelected!.Value, pictureList, pictureMode, currentPictureIndex);
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
            AppDeps.Settings.SavePictureSet(pictureSetSelected!.Value, pictureList, pictureMode, currentPictureIndex);
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
        pictureSetLoader.Dispose();
        pictureSetLoader = Disposable.Empty;

        if (AppDeps.Settings.LoadPictureSet(setIndex) is { } existed){
            pictureList = existed.Set;
            currentPictureIndex = existed.ShownIndex;
            pictureMode = existed.Mode;
            lastCached = existed.Timestamp;

            var settings = AppDeps.Settings.Value;
            if (settings.SlideMode != pictureMode || DateTimeOffset.Now - lastCached > settings.CacheDuration)
                pictureSetLoader = ReloadPictureSet(setIndex, settings.SlideMode);
        }
        else
            pictureSetLoader = LoadInitialPictureSet(setIndex, SlideMode.Random);

        if (IsStarted){
            pictureSetChanged.OnNext(Unit.Default);
            myDispatcher.InvokeAsync(NotifyNextImage);
        }
    }

    public IObservable<Unit> PictureSetChanged { get; }
    public IObservable<PictureChangedEventArgs> PictureChanged { get; }

    #region Image order generators

    static List<ImagePath> GenerateImageOrder(SlideMode mode, IEnumerable<ImagePath> list)
        => mode switch {
            SlideMode.Sequence => list as List<ImagePath> ?? list.ToList(),
            SlideMode.SortedByFilenamePerFolder => (from x in list
                                                    group x by Path.GetDirectoryName(x.Path) into g
                                                    from p in g
                                                    orderby p.Path
                                                    select p).ToList(),
            SlideMode.SortedByFilenameAllFolders => (from x in list orderby Path.GetFileName(x.Path) select x).ToList(),
            SlideMode.SortedByDatePerFolder => (from x in list
                                                group x by Path.GetDirectoryName(x.Path) into g
                                                from p in g
                                                orderby p.FileDate
                                                select p).ToList(),
            SlideMode.SortedByDateAllFolders => (from x in list orderby x.FileDate select x).ToList(),
            SlideMode.Random                 => GenerateRandomSequence(list),

            _ => throw new NotSupportedException($"Unknown slide mode {mode}")
        };

    void ChangePictureEvent(object sender, EventArgs e){
        NotifyNextImage();
    }

    static List<ImagePath> GenerateRandomSequence(IEnumerable<ImagePath> list) {
        var result = list.ToArray();
        var count = result.Length;
        ShuffleItemByItem(count, result);
        result = ShuffleSequenceDeck(count, result);
        ShuffleItemByItem(count, result);
        return [..result];
    }

    static ImagePath[] ShuffleSequenceDeck(int count, ImagePath[] sequence) {
        var output = new ImagePath[sequence.Length];
        for(var i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Shuffle(pos1, pos2, ref output);
            Swap(ref sequence, ref output);
        }
        return sequence;
    }
    static void ShuffleItemByItem(int count, ImagePath[] sequence){
        for(var i=0; i < count; ++i){
            var pos1 = MainApp.Rand(count);
            var pos2 = MainApp.Rand(count);
            sequence.Swap(pos1, pos2);
        }
    }
    static void Swap<T>(ref T a, ref T b){
        (a, b) = (b, a);
    }

    #endregion

    void NotifyNextImage() {
        if (FetchNextPicture(out var fileName, out var fileDate) is not { } image) return;

        currentPicture = image;
        pictureChanged.OnNext(new PictureChangedEventArgs(fileName, fileDate, image));
    }
    ImageSource? FetchNextPicture(out string fileName, out DateTime fileDate){
    retry:
        fileName = String.Empty;
        fileDate = DateTime.MinValue;
        var list = pictureList;
        if (list.Count == 0) return null;

        var pictureFile = String.Empty;
        try{
            if (currentPictureIndex >= list.Count){
                currentPictureIndex = 0;
                if (DateTimeOffset.Now - lastCached > AppDeps.Settings.Value.CacheDuration){
                    pictureSetLoader = ReloadPictureSet(pictureSetSelected!.Value, AppDeps.Settings.Value.SlideMode);
                }
                else if (pictureMode == SlideMode.Random){
                    list = pictureList = GenerateRandomSequence(pictureList);
                    AppDeps.Settings.SavePictureSet(pictureSetSelected!.Value, pictureList, pictureMode, currentPictureIndex);
                }
            }

            fileName = pictureFile = list[currentPictureIndex].Path;
            fileDate = list[currentPictureIndex].FileDate;

            using var s = File.OpenRead(pictureFile);
            // use stream so file won't be locked.
            var decoder = BitmapDecoder.Create(s, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            ++currentPictureIndex;
            AppDeps.Settings.UpdateShownIndex(pictureSetSelected!.Value, currentPictureIndex);
            return decoder.Frames[0];
        }
        catch (Exception e){
            switch (e){
                case ArgumentException or NotSupportedException: Trace.WriteLine(pictureFile + " is not a recognized image format!"); break;
                case FileNotFoundException: Trace.WriteLine(pictureFile + " is no longer existed"); break;
                default: throw;
            }
            list.RemoveAt(currentPictureIndex);
            AppDeps.Settings.SavePictureSet(pictureSetSelected!.Value, pictureList, pictureMode, currentPictureIndex, DateTimeOffset.MinValue);
            goto retry;
        }
    }

    #region Picture loaders

    IDisposable LoadInitialPictureSet(int setIndex, SlideMode mode) {
        var fc = picturePaths[setIndex];
        var timing = Observable.Timer(TimeSpan.FromSeconds(1))
                               .Concat(Observable.Interval(TimeSpan.FromSeconds(5)))
                               .StartWith(0)
                               .Select(_ => new List<ImagePath>());

        List<ImagePath>? lastList = null;
        var source = RegeneratePictureList(fc).Append(ImagePath.Empty).ToObservable(ThreadPoolScheduler.Instance);

        var firstSet = true;
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
                                 Trace.WriteLine($"Picture list changed. {list!.Count} pictures.");

                                 pictureList = GenerateImageOrder(mode, pictureList.Concat(list));
                                 pictureMode = mode;
                                 lastCached = DateTimeOffset.Now;

                                 // temporary save picture set
                                 AppDeps.Settings.SavePictureSet(setIndex, pictureList, mode, currentPictureIndex, DateTimeOffset.MinValue);

                                 if (firstSet){
                                     firstSet = false;
                                     myDispatcher.InvokeAsync(NotifyNextImage);
                                 }

                                 if (ended){
                                     Debug.WriteLine("Picture list ended.");
                                     AppDeps.Settings.SavePictureSet(setIndex, pictureList, mode, currentPictureIndex);

                                     // ReSharper disable once AccessToModifiedClosure
                                     disposables.Dispose();
                                 }
                             });
        return disposables;
    }

    IDisposable ReloadPictureSet(int setIndex, SlideMode mode) {
        var fc = picturePaths[setIndex];
        var source = GenerateImageOrder(mode, RegeneratePictureList(fc)).ToObservable(ThreadPoolScheduler.Instance);

        return source.Aggregate(new List<ImagePath>(), (list, img) => {
            list.Add(img);
            return list;
        }).Subscribe(list => {
            Trace.WriteLine($"Picture list changed. {list.Count} pictures.");
            pictureList = list;
            pictureMode = mode;
            lastCached = DateTimeOffset.Now;
            AppDeps.Settings.SavePictureSet(setIndex, pictureList, mode, currentPictureIndex);
        });
    }

    #endregion

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
}