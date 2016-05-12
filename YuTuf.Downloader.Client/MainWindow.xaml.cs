using System.Collections.ObjectModel;
using System.Windows;
using WPFFolderBrowser;

namespace YuTuf.Downloader.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Video> videos;
        private string destinationPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnMainWindowsLoaded;
        }

        private void OnMainWindowsLoaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        #region Private Methods
        private void Initialize()
        {
            Download.IsEnabled = false;
            videos = new ObservableCollection<Video>();
            Videos.ItemsSource = videos;
        }
        #endregion

        private void OnSelectFolderClick(object sender, RoutedEventArgs e)
        {

            WPFFolderBrowserDialog folderDialog = new WPFFolderBrowserDialog("Select destination folder");
            var result = folderDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                destinationPath = folderDialog.FileName;
                DestinationPath.Text = destinationPath;
                SelectFolder.IsEnabled = false;
            }

        }
    }

    public class Video
    {
        public string Title { get; set; }
        public bool IsDownloaded { get; set; }
    }
}
