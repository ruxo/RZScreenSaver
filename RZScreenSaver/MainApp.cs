using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace RZScreenSaver;

enum MainCommand{
    Invalid,
    ShowSaver,
    ConfigureSaver,
    PreviewSaver,
    RunAsBackground,
}
class InvalidArgumentException(int code, string message) : Exception(message)
{
    public int ExitCode { get; private set; } = code;
}

public static class MainApp{
    public static int Rand(int maxValue){
        //return r.Next(maxValue * 2) % maxValue;
        return r.Next(maxValue);
    }
    public static int Rand(){
        return r.Next();
    }
    public const int InvalidArgumentError = -1;
    public const int InvalidWindowHandleError = -2;

    [STAThread]
    public static int Main(string[] arg){
        AppDomain.CurrentDomain.UnhandledException += onUnhandledException;
        System.Windows.Forms.Application.EnableVisualStyles();

        #region Debug: print arguments & Settings

        Debug.WriteLine("User app path = " + System.Windows.Forms.Application.LocalUserAppDataPath);
        Debug.WriteLine("Principal = " + System.Threading.Thread.CurrentPrincipal.ToString());
        Debug.WriteLine("Authenticated? " + System.Threading.Thread.CurrentPrincipal.Identity.IsAuthenticated);

        Debug.WriteLine("arg count = " + arg.Length);
        for(int i=0; i < arg.Length; ++i)
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
        var a = new Application();
        var w = new ScreenSaverEngine();
        switch (command){
            case MainCommand.ShowSaver:
                w.SaveScreen();
                break;
            case MainCommand.PreviewSaver:
                w.PreviewScreen(previewWindow);
                break;
            case MainCommand.ConfigureSaver:
                w.ConfigureSaver(previewWindow);
                break;
            case MainCommand.RunAsBackground:
                w.RunAsBackground();
                break;
            default:
                Trace.WriteLine("FATAL: Unhandle main command " + command);
                break;
        }
        return a.Run();
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
            Match settingsParamMatch = settingsParam.Match(arg[0]);
            previewWindow = IntPtr.Zero;
            if (settingsParamMatch.Success){
                CaptureCollection captures = settingsParamMatch.Groups[1].Captures;
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
    static void onUnhandledException(object sender, UnhandledExceptionEventArgs e){
        MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    }
    static readonly Random r = new Random();
}