using GamelistManager.classes.api;
using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using GamelistManager.classes.services;
using GamelistManager.controls;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private bool _mediaGridSetupDone = false;
        private readonly ScraperService _scraperService = new ScraperService();


        private const double PREVIEW_MAX_WIDTH_RATIO = 0.7;
        private const double PREVIEW_MAX_HEIGHT_RATIO = 0.7;
        private const int FADE_DURATION_MS = 250;
        private const double ZOOM_START_SCALE = 0.95;
        private const double ZOOM_END_SCALE = 1.0;

        private Grid MediaContentGrid;
        private bool _scaledDisplay;
        private bool _showAllMedia = false;

        public MediaPage()
        {
            InitializeComponent();

            MediaContentGrid = new Grid();
            MediaContentGrid.SetResourceReference(Grid.BackgroundProperty, "SecondaryBackgroundBrush");
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

            _scaledDisplay = Properties.Settings.Default.ScaleToFit;

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
            Button_ToggleAll.Content = "Show All";
            Loaded += MediaPage_Loaded;
            SetupMediaGrid();
        }

        private void ToggleAllButton_Click(object sender, RoutedEventArgs e)
        {
            _showAllMedia = !_showAllMedia;
            Button_ToggleAll.Content = _showAllMedia ? "Hide Empty" : "Show All";

            // Refresh the current display
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.MainDataGrid.SelectedItem is DataRowView selectedRowView)
            {
                ShowMedia(selectedRowView);
            }
        }

        public async void ShowMedia(DataRowView selectedRow)
        {
            if (selectedRow == null || _mediaGridSetupDone == false)
                return;

            // Stop media FIRST so the clear→reload block below has no awaits in it —
            // old images stay visible on screen while this completes
            if (_mediaPlayerControl != null)
            {
                await StopPlayingAsync();
                await _mediaPlayerControl.DisposeMediaAsync();
            }

            // Freeze the grid's measured size so the Viewbox scale factor stays
            // constant during the synchronous clear+reload and never sees an
            // intermediate state where images are null and headers fill the view
            if (_scaledDisplay && MediaContentGrid.ActualWidth > 0)
            {
                MediaContentGrid.Width = MediaContentGrid.ActualWidth;
                MediaContentGrid.Height = MediaContentGrid.ActualHeight;
            }

            try
            {
                // Everything from here to end of try is synchronous — no awaits,
                // so the UI thread never renders the intermediate cleared state
                ClearAllImages();

                string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;
                bool nomedia = true;

                string mediaPathsJsonString = Properties.Settings.Default.MediaPaths;
                Dictionary<string, string> mediaPaths;
                try
                {
                    mediaPaths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)
                                 ?? new Dictionary<string, string>();
                }
                catch
                {
                    mediaPaths = new Dictionary<string, string>();
                }

                foreach (var column in MediaContentGrid.ColumnDefinitions)
                {
                    string columnName = column.Name.Replace("__", " ");
                    int columnIndex = MediaContentGrid.ColumnDefinitions.IndexOf(column);

                    string mediaType = columnName.Replace(" ", "").ToLower();
                    string enabledKey = $"{mediaType}_enabled";
                    bool isMediaEnabled = !mediaPaths.TryGetValue(enabledKey, out string? enabledValue) || enabledValue != "false";

                    if (!isMediaEnabled)
                    {
                        column.Width = new GridLength(0);
                        continue;
                    }

                    if (columnName == "Video")
                    {
                        var videoCellValue = selectedRow["Video"];
                        string? videoPath = videoCellValue == null || videoCellValue == DBNull.Value
                            ? null
                            : videoCellValue.ToString();

                        if (!string.IsNullOrEmpty(videoPath))
                        {
                            string fullPath = FilePathHelper.ConvertGamelistPathToFullPath(videoPath, parentFolderPath);
                            column.Width = new GridLength(1, GridUnitType.Star);
                            PlayFile(fullPath);
                            nomedia = false;
                        }
                        else if (_showAllMedia)
                        {
                            column.Width = new GridLength(1, GridUnitType.Star);
                            DisplayPlaceholder(columnIndex);
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
                        string fullPath = FilePathHelper.ConvertGamelistPathToFullPath(imagePath, parentFolderPath);
                        column.Width = new GridLength(1, GridUnitType.Star);
                        DisplayItem(fullPath, columnName);
                        nomedia = false;
                    }
                    else if (_showAllMedia)
                    {
                        column.Width = new GridLength(1, GridUnitType.Star);
                        DisplayPlaceholder(columnIndex);
                    }
                    else
                    {
                        column.Width = new GridLength(0);
                    }
                }

                if (nomedia && !_showAllMedia)
                {
                    var image = MediaContentGrid.Children
                        .OfType<Border>()
                        .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == 0)?
                        .Child as Image;

                    if (image != null)
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/nomedia.png"));
                }
            }
            finally
            {
                // Release the frozen size so the Viewbox rescales naturally to the new content
                if (_scaledDisplay)
                {
                    MediaContentGrid.Width = double.NaN;
                    MediaContentGrid.Height = double.NaN;
                }
            }
        }

        private void DisplayPlaceholder(int columnIndex)
        {
            var container = MediaContentGrid.Children
                .OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == columnIndex);

            if (container?.Child is Image image)
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/icons/dropicon.png"));
            }
        }

        public async void PlayFile(string fileName)
        {
            await StopPlayingAsync();
            _currentVideoPath = fileName;

            _mediaPlayerControl?.PlayMediaAsync(fileName, Properties.Settings.Default.VideoAutoplay);
        }

        public async Task StopPlayingAsync() => await _mediaPlayerControl?.StopPlayingAsync();
        public async Task PausePlayingAsync() => await _mediaPlayerControl?.PausePlayingAsync();
        public async Task ResumePlayingAsync() => await _mediaPlayerControl?.ResumePlayingAsync();


        private void ToggleScaling()
        {
            _scaledDisplay = !_scaledDisplay;
            Properties.Settings.Default.ScaleToFit = _scaledDisplay;
            Properties.Settings.Default.Save();

            _mediaPlayerControl?.StopPlayingAsync();

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

        private void MediaPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatus("Tip: Drag & drop media from file explorer or a browser to update images; videos and manuals from files.");
        }

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
            }
            else if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                image.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/missing.png"));
            }
            else
            {
                var loadedImage = ImageHelper.LoadImageWithoutLock(filePath);
                image.Source = loadedImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/images/missing.png"));
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

        private int GetColumnIndexFromButton(Button btn)
        {
            if (btn.Parent is StackPanel stack)
                return Grid.GetColumn(stack);
            return -1;
        }

        private string? GetMediaPathForColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= MediaContentGrid.ColumnDefinitions.Count)
                return null;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                return null;

            var column = MediaContentGrid.ColumnDefinitions[columnIndex];
            string columnName = column.Name.Replace("__", " ");

            var cellValue = selectedRowView[columnName];
            if (cellValue == null || cellValue == DBNull.Value)
                return null;

            string relativePath = cellValue.ToString()!;
            if (string.IsNullOrEmpty(relativePath))
                return null;

            string parentFolder = Path.GetDirectoryName(SharedData.XMLFilename)!;
            return FilePathHelper.ConvertGamelistPathToFullPath(relativePath, parentFolder);
        }

        private string GetColumnNameFromIndex(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= MediaContentGrid.ColumnDefinitions.Count)
                return string.Empty;

            return MediaContentGrid.ColumnDefinitions[columnIndex].Name.Replace("__", " ");
        }


        private void SetupMediaGrid()
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

                var shadowColor = TryFindResource("ShadowColor") as Color? ?? Colors.Black;

                var header = new TextBlock
                {
                    Text = item,
                    FontSize = 44,
                    FontWeight = FontWeights.Bold,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 2),
                    Effect = new DropShadowEffect
                    {
                        Color = shadowColor,
                        BlurRadius = 4,
                        ShadowDepth = 2,
                        Direction = 315,
                        Opacity = 0.5
                    }
                };
                header.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
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
                    };

                container = new Border
                {
                    Child = content,
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

                // Enable drag & drop for images, videos, and manuals
                container.AllowDrop = true;
                container.DragEnter += Container_DragEnter;
                container.DragLeave += Container_DragLeave;
                container.Drop += Container_Drop;

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

            _mediaGridSetupDone = true;
        }

        // Drag & Drop Event Handlers
        private void Container_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is not Border container)
                return;

            bool isValidDrop = false;

            // Get the column name to determine what file type is expected
            int columnIndex = Grid.GetColumn(container);
            var column = MediaContentGrid.ColumnDefinitions[columnIndex];
            string columnName = column.Name.Replace("__", " ");

            // Check for file drops (from file explorer)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    string file = files[0];
                    if (columnName == "Video" && IsVideoFile(file))
                    {
                        isValidDrop = true;
                    }
                    else if (columnName == "Manual" && IsPdfFile(file))
                    {
                        isValidDrop = true;
                    }
                    else if (columnName != "Video" && columnName != "Manual" && IsImageFile(file))
                    {
                        isValidDrop = true;
                    }
                }
            }
            // Check for image drops from web browsers (only for image columns)
            else if (columnName != "Video" && columnName != "Manual" &&
                     (e.Data.GetDataPresent(DataFormats.Html) ||
                      e.Data.GetDataPresent(DataFormats.Bitmap) ||
                      e.Data.GetDataPresent("FileContents") ||
                      e.Data.GetDataPresent("UniformResourceLocator")))
            {
                isValidDrop = true;
            }

            if (isValidDrop)
            {
                e.Effects = DragDropEffects.Copy;

                // Visual feedback - highlight the border
                container.BorderBrush = Brushes.LimeGreen;
                container.BorderThickness = new Thickness(3);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Container_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is not Border container)
                return;

            // Remove visual feedback
            container.BorderBrush = null;
            container.BorderThickness = new Thickness(0);

            e.Handled = true;
        }

        private async void Container_Drop(object sender, DragEventArgs e)
        {
            if (sender is not Border container)
                return;

            // Remove visual feedback
            container.BorderBrush = null;
            container.BorderThickness = new Thickness(0);

            // Get the column name from the container's position
            int columnIndex = Grid.GetColumn(container);
            var column = MediaContentGrid.ColumnDefinitions[columnIndex];
            string columnName = column.Name.Replace("__", " ");

            string? droppedFile = null;
            bool isTemporaryFile = false;

            try
            {
                // Handle file drops (from file explorer)
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length != 1)
                    {
                        MessageBox.Show(
                            Window.GetWindow(this),
                            "Please drop only one file at a time.",
                            "Invalid Drop", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    droppedFile = files[0];

                    // Validate file type based on column
                    if (columnName == "Video" && !IsVideoFile(droppedFile))
                    {
                        MessageBox.Show(
                            Window.GetWindow(this),
                            "Only video files are supported.\n\nSupported formats: MP4, AVI, MKV, MOV, WMV, FLV",
                            "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else if (columnName == "Manual" && !IsPdfFile(droppedFile))
                    {
                        MessageBox.Show(
                            Window.GetWindow(this),
                            "Only PDF files are supported for manuals.",
                            "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    else if (columnName != "Video" && columnName != "Manual" && !IsImageFile(droppedFile))
                    {
                        MessageBox.Show(
                            Window.GetWindow(this),
                            "Only image files are supported.\n\nSupported formats: JPG, PNG, BMP, GIF, TIFF, WEBP",
                            "Invalid File Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                // Handle drops from web browsers (images only)
                else if (columnName != "Video" && columnName != "Manual" && e.Data.GetDataPresent("FileContents"))
                {
                    // Chrome/Edge drops
                    using var stream = e.Data.GetData("FileContents") as Stream;
                    if (stream != null)
                    {
                        droppedFile = await SaveStreamToTempFile(stream, "png");
                        isTemporaryFile = true;
                    }
                }
                else if (columnName != "Video" && columnName != "Manual" && e.Data.GetDataPresent(DataFormats.Bitmap))
                {
                    // Some browsers provide bitmap directly
                    var bitmap = e.Data.GetData(DataFormats.Bitmap) as System.Drawing.Bitmap;
                    if (bitmap != null)
                    {
                        droppedFile = SaveBitmapToTempFile(bitmap);
                        isTemporaryFile = true;
                    }
                }
                else if (columnName != "Video" && columnName != "Manual" && e.Data.GetDataPresent("UniformResourceLocator"))
                {
                    // URL drops - download the image
                    string? url = e.Data.GetData("UniformResourceLocator") as string;
                    if (!string.IsNullOrEmpty(url))
                    {
                        droppedFile = await DownloadImageFromUrl(url);
                        isTemporaryFile = true;
                    }
                }
                else if (columnName != "Video" && columnName != "Manual" && e.Data.GetDataPresent(DataFormats.Html))
                {
                    // Try to extract image URL from HTML
                    string? html = e.Data.GetData(DataFormats.Html) as string;
                    string imageUrl = ExtractImageUrlFromHtml(html ?? string.Empty);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        droppedFile = await DownloadImageFromUrl(imageUrl);
                        isTemporaryFile = true;
                    }
                }

                if (string.IsNullOrEmpty(droppedFile))
                {
                    string mediaTypeName = columnName == "Video" ? "video" : columnName == "Manual" ? "PDF manual" : "image";
                    MessageBox.Show(
                        Window.GetWindow(this),
                        $"Could not process the dropped {mediaTypeName}.\n\nTry saving the file to your computer first, then drag it from file explorer.",
                        "Drop Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await HandleMediaDrop(droppedFile, columnName);
            }
            finally
            {
                // Clean up temporary file if created
                if (isTemporaryFile && !string.IsNullOrEmpty(droppedFile) && File.Exists(droppedFile))
                {
                    try
                    {
                        File.Delete(droppedFile);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            e.Handled = true;
        }

        // Handle the dropped media (image, video, or manual)
        private async Task HandleMediaDrop(string droppedFile, string columnName)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Stop video playback if dropping on Video column
                if (columnName == "Video" && _mediaPlayerControl != null)
                {
                    await _mediaPlayerControl.StopPlayingAsync();
                }

                // Get the main window's selected item
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                {
                    MessageBox.Show(
                        Window.GetWindow(this),
                        "No item selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DataRow row = selectedRowView.Row;

                // Get the media paths
                string jsonString = Properties.Settings.Default.MediaPaths;
                var mediaPaths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) ?? new();

                // Get media type from column name
                string mediaType = GamelistMetaData.GetMetadataTypeByName(columnName);

                if (!mediaPaths.TryGetValue(mediaType, out string? mediaFolder))
                {
                    MessageBox.Show(
                        Window.GetWindow(this),
                        $"Media path not configured for {columnName}.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get parent folder and construct full media path
                string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;
                string fullMediaPath = FilePathHelper.ConvertGamelistPathToFullPath(mediaFolder, parentFolderPath);

                // Ensure media directory exists
                if (!Directory.Exists(fullMediaPath))
                {
                    Directory.CreateDirectory(fullMediaPath);
                }

                // This is how ES does it, even though it does not matter
                if (mediaType == "thumbnail")
                    mediaType = "thumb";

                // Generate destination filename (use ROM name + extension from dropped file)

                string romName = row["Name"].ToString()!;
                string romPath = row["Rom Path"].ToString()!;
                string romFileName = Path.GetFileNameWithoutExtension(romPath);
                string extension = Path.GetExtension(droppedFile);
                string destFileName = $"{romFileName}-{mediaType}{extension}";
                string destFullPath = Path.Combine(fullMediaPath, destFileName);

                // If an existing media file is already associated with this column, confirm replacement once
                bool overwriteApproved = false;
                string? existingRelativePath = null;
                if (row.Table.Columns.Contains(columnName))
                {
                    var existingValue = row[columnName];
                    if (existingValue != null && existingValue != DBNull.Value)
                        existingRelativePath = existingValue.ToString();
                }

                if (!string.IsNullOrWhiteSpace(existingRelativePath))
                {
                    string existingFullPath = FilePathHelper.ConvertGamelistPathToFullPath(existingRelativePath, parentFolderPath);

                    // If the existing file is missing, allow overwrite silently
                    if (!File.Exists(existingFullPath))
                    {
                        overwriteApproved = true;
                    }
                    else
                    {
                        var replaceExisting = MessageBox.Show(
                            Window.GetWindow(this),
                            $"A {columnName} file already exists for '{romName}'.\n\n" +
                            $"Current: {Path.GetFileName(existingFullPath)}\n" +
                            $"Path: {existingFullPath}\n\n" +
                            "Do you want to replace it?",
                            "Confirm Replace",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (replaceExisting != MessageBoxResult.Yes)
                            return;

                        overwriteApproved = true;

                        // Attempt to remove the existing file to avoid orphaned media with different extensions
                        try { File.Delete(existingFullPath); } catch { /* ignore */ }
                    }
                }

                // Check if file already exists
                if (File.Exists(destFullPath) && !overwriteApproved)
                {
                    var result = MessageBox.Show(
                        Window.GetWindow(this),
                        $"A {columnName} file already exists for '{romName}'.\n\n" +
                        $"Do you want to replace it?\n\n" +
                        $"File: {destFileName}",
                        "Confirm Replace",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                // Copy the file
                await Task.Run(() => File.Copy(droppedFile, destFullPath, overwrite: true));

                // Update the database with the relative path (with ./ prefix)
                string relativePath = FilePathHelper.ConvertPathToGamelistRomPath(destFullPath, parentFolderPath);

                if (row.Table.Columns.Contains(columnName))
                {
                    row[columnName] = relativePath;
                    SharedData.IsDataChanged = true;
                }

                // Refresh the display
                ShowMedia(selectedRowView);
                UpdateStatus($"Successfully added {columnName} for '{romName}'");
            }
            catch (Exception ex)
            {
                UpdateStatusError($"Error adding media: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // Helper method to check if file is an image
        private static bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or
                   ".gif" or ".tiff" or ".tif" or ".webp";
        }

        // Helper method to check if file is a video
        private static bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".mp4" or ".avi" or ".mkv" or ".mov" or
                   ".wmv" or ".flv" or ".webm" or ".m4v";
        }

        // Helper method to check if file is a PDF
        private static bool IsPdfFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".pdf";
        }

        // Helper methods for browser drops
        private static async Task<string> SaveStreamToTempFile(Stream stream, string extension)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"dropped_image_{Guid.NewGuid()}.{extension}");

            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            return tempPath;
        }

        private static string SaveBitmapToTempFile(System.Drawing.Bitmap bitmap)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"dropped_image_{Guid.NewGuid()}.png");
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            return tempPath;
        }

        private static async Task<string> DownloadImageFromUrl(string url)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Try to determine extension from content type or URL
                string extension = "png";
                if (response.Content.Headers.ContentType?.MediaType != null)
                {
                    var contentType = response.Content.Headers.ContentType.MediaType.ToLower();
                    extension = contentType switch
                    {
                        "image/jpeg" => "jpg",
                        "image/png" => "png",
                        "image/gif" => "gif",
                        "image/bmp" => "bmp",
                        "image/webp" => "webp",
                        _ => Path.GetExtension(url).TrimStart('.').ToLower()
                    };
                }
                else
                {
                    extension = Path.GetExtension(url).TrimStart('.').ToLower();
                }

                if (string.IsNullOrEmpty(extension))
                    extension = "png";

                string tempPath = Path.Combine(Path.GetTempPath(), $"downloaded_image_{Guid.NewGuid()}.{extension}");

                using (var fileStream = File.Create(tempPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                return tempPath;
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractImageUrlFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            // Look for img src in HTML
            var match = System.Text.RegularExpressions.Regex.Match(html, @"<img[^>]+src=[""']([^""']+)[""']",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private async void MediaExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            int columnIndex = GetColumnIndexFromButton(btn);
            if (columnIndex < 0)
                return;

            string? path = GetMediaPathForColumn(columnIndex);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var container = MediaContentGrid.Children
                .OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == columnIndex);

            if (container == null)
                return;

            if (container.Child == _mediaPlayerControl)
                await ShowVideoPreviewWindow(path);
            else if (container.Child is Image img && img.Source != null)
                ShowImagePreviewWindow(img.Source, path);
        }


        private void ShowImagePreviewWindow(ImageSource imageSource, string filePath)
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

        private async Task ShowVideoPreviewWindow(string videoPath)
        {
            _previewWindow?.Close();

            _wasVideoPlaying = _mediaPlayerControl.IsPlaying;
            
            if (_wasVideoPlaying) 
                await _mediaPlayerControl.PausePlayingAsync();

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
                _previewMediaPlayer?.StopPlayingAsync();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(FADE_DURATION_MS));
                fadeOut.Completed += async (s, e) =>
                {
                    _previewWindow.Close();
                    _previewMediaPlayer = null;
                    if (_wasVideoPlaying)                   
                        await _mediaPlayerControl.ResumePlayingAsync();       
                };

                _previewWindow.BeginAnimation(Window.OpacityProperty, fadeOut);
            }

            _previewWindow.MouseLeftButtonDown += (s, e) => CloseWindow();
            _previewWindow.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) CloseWindow(); };

            _previewWindow.Show();
            _previewWindow.Focus();

            await _previewMediaPlayer.PlayMediaAsync(videoPath, true);
        }

       
              
        private void ClearMediaPath(string columnName)
        {
            try
            {
                // Get the main window's selected item
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                {
                    UpdateStatusError("No item selected.");
                    return;
                }

                DataRow row = selectedRowView.Row;
                string romName = row["Name"].ToString() ?? "Unknown";

                // Clear the media path in the dataset
                if (row.Table.Columns.Contains(columnName))
                {
                    row[columnName] = DBNull.Value;
                    SharedData.IsDataChanged = true;

                    // Refresh the display
                    ShowMedia(selectedRowView);

                    StatusText.Text = $"Cleared {columnName} path for '{romName}'";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusError($"Error clearing media path: {ex.Message}");
            }
        }

        private async void DeleteMediaFile(string filePath, string columnName)
        {
            try
            {
                // SAFETY CHECK: Ensure filePath is not null/empty
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    UpdateStatusError("Invalid file path.");
                    return;
                }

                // Get the main window's selected item
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                {
                    UpdateStatusError("No item selected.");
                    return;
                }

                DataRow row = selectedRowView.Row;
                string romName = row["Name"].ToString() ?? Path.GetFileName(filePath);

                // Convert to full path
                string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;
                string fullPath = FilePathHelper.ConvertGamelistPathToFullPath(filePath, parentFolderPath);

                // SAFETY CHECK: Verify the resolved path is not empty/root
                if (string.IsNullOrWhiteSpace(fullPath) ||
                    fullPath == "/" ||
                    fullPath == "\\" ||
                    fullPath.Length < 4) // Minimum like "C:\x"
                {
                    UpdateStatusError("Invalid resolved file path.");
                    return;
                }

                // SAFETY CHECK: Verify file exists
                if (!File.Exists(fullPath))
                {
                    UpdateStatusError("File not found.");
                    return;
                }

                // SAFETY CHECK: Verify the file is within the expected media directories
                string mediaPathsJson = Properties.Settings.Default.MediaPaths;
                var mediaPaths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJson) ?? new();

                bool isInMediaFolder = false;
                foreach (var mediaPath in mediaPaths.Values)
                {
                    if (string.IsNullOrWhiteSpace(mediaPath)) continue;

                    string fullMediaPath = FilePathHelper.ConvertGamelistPathToFullPath(mediaPath, parentFolderPath);
                    if (!string.IsNullOrWhiteSpace(fullMediaPath) &&
                        fullPath.StartsWith(fullMediaPath, StringComparison.OrdinalIgnoreCase))
                    {
                        isInMediaFolder = true;
                        break;
                    }
                }

                if (!isInMediaFolder)
                {
                    UpdateStatusError("Cannot delete file - it is not located in a configured media folder.");
                    return;
                }

                // Confirm deletion
                var result = MessageBox.Show(
                    $"Delete {columnName} file for '{romName}'?\n\n" +
                    $"File: {Path.GetFileName(fullPath)}\n" +
                    $"Path: {fullPath}\n\n" +
                    $"This cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                if (columnName == "Video")
                    if (_mediaPlayerControl != null)
                    { 
                        await StopPlayingAsync();
                        await _mediaPlayerControl.DisposeMediaAsync();
                    }

                // Delete the file
                File.Delete(fullPath);

                // Clear the path in the dataset
                if (row.Table.Columns.Contains(columnName))
                {
                    row[columnName] = DBNull.Value;
                    SharedData.IsDataChanged = true;
                }

                // Refresh the display
                ShowMedia(selectedRowView);

                UpdateStatus($"Successfully deleted {columnName} file for '{romName}'");
            }
            catch (UnauthorizedAccessException)
            {
                UpdateStatusError("Access denied. The file may be in use or you don't have permission to delete it.");
            }
            catch (IOException ex)
            {
                UpdateStatusError($"File operation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                UpdateStatusError($"Error deleting file: {ex.Message}");
            }
        }

        private void MediaMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            int columnIndex = GetColumnIndexFromButton(btn);
            if (columnIndex < 0)
                return;

            string columnName = GetColumnNameFromIndex(columnIndex);
            string mediaType = GamelistMetaData.GetMetadataTypeByName(columnName);

            // Get the path from dataset, not from tag
            string? path = GetMediaPathForColumn(columnIndex);
            bool hasMedia = !string.IsNullOrEmpty(path);

            // Create context menu - if no media exists, only show scrape option
            if (!hasMedia)
            {
                // No media - only show scrape options
                var contextMenu = UIHelper.CreateContextMenu(
                    "",
                    onScrapeRequested: async (data) =>
                    {
                        var parts = data.Split('|');
                        string scraperName = parts.Length > 1 ? parts[1] : "ArcadeDB";
                        await ScrapeImageForItem("", mediaType, columnName, scraperName);
                    },
                    onClearRequested: null,
                    onDeleteRequested: null
                );

                contextMenu.PlacementTarget = btn;
                contextMenu.IsOpen = true;
            }
            else
            {
                // Has media - show all options
                var contextMenu = UIHelper.CreateContextMenu(
                    path,
                    onScrapeRequested: async (data) =>
                    {
                        var parts = data.Split('|');
                        string filePath = parts[0];
                        string scraperName = parts.Length > 1 ? parts[1] : "ArcadeDB";
                        await ScrapeImageForItem(filePath, mediaType, columnName, scraperName);
                    },
                    onClearRequested: () => ClearMediaPath(columnName),
                    onDeleteRequested: () => DeleteMediaFile(path, columnName)
                );

                contextMenu.PlacementTarget = btn;
                contextMenu.IsOpen = true;
            }
        }

        

        // Helper to get the image source setting for the specific media type
        private static string? GetImageSourceForType(
             Dictionary<string, Dictionary<string, string>> iniSections,
             string currentScraper,
             string mediaType)
        {
            // Map mediaType to JSON / INI key
            string? sectionName = mediaType switch
            {
                "image" => "ImageSource",
                "thumbnail" => "ThumbnailSource",
                "wheel" => "WheelSource",
                "boxart" => "BoxArtSource",
                "marquee" => "MarqueeSource",
                "cartridge" => "CartridgeSource",
                "video" => "VideoSource",
                _ => null
            };

            if (sectionName == null)
                return null;

            // Load saved media sources
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> allMediaSources;
            var mediaSourcesJson = Properties.Settings.Default.MediaSources;

            try
            {
                allMediaSources =
                    JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(mediaSourcesJson)
                    ?? new();
            }
            catch
            {
                allMediaSources = new();
            }

            // Get saved source (using correct key!)
            string? savedSource = null;

            if (allMediaSources.TryGetValue(currentScraper, out var scraperDict) &&
                scraperDict.TryGetValue(SharedData.CurrentSystem, out var systemDict) &&
                systemDict.TryGetValue(sectionName, out savedSource))
            {
                // savedSource found
            }

            // Resolve from INI
            if (iniSections.TryGetValue(sectionName, out var section))
            {
                string? key = savedSource ?? section.Keys.FirstOrDefault();

                if (key != null && section.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return null;
        }


        // Helper methods from Scraper page
        private static string GetScreenScraperLanguage()
        {
            string language = Properties.Settings.Default.Language;
            if (!string.IsNullOrEmpty(language))
            {
                var match = System.Text.RegularExpressions.Regex.Match(language, @"\((.*?)\)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return "en";
        }

        private static List<string> GetScreenScraperRegions()
        {
            var regions = new List<string>();

            string primaryRegion = Properties.Settings.Default.Region;
            if (!string.IsNullOrEmpty(primaryRegion))
            {
                var match = System.Text.RegularExpressions.Regex.Match(primaryRegion, @"\((.*?)\)");
                if (match.Success)
                {
                    regions.Add(match.Groups[1].Value);
                }
            }
            else
            {
                regions.Add("us");
            }

            string fallbackJson = Properties.Settings.Default.Region_Fallback;
            var fallbackRegions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(fallbackJson) ?? new List<string>();

            regions.AddRange(
                fallbackRegions
                    .Select(r => System.Text.RegularExpressions.Regex.Match(r, @"\((.*?)\)").Groups[1].Value)
                    .Where(code => !string.IsNullOrEmpty(code))
            );

            return regions.Distinct().ToList();
        }

        private void Button_Rescrape_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = UIHelper.CreateScraperMenu(async (scraperName) =>
            {
                await RescrapeAllMedia(scraperName);
            });

            contextMenu.PlacementTarget = Button_Rescrape;
            contextMenu.IsOpen = true;
        }


        private async Task ScrapeImageForItem(string filePath, string mediaType, string columnName, string scraperName)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                SetStatus($"Scraping {columnName}...");
                // Get the selected row
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                {
                    UpdateStatusError("No item selected.");
                    return;
                }

                DataRow row = selectedRowView.Row;
                string romName = row["Name"].ToString() ?? (!string.IsNullOrEmpty(filePath) ? Path.GetFileName(filePath) : "Unknown");
                                
                // Use the common scraping logic - just scrape this ONE media type
                var mediaTypesToScrape = new List<string> { mediaType };

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    mainWindow.IsEnabled = false;
                    bool includeMetadata = false; // For single media scrape, do not include metadata   
                    var (success, scrapedCount, errorMessage) = await PerformScrape(row, scraperName, mediaTypesToScrape, includeMetadata);

                    if (success)
                    {
                        ShowMedia(selectedRowView);

                        if (scrapedCount == 0)
                        {
                            UpdateStatus($"Scrape completed but no media was found for '{romName}'");
                        }
                        else
                        {
                            UpdateStatus($"Successfully scraped {scrapedCount} media item(s) for '{romName}'");
                        }
                    }
                    else
                    {
                        UpdateStatusError($"Failed to scrape {columnName}: {errorMessage}");
                    }
                }
                finally
                {
                    mainWindow.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusError($"Error scraping media: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task RescrapeAllMedia(string scraperName)
        {
            try
            {
                // Get the selected row
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.MainDataGrid.SelectedItem is not DataRowView selectedRowView)
                {
                    MessageBox.Show("No item selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DataRow row = selectedRowView.Row;
                string romName = row["Name"].ToString() ?? "Unknown";

                Mouse.OverrideCursor = null; // Remove cursor before showing dialog

                // Show custom dialog with metadata checkbox
                // Prepare the message
                string message = $"Re-scrape ALL media for '{romName}' using {scraperName}?\n\n" +
                    "This will overwrite all existing media (images and video).";

                // Determine if checkbox should be shown
                string? checkboxText = scraperName == "EmuMovies" ? null : "Include metadata";
                bool defaultChecked = false; // or true if you want pre-checked

                // Show the custom message box
                var result = MessageBoxWithCheckbox.Show(
                    owner: mainWindow,
                    message: message,
                    out bool includeMetadata,
                    title: "Confirm Re-Scrape",
                    buttons: MessageBoxButton.OKCancel,
                    icon: MessageBoxImage.Warning,
                    checkboxText: checkboxText,
                    checkboxDefaultChecked: defaultChecked
                );

                // If user canceled, exit
                if (result != MessageBoxResult.OK)
                    return;

                // Use checkbox value
                bool includeMetadataFlag = includeMetadata;

                Mouse.OverrideCursor = Cursors.Wait;

                string scrapeMessage = includeMetadata
                   ? $"Scraping all media and metadata for '{romName}'..."
                   : $"Scraping all media for '{romName}'...";

                SetStatus(scrapeMessage);

                // Get all enabled media types
                string jsonString = Properties.Settings.Default.MediaPaths;
                var mediaPaths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) ?? new();

                var enabledMediaTypes = _mediaNames
                    .Select(name => GamelistMetaData.GetMetadataTypeByName(name))
                    .Where(mediaType =>
                    {
                        string enabledKey = $"{mediaType}_enabled";
                        return !mediaPaths.TryGetValue(enabledKey, out string? enabledValue) || enabledValue != "false";
                    })
                    .ToList();

                try
                {
                    mainWindow.IsEnabled = false;

                    // Use the common scraping logic - scrape ALL enabled media types
                    var (success, scrapedCount, errorMessage) = await PerformScrape(
                        row, scraperName, enabledMediaTypes, includeMetadata);

                    if (success)
                    {
                        ShowMedia(selectedRowView);

                        if (scrapedCount > 0)
                        {
                            string resultMessage = includeMetadata
                               ? $"Successfully scraped {scrapedCount} media item(s) and metadata for '{romName}'"
                               : $"Successfully scraped {scrapedCount} media item(s) for '{romName}'";
                            UpdateStatus(resultMessage);
                        }
                        else
                        {
                            UpdateStatus($"Scrape completed but no media was found for '{romName}'");
                        }
                    }
                    else
                    {
                        UpdateStatusError($"Failed to scrape media: {errorMessage}");
                    }
                }
                finally
                {
                    mainWindow.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusError($"Error scraping all media: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<(bool success, int scrapedCount, string errorMessage)> PerformScrape(
            DataRow row,
            string scraperName,
            List<string> itemsToScrape,
            bool includeMetadata)
        {
            try
            {
                if (itemsToScrape.Contains("video"))
                {
                    await StopPlayingAsync();
                }

                // Validate ArcadeDB scraper usage
                if (scraperName == "ArcadeDB")
                {
                    if (!ArcadeSystemID.IsInitialized)
                    {
                        return (false, 0, "Arcade systems configuration is missing!\n\nThe arcadesystems.ini file could not be loaded.");
                    }

                    bool isArcade = ArcadeSystemID.HasArcadeSystemName(SharedData.CurrentSystem);
                    if (!isArcade)
                    {
                        return (false, 0, $"You cannot scrape this system with the currently selected scraper.\n\n'{SharedData.CurrentSystem}' is not an arcade system.");
                    }
                }

                // Get system ID
                string systemID = string.Empty;
                Dictionary<string, Dictionary<string, string>> iniSections = null;

                if (scraperName != "ArcadeDB")
                {
                    string iniFile = $".\\ini\\{scraperName.ToLower()}_options.ini";
                    iniSections = IniFileReader.ReadIniFile(iniFile);

                    if (!iniSections.TryGetValue("Systems", out var systemSection))
                    {
                        return (false, 0, $"Could not find [Systems] section in '{iniFile}'");
                    }

                    if (!systemSection.TryGetValue(SharedData.CurrentSystem, out systemID))
                    {
                        string availableSystems = string.Join(", ", systemSection.Keys);
                        return (false, 0, $"System ID not found for '{SharedData.CurrentSystem}' in {scraperName}\n\nAvailable systems in INI:\n{availableSystems}");
                    }
                }

                // Setup scraper properties
                ScraperProperties scraperProperties = new ScraperProperties
                {
                    SystemID = systemID,
                    LogVerbosity = 0
                };

                // Authenticate
                bool isAuthenticated = false;
                switch (scraperName)
                {
                    case "ArcadeDB":
                        isAuthenticated = true;
                        break;
                    case "EmuMovies":
                        isAuthenticated = await _scraperService.AuthenticateEmuMoviesAsync(scraperProperties);
                        if (isAuthenticated)
                        {
                            await _scraperService.GetEmuMoviesMediaLists(scraperProperties);
                        }
                        break;
                    case "ScreenScraper":
                        isAuthenticated = await _scraperService.AuthenticateScreenScraperAsync(scraperProperties);
                        if (isAuthenticated)
                        {
                            scraperProperties.Language = GetScreenScraperLanguage();
                            scraperProperties.Regions = GetScreenScraperRegions();
                        }
                        break;
                }

                if (!isAuthenticated)
                {
                    return (false, 0, "Authentication failed. Please check your scraper credentials.");
                }

                scraperProperties.CacheFolder = _scraperService.CreateCacheFolder(scraperName);

                // Get media paths
                string jsonString = Properties.Settings.Default.MediaPaths;
                var mediaPaths = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) ?? new();

                // Get all elements to scrape
                if (includeMetadata)
                {
                    var elements = GamelistMetaData.GetScraperElements(scraperName);
                    itemsToScrape = elements; // Everything including metadata
                }

                // Load INI sections for image sources
                if (scraperName == "ArcadeDB" && iniSections == null)
                {
                    string iniFile = $".\\ini\\{scraperName.ToLower()}_options.ini";
                    iniSections = IniFileReader.ReadIniFile(iniFile);
                }

                // Setup parameters
                ScraperParameters parameters = new ScraperParameters
                {
                    SystemID = systemID,
                    UserID = scraperProperties.UserName,
                    UserPassword = scraperProperties.Password,
                    ParentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename),
                    SSLanguage = scraperProperties.Language,
                    ScrapeEnglishGenreOnly = Properties.Settings.Default.ScrapeEnglishGenreOnly,
                    ScrapeAnyMedia = Properties.Settings.Default.ScrapeAnyMedia,
                    SSRegions = scraperProperties.Regions,
                    OverwriteMedia = true,
                    OverwriteMetadata = includeMetadata,  
                    OverwriteName = includeMetadata,      
                    UserAccessToken = scraperProperties.AccessToken,
                    ScraperPlatform = scraperName,
                    MediaPaths = mediaPaths,
                    ElementsToScrape = itemsToScrape,
                    ScrapeByCache = Properties.Settings.Default.BatchProcessing && scraperName == "ArcadeDB",
                    SkipNonCached = false,
                    CacheFolder = scraperProperties.CacheFolder,
                    VerifyImageDownloads = Properties.Settings.Default.VerifyDownloadedImages
                };


                // Set image sources for each media type
                foreach (var mediaType in itemsToScrape)
                {
                    var imageSource = GetImageSourceForType(iniSections, scraperName, mediaType);

                    switch (mediaType)
                    {
                        case "image": parameters.ImageSource = imageSource; break;
                        case "thumbnail": parameters.ThumbnailSource = imageSource; break;
                        case "wheel": parameters.WheelSource = imageSource; break;
                        case "boxart": parameters.BoxArtSource = imageSource; break;
                        case "marquee": parameters.MarqueeSource = imageSource; break;
                        case "cartridge": parameters.CartridgeSource = imageSource; break;
                        case "video": parameters.VideoSource = imageSource; break;
                    }
                }

                // Scrape the media
                var scrapedData = await _scraperService.ScrapeGameAsync(
                    row,
                    parameters,
                    scraperProperties,
                    scraperName,
                    null,
                    System.Threading.CancellationToken.None
                );

                if (scrapedData.WasSuccessful)
                {
                    await _scraperService.SaveScrapedDataAsync(row, scrapedData, parameters);

                    // Count how many media items were scraped
                    int scrapedCount = 0;
                    foreach (var mediaType in itemsToScrape)
                    {
                        string columnName = _mediaNames.FirstOrDefault(n =>
                            GamelistMetaData.GetMetadataTypeByName(n).Equals(mediaType, StringComparison.OrdinalIgnoreCase));

                        if (columnName != null && row.Table.Columns.Contains(columnName))
                        {
                            var value = row[columnName];
                            if (value != null && value != DBNull.Value && !string.IsNullOrEmpty(value.ToString()))
                            {
                                scrapedCount++;
                            }
                        }
                    }

                    return (true, scrapedCount, string.Empty);
                }
                else
                {
                    string errorMsg = scrapedData.Messages?.Any() == true
                        ? string.Join("\n", scrapedData.Messages)
                        : "Unknown error occurred";

                    return (false, 0, errorMsg);
                }
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        public void SetStatus(string message)
        {
            StatusIcon.Visibility = Visibility.Collapsed;
            StatusText.Text = message;
            StatusText.Foreground = Brushes.Black;
        }

        public void UpdateStatus(string message)
        {
            StatusIcon.Visibility = Visibility.Visible;
            StatusIcon.Text = "✓";
            StatusIcon.Foreground = Brushes.Green;
            StatusText.Text = message;
            StatusText.Foreground = Brushes.Green;
        }

        public void UpdateStatusError(string message)
        {
            StatusIcon.Visibility = Visibility.Visible;
            StatusIcon.Text = "✗";
            StatusIcon.Foreground = Brushes.Red;
            StatusText.Text = message;
            StatusText.Foreground = Brushes.Red;
        }
    }
}
