using MyToolkit.Multimedia;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
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
                Download.IsEnabled = true;
            }
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(VideoUrl.Text))
            {
                var url = VideoUrl.Text;
                Task.Run(() => DownloadVideo(url));
            }
            else
            {
                MessageBox.Show("Video Url can't be empty");
            }
        }

        private async Task DownloadVideo(string url)
        {
            try
            {
                var id = url.Substring(url.IndexOf("watch?v=")).Replace("watch?v=", "");
                var thumbnail = YouTube.GetThumbnailUri(id, YouTubeThumbnailSize.Small);
                var video = await YouTube.GetVideoUriAsync(id, YouTubeQuality.QualityHigh);

                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    videos.Add(new Video() { Thumbnail = thumbnail.OriginalString, IsDownloaded = false });
                }));
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }
    }

    public class Video
    {
        public string Thumbnail { get; set; }
        public bool IsDownloaded { get; set; }
    }
}
