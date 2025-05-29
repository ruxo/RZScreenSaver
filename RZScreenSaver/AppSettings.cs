using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RZScreenSaver;

public sealed record AppSettings
{
    public int SlideShowDelay { get; set; } = 10;
    public SlideMode SlideMode { get; set; } = SlideMode.Random;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.OriginalOrFit;
    public bool ShowTitle { get; set; }
    public string? LastSelectedFolder { get; set; }
    public SaverMode SaverMode { get; set; } = SaverMode.SlideShow;
    public int? BackgroundPictureSetSelected { get; set; }
    public int PhotoCollageAngle { get; set; } = 40;
    public double MinSquareCardSize { get; set; } = 0.25;
    public double MaxSquareCardSize { get; set; } = 0.5;
    public string? BackgroundPicturePath { get; set; }

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(1);

    public IReadOnlyList<FolderCollection> PicturePaths { get; set; } = [];

    /// <summary>
    /// Selected picture set index in PicturePaths. If null, meaning <see cref="PicturePaths"/> is empty.
    /// </summary>
    public int? PictureSetSelected { get; set; }
}

public readonly record struct ImagePath(int Index, string Path, DateTime FileDate)
{
    public static readonly ImagePath Empty = new(-1, string.Empty, DateTime.MinValue);
}

public sealed record PictureCache(List<ImagePath> Set, SlideMode Mode, int ShownIndex, DateTimeOffset Timestamp);

public interface IAppSettingsRepository : IDisposable
{
    AppSettings Value { get; }
    void Save(AppSettings? settings = null);

    PictureCache? LoadPictureSet(int setIndex);
    void SavePictureSet(int setIndex, IReadOnlyList<ImagePath> set, SlideMode mode, int shownIndex, DateTimeOffset? timestamp = null);
    void UpdateShownIndex(int setIndex, int shownIndex);
    void InvalidPictureSet(int setIndex);
}

sealed class AppSettingsRepository : IAppSettingsRepository
{
    static readonly string AppSettingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RZScreenSaver");
    static readonly string AppSettingFile = Path.Combine(AppSettingFolder, "settings.json");
    static readonly string CacheFile = Path.Combine(AppSettingFolder, "cache.json");

    readonly record struct Cache(SlideMode Mode, int ShownIndex, DateTimeOffset Timestamp);
    readonly Dictionary<int, Cache> pictureCache;
    readonly CancellationTokenSource cacheSaverCancel = new();

    bool cacheHeaderDirty;

    public AppSettingsRepository() {
        var content = File.Exists(CacheFile)? File.ReadAllText(CacheFile) : "{}";
        pictureCache = JsonConvert.DeserializeObject<Dictionary<int, Cache>>(content)!;

        Task.Run(async () => {
            while(!cacheSaverCancel.IsCancellationRequested){
                await Task.Delay(TimeSpan.FromSeconds(20));
                if (cacheHeaderDirty)
                    SaveCacheHeader();
            }
        });
    }

    public AppSettings Value { get; private set; } = FirstLoad();

    public void Save(AppSettings? value = null) {
        if (!Directory.Exists(AppSettingFolder))
            Directory.CreateDirectory(AppSettingFolder);
        var json = JsonConvert.SerializeObject(value ?? Value);
        File.WriteAllText(AppSettingFile, json);
        Value = value ?? Value;
    }

    public PictureCache? LoadPictureSet(int setIndex) {
        var setFile = Path.Combine(AppSettingFolder, $"set-{setIndex}.json");
        if (!File.Exists(setFile)) return null;

        var json = File.ReadAllText(setFile);
        var set = JsonConvert.DeserializeObject<List<ImagePath>>(json)!;
        var cache = pictureCache.TryGetValue(setIndex, out var v)? v : new(SlideMode.Random, 0, DateTimeOffset.MinValue);
        return new PictureCache(set, cache.Mode, cache.ShownIndex, cache.Timestamp);
    }

    public void SavePictureSet(int setIndex, IReadOnlyList<ImagePath> set, SlideMode mode, int shownIndex, DateTimeOffset? timestamp) {
        var json = JsonConvert.SerializeObject(set);
        File.WriteAllText(Path.Combine(AppSettingFolder, $"set-{setIndex}.json"), json);

        pictureCache[setIndex] = new(mode, shownIndex, timestamp ?? DateTimeOffset.Now);
        SaveCacheHeader();
    }

    public void UpdateShownIndex(int setIndex, int shownIndex) {
        pictureCache[setIndex] = pictureCache[setIndex] with { ShownIndex = shownIndex };
    }

    public void InvalidPictureSet(int setIndex) {
        pictureCache[setIndex] = pictureCache[setIndex] with { Timestamp = DateTimeOffset.MinValue };
        cacheHeaderDirty = true;
    }

    void SaveCacheHeader() {
        var json = JsonConvert.SerializeObject(pictureCache);
        File.WriteAllText(CacheFile, json);
        cacheHeaderDirty = false;
    }

    static AppSettings FirstLoad() {
        if (!Directory.Exists(AppSettingFolder) || !File.Exists(AppSettingFile))
            return new();
        var json = File.ReadAllText(AppSettingFile);
        return JsonConvert.DeserializeObject<AppSettings>(json) ?? new();
    }

    public void Dispose() {
        SaveCacheHeader();
    }
}