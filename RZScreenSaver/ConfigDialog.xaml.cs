using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using RZScreenSaver.Properties;
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
    #region ctors
    public ConfigDialog() {
        InitializeComponent();
        defaultRadioButtons = [imageFolderDefault1, imageFolderDefault2, imageFolderDefault3, imageFolderDefault4];
        Debug.Assert(defaultRadioButtons.Length == NumberOfPictureSet);
        imageFolderList = [imageFoldersList1, imageFoldersList2, imageFoldersList3, imageFoldersList4];
        Debug.Assert(imageFolderList.Length == NumberOfPictureSet);

        // enable only there are more than 1 monitor.
        asMixedMode.IsEnabled = Screen.AllScreens.Length > 1;

        slideDelayInput.ValueChanged += delegate { enableButtons(true); };
        LoadSettingsToUi();
        enableButtons(false);
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
        slideDelayInput.Value = Settings.Default.SlideShowDelay;
        slideModeList.SelectedItem = Settings.Default.SlideMode;
        displayModeList.SelectedItem = Settings.Default.DisplayMode;
        folderDialog.SelectedPath = Settings.Default.LastSelectedFolder;

        folderSet = Settings.Default.PicturePaths ?? new FolderCollectionSet();
        folderSet.SelectedIndex = Settings.Default.PictureSetSelected;
        for(int setIndex=0; setIndex < NumberOfPictureSet; ++setIndex)
            if (setIndex >= folderSet.Count)
                folderSet.Add(new FolderCollection());
            else
                foreach(var item in folderSet[setIndex])
                    imageFolderList[setIndex].Items.Add(item);
        Debug.Assert(folderSet.SelectedIndex < NumberOfPictureSet);
        defaultRadioButtons[folderSet.SelectedIndex].IsChecked = true;

        LoadUiSaverMode(Settings.Default.SaverMode);
        backgroundPicture.Text = Settings.Default.BackgroundPicturePath;
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
                Debug.Write(Settings.Default.SaverMode);
                Debug.WriteLine(" is not handled!!");
                break;
        }
    }
    void storeUiToSettings(){
        var defaultSetIndex = Array.FindIndex(defaultRadioButtons, radio => radio.IsChecked != null && radio.IsChecked.Value);
        Debug.Assert(defaultSetIndex != -1);

        folderSet.SelectedIndex = defaultSetIndex;

        Settings.Default.SlideShowDelay = (int) slideDelayInput.Value;
        Settings.Default.SlideMode = (SlideMode) slideModeList.SelectedItem;
        Settings.Default.DisplayMode = (DisplayMode) displayModeList.SelectedItem;
        Settings.Default.PicturePaths = folderSet;
        Settings.Default.LastSelectedFolder = folderDialog.SelectedPath;
        Settings.Default.SaverMode = (bool) asSlideShowMode.IsChecked
                                         ? SaverMode.SlideShow
                                         : (bool) asPhotoCollageMode.IsChecked ? SaverMode.PhotoCollage : SaverMode.Mixed;
        Settings.Default.PictureSetSelected = folderSet.SelectedIndex;
        Settings.Default.BackgroundPicturePath = backgroundPicture.Text;
    }
    #endregion

    void enableButtons(bool enable){
        if (okButton != null)
            okButton.IsEnabled = enable;
    }
    void SelectAndAddFolder(InclusionMode mode){
        if (folderDialog.ShowDialog(new Winform32Helper(this)) == System.Windows.Forms.DialogResult.OK){
            if (!currentFolderList.Contains(folderDialog.SelectedPath)){
                var folder = currentFolderList.Add(folderDialog.SelectedPath, mode);
                currentFolderListView.Items.Add(folder);
            }
            enableButtons(true);
        }
    }

    #region Event Handlers

    void onClearList(object sender, RoutedEventArgs e){
        if (currentFolderList.Count > 0
         && MessageBox.Show("Are you sure to clear the list?", "Clear folder list",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes){
            currentFolderListView.Items.Clear();
            currentFolderList.Clear();
            enableButtons(true);
        }
    }
    void onContentChanged(object sender,RoutedEventArgs e) {
        Debug.WriteLine("Event:" + e + ", Source=" + e.Source);
        var selectionArg = e as SelectionChangedEventArgs;
        enableButtons(selectionArg == null || selectionArg.RemovedItems.Count > 0);
        if (selectionArg != null){
            Debug.WriteLine("Add Items = " + selectionArg.AddedItems.Count);
            if (selectionArg.AddedItems.Count > 0)
                Debug.WriteLine("\tFirst Item: " + selectionArg.AddedItems[0]);
            Debug.WriteLine("Remove Items = " + selectionArg.RemovedItems.Count);
            if (selectionArg.RemovedItems.Count > 0)
                Debug.WriteLine("\tFirst Item: " + selectionArg.RemovedItems[0]);
        }
    }
    void onSave(object sender, RoutedEventArgs e){
        storeUiToSettings();
        Settings.Default.Save();
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
            foreach (FolderInclusion folder in selectedFolders){
                currentFolderListView.Items.Remove(folder);
                currentFolderList.Remove(folder);
            }
            enableButtons(true);
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
            enableButtons(true);
        }
    }
    void onTextChanged(object sender, TextChangedEventArgs e){
        enableButtons(true);
    }

    #endregion

    readonly RadioButton[] defaultRadioButtons;
    readonly ListView[] imageFolderList;
    FolderCollectionSet folderSet;
    GlassHelper glassMaker;
    readonly FolderBrowserDialog folderDialog = new FolderBrowserDialog{
        Description = "Select a folder containing any picture.",
        ShowNewFolderButton = false,
    };
    void onChangeSlideMode(object sender, SelectionChangedEventArgs e){
        Settings.Default.LastShownIndex = 0;
        onContentChanged(sender, e);
    }
}