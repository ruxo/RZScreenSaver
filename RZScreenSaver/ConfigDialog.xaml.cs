using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs=System.Windows.Input.KeyEventArgs;
using ListView=System.Windows.Controls.ListView;
using MessageBox=System.Windows.MessageBox;
using RadioButton=System.Windows.Controls.RadioButton;

namespace RZScreenSaver;

/// <summary>
/// Interaction logic for ConfigDialog.xaml
/// </summary>
partial class ConfigDialog{
    const int NumberOfPictureSet = 4;

    readonly RadioButton[] defaultRadioButtons;
    readonly ListView[] imageFolderList;
    readonly List<FolderCollection> folderSet = new();

    int? selectedIndex;
    GlassHelper glassMaker = null!;

    #region ctors

    public ConfigDialog() {
        InitializeComponent();
        defaultRadioButtons = [imageFolderDefault1, imageFolderDefault2, imageFolderDefault3, imageFolderDefault4];
        Debug.Assert(defaultRadioButtons.Length == NumberOfPictureSet);
        imageFolderList = [imageFoldersList1, imageFoldersList2, imageFoldersList3, imageFoldersList4];
        Debug.Assert(imageFolderList.Length == NumberOfPictureSet);

        // enable only there are more than 1 monitor.
        asMixedMode.IsEnabled = Screen.AllScreens.Length > 1;

        slideDelayInput.ValueChanged += delegate { EnableButtons(true); };
        LoadSettingsToUi();
        EnableButtons(false);
    }

    #endregion

    protected override void OnSourceInitialized(EventArgs e) {
        base.OnSourceInitialized(e);
        glassMaker = new GlassHelper(this, new Thickness(-1));
        glassMaker.ExtendGlassFrame();
    }
    ListView currentFolderListView{
        get{
            var index = imageFolderTab.SelectedIndex;
            return imageFolderList[index];
        }
    }
    FolderCollection currentFolderList{
        get{
            var index = imageFolderTab.SelectedIndex;
            return folderSet[index];
        }
    }

    #region Save/Restore Settings

    void LoadSettingsToUi(){
        slideDelayInput.Value = AppDeps.Settings.Value.SlideShowDelay;
        slideModeList.SelectedItem = AppDeps.Settings.Value.SlideMode;
        displayModeList.SelectedItem = AppDeps.Settings.Value.DisplayMode;
        folderDialog.SelectedPath = AppDeps.Settings.Value.LastSelectedFolder;

        folderSet.AddRange(AppDeps.Settings.Value.PicturePaths);
        selectedIndex = AppDeps.Settings.Value.PictureSetSelected;

        for(var setIndex=0; setIndex < NumberOfPictureSet; ++setIndex)
            if (setIndex >= folderSet.Count)
                folderSet.Add(new FolderCollection());
            else
                foreach(var item in folderSet[setIndex])
                    imageFolderList[setIndex].Items.Add(item);

        defaultRadioButtons[selectedIndex ?? 0].IsChecked = true;

        LoadUiSaverMode(AppDeps.Settings.Value.SaverMode);

        if (AppDeps.Settings.Value.BackgroundPicturePath is {} path)
            backgroundPicture.Text = path;
    }

    void LoadUiSaverMode(SaverMode mode){
        switch (mode){
            case SaverMode.SlideShow:
                asSlideShowMode.IsChecked = true;
                break;
            case SaverMode.PhotoCollage:
                asPhotoCollageMode.IsChecked = true;
                break;
            case SaverMode.Mixed:
                asMixedMode.IsChecked = true;
                break;
            default:
                asSlideShowMode.IsChecked = true;
                Debug.Write("Saver mode ");
                Debug.Write(AppDeps.Settings.Value.SaverMode);
                Debug.WriteLine(" is not handled!!");
                break;
        }
    }
    void StoreUiToSettings(){
        var defaultSetIndex = Array.FindIndex(defaultRadioButtons, radio => radio.IsChecked != null && radio.IsChecked.Value);
        Debug.Assert(defaultSetIndex != -1);

        selectedIndex = defaultSetIndex;

        AppDeps.Settings.Value.SlideShowDelay = (int) slideDelayInput.Value;
        AppDeps.Settings.Value.SlideMode = (SlideMode) slideModeList.SelectedItem;
        AppDeps.Settings.Value.DisplayMode = (DisplayMode) displayModeList.SelectedItem;
        AppDeps.Settings.Value.PicturePaths = folderSet;
        AppDeps.Settings.Value.LastSelectedFolder = folderDialog.SelectedPath;
        AppDeps.Settings.Value.SaverMode =  asSlideShowMode.IsChecked ?? false
                                         ? SaverMode.SlideShow
                                         : asPhotoCollageMode.IsChecked ?? false ? SaverMode.PhotoCollage : SaverMode.Mixed;
        AppDeps.Settings.Value.PictureSetSelected = selectedIndex;
        AppDeps.Settings.Value.BackgroundPicturePath = backgroundPicture.Text;
    }
    #endregion

    void EnableButtons(bool enable){
        if (okButton != null)
            okButton.IsEnabled = enable;
    }
    void SelectAndAddFolder(InclusionMode mode){
        if (folderDialog.ShowDialog(new Winform32Helper(this)) == System.Windows.Forms.DialogResult.OK){
            if (!currentFolderList.Contains(folderDialog.SelectedPath)){
                var folder = currentFolderList.Add(folderDialog.SelectedPath, mode);
                currentFolderListView.Items.Add(folder);
            }
            EnableButtons(true);
        }
    }

    #region Event Handlers

    void onClearList(object sender, RoutedEventArgs e){
        if (currentFolderList.Count > 0
         && MessageBox.Show("Are you sure to clear the list?", "Clear folder list",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes){
            currentFolderListView.Items.Clear();
            currentFolderList.Clear();
            EnableButtons(true);
        }
    }
    void OnContentChanged(object sender,RoutedEventArgs e) {
        Debug.WriteLine("Event:" + e + ", Source=" + e.Source);
        var selectionArg = e as SelectionChangedEventArgs;
        EnableButtons(selectionArg == null || selectionArg.RemovedItems.Count > 0);
        if (selectionArg != null){
            Debug.WriteLine("Add Items = " + selectionArg.AddedItems.Count);
            if (selectionArg.AddedItems.Count > 0)
                Debug.WriteLine("\tFirst Item: " + selectionArg.AddedItems[0]);
            Debug.WriteLine("Remove Items = " + selectionArg.RemovedItems.Count);
            if (selectionArg.RemovedItems.Count > 0)
                Debug.WriteLine("\tFirst Item: " + selectionArg.RemovedItems[0]);
        }
    }
    void OnSave(object sender, RoutedEventArgs e){
        StoreUiToSettings();
        AppDeps.Settings.Save();
        DialogResult = true;
    }
    void onAddExcludedFolder(object sender, RoutedEventArgs e){
        SelectAndAddFolder(InclusionMode.Exclude);
    }
    void OnAddSingleFolder(object sender, RoutedEventArgs e){
        SelectAndAddFolder(InclusionMode.Single);
    }
    void onAddRecursiveFolder(object sender, RoutedEventArgs e){
        SelectAndAddFolder(InclusionMode.Recursive);
    }
    void onKeyPressedInGrid(object sender, KeyEventArgs e){
        if (e.Key == Key.Delete && currentFolderListView.SelectedItems.Count > 0){
            e.Handled = true;
            var selectedFolders = currentFolderListView.SelectedItems.CastToArray<FolderInclusion>();
            foreach (var folder in selectedFolders){
                currentFolderListView.Items.Remove(folder);
                currentFolderList.Remove(folder);
            }
            EnableButtons(true);
        }
    }
    void onSelectBackground(object sender, RoutedEventArgs e){
        var fileDialog = new Microsoft.Win32.OpenFileDialog{
            InitialDirectory = backgroundPicture.Text,
            Title = "Browse background picture file",
            CheckFileExists = true,
            DefaultExt = "jpg",
            Filter = "JPEG|*.jpg;*.jpeg|GIF|*.gif|PNG|*.png|Bitmap|*.bmp|TIFF|*.tiff|All supported files|*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tiff"
        };
        var result = fileDialog.ShowDialog();
        if ((bool)result && !fileDialog.FileName.Equals(backgroundPicture.Text, StringComparison.OrdinalIgnoreCase)){
            backgroundPicture.Text = fileDialog.FileName;
            EnableButtons(true);
        }
    }
    void onTextChanged(object sender, TextChangedEventArgs e){
        EnableButtons(true);
    }

    #endregion

    readonly FolderBrowserDialog folderDialog = new() {
        Description = "Select a folder containing any picture.",
        ShowNewFolderButton = false,
    };
    void OnChangeSlideMode(object sender, SelectionChangedEventArgs e){
        AppDeps.Settings.Value.LastShownIndex = 0;
        OnContentChanged(sender, e);
    }
}