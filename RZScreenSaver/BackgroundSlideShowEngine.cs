using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using RZScreenSaver.Properties;

namespace RZScreenSaver;

class BackgroundSlideShowEngine{
    public BackgroundSlideShowEngine(IPictureSource source){
        pictureSource = source;

        screenSaverCheck.Interval = TimeSpan.FromSeconds(5);
        screenSaverCheck.Tick += OnCheckScreenSaver;
    }
    void OnCheckScreenSaver(object sender, EventArgs e){
        var success = Win32.SystemParametersInfo(Win32.SPI_GETSCREENSAVERRUNNING, 0, out var isRunning, 0);
        Debug.Assert(success);
        if (isRunning ^ pictureSource.IsPaused)
            if (isRunning)
                pictureSource.Pause();
            else
                pictureSource.Resume();
    }
    public void Start(PageHost[] slideShowList){
        var engineAssemblyPath = Assembly.GetExecutingAssembly().Location;
        Debug.Assert(engineAssemblyPath != null);
        var aboutDomain = AppDomain.CreateDomain("Background Domain");
        var helperTypeName = typeof (ForegroundDomain).FullName!;
        var foregroundDomain = (ForegroundDomain) aboutDomain.CreateInstanceFromAndUnwrap(engineAssemblyPath,helperTypeName);
        foregroundDomain.MainApplication = new SaverEngine(pictureSource, slideShowList);

        var aboutThread = new Thread(foregroundDomain.RunAbout);
        aboutThread.SetApartmentState(ApartmentState.STA);
        aboutThread.Start();

        screenSaverCheck.Start();
    }

    public class SaverEngine(IPictureSource source, PageHost[] slideShowList) : MarshalByRefObject, ScreenSaverEngine.ISaverEngine, IDisposable
    {
        public void SwitchToSet(int setIndex){
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                                                       new Action<int>(source.SwitchToSet), setIndex);
            Settings.Default.BackgroundPictureSetSelected = setIndex;
            Settings.Default.Save();
        }
        public void ToggleShowTitle(){
            Settings.Default.ShowTitle = !Settings.Default.ShowTitle;
            foreach (var host in slideShowList){
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind,
                                                           new Action<PageHost>(SetHostTitle), host);
            }
            Settings.Default.Save();
        }
        public void Dispose(){
            Application.Current.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }
        public override object InitializeLifetimeService() {
            return null;
        }
        /// <summary>
        /// must be called from its thread
        /// </summary>
        void SetHostTitle(PageHost host){
            host.SlidePage.ShowTitle = Settings.Default.ShowTitle;
        }
    }

    public class ForegroundDomain : MarshalByRefObject{
        public SaverEngine? MainApplication { get; set; }
        public void RunAbout(){
            Debug.Assert(MainApplication is not null);

            Debug.WriteLine("App Domain Name: " + AppDomain.CurrentDomain.FriendlyName);
            Debug.WriteLine("Has Application? " + (Application.Current != null));
            var aboutDialog = new AboutRz(MainApplication!);
            aboutDialog.Hide();
            new Application{ShutdownMode = ShutdownMode.OnExplicitShutdown}.Run();

            MainApplication!.Dispose();
        }
        public override object InitializeLifetimeService() {
            return null;
        }
    }

    readonly DispatcherTimer screenSaverCheck = new(DispatcherPriority.Background);
    readonly IPictureSource pictureSource;
}