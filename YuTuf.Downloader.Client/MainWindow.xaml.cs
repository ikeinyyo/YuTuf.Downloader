using HtmlAgilityPack;
using MyToolkit.Multimedia;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using WPFFolderBrowser;
using YuTuf.Downloader.Client.Properties;

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
            DestinationPath.Text = destinationPath = Settings.Default.LastDestinationFolder;
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
                Settings.Default.LastDestinationFolder = destinationPath;
                Settings.Default.Save();
            }
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            DoDownloadVideo();
        }

        private void DoDownloadVideo()
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                MessageBox.Show("Destination Path is required");
                return;
            }

            if (!string.IsNullOrWhiteSpace(VideoUrl.Text))
            {
                var url = VideoUrl.Text;
                VideoUrl.Text = string.Empty;
                Task.Run(() => DownloadVideo(url));
            }
            else
            {
                MessageBox.Show("Video Url is required");
            }
        }

        private async Task DownloadVideo(string url)
        {
            try
            {
                var title = GetVideoTitle(url);
                var id = url.Substring(url.IndexOf("watch?v=")).Replace("watch?v=", "");
                var thumbnail = YouTube.GetThumbnailUri(id, YouTubeThumbnailSize.Small);
                var youtubeVideo = await YouTube.GetVideoUriAsync(id, YouTubeQuality.QualityHigh);
                var video = new Video() { Thumbnail = thumbnail.OriginalString, Progress = 0, Title = title };

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
                    client.DownloadFileAsync(youtubeVideo.Uri, Path.Combine(destinationPath, $"{video.Title}.mp4"));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Debug.Write(ex.Message);
            }
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoDownloadVideo();
            }
        }

        private string GetVideoTitle(string url)
        {
            using (var client = new WebClient())
            {
                try
                {
                    var html = client.DownloadString(url);
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(html);
                    var node = document.GetElementbyId("eow-title");
                    return node.InnerText.Replace("\n", string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                }
            }

            return string.Empty;
        }
    }

    public class Video : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        private int progress;
        public int Progress
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
