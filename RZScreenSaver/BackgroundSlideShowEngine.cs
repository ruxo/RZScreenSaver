using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using RZScreenSaver.Properties;

namespace RZScreenSaver{
    class BackgroundSlideShowEngine{
        public BackgroundSlideShowEngine(IPictureSource source){
            pictureSource = source;

            screenSaverCheck.Interval = TimeSpan.FromSeconds(5);
            screenSaverCheck.Tick += onCheckScreenSaver;
        }
        void onCheckScreenSaver(object sender, EventArgs e){
            bool isRunning;
            var success = Win32.SystemParametersInfo(Win32.SPI_GETSCREENSAVERRUNNING, 0, out isRunning, 0);
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
            var helperTypeName = typeof (ForegroundDomain).FullName;
            var foregroundDomain = (ForegroundDomain) aboutDomain.CreateInstanceFromAndUnwrap(engineAssemblyPath,helperTypeName);
            foregroundDomain.MainApplication = new SaverEngine(pictureSource, slideShowList);

            var aboutThread = new Thread(foregroundDomain.RunAbout);
            aboutThread.SetApartmentState(ApartmentState.STA);
            aboutThread.Start();

            screenSaverCheck.Start();
        }
        public class SaverEngine : MarshalByRefObject, ScreenSaverEngine.ISaverEngine, IDisposable{
            public SaverEngine(IPictureSource source, PageHost[] slideShowList){
                pictureSource = source;
                this.slideShowList = slideShowList;
            }
            public void SwitchToSet(int setIndex){
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                                                           new Action<int>(pictureSource.SwitchToSet), setIndex);
                Settings.Default.BackgroundPictureSetSelected = setIndex;
                Settings.Default.Save();
            }
            public void ToggleShowTitle(){
                Settings.Default.ShowTitle = !Settings.Default.ShowTitle;
                foreach (var host in slideShowList){
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind,
                                                               new Action<PageHost>(setHostTitle), host);
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
            void setHostTitle(PageHost host){
                host.SlidePage.ShowTitle = Settings.Default.ShowTitle;
            }
            readonly IPictureSource pictureSource;
            readonly PageHost[] slideShowList;
        }
        public class ForegroundDomain : MarshalByRefObject{
            public SaverEngine MainApplication { get; set; }
            public void RunAbout(){
                Debug.WriteLine("App Domain Name: " + AppDomain.CurrentDomain.FriendlyName);
                Debug.WriteLine("Has Application? " + (Application.Current != null));
                var aboutDialog = new AboutRZ(MainApplication);
                aboutDialog.Hide();
                new Application{ShutdownMode = ShutdownMode.OnExplicitShutdown}.Run();
                
                if (MainApplication != null)
                    MainApplication.Dispose();
            }
            public override object InitializeLifetimeService() {
                return null;
            }
        }
        readonly DispatcherTimer screenSaverCheck = new DispatcherTimer(DispatcherPriority.Background);
        readonly IPictureSource pictureSource;
    }
}