using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace GamelistManager.pages
{
    /// <summary>
    /// Interaction logic for MediaDisplay.xaml
    /// </summary>
    public partial class MediaDisplay : Page
    {

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private VideoView _videoView;

        public MediaDisplay()
        {
            InitializeComponent();

            _libVLC = new LibVLC("--input-repeat=2");
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _videoView = new VideoView();
        }

        public void FadeIn()
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)) // Set the duration of the fade-in animation
            };

            this.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }

        public void ClearContent()
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC); // Reinitialize _mediaPlayer

            if (_videoView != null)
            {
                _videoView.Dispose();
                _videoView = null!;
            }

            MediaContentGrid.Children.Clear();
            MediaContentGrid.ColumnDefinitions.Clear();
        }

        public void DisplayImage(string imagePath, string name)
        {

            // Define columns and rows
            MediaContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            MediaContentGrid.RowDefinitions.Clear();
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Text row
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Image row

            BitmapImage imageSource;
            if (imagePath.StartsWith("pack://"))
            {
                // Resource URI
                imageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }
            else
            {
                // File path
                string missingImagePath = "pack://application:,,,/Resources/missing.png";
                imageSource = File.Exists(imagePath)
                    ? new BitmapImage(new Uri(imagePath, UriKind.Absolute))
                    : new BitmapImage(new Uri(missingImagePath, UriKind.Absolute));
            }

            var image = new System.Windows.Controls.Image
            {
                Source = imageSource,
                MaxWidth = 400,
                MaxHeight = 300,
                Stretch = Stretch.Uniform
            };

            if (!string.IsNullOrEmpty(name))
            {
                var textBlock = new TextBlock
                {
                    Text = name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkSlateGray),
                    Margin = new Thickness(0, 2, 0, 2) // Add margin to text block
                };

                var textBlockCell = new ContentControl
                {
                    Content = textBlock,
                    Margin = new Thickness(10) // Add margin around text block container
                };

                Grid.SetRow(textBlockCell, 0);
                Grid.SetColumn(textBlockCell, MediaContentGrid.ColumnDefinitions.Count - 1);
                MediaContentGrid.Children.Add(textBlockCell);
            }

            var imageCell = new ContentControl
            {
                Content = image,
                Margin = new Thickness(10) // Add margin around image container
            };
            Grid.SetRow(imageCell, 1);
            Grid.SetColumn(imageCell, MediaContentGrid.ColumnDefinitions.Count - 1);
            MediaContentGrid.Children.Add(imageCell);
        }
        public void DisplayVideo(string videoPath, string name)
        {
            // Define columns and rows
            MediaContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            MediaContentGrid.RowDefinitions.Clear();
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Text row
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Video/Image row

            var textBlock = new TextBlock
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkSlateGray),
                Margin = new Thickness(0, 2, 0, 2) // Add margin to text block
            };

            var contentCell = new ContentControl
            {
                Margin = new Thickness(10) // Add margin around content container
            };

            string missingVideoImagePath = "pack://application:,,,/Resources/missing.png";
            if (File.Exists(videoPath))
            {
                // Setup Video View
                _videoView = new VideoView
                {
                    MediaPlayer = _mediaPlayer,
                    Width = 400,
                    Height = 300
                };

                var media = new Media(_libVLC, new Uri(videoPath));
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();

                contentCell.Content = _videoView;
            }
            else
            {
                // Display missing image if video file is not found
                var missingImage = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(missingVideoImagePath, UriKind.Absolute)),
                    MaxWidth = 400,
                    MaxHeight = 300,
                    Stretch = Stretch.Uniform
                };

                contentCell.Content = missingImage;
            }

            // Add text block and content to grid
            var textBlockCell = new ContentControl
            {
                Content = textBlock,
                Margin = new Thickness(10) // Add margin around text block container
            };
            Grid.SetRow(textBlockCell, 0);
            Grid.SetColumn(textBlockCell, MediaContentGrid.ColumnDefinitions.Count - 1);
            MediaContentGrid.Children.Add(textBlockCell);

            Grid.SetRow(contentCell, 1);
            Grid.SetColumn(contentCell, MediaContentGrid.ColumnDefinitions.Count - 1);
            MediaContentGrid.Children.Add(contentCell);
        }

        public void StopPlaying()
        {
            _mediaPlayer.Stop();
            _mediaPlayer?.Dispose();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC); // Reinitialize _mediaPlayer

            if (_videoView != null)
            {
                _videoView.Dispose();
                _videoView = null!;
            }
        }

    }
}
