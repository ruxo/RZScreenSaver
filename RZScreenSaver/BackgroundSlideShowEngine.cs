using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace RZScreenSaver;

static class BackgroundSlideShowEngine{
    public static Application Start(IPictureSource source, PageHost[] slideShowList){
        var pictureSource = source;

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

        var aboutDialog = new AboutRz(new SaverEngine(pictureSource, slideShowList));
        aboutDialog.Hide();
        return new Application{ShutdownMode = ShutdownMode.OnExplicitShutdown};
    }

    sealed class SaverEngine(IPictureSource source, PageHost[] slideShowList) : ScreenSaverEngine.ISaverEngine
    {
        public void SwitchToSet(int setIndex){
            source.SwitchToSet(setIndex);
            AppDeps.Settings.Value.BackgroundPictureSetSelected = setIndex;
            AppDeps.Settings.Save();
        }
        public void ToggleShowTitle(){
            AppDeps.Settings.Value.ShowTitle = !AppDeps.Settings.Value.ShowTitle;
            foreach (var host in slideShowList)
                if (host.SlidePage is not null)
                    host.SlidePage.ShowTitle = AppDeps.Settings.Value.ShowTitle;
            AppDeps.Settings.Save();
        }
    }
}