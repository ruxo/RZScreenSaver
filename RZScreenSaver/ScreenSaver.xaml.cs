using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RZScreenSaver.Properties;
using Application=System.Windows.Application;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;
using KeyEventHandler=System.Windows.Input.KeyEventHandler;
using MessageBox=System.Windows.MessageBox;
using MouseEventArgs=System.Windows.Input.MouseEventArgs;

namespace RZScreenSaver;

/// <summary>
/// Interaction logic for ScreenSaver.xaml
/// </summary>
partial class ScreenSaver{
    const int MouseMoveAllowedThreshold = 5;
    System.Drawing.Point lastMousePosition;
    #region ctors
    public ScreenSaver() {
        InitializeComponent();
    }
    internal ScreenSaver(IPictureSource source, Rect displayArea) : base(source, displayArea){
        InitializeComponent();
    }
    #endregion

    internal KeyEventHandler? HandleKey;
    internal void RefreshShowTitle(){
        slide.ShowTitle = Settings.Default.ShowTitle;
    }
    protected override void OnActivated(EventArgs e) {
        lastMousePosition = System.Windows.Forms.Cursor.Position;
        base.OnActivated(e);
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (!userInteractionMode){
            var current = System.Windows.Forms.Cursor.Position;
            var distant = Math.Abs(current.X - lastMousePosition.X) + Math.Abs(current.Y - lastMousePosition.Y);
            if (distant > MouseMoveAllowedThreshold)
                Close();
        }
    }
    #region Keyboard handle
    static readonly Key[] SetChangingKeys = new[]{Key.D1, Key.D2, Key.D3, Key.D4};
    protected override void OnKeyUp(KeyEventArgs e) {
        if (IsKeyExpected()){
            e.Handled = true;
            return;
        }
        if (HandleKey != null){
            HandleKey(this, e);
            if (e.Handled)
                return;
        }
        e.Handled = true;
        if (SetChangingKeys.Contains(e.Key)){
            var setIndex = Array.IndexOf(SetChangingKeys, e.Key);
            if (setIndex != Settings.Default.PictureSetSelected){
                pictureSource.SwitchToSet(setIndex);
                Settings.Default.PictureSetSelected = setIndex;
                Settings.Default.Save();
            }
            return;
        }
        switch(e.Key){
            case Key.F2:
                Settings.Default.ShowTitle = !Settings.Default.ShowTitle;
                Settings.Default.Save();
                NotifyAllScreenSavers(saver => saver.RefreshShowTitle());
                return;
            case Key.Scroll:
                Debug.WriteLine("Scroll Lock with State = " + e.KeyStates.ToString());
                if (e.KeyStates == KeyStates.Toggled)
                    pictureSource.Stop();
                else
                    pictureSource.Start();
                return;
            case Key.F9:
                DeleteOrMoveCurrentFile();
                return;
            case Key.F12:
                using(new UserInteractionScope(this)){
                    var configDialog = new ConfigDialog {Owner=this};
                    var result = configDialog.ShowDialog();
                    if (result != null && result.Value)
                        pictureSource.SwitchToSet(Settings.Default.PictureSetSelected);
                }
                return;
        }
        Debug.Write("Exit with key: ");
        Debug.WriteLine(e.Key.ToString());
        Application.Current.Shutdown();
    }
    void ExpectKeySoon(){
        expectedKey = true;
        expectedKeyTime = DateTime.Now;
        Debug.WriteLine("Expect key @ " + expectedKeyTime);
    }
    bool IsKeyExpected(){
        var result = expectedKey && (DateTime.Now - expectedKeyTime).Milliseconds < 500;
        expectedKey = false;
        if (result)
            Debug.WriteLine("Key is expected!");
        return result;
    }
    bool expectedKey;
    DateTime expectedKeyTime;
    #endregion

    #region User Interaction
    bool userInteractionMode;
    void BeginUserInteraction(){
        userInteractionMode = true;
    }
    void EndUserInteraction(){
        userInteractionMode = false;
    }
    class UserInteractionScope : IDisposable{
        public UserInteractionScope(ScreenSaver parent){
            caller = parent;
            caller.pictureSource.Pause();
            NotifyAllScreenSavers(saver => saver.BeginUserInteraction());
            System.Windows.Forms.Cursor.Show();
        }
        public void Dispose(){
            NotifyAllScreenSavers(saver => saver.EndUserInteraction());
            System.Windows.Forms.Cursor.Hide();
            caller.pictureSource.Resume();
            caller.Focus();
            caller.ExpectKeySoon();
        }
        readonly ScreenSaver caller;
    }
    #endregion

    void DeleteOrMoveCurrentFile(){
        Debug.Assert(pictureSource.CurrentPicture is not null);

        using (new UserInteractionScope(this)){
            var targetFile = pictureSource.CurrentPictureFile;
            var dialog = new DeleteDialog(pictureSource.CurrentPicture!, targetFile);
            var dialogResult = dialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value){
                if (dialog.MoveFileNeeded){
                    var moveDialog = new Microsoft.Win32.SaveFileDialog{
                        InitialDirectory = Path.GetDirectoryName(targetFile),
                        Title = "Select the target folder",
                        FileName = targetFile,
                    };
                    if ((bool)moveDialog.ShowDialog() && !pictureSource.MoveCurrentPictureTo(moveDialog.FileName)){
                        MessageBox.Show(this, "Path too long or No permission on the target folder.",
                                        "Move failed", MessageBoxButton.OK);
                    }
                }else if (!pictureSource.DeleteCurrentPicture())
                    MessageBox.Show(this,
                                    "This picture cannot be deleted (locked?)\nHowever, it's removed from picture list temporarily",
                                    "File Deletion Failed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    static void NotifyAllScreenSavers(Action<ScreenSaver> saverHandler){
        var saverList = from Window w in Application.Current.Windows where w is ScreenSaver select w;
        foreach (ScreenSaver saver in saverList){
            saverHandler(saver);
        }
    }
}