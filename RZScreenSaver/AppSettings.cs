using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RZScreenSaver;

public sealed record AppSettings
{
    public int SlideShowDelay { get; set; } = 10;
    public SlideMode SlideMode { get; set; } = SlideMode.Random;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.OriginalOrFit;
    public int? LastImageIndex { get; set; }
    public bool ShowTitle { get; set; }
    public string? LastSelectedFolder { get; set; }
    public SaverMode SaverMode { get; set; } = SaverMode.SlideShow;
    public int? BackgroundPictureSetSelected { get; set; }
    public int PhotoCollageAngle { get; set; } = 40;
    public double MinSquareCardSize { get; set; } = 0.25;
    public double MaxSquareCardSize { get; set; } = 0.5;
    public string? BackgroundPicturePath { get; set; }
    public int? LastShownIndex { get; set; }

    public IReadOnlyList<FolderCollection> PicturePaths { get; set; } = [];

    /// <summary>
    /// Selected picture set index in PicturePaths. If null, meaning <see cref="PicturePaths"/> is empty.
    /// </summary>
    public int? PictureSetSelected { get; set; }
}

public interface IAppSettingsRepository
{
    AppSettings Value { get; }
    void Save(AppSettings? settings = null);
}

sealed class AppSettingsRepository : IAppSettingsRepository
{
    static readonly string AppSettingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RZScreenSaver");
    static readonly string AppSettingFile = Path.Combine(AppSettingFolder, "settings.json");

    public AppSettings Value { get; private set; } = FirstLoad();

    public void Save(AppSettings? value = null) {
        if (!Directory.Exists(AppSettingFolder))
            Directory.CreateDirectory(AppSettingFolder);
        var json = JsonConvert.SerializeObject(value ?? Value);
        File.WriteAllText(AppSettingFile, json);
        Value = value ?? Value;
    }

    static AppSettings FirstLoad() {
        if (!Directory.Exists(AppSettingFolder) || !File.Exists(AppSettingFile))
            return new();
        var json = File.ReadAllText(AppSettingFile);
        return JsonConvert.DeserializeObject<AppSettings>(json) ?? new();
    }
}