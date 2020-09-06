using System.Configuration;

namespace RZWScreenSaver.Properties{
    [SettingsProvider(typeof (PortableSettingsProvider))]
    partial class Settings{
        /// <summary>
        /// Complete objects relationship in Settings.
        /// </summary>
        public void AppInitialize(){
            if (PicturePaths != null)
                PicturePaths.SelectedIndex = PictureSetSelected;
        }
    }
}