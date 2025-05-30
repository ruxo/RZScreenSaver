using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace RZScreenSaver;

enum MainCommand{
    ShowSaver,
    ConfigureSaver,
    PreviewSaver,
    RunAsBackground,
}
class InvalidArgumentException(int code, string message) : Exception(message)
{
    public int ExitCode => code;
}

public static class MainApp{
    public static int Rand(int maxValue){
        //return r.Next(maxValue * 2) % maxValue;
        return r.Next(maxValue);
    }
    public static int Rand(){
        return r.Next();
    }
    public const int InvalidWindowHandleError = -2;

    [STAThread]
    public static int Main(string[] arg) {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        var currentDirectory = Directory.GetCurrentDirectory();
        Trace.WriteLine($"[RZScreenSaver] Current directory: {currentDirectory}");

        System.Windows.Forms.Application.EnableVisualStyles();

        #region Debug: print arguments & Settings

        Debug.WriteLine("arg count = " + arg.Length);
        for(var i=0; i < arg.Length; ++i)
            Debug.WriteLine($"arg #{i.ToString()} = {arg[i]}");

        if (AppDeps.Settings.Value.PicturePaths.Count > 0)
            foreach (var set in AppDeps.Settings.Value.PicturePaths){
                Debug.Write("Begin Set...");
                foreach(var folder in set){
                    var inclusion = folder.Inclusion == InclusionMode.Exclude
                                        ? "[X]"
                                        : folder.Inclusion == InclusionMode.Single ? "[S]" : "[R]";
                    Debug.WriteLine(inclusion + folder.Path);
                }
            }
        else
            Debug.WriteLine("No image source!");

        #endregion

        MainCommand command;
        IntPtr previewWindow;
        try{
            command = ExtractCommand(arg, out previewWindow);
        }
        catch (InvalidArgumentException e){
            Trace.WriteLine(e.Message);
            return e.ExitCode;
        }
        var w = new ScreenSaverEngine();
        var a = command switch {
            MainCommand.ShowSaver       => w.SaveScreen(),
            MainCommand.PreviewSaver    => w.PreviewScreen(previewWindow),
            MainCommand.ConfigureSaver  => ScreenSaverEngine.ConfigureSaver(previewWindow),
            MainCommand.RunAsBackground => w.RunAsBackground(),

            _ => throw new NotSupportedException("FATAL: Unhandle main command " + command)
        };
        try{
            return a?.Run() ?? 0;
        }
        finally{
            AppDeps.Shutdown();
        }
    }
    static MainCommand ExtractCommand(string[] arg, out IntPtr previewWindow){
        if (arg.Length == 0){
            previewWindow = IntPtr.Zero;
            return MainCommand.RunAsBackground;
        }
        var mode = arg.Length > 0 ? arg[0].ToUpper() : "/S";
        if (mode.StartsWith("/S")){
            previewWindow = IntPtr.Zero;
            return MainCommand.ShowSaver;
        }
        else if (mode.StartsWith("/P")){
            if (arg.Length > 1){
                long hwnd;
                if (!long.TryParse(arg[1], out hwnd)){
                    throw new InvalidArgumentException(InvalidWindowHandleError, "ERROR: Invalid window handle " + arg[1]);
                }else
                    Debug.WriteLine("Preview window handle = " + arg[1]);
                previewWindow = new IntPtr(hwnd);
                return MainCommand.PreviewSaver;
            } else{
                throw new InvalidArgumentException(InvalidWindowHandleError, "ERROR: No window handle suppplied.");
            }
        }else if (mode.StartsWith("/C")){
            // Configuration mode
            // E.g. from "/c:12345" we need to capture 12345, which is the parent HWND in decimal
            var settingsParam = new Regex(@"/c:(\d+)", RegexOptions.IgnoreCase);
            var settingsParamMatch = settingsParam.Match(arg[0]);
            previewWindow = IntPtr.Zero;
            if (settingsParamMatch.Success){
                var captures = settingsParamMatch.Groups[1].Captures;
                if (captures.Count == 1){
                    long hwnd;
                    if (!long.TryParse(captures[0].ToString(), out hwnd))
                        throw new InvalidArgumentException(InvalidWindowHandleError,
                                                           "ERROR: Invalid window handle " + arg[1]);
                    previewWindow = new IntPtr(hwnd);
                }
            }
            return MainCommand.ConfigureSaver;
        }else{
            if (mode != "/D")
                Trace.WriteLine("Mode not support: " + mode);
            previewWindow = IntPtr.Zero;
            return MainCommand.RunAsBackground;
        }
    }
    static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e){
        Trace.WriteLine("Unhandled exception: " + e.ExceptionObject);
    }
    static readonly Random r = new();
}