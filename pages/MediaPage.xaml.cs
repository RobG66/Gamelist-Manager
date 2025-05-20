using GamelistManager.classes;
using GamelistManager.controls;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GamelistManager.pages
{
    public partial class MediaPage : Page
    {
        MediaPlayerControl _mediaPlayerControl;
        List<string> _mediaNames;

        public MediaPage()
        {
            InitializeComponent();
            _mediaPlayerControl = new MediaPlayerControl();
           
            _mediaPlayerControl.SetVolume((int)Properties.Settings.Default.Volume);

            var metaDataDictionary = GamelistMetaData.GetMetaDataDictionary();
            _mediaNames = metaDataDictionary
                .Where(entry => entry.Value.DataType == MetaDataType.Image)
                .Select(entry => entry.Value.Name)
                .ToList();
            _mediaNames.Add("Manual");
            _mediaNames.Add("Video");
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

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            foreach (var column in MediaContentGrid.ColumnDefinitions)
            {
                string columnName = column.Name.Replace("__", " ");
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
                        PlayFile(videoFilePath);
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
                    pathValue = Path.Combine(parentFolderPath!, pathValue.TrimStart('.', '/').Replace('/', '\\'));
                    DisplayItem(pathValue, columnName);
                }
                else
                {
                    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                }
            }
        }

        public void PlayFile(string fileName)
        {
            StopPlaying();
            bool autoPlay = Properties.Settings.Default.VideoAutoplay;
            _mediaPlayerControl.PlayMedia(fileName, autoPlay);
        }

        public void StopPlaying()
        {
            if (_mediaPlayerControl != null)
            {
                _mediaPlayerControl.StopPlaying();
            }
        }


        public void DisplayItem(string? filePath, string columnName)
        {
            BitmapImage imageSource;

            if (columnName == "Manual")
            {
                string resourcePdfIcon = "pack://application:,,,/Resources/manual.png";
                imageSource = new BitmapImage(new Uri(resourcePdfIcon, UriKind.Absolute));
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    string resourceMissingIcon = "pack://application:,,,/Resources/missing.png";
                    imageSource = new BitmapImage(new Uri(resourceMissingIcon, UriKind.Absolute));
                    filePath = null;
                }
                else
                {
                    imageSource = new BitmapImage(new Uri(filePath, UriKind.Absolute));
                }
            }

            //        imageSource = new BitmapImage();
            //        imageSource.BeginInit();
            //        imageSource.UriSource = new Uri(filePath, UriKind.Absolute);
            //        imageSource.CacheOption = BitmapCacheOption.OnLoad; // Ensure the file is fully loaded and unlocked
            //        imageSource.EndInit();


            int index = _mediaNames.FindIndex(item => item.Contains(columnName));

            var image = MediaContentGrid.Children
                .OfType<System.Windows.Controls.Image>()
                .FirstOrDefault(img => Grid.GetRow(img) == 1 && Grid.GetColumn(img) == index);

            if (image != null)
            {
                image.Source = imageSource;
                image.Tag = filePath;
            }

        }

        public void ClearAllImages()
        {
            foreach (UIElement element in MediaContentGrid.Children)
            {
                if (element is System.Windows.Controls.Image image)
                {
                    // Check if the control is in row 1
                    if (Grid.GetRow(element) == 1)
                    {
                        image.Source = null;
                    }
                }
            }
        }

        private void SetupGrid()
        {
            int columnIndex = 0;

            foreach (var item in _mediaNames)
            {
                string columnName = item;
                var newColumn = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    Name = columnName.Replace(" ", "__")
                };

                MediaContentGrid.ColumnDefinitions.Add(newColumn);

                TextBlock textBlock = new TextBlock
                {
                    Text = columnName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkSlateGray),
                    Margin = new Thickness(0, 2, 0, 2) // Add margin to text block
                };

                Grid.SetRow(textBlock, 0);
                Grid.SetColumn(textBlock, columnIndex);
                MediaContentGrid.Children.Add(textBlock);

                if (item == "Video")
                {
                    var container = new System.Windows.Controls.Border
                    {
                        Child = _mediaPlayerControl,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = System.Windows.Media.Colors.Black,
                            ShadowDepth = 10,
                            Direction = 315,
                            BlurRadius = 15,
                            Opacity = 0.7
                        },
                        Margin = new Thickness(20)
                    };
                    Grid.SetRow(container, 1);
                    Grid.SetColumn(container, columnIndex);
                    MediaContentGrid.Children.Add(container);
                }
                else
                {
                    var image = new System.Windows.Controls.Image
                    {
                        MaxWidth = 400,
                        MaxHeight = 300,
                        Margin = new Thickness(20),
                        Stretch = Stretch.Uniform,
                        Tag = columnName,
                        Source = null, // No initial image source
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = System.Windows.Media.Colors.Gray,   // Softer shadow color
                            ShadowDepth = 10,                           // Increase shadow distance
                            Direction = 320,                            // Angle slightly adjusted
                            BlurRadius = 20,                            // More diffuse shadow
                            Opacity = 0.7                               // Slightly stronger shadow visibility
                        },
                        RenderTransform = new System.Windows.Media.ScaleTransform
                        {
                            ScaleX = 1.05,                              // Slight scale-up
                            ScaleY = 1.05
                        },
                        RenderTransformOrigin = new System.Windows.Point(0.5, 0.5) // Transform from center
                    };
                    Grid.SetRow(image, 1);
                    Grid.SetColumn(image, columnIndex);
                    MediaContentGrid.Children.Add(image);

                    ContextMenu contextMenu = ContextMenuHelper.CreateContextMenu();
                    image.ContextMenu = contextMenu;
                }

                columnIndex++;
            }
        }

        public void PausePlaying()
        {
            if (_mediaPlayerControl != null)
            {
                _mediaPlayerControl.PausePlaying();
            }
        }
    }
}

