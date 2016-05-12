using MyToolkit.Multimedia;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
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
                var youtubeVideo = await YouTube.GetVideoUriAsync(id, YouTubeQuality.QualityHigh);
                var video = new Video() { Thumbnail = thumbnail.OriginalString, Progress = 0 };

                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    videos.Add(video);
                }));

                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        video.Progress = e.ProgressPercentage;
                    };
                    client.DownloadFileAsync(youtubeVideo.Uri, Path.Combine(destinationPath, "video.mp4"));
                }

            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public class Video : INotifyPropertyChanged
    {
        public string Thumbnail { get; set; }
        private float progress;
        public float Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
                NotifyPropertyChanged("Progress");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
