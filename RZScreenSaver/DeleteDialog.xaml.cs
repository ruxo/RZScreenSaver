using System.Windows;
using System.Windows.Media;

namespace RZScreenSaver {
    /// <summary>
    /// Interaction logic for DeleteDialog.xaml
    /// </summary>
    public partial class DeleteDialog{
        public DeleteDialog() {
            InitializeComponent();
        }
        public DeleteDialog(ImageSource picture, string pictureName) : this() {
            previewPicture.Source = picture;
            this.pictureName.Text = pictureName;
        }
        public bool MoveFileNeeded{ get; private set; }
        void onOk(object sender, RoutedEventArgs e){
            DialogResult = true;
        }
        void onMoveFile(object sender, RoutedEventArgs e){
            MoveFileNeeded = true;
            DialogResult = true;
        }
    }
}
