using System;

namespace RZScreenSaver;

sealed class BackgroundSlideShowEngine(IPictureSource source, PageHost[] slideShowList) : MarshalByRefObject, ISaverEngine
{
    public void SwitchToSet(int setIndex) {
        source.SwitchToSet(setIndex);
        AppDeps.Settings.Value.BackgroundPictureSetSelected = AppDeps.Settings.Value.PicturePaths.Count > setIndex? setIndex : null;
        AppDeps.Settings.Save();
    }

    public void ToggleShowTitle() {
        AppDeps.Settings.Value.ShowTitle = !AppDeps.Settings.Value.ShowTitle;
        foreach (var host in slideShowList)
            if (host.SlidePage is not null)
                host.SlidePage.ShowTitle = AppDeps.Settings.Value.ShowTitle;
        AppDeps.Settings.Save();
    }

    public override object? InitializeLifetimeService() {
        return null;
    }
}