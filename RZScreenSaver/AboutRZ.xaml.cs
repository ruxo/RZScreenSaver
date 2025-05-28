using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RZScreenSaver;

/// <summary>
/// Interaction logic for AboutRZ.xaml
/// </summary>
public partial class AboutRz{
    readonly ISaverEngine saverEngine = NullSaverEngine.Default;

    public AboutRz() {
        InitializeComponent();
    }
    internal AboutRz(ISaverEngine engine) {
        InitializeComponent();
        saverEngine = engine;
        showTitleMenu.IsChecked = AppDeps.Settings.Value.ShowTitle;
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
    void OnHideWindow(object sender, RoutedEventArgs e){
        e.Handled = true;
        Hide();
    }
    void OnQuitApplications(object sender, RoutedEventArgs e){
        e.Handled = true;
        allowClose = true;
        Close();
    }
    void OnShowAboutWindow(object sender, RoutedEventArgs e){
        e.Handled = true;
        Show();
    }
    bool allowClose;
    void OnShowConfigDialog(object sender, RoutedEventArgs e){
        Debug.WriteLine(e.Handled);
        e.Handled = true;
        new ConfigDialog().ShowDialog();
    }

    void OnSwitchSet(object sender, RoutedEventArgs e){
        var menuItem = (MenuItem) sender;
        var setIndex = Int32.Parse(menuItem.Tag.ToString());
        saverEngine.SwitchToSet(setIndex);
    }

    void OnToggleShowTitle(object sender, RoutedEventArgs e){
        saverEngine.ToggleShowTitle();
    }
}