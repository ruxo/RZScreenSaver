using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using RZScreenSaver.Properties;
using RZScreenSaver.SlidePages;
using Application=System.Windows.Application;
using Cursor=System.Windows.Forms.Cursor;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;

namespace RZScreenSaver{
    class ScreenSaverEngine{
        #region Save Screen
        public void SaveScreen(){
            savers = createPageHostAndRun<ScreenSaver>(screenSaverFactory, screenSaverConfigurer);
            Cursor.Hide();
        }
        static ScreenSaver screenSaverFactory(IPictureSource source, Rect rect, ISlidePage page){
            return new ScreenSaver(source, rect) { SlidePage = page};
        }
        void screenSaverConfigurer(ScreenSaver saver){
            saver.SlidePage.ShowTitle = Settings.Default.ShowTitle;
            saver.Closed += onSaverClosed;
            saver.HandleKey += onHandleKeyUp;
            saver.Show();
        }
        void onHandleKeyUp(object sender, KeyEventArgs e){
            if (e.Key == Key.F11){
                e.Handled = true;
                pictureSource.SwitchToCurrentFolder();
            } else if (e.Key == Key.System && e.SystemKey == Key.F10){
                e.Handled = true;
                pictureSource.RevertToMainSet();
            }
        }

        #endregion

        #region Run As Background
        public void RunAsBackground(){
            var pictureSet = Settings.Default.PicturePaths.Clone();
            pictureSet.SelectedIndex = Settings.Default.BackgroundPictureSetSelected;

            pictureSource = new TemporaryPictureSource(pictureSet, Settings.Default.SlideMode, Settings.Default.SlideShowDelay);
            var slideShowList = createPageHostAndRun<PageHost>(pageHostFactory, pageHostConfigurer, pictureSource);

            new BackgroundSlideShowEngine(pictureSource).Start(slideShowList);
        }
        static PageHost pageHostFactory(IPictureSource source, Rect rect, ISlidePage page){
            return new PageHost(source, rect) {SlidePage = page};
        }
        static void pageHostConfigurer(PageHost host){
            host.IsHitTestVisible = false;
            host.SlidePage.ShowTitle = Settings.Default.ShowTitle;
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
            var source = createSourceFromSettings();
            var slidePage = SlidePageFactory.Create(Settings.Default.SaverMode).Create(Settings.Default.DisplayMode);
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
        T[] createPageHostAndRun<T>(Func<IPictureSource,Rect,ISlidePage,T> hostCreator, Action<T> hostConfigurer) where T : PageHost{
            return createPageHostAndRun(hostCreator, hostConfigurer, createSourceFromSettings());
        }
        static T[] createPageHostAndRun<T>(Func<IPictureSource,Rect,ISlidePage,T> hostCreator, Action<T> hostConfigurer, IPictureSource source)
            where T : PageHost{
            source.RestorePicturePosition(Settings.Default.LastShownIndex);
            var slideCreator = createPageFactoryFromSettings();
            var result = (from screen in Screen.AllScreens
                          let b = screen.Bounds
                          let rect = new Rect((b.Left), (b.Top), (b.Width), (b.Height))
                          select hostCreator(source, rect, slideCreator.Create(Settings.Default.DisplayMode))).ToArray();
            foreach (var host in result){
                hostConfigurer(host);
            }
            source.Start();
            return result;
        }
        static SlidePageFactory.ICreator createPageFactoryFromSettings(){
            return SlidePageFactory.Create(Settings.Default.SaverMode);
        }
        IPictureSource createSourceFromSettings(){
            pictureSource = new TemporaryPictureSource(Settings.Default.PicturePaths, Settings.Default.SlideMode, Settings.Default.SlideShowDelay);
            return pictureSource;
        }
        void onSaverClosed(object sender, EventArgs e){
            foreach (var saver in savers){
                if (saver.IsVisible)
                    saver.Close();
            }
            Cursor.Show();
            if (Settings.Default.LastShownIndex != pictureSource.PictureIndex){
                Settings.Default.LastShownIndex = pictureSource.PictureIndex;
                Settings.Default.Save();
            }
        }
        public interface ISaverEngine{
            void SwitchToSet(int setIndex);
            void ToggleShowTitle();
        }
        ScreenSaver[] savers;
        TemporaryPictureSource pictureSource;
    }
}