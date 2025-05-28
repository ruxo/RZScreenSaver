using System;
using System.Windows.Threading;

namespace RZScreenSaver;

sealed class BackgroundSlideShowEngine(Dispatcher dispatcher, IPictureSource source, PageHost[] slideShowList) : MarshalByRefObject, ISaverEngine
{
    public void SwitchToSet(int setIndex) {
        dispatcher.InvokeAsync(() => source.SwitchToSet(setIndex));
        AppDeps.Settings.Value.BackgroundPictureSetSelected = AppDeps.Settings.Value.PicturePaths.Count > setIndex? setIndex : null;
        AppDeps.Settings.Save();
    }

    public void ToggleShowTitle() {
        AppDeps.Settings.Value.ShowTitle = !AppDeps.Settings.Value.ShowTitle;
        AppDeps.Settings.Save();

        dispatcher.InvokeAsync(() => {
            foreach (var host in slideShowList)
                if (host.SlidePage is not null)
                    host.SlidePage.ShowTitle = AppDeps.Settings.Value.ShowTitle;
        });
    }

    public override object? InitializeLifetimeService() {
        return null;
    }
}