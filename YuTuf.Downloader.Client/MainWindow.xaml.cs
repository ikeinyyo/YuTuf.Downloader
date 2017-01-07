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
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Windows.Threading;

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
                var isVideo = VideoContent.IsChecked.HasValue && VideoContent.IsChecked.Value;
                Task.Run(() => DownloadVideo(url, isVideo));
            }
            else
            {
                MessageBox.Show("Video Url is required");
            }
        }

        private async Task DownloadVideo(string url, bool isVideo)
        {
            try
            {
                var title = GetVideoTitle(url);
                var id = url.Substring(url.IndexOf("watch?v=")).Replace("watch?v=", "");
                var thumbnail = YouTube.GetThumbnailUri(id, YouTubeThumbnailSize.Small);
                var results = await YouTube.GetUrisAsync(id);

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
                    var file = isVideo ? results
                        .Where(result => result.HasVideo && result.HasAudio)
                        .OrderByDescending(result => result.VideoQuality)
                        .FirstOrDefault() :
                        results.Where(result => result.HasAudio && !result.HasVideo)
                        .OrderByDescending(result => result.AudioQuality)
                        .FirstOrDefault();

                    if (file == null)
                    {
                        MessageBox.Show("No video or audi available");
                        return;
                    }

                    var path = Path.Combine(destinationPath, isVideo ? $"{video.Title}.mp4" : $"{video.Title}.mp3");
                    client.DownloadFileAsync(file.Uri, path);
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
                    client.Encoding = Encoding.UTF8;
                    var html = client.DownloadString(url);
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(html);
                    var node = document.GetElementbyId("eow-title");
                    var name = node.InnerText.Replace("\n", string.Empty);
                    return ClearFileName(name);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                }
            }

            return string.Empty;
        }

        private string ClearFileName(string path)
        {
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                path = path.Replace(character, '_');
            }
            return path;
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
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName]string info = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
