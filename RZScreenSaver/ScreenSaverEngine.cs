using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using RZScreenSaver.SlidePages;
using Application=System.Windows.Application;
using Cursor=System.Windows.Forms.Cursor;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;

namespace RZScreenSaver;

sealed class ScreenSaverEngine{
    #region Save Screen
    public void SaveScreen(){
        savers = CreatePageHostAndRun(ScreenSaverFactory, ScreenSaverConfigurer);
        Cursor.Hide();
    }
    static ScreenSaver ScreenSaverFactory(IPictureSource source, Rect rect, ISlidePage page){
        return new ScreenSaver(source, rect) { SlidePage = page};
    }
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

    public void RunAsBackground() {
        var pictureSet = AppDeps.Settings.Value.PicturePaths;
        var selectedIndex = AppDeps.Settings.Value.BackgroundPictureSetSelected;

        pictureSource = new TemporaryPictureSource(pictureSet, selectedIndex, AppDeps.Settings.Value.SlideMode, AppDeps.Settings.Value.SlideShowDelay);
        var slideShowList = CreatePageHostAndRun(PageHostFactory, PageHostConfigurer, pictureSource);

        new BackgroundSlideShowEngine(pictureSource).Start(slideShowList);
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
    public void PreviewScreen(IntPtr previewWindow){
        Win32.RECT parentRect;
        Win32.GetWindowRect(previewWindow, out parentRect);

        var wpfWin32 = new HwndSource(0, Win32.WS_VISIBLE | Win32.WS_CHILD,
                                      0, 0, 0, parentRect.Width, parentRect.Height, "RZ Screen Saver Preview",
                                      previewWindow, false);
        var source = CreateSourceFromSettings();
        var slidePage = SlidePageFactory.Create(AppDeps.Settings.Value.SaverMode).Create(AppDeps.Settings.Value.DisplayMode);
        source.PictureChanged += slidePage.OnShowPicture;
        wpfWin32.RootVisual = (Visual) slidePage;
        wpfWin32.Disposed += delegate { Application.Current.Shutdown(); };
        source.Start();
    }
    public void ConfigureSaver(IntPtr previewWindow){
        var configDialog = new ConfigDialog();
        if (previewWindow != IntPtr.Zero){
            new WindowInteropHelper(configDialog){Owner = previewWindow};
        }
        configDialog.ShowDialog();
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
    public interface ISaverEngine{
        void SwitchToSet(int setIndex);
        void ToggleShowTitle();
    }
    ScreenSaver[] savers;
    TemporaryPictureSource pictureSource;
}