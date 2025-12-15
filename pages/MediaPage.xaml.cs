using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
using GamelistManager.controls;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace GamelistManager.pages
{
    public partial class MediaPage : Page
    {
        private MediaPlayerControl? _mediaPlayerControl;
        private MediaPlayerControl? _previewMediaPlayer;
        private List<string> _mediaNames;
        private Window? _previewWindow;
        private string? _currentVideoPath;
        private bool _wasVideoPlaying;

        private const double PREVIEW_MAX_WIDTH_RATIO = 0.7;
        private const double PREVIEW_MAX_HEIGHT_RATIO = 0.7;
        private const int FADE_DURATION_MS = 250;
        private const double ZOOM_START_SCALE = 0.95;
        private const double ZOOM_END_SCALE = 1.0;

        private Grid MediaContentGrid;
        private bool _scaledDisplay;

        public MediaPage()
        {
            InitializeComponent();

            MediaContentGrid = new Grid
            {
                Background = (Brush)FindResource("SecondaryBackgroundBrush")
            };
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            MediaContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _mediaPlayerControl = new MediaPlayerControl();
            _mediaPlayerControl.SetVolume((int)Properties.Settings.Default.Volume);

            var metaDataDictionary = GamelistMetaData.GetMetaDataDictionary();
            _mediaNames = metaDataDictionary
                .Where(entry => entry.Value.DataType == MetaDataType.Image)
                .Select(entry => entry.Value.Name)
                .ToList();
            _mediaNames.Add("Manual");
            _mediaNames.Add("Video");

            _scaledDisplay = Properties.Settings.Default.Scaling;

            if (_scaledDisplay)
            {
                ScrollModeScrollViewer.Visibility = Visibility.Collapsed;
                ScaledModeViewbox.Visibility = Visibility.Visible;
                ScaledContentHost.Children.Clear();
                ScaledContentHost.Children.Add(MediaContentGrid);
            }
            else
            {
                ScaledModeViewbox.Visibility = Visibility.Collapsed;
                ScrollModeScrollViewer.Visibility = Visibility.Visible;
                ScrollContentHost.Children.Clear();
                ScrollContentHost.Children.Add(MediaContentGrid);
            }

            Button_ToggleMode.Content = _scaledDisplay ? "Fit To View: On" : "Fit To View: Off";

        }

        public void ShowMedia(DataRowView selectedRow)
        {
            if (selectedRow == null) return;

            if (MediaContentGrid.ColumnDefinitions.Count == 0) SetupGrid();

            ClearAllImages();
            StopPlaying();

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;
            bool nomedia = true;

            foreach (var column in MediaContentGrid.ColumnDefinitions)
            {
                string columnName = column.Name.Replace("__", " ");
                int columnIndex = MediaContentGrid.ColumnDefinitions.IndexOf(column);

                if (columnName == "Video")
                {
                    var videoCellValue = selectedRow["Video"];
                    string? videoPath = videoCellValue == null || videoCellValue == DBNull.Value
                        ? null
                        : videoCellValue.ToString();

                    if (!string.IsNullOrEmpty(videoPath))
                    {
                        string fullPath = PathHelper.ConvertGamelistPathToFullPath(videoPath, parentFolderPath);
                        column.Width = new GridLength(1, GridUnitType.Star);
                        PlayFile(fullPath);
                        nomedia = false;
                    }
                    else
                    {
                        column.Width = new GridLength(0);
                    }
                    continue;
                }

                var cellValue = selectedRow[columnName];
                string? imagePath = cellValue == null || cellValue == DBNull.Value ? null : cellValue.ToString();

                if (!string.IsNullOrEmpty(imagePath))
                {
                    string fullPath = PathHelper.ConvertGamelistPathToFullPath(imagePath, parentFolderPath);
                    column.Width = new GridLength(1, GridUnitType.Star);
                    DisplayItem(fullPath, columnName);
                    nomedia = false;
                }
                else
                {
                    column.Width = new GridLength(0);
                }
            }

            if (nomedia)
            {
                var image = MediaContentGrid.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == 0)?
                    .Child as Image;

                if (image != null)
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/nomedia.png"));
            }
        }

        public void PlayFile(string fileName)
        {
            StopPlaying();
            _currentVideoPath = fileName;

            var videoContainer = MediaContentGrid.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Child == _mediaPlayerControl);

            if (videoContainer != null)
            {
                videoContainer.Tag = fileName;
                int columnIndex = MediaContentGrid.ColumnDefinitions.IndexOf(
                    MediaContentGrid.ColumnDefinitions.First(c => c.Name == "Video"));

                SetButtonTags(columnIndex, fileName);
            }

            _mediaPlayerControl?.PlayMedia(fileName, Properties.Settings.Default.VideoAutoplay);
        }

        public void StopPlaying() => _mediaPlayerControl?.StopPlaying();
        public void PausePlaying() => _mediaPlayerControl?.PausePlaying();
        public void ResumePlaying() => _mediaPlayerControl?.ResumePlaying();

        private void ToggleScaling()
        {
            _scaledDisplay = !_scaledDisplay;
            Properties.Settings.Default.Scaling = _scaledDisplay;
            Properties.Settings.Default.Save();

            _mediaPlayerControl?.StopPlaying();

            if (MediaContentGrid.Parent is Panel currentParent)
                currentParent.Children.Remove(MediaContentGrid);

            if (_scaledDisplay)
            {
                ScrollModeScrollViewer.Visibility = Visibility.Collapsed;
                ScaledModeViewbox.Visibility = Visibility.Visible;
                ScaledContentHost.Children.Clear();
                ScaledContentHost.Children.Add(MediaContentGrid);
            }
            else
            {
                ScaledModeViewbox.Visibility = Visibility.Collapsed;
                ScrollModeScrollViewer.Visibility = Visibility.Visible;
                ScrollContentHost.Children.Clear();
                ScrollContentHost.Children.Add(MediaContentGrid);
            }

            MediaContentGrid.UpdateLayout();

            if (!string.IsNullOrEmpty(_currentVideoPath) && File.Exists(_currentVideoPath))
                PlayFile(_currentVideoPath);

            Button_ToggleMode.Content = _scaledDisplay ? "Fit To View: On" : "Fit To View: Off";

        }

        private void Button_ToggleMode_Click(object sender, RoutedEventArgs e) => ToggleScaling();

        private void DisplayItem(string filePath, string columnName)
        {
            int index = _mediaNames.FindIndex(n => n == columnName);
            if (index < 0) return;

            var container = MediaContentGrid.Children
                .OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == index);

            if (container?.Child is not Image image) return;

            if (columnName == "Manual")
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/icons/manual.png"));
                container.Tag = filePath;
            }
            else if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/missing.png"));
                container.Tag = null;
            }
            else
            {
                var loadedImage = ImageHelper.LoadImageWithoutLock(filePath);
                image.Source = loadedImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/images/missing.png"));
                container.Tag = filePath;
            }

            // Set both menu and expand button tags
            SetButtonTags(index, filePath);
        }

        private void SetButtonTags(int columnIndex, string? path)
        {
            var stack = MediaContentGrid.Children
                .OfType<StackPanel>()
                .FirstOrDefault(sp => Grid.GetRow(sp) == 2 && Grid.GetColumn(sp) == columnIndex);

            if (stack != null)
            {
                // First button = menu, second = expand
                if (stack.Children.Count > 0 && stack.Children[0] is Button menuBtn)
                    menuBtn.Tag = path;

                if (stack.Children.Count > 1 && stack.Children[1] is Button expandBtn)
                    expandBtn.Tag = path;
            }
        }

        public void ClearAllImages()
        {
            foreach (var element in MediaContentGrid.Children)
            {
                if (element is Border b && b.Child is Image img)
                    img.Source = null;
            }
        }

        private void SetupGrid()
        {
            int columnIndex = 0;

            foreach (var item in _mediaNames)
            {
                var column = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    Name = item.Replace(" ", "__")
                };
                MediaContentGrid.ColumnDefinitions.Add(column);

                var header = new TextBlock
                {
                    Text = item,
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                header.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, columnIndex);
                MediaContentGrid.Children.Add(header);

                Border container;
                UIElement content;

                if (item == "Video")
                    content = _mediaPlayerControl!;
                else
                    content = new Image
                    {
                        MaxWidth = 400,
                        MaxHeight = 300,
                        Stretch = Stretch.Uniform,
                        RenderTransform = new ScaleTransform(1.05, 1.05),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                var shadowColor = TryFindResource("ShadowColor") as Color? ?? Colors.Black;

                container = new Border
                {
                    Child = content,
                    Tag = null,
                    Margin = new Thickness(20),
                    Effect = new DropShadowEffect
                    {
                        Color = shadowColor,
                        ShadowDepth = 10,
                        Direction = 315,
                        BlurRadius = 15,
                        Opacity = 0.7
                    }
                };

                Grid.SetRow(container, 1);
                Grid.SetColumn(container, columnIndex);
                MediaContentGrid.Children.Add(container);

                // Buttons stack
                var buttonStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                // Menu button
                var menuButton = new Button
                {
                    Style = (Style)FindResource("CustomImageButton"),
                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/icons/menu.png"))
                    },
                    Width = 50,
                    Height = 50,
                    Opacity = 0.7,
                    Tag = null,
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                menuButton.Click += MediaMenuButton_Click;
                buttonStack.Children.Add(menuButton);

                // Expand button
                if (item != "Manual")
                {
                    var expandButton = new Button
                    {
                        Style = (Style)FindResource("CustomImageButton"),
                        Content = new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/icons/expand.png"))
                        },
                        Width = 50,
                        Height = 50,
                        Opacity = 0.7,
                        Tag = container,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    expandButton.Click += MediaExpandButton_Click;
                    buttonStack.Children.Add(expandButton);
                }
                Grid.SetRow(buttonStack, 2);
                Grid.SetColumn(buttonStack, columnIndex);
                MediaContentGrid.Children.Add(buttonStack);

                columnIndex++;
            }
        }

        private void MediaMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path || string.IsNullOrEmpty(path)) return;

            var contextMenu = ContextMenuHelper.CreateContextMenu(path);
            contextMenu.PlacementTarget = btn;
            contextMenu.IsOpen = true;
        }

        private void MediaExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path || string.IsNullOrEmpty(path))
                return;

            // Determine whether it’s video or image
            var columnIndex = Grid.GetColumn(btn.Parent as UIElement);
            var container = MediaContentGrid.Children
                .OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == columnIndex);

            if (container == null) return;

            if (container.Child == _mediaPlayerControl)
                ShowVideoPreviewWindow(path);
            else if (container.Child is Image img && img.Source != null)
                ShowPreviewWindow(img.Source, path);
        }


        private void ShowPreviewWindow(ImageSource imageSource, string filePath)
        {
            _previewWindow?.Close();

            var previewImage = new Image
            {
                Source = imageSource,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(ZOOM_START_SCALE, ZOOM_START_SCALE)
            };

            var border = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(2),
                Child = previewImage
            };

            double maxWidth = SystemParameters.PrimaryScreenWidth * PREVIEW_MAX_WIDTH_RATIO;
            double maxHeight = SystemParameters.PrimaryScreenHeight * PREVIEW_MAX_HEIGHT_RATIO;

            _previewWindow = new Window
            {
                Content = border,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Topmost = true,
                Background = Brushes.Black,
                Title = Path.GetFileName(filePath),
                Opacity = 0
            };

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FADE_DURATION_MS));
            _previewWindow.BeginAnimation(Window.OpacityProperty, fadeIn);

            var scaleTransform = (ScaleTransform)previewImage.RenderTransform;
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(ZOOM_START_SCALE, ZOOM_END_SCALE, TimeSpan.FromMilliseconds(FADE_DURATION_MS)));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(ZOOM_START_SCALE, ZOOM_END_SCALE, TimeSpan.FromMilliseconds(FADE_DURATION_MS)));

            void CloseWindow()
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FADE_DURATION_MS));
                fadeOut.Completed += (s, e) => _previewWindow.Close();
                _previewWindow.BeginAnimation(Window.OpacityProperty, fadeOut);

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(ZOOM_END_SCALE, ZOOM_START_SCALE, TimeSpan.FromMilliseconds(FADE_DURATION_MS)));
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(ZOOM_END_SCALE, ZOOM_START_SCALE, TimeSpan.FromMilliseconds(FADE_DURATION_MS)));
            }

            _previewWindow.MouseLeftButtonDown += (s, e) => CloseWindow();
            _previewWindow.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) CloseWindow(); };

            _previewWindow.Show();
            _previewWindow.Focus();
        }

        private void ShowVideoPreviewWindow(string videoPath)
        {
            _previewWindow?.Close();

            _wasVideoPlaying = _mediaPlayerControl.IsPlaying;
            if (_wasVideoPlaying) _mediaPlayerControl.PausePlaying();

            _previewMediaPlayer = new MediaPlayerControl();
            _previewMediaPlayer.SetVolume((int)Properties.Settings.Default.Volume);

            var border = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(2),
                Child = _previewMediaPlayer
            };

            double maxWidth = SystemParameters.PrimaryScreenWidth * PREVIEW_MAX_WIDTH_RATIO;
            double maxHeight = SystemParameters.PrimaryScreenHeight * PREVIEW_MAX_HEIGHT_RATIO;

            _previewWindow = new Window
            {
                Content = border,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Width = maxWidth,
                Height = maxHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Topmost = true,
                Background = Brushes.Black,
                Title = Path.GetFileName(videoPath),
                Opacity = 0
            };

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(FADE_DURATION_MS));
            _previewWindow.BeginAnimation(Window.OpacityProperty, fadeIn);

            void CloseWindow()
            {
                _previewMediaPlayer?.StopPlaying();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FADE_DURATION_MS));
                fadeOut.Completed += (s, e) =>
                {
                    _previewWindow.Close();
                    _previewMediaPlayer = null;
                    if (_wasVideoPlaying) _mediaPlayerControl.ResumePlaying();
                };
                _previewWindow.BeginAnimation(Window.OpacityProperty, fadeOut);
            }

            _previewWindow.MouseLeftButtonDown += (s, e) => CloseWindow();
            _previewWindow.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) CloseWindow(); };

            _previewWindow.Show();
            _previewWindow.Focus();

            _previewMediaPlayer.PlayMedia(videoPath, true);
        }
                
    }
}
