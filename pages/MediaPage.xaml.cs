using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
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

            bool nomedia = true;
            // Reset grid row 0 to default state
            MediaContentGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);

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
                        nomedia = false;
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
                    nomedia = false;
                    DisplayItem(pathValue, columnName);
                }
                else
                {
                    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                }
            }

            if (nomedia)
            {
                var imageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/nomedia.png", UriKind.Absolute));

                MediaContentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                MediaContentGrid.RowDefinitions[0].Height = new GridLength(0);

                var image = MediaContentGrid.Children
                .OfType<System.Windows.Controls.Image>()
                .FirstOrDefault(img => Grid.GetRow(img) == 1 && Grid.GetColumn(img) == 0);

                if (image != null)
                {
                    image.Source = imageSource;
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


        public void DisplayItem(string filePath, string columnName)
        {
            ImageSource? imageSource = null;

            // Determine which image to load
            if (columnName == "Manual")
            {
                string resourcePdfIcon = "pack://application:,,,/Resources/icons/manual.png";
                imageSource = new BitmapImage(new Uri(resourcePdfIcon, UriKind.Absolute));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    string resourceMissingIcon = "pack://application:,,,/Resources/missing.png";
                    imageSource = new BitmapImage(new Uri(resourceMissingIcon, UriKind.Absolute));
                    filePath = null!;
                }
                else
                {
                    imageSource = ImageHelper.LoadImageWithoutLock(filePath);

                    // Fallback to "missing" image if loading failed (e.g., corrupt)
                    if (imageSource == null)
                    {
                        string resourceMissingIcon = "pack://application:,,,/Resources/missing.png";
                        imageSource = new BitmapImage(new Uri(resourceMissingIcon, UriKind.Absolute));
                        filePath = null!;
                    }
                }
            }

            // Get the image control at the target column
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

                TextBlock textBlock = new()
                {
                    Text = columnName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                // Set Foreground as DynamicResource so it updates with theme changes
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

                Grid.SetRow(textBlock, 0);
                Grid.SetColumn(textBlock, columnIndex);
                MediaContentGrid.Children.Add(textBlock);

                if (item == "Video")
                {
                    // Get theme-aware shadow color
                    var shadowColor = TryFindResource("ShadowColor") as Color?
                        ?? Colors.Black;

                    var container = new System.Windows.Controls.Border
                    {
                        Child = _mediaPlayerControl,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = shadowColor,
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
                    // Get theme-aware shadow color
                    var shadowColor = TryFindResource("ShadowColor") as Color?
                        ?? Colors.Gray;

                    var image = new System.Windows.Controls.Image
                    {
                        MaxWidth = 400,
                        MaxHeight = 300,
                        Margin = new Thickness(20),
                        Stretch = Stretch.Uniform,
                        Tag = columnName,
                        Source = null,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = shadowColor,
                            ShadowDepth = 10,
                            Direction = 320,
                            BlurRadius = 20,
                            Opacity = 0.7
                        },
                        RenderTransform = new System.Windows.Media.ScaleTransform
                        {
                            ScaleX = 1.05,
                            ScaleY = 1.05
                        },
                        RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
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