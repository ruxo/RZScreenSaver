using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using RZScreenSaver.Properties;

namespace RZScreenSaver {
    /// <summary>
    /// Interaction logic for AboutRZ.xaml
    /// </summary>
    public partial class AboutRZ{
        public AboutRZ() {
            InitializeComponent();
        }
        internal AboutRZ(ScreenSaverEngine.ISaverEngine engine) {
            InitializeComponent();
            saverEngine = engine;
            showTitleMenu.IsChecked = Settings.Default.ShowTitle;
        }
        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            new GlassHelper(this).ExtendGlassFrame();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            base.OnClosing(e);
            e.Cancel = !allowClose;
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            trayIcon.Dispose();
            trayIcon = null;
            Application.Current.Shutdown();
        }
        void onHideWindow(object sender, RoutedEventArgs e){
            e.Handled = true;
            Hide();
        }
        void onQuitApplications(object sender, RoutedEventArgs e){
            e.Handled = true;
            allowClose = true;
            Close();
        }
        void onShowAboutWindow(object sender, RoutedEventArgs e){
            e.Handled = true;
            Show();
        }
        bool allowClose;
        void onShowConfigDialog(object sender, RoutedEventArgs e){
            Debug.WriteLine(e.Handled);
            e.Handled = true;
            new ConfigDialog().ShowDialog();
        }
        void onSwitchSet(object sender, RoutedEventArgs e){
            var menuItem = (MenuItem) sender;
            var setIndex = Int32.Parse(menuItem.Tag.ToString());
            saverEngine.SwitchToSet(setIndex);
        }
        ScreenSaverEngine.ISaverEngine saverEngine;
        void onToggleShowTitle(object sender, RoutedEventArgs e){
            saverEngine.ToggleShowTitle();
        }
    }
}
