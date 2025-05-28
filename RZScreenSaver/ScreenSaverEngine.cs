using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RZScreenSaver.SlidePages;
using Application=System.Windows.Application;
using Cursor=System.Windows.Forms.Cursor;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;

namespace RZScreenSaver;

public interface ISaverEngine{
    void SwitchToSet(int setIndex);
    void ToggleShowTitle();
}

sealed class NullSaverEngine : ISaverEngine
{
    public static readonly ISaverEngine Default = new NullSaverEngine();

    public void SwitchToSet(int setIndex) { }
    public void ToggleShowTitle() { }
}

sealed class ScreenSaverEngine{

    #region Save Screen

    public Application? SaveScreen(){
        savers = CreatePageHostAndRun(ScreenSaverFactory, ScreenSaverConfigurer);
        Cursor.Hide();
        return null;
    }
    static ScreenSaver ScreenSaverFactory(IPictureSource source, Rect rect, ISlidePage page)
        => new(source, rect) { SlidePage = page};

    void ScreenSaverConfigurer(ScreenSaver saver){
        saver.SlidePage.ShowTitle = AppDeps.Settings.Value.ShowTitle;
        saver.Closed += OnSaverClosed;
        saver.HandleKey += OnHandleKeyUp;
        saver.Show();
    }
    void OnHandleKeyUp(object sender, KeyEventArgs e){
        if (e.Key == Key.F11){
            e.Handled = true;
            pictureSource.SwitchToCurrentFolder();
        } else if (e is { Key: Key.System, SystemKey: Key.F10 }){
            e.Handled = true;
            pictureSource.RevertToMainSet();
        }
    }

    #endregion

    #region Run As Background

    public Application? RunAsBackground() {
        var pictureSet = AppDeps.Settings.Value.PicturePaths;
        var selectedIndex = AppDeps.Settings.Value.BackgroundPictureSetSelected;

        pictureSource = new TemporaryPictureSource(pictureSet, selectedIndex, AppDeps.Settings.Value.SlideMode, AppDeps.Settings.Value.SlideShowDelay);
        var slideShowList = CreatePageHostAndRun(PageHostFactory, PageHostConfigurer, pictureSource);

        var screenSaverCheck = new DispatcherTimer(DispatcherPriority.Background) {
            Interval = TimeSpan.FromSeconds(5)
        };
        screenSaverCheck.Tick += (_, _) => {
            var success = Win32.SystemParametersInfo(Win32.SPI_GETSCREENSAVERRUNNING, 0, out var isRunning, 0);
            Debug.Assert(success);
            if (isRunning ^ pictureSource.IsPaused)
                if (isRunning)
                    pictureSource.Pause();
                else
                    pictureSource.Resume();
        };
        screenSaverCheck.Start();

        // Need another appdomain in order to display task bar and dialogs. Otherwise, unexpected events will occur.. (I don't know why yet)
        var engineAssemblyPath = Assembly.GetExecutingAssembly().Location;
        Debug.Assert(engineAssemblyPath != null);
        var aboutDomain = AppDomain.CreateDomain("Background Domain");
        var helperTypeName = typeof (ForegroundDomain).FullName!;
        var foregroundDomain = (ForegroundDomain) aboutDomain.CreateInstanceFromAndUnwrap(engineAssemblyPath,helperTypeName);
        foregroundDomain.MainApplication = new BackgroundSlideShowEngine(pictureSource, slideShowList);

        var aboutThread = new Thread(foregroundDomain.RunAbout);
        aboutThread.SetApartmentState(ApartmentState.STA);
        aboutThread.Start();

        return null;
    }

    public class ForegroundDomain : MarshalByRefObject{
        public ISaverEngine? MainApplication { get; set; }
        public void RunAbout(){
            Debug.Assert(MainApplication is not null);

            Debug.WriteLine("App Domain Name: " + AppDomain.CurrentDomain.FriendlyName);
            Debug.WriteLine("Has Application? " + (Application.Current != null));
            var aboutDialog = new AboutRz(MainApplication!);
            aboutDialog.Hide();
            new Application{ShutdownMode = ShutdownMode.OnExplicitShutdown}.Run();

            Environment.Exit(0);
        }
        public override object? InitializeLifetimeService() {
            return null;
        }
    }

    static PageHost PageHostFactory(IPictureSource source, Rect rect, ISlidePage page)
        => new(source, rect) {SlidePage = page};

    static void PageHostConfigurer(PageHost host){
        host.IsHitTestVisible = false;
        host.SlidePage.ShowTitle = AppDeps.Settings.Value.ShowTitle;
        var hostTemp = host;
        host.Activated += delegate { hostTemp.SendToBottom(); };
        host.Show();
        host.SendToBottom();
    }
    #endregion

    public Application? PreviewScreen(IntPtr previewWindow){
        Win32.RECT parentRect;
        Win32.GetWindowRect(previewWindow, out parentRect);

        var wpfWin32 = new HwndSource(0, Win32.WS_VISIBLE | Win32.WS_CHILD,
                                      0, 0, 0, parentRect.Width, parentRect.Height, "RZ Screen Saver Preview",
                                      previewWindow, false);
        var source = CreateSourceFromSettings();
        var slidePage = SlidePageFactory.Create(AppDeps.Settings.Value.SaverMode).Create(AppDeps.Settings.Value.DisplayMode);
        source.PictureChanged.Subscribe(e => slidePage.OnShowPicture(e));
        wpfWin32.RootVisual = (Visual) slidePage;
        wpfWin32.Disposed += delegate { Application.Current.Shutdown(); };
        source.Start();
        return null;
    }
    public Application? ConfigureSaver(IntPtr previewWindow){
        var configDialog = new ConfigDialog();
        if (previewWindow != IntPtr.Zero){
            new WindowInteropHelper(configDialog){Owner = previewWindow};
        }
        configDialog.ShowDialog();
        return null;
    }

    T[] CreatePageHostAndRun<T>(Func<IPictureSource,Rect,ISlidePage,T> hostCreator, Action<T> hostConfigurer) where T : PageHost
        => CreatePageHostAndRun(hostCreator, hostConfigurer, CreateSourceFromSettings());

    static T[] CreatePageHostAndRun<T>(Func<IPictureSource,Rect,ISlidePage,T> hostCreator, Action<T> hostConfigurer, IPictureSource source)
        where T : PageHost{
        if (AppDeps.Settings.Value.LastShownIndex is {} shownIndex)
            source.RestorePicturePosition(shownIndex);

        var slideCreator = SlidePageFactory.Create(AppDeps.Settings.Value.SaverMode);
        var result = (from screen in Screen.AllScreens
                      let b = screen.Bounds
                      let rect = new Rect(b.Left, b.Top, b.Width, b.Height)
                      select hostCreator(source, rect, slideCreator.Create(AppDeps.Settings.Value.DisplayMode))).ToArray();
        foreach (var host in result)
            hostConfigurer(host);
        source.Start();
        return result;
    }

    IPictureSource CreateSourceFromSettings()
        => pictureSource = new TemporaryPictureSource(AppDeps.Settings.Value.PicturePaths,
                                                      AppDeps.Settings.Value.PictureSetSelected,
                                                      AppDeps.Settings.Value.SlideMode,
                                                      AppDeps.Settings.Value.SlideShowDelay);

    void OnSaverClosed(object sender, EventArgs e){
        foreach (var saver in savers){
            if (saver.IsVisible)
                saver.Close();
        }
        Cursor.Show();
        if (AppDeps.Settings.Value.LastShownIndex != pictureSource.PictureIndex){
            AppDeps.Settings.Value.LastShownIndex = pictureSource.PictureIndex;
            AppDeps.Settings.Save();
        }
    }
    ScreenSaver[] savers;
    TemporaryPictureSource pictureSource;
}