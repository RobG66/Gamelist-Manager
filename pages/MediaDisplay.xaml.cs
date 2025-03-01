using GamelistManager.classes;
using GamelistManager.controls;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private string _currentRomPath;
        
        public MediaDisplay()
        {
            InitializeComponent();
            _libVLC = new LibVLC("--input-repeat=2");
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _videoView = new VideoView();
            _currentRomPath = string.Empty;
        }



        public void ShowMedia(DataRowView selectedRow)
        {
            if (selectedRow == null)
            {
                return;
            }

            if (MediaContentGrid.ColumnDefinitions.Count == 0)
            {
                SetupGrid();
            }

            ClearAllImages();
            StopPlaying();
          
            _currentRomPath = selectedRow["Rom Path"].ToString()!;

           string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            foreach (var column in MediaContentGrid.ColumnDefinitions)
            {
                string columnName = column.Name.Replace("__"," ");
                int columnIndex = MediaContentGrid.ColumnDefinitions.IndexOf(column);

                if (columnName == "Video")
                {                    
                    var videoCellValue = selectedRow["Video"];
                    string? videoPath = videoCellValue == null || videoCellValue == DBNull.Value
                        ? string.Empty
                        : videoCellValue.ToString();

                    if (!string.IsNullOrEmpty(videoPath))
                    {
                        MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);
                        string videoFilePath = Path.Combine(parentFolderPath!, videoPath.TrimStart('.', '/').Replace('/', '\\'));
                        DisplayVideo(videoFilePath);
                    }
                    else
                    {                        
                        MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                    }
                    continue;
                }

                var cellValue = selectedRow[columnName];

                string? pathValue = cellValue == null || cellValue == DBNull.Value
                    ? string.Empty
                    : cellValue.ToString();

                if (!string.IsNullOrEmpty(pathValue))
                {
                    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);

                    if (columnName == "Manual")
                    {
                        pathValue = "pack://application:,,,/Resources/manual.png";
                    }
                    else
                    {
                        pathValue = Path.Combine(parentFolderPath!, pathValue.TrimStart('.', '/').Replace('/', '\\'));
                    }
                    DisplayImage(pathValue, columnName);
                }
                else
                {
                    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                }
            }
        }

        public void DisplayVideo(string videoPath)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            _mediaPlayer.Media?.Dispose();

            var media = new Media(_libVLC, new Uri(videoPath));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

        }

        public void DisplayImage(string imagePath, string columnName)
        {
         
            BitmapImage imageSource;
            if (imagePath.StartsWith("pack://"))
            {
                // Resource URI
                imageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }
            else
            {
                string missingImagePath = "pack://application:,,,/Resources/missing.png";

                if (File.Exists(imagePath))
                {
                    // Load the image from file
                    imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.UriSource = new Uri(imagePath, UriKind.Absolute);
                    imageSource.CacheOption = BitmapCacheOption.OnLoad; // Ensure the file is fully loaded and unlocked
                    imageSource.EndInit();
                }
                else
                {
                    // Use the missing image
                    imageSource = new BitmapImage(new Uri(missingImagePath, UriKind.Absolute));
                }
            }

            var contentControl = MediaContentGrid.Children
                 .OfType<ContentControl>()
                 .FirstOrDefault(cc => Grid.GetRow(cc) == 1
                  && cc.Name == columnName);

            if (contentControl != null && contentControl.Content is Image image)
            {
                // Update the image source
                image.Source = imageSource;
            }
        }

        private ContentControl CreateTextBlockContentControl(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkSlateGray),
                Margin = new Thickness(0, 2, 0, 2) // Add margin to text block
            };

            return new ContentControl
            {
                Content = textBlock,
                Margin = new Thickness(10) // Add margin around text block container
            };
        }

        private ContentControl CreateImageContentControl()
        {
            var image = new System.Windows.Controls.Image
            {
                MaxWidth = 400,
                MaxHeight = 300,
                Stretch = Stretch.Uniform,
                Tag = string.Empty,
                Source = null // No initial image source
            };

            return new ContentControl
            {
                Content = image,
                Margin = new Thickness(10),
            };
        }

        private ContentControl CreateVideoContentControl()
        {
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            // Setup Video View
            _videoView = new VideoView
            {
                MediaPlayer = _mediaPlayer,
                Width = 400,
                Height = 300
            };

            return new ContentControl
            {
                Content = _videoView,
                Margin = new Thickness(10)
            };

        }


        public void StopPlaying()
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
        }

        public void ClearAllImages()
        {
            foreach (UIElement element in MediaContentGrid.Children)
            {
                if (element is ContentControl contentControl)
                {
                    // Check if the control is in row 1
                    if (Grid.GetRow(contentControl) == 1)
                    {
                        if (contentControl.Content is Image image)
                        {
                            // Clear the image source
                            image.Source = null;
                        }
                    }
                }
            }
        }
        

        private void SetupGrid()
        {
            var metaDataDictionary = GamelistMetaData.GetMetaDataDictionary();
            var mediaItems = metaDataDictionary
                .Where(entry => entry.Value.DataType == MetaDataType.Image)
                .Select(entry => entry.Value.Name)
                .ToList();

            mediaItems.Add("Manual");
            mediaItems.Add("Video");

            int columnIndex = 0;

            foreach (var item in mediaItems)
            {
                string columnName = item;
                var newColumn = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    Name = columnName.Replace(" ", "__")
                };

                MediaContentGrid.ColumnDefinitions.Add(newColumn);

                ContentControl textboxContentControl = CreateTextBlockContentControl(columnName);
                Grid.SetRow(textboxContentControl, 0);
                Grid.SetColumn(textboxContentControl, columnIndex);
                MediaContentGrid.Children.Add(textboxContentControl);

                ContentControl contentControl;
                if (item == "Video")
                {
                    contentControl = CreateVideoContentControl();
                }
                else
                {
                    contentControl = CreateImageContentControl();
                }
                contentControl.Name = columnName;
                Grid.SetRow(contentControl, 1);
                Grid.SetColumn(contentControl, columnIndex);
                MediaContentGrid.Children.Add(contentControl);
               
                columnIndex++;
            }
        }
    }
}

