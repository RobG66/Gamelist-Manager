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
        private List<MediaGridInfo> _mediaGridInfolist;


        public class MediaGridInfo
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string OldPath { get; set; }
            public string CurrentPath { get; set; }

            public MediaGridInfo(string name, int index, string oldPath, string newPath, string currentPath)
            {
                Name = name;
                Index = index;
                OldPath = oldPath;
                CurrentPath = currentPath;
            }

        }

        public MediaDisplay()
        {
            InitializeComponent();
            _libVLC = new LibVLC("--input-repeat=2");
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _videoView = new VideoView();
            _currentRomPath = string.Empty;
            _mediaGridInfolist = new List<MediaGridInfo>();
        }



        public void ShowMedia(DataRowView selectedRow)
        {
            if (selectedRow == null)
            {
                return;
            }

            if (_mediaGridInfolist.Count == 0)
            {
                SetupGrid();
            }

            _currentRomPath = selectedRow["Rom Path"].ToString()!;

            bool mediaDisplayed = false;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            foreach (var item in _mediaGridInfolist)
            {
                string columnName = item.Name;
                int columnIndex = item.Index;

                MediaEditButtons? mediaEditButtons = MediaContentGrid.Children
               .OfType<MediaEditButtons>()
               .FirstOrDefault(child => Grid.GetRow(child) == 2 && Grid.GetColumn(child) == columnIndex);

                mediaEditButtons!.Visibility = Visibility.Collapsed;

                //mediaEditButtons!.Visibility = SharedData.EditMode ? Visibility.Visible : Visibility.Collapsed;

                mediaEditButtons.button_Accept.Visibility = Visibility.Collapsed;
                mediaEditButtons.button_Clear.Visibility = Visibility.Visible;
                mediaEditButtons.button_Undo.Visibility = Visibility.Collapsed;

                if (columnName == "Video")
                {
                    StopPlaying();

                    var videoCellValue = selectedRow["Video"];
                    string? videoPath = videoCellValue == null || videoCellValue == DBNull.Value
                        ? string.Empty
                        : videoCellValue.ToString();

                    if (!string.IsNullOrEmpty(videoPath))
                    {
                        MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);
                        string videoFilePath = Path.Combine(parentFolderPath!, videoPath.TrimStart('.', '/').Replace('/', '\\'));
                        DisplayVideo(videoFilePath);
                        mediaDisplayed = true;
                    }
                    else
                    {
                        MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                        if (SharedData.EditMode)
                        {
                            //    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);
                        }
                        else
                        {
                            //   MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                        }
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
                    mediaDisplayed = true;
                }
                else
                {
                    pathValue = "pack://application:,,,/Resources/dropicon.png";
                    mediaEditButtons.button_Clear.Visibility = Visibility.Collapsed;
                    DisplayImage(pathValue, columnName);

                    MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                    if (SharedData.EditMode)
                    {
                        //  MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        //  MediaContentGrid.ColumnDefinitions[columnIndex].Width = new GridLength(0);
                    }
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
            MediaGridInfo mediaGridInfo = _mediaGridInfolist.First(m => m.Name == columnName);
            int columnIndex = mediaGridInfo.Index;
            mediaGridInfo.CurrentPath = imagePath;

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

            var contentControl = MediaContentGrid.Children
             .OfType<ContentControl>()
              .FirstOrDefault(cc => Grid.GetRow(cc) == 1 && Grid.GetColumn(cc) == columnIndex);

            bool allowDrop = SharedData.EditMode;

            if (contentControl != null && contentControl.Content is Image image)
            {
                // Update the image source
                image.Source = imageSource;
                image.AllowDrop = allowDrop;
            }

        }

        public void ClearButton_Click(object sender, EventArgs e)
        {

            if (sender is Button button && button.Parent is FrameworkElement parent && parent.Parent is MediaEditButtons mediaEditButtons)
            {
                int columnIndex = Grid.GetColumn(mediaEditButtons);
                string columnName = MediaContentGrid.ColumnDefinitions[columnIndex].Tag.ToString()!;
                MediaGridInfo? mediaGridInfo = _mediaGridInfolist.FirstOrDefault(m => m.Name == columnName);

                if (mediaGridInfo == null)
                {
                    return;
                }

                mediaEditButtons.button_Clear.Visibility = Visibility.Collapsed;
                mediaEditButtons.button_Undo.Visibility = Visibility.Visible;
                mediaEditButtons.button_Accept.Visibility = Visibility.Visible;

                ContentControl? existingControl = MediaContentGrid.Children
                 .OfType<ContentControl>()
                 .FirstOrDefault(child => Grid.GetRow(child) == 1 && Grid.GetColumn(child) == columnIndex);

                // If this is the video column, need to stop vlc 
                // Everything else is just an image
                if (columnName == "Video")
                {
                    if (_mediaPlayer != null)
                    {
                        mediaGridInfo.OldPath = _mediaPlayer.Media?.Mrl;
                        _mediaPlayer.Stop();
                        _mediaPlayer.Media.Dispose();
                    }
                    return;
                }

                if (columnName == "Manual")
                {
                    return;
                }

                string oldPath = mediaGridInfo.CurrentPath;
                mediaGridInfo.OldPath = oldPath;
                mediaGridInfo.CurrentPath = string.Empty;

                string imagePath = "pack://application:,,,/Resources/dropicon.png";
                DisplayImage(imagePath, columnName);

            }
        }

        public void AcceptButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Parent is FrameworkElement parent && parent.Parent is MediaEditButtons mediaEditButtons)
            {
                int columnIndex = Grid.GetColumn(mediaEditButtons);
                string columnName = MediaContentGrid.ColumnDefinitions[columnIndex].Tag.ToString()!;
                MediaGridInfo? mediaGridInfo = _mediaGridInfolist.FirstOrDefault(m => m.Name == columnName);

                if (mediaGridInfo == null)
                {
                    return;
                }

                if (columnName == "Video")
                {
                    return;
                }

                if (columnName == "Manual")
                {
                    return;
                }

                string currentPath = mediaGridInfo.CurrentPath;
                string oldPath = mediaGridInfo.OldPath;

                if (!string.IsNullOrEmpty(oldPath))
                {
                    // Backup old image
                    string parentDirectory = Directory.GetParent(oldPath)!.Name;
                    string fileName = Path.GetFileName(oldPath);
                    string backupFolder = $"{SharedData.ProgramDirectory}\\media backup\\replaced\\{SharedData.CurrentSystem}\\{parentDirectory}";

                    if (!Directory.Exists(backupFolder))
                    {
                        Directory.CreateDirectory(backupFolder);
                    }
                    string backupFile = Path.Combine(backupFolder, fileName);
                    System.IO.File.Copy(oldPath, backupFile, true);
                    System.IO.File.Copy(currentPath, oldPath, true);
                }
                else
                {
                    var row = SharedData.DataSet.Tables[0].AsEnumerable()
                      .FirstOrDefault(row => row.Field<string>("Rom Path") == _currentRomPath);

                    if (row == null)
                    {
                        return;
                    }

                    string jsonString = Properties.Settings.Default.MediaPaths;
                    Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;
                    var mediaElementName = GamelistMetaData.GetMetaDataTypeByName(columnName);

                    string destinationFolder = mediaPaths[mediaElementName];
                    string parentFolderPath = System.IO.Path.GetDirectoryName(SharedData.XMLFilename)!;

                    string extension = "png";
                    string romName = System.IO.Path.GetFileNameWithoutExtension(_currentRomPath);
                    string fileName = $"{romName}-{mediaElementName}.{extension}";
                    string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                    string fileToDownload = $"{downloadPath}\\{fileName}";




                    System.IO.File.Copy(currentPath, oldPath, true);
                }




            }
        }

        public void UndoButton_Click(object sender, EventArgs e)
        {

            if (sender is Button button && button.Parent is FrameworkElement parent && parent.Parent is MediaEditButtons mediaEditButtons)
            {
                int columnIndex = Grid.GetColumn(mediaEditButtons);
                string columnName = MediaContentGrid.ColumnDefinitions[columnIndex].Tag.ToString()!;
                MediaGridInfo? mediaGridInfo = _mediaGridInfolist.FirstOrDefault(m => m.Name == columnName);

                if (mediaGridInfo == null)
                {
                    return;
                }

                mediaEditButtons.button_Clear.Visibility = Visibility.Collapsed;
                mediaEditButtons.button_Undo.Visibility = Visibility.Collapsed;
                mediaEditButtons.button_Accept.Visibility = Visibility.Collapsed;

                ContentControl? existingControl = MediaContentGrid.Children
                 .OfType<ContentControl>()
                 .FirstOrDefault(child => Grid.GetRow(child) == 1 && Grid.GetColumn(child) == columnIndex);

                // If this is the video column
                // Get the previous file and play it
                if (columnName == "Video")
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Media?.Dispose();
                    string videoPath = mediaGridInfo.OldPath ?? string.Empty;
                    mediaGridInfo.OldPath = string.Empty;

                    if (!string.IsNullOrEmpty(videoPath))
                    {
                        var media = new Media(_libVLC, new Uri(videoPath));
                        _mediaPlayer.Media = media;
                        _mediaPlayer.Play();
                        mediaGridInfo.CurrentPath = videoPath;
                        mediaEditButtons.button_Clear.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        mediaGridInfo.CurrentPath = string.Empty;
                    }
                    return;
                }


                if (columnName == "Manual")
                {
                    return;
                }

                string oldPath = mediaGridInfo.OldPath ?? string.Empty;
                mediaGridInfo.OldPath = string.Empty;

                if (existingControl?.Content is Image image && !string.IsNullOrEmpty(oldPath))
                {
                    DisplayImage(oldPath, columnName);
                    mediaEditButtons.button_Clear.Visibility = Visibility.Visible;
                    mediaGridInfo.CurrentPath = oldPath;
                }
                else
                {
                    string imagePath = "pack://application:,,,/Resources/dropicon.png";
                    DisplayImage(imagePath, columnName);
                    mediaGridInfo.CurrentPath = string.Empty;
                }
            }

        }


        private void OnCellDrop(object sender, DragEventArgs e)
        {

        }




        // Event handler for the DragOver event
        private void OnCellDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(System.Windows.Controls.Image)) || e.Data.GetDataPresent(typeof(Uri))) // Allow images or video
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
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
                Margin = new Thickness(10) // Add margin around text block container
            };

        }

        private void Allow(bool allowDrop)
        {
            foreach (UIElement child in MediaContentGrid.Children)
            {
                // Check if the child is in the target row
                if (Grid.GetRow(child) == 1)
                {
                    // Add DragOver and Drop event handlers to the cell
                    child.AllowDrop = allowDrop;

                    if (allowDrop)
                    {
                        child.Drop += OnCellDrop;
                        child.DragOver += OnCellDragOver;
                    }
                    else
                    {
                        child.Drop += OnCellDrop;
                        child.DragOver += OnCellDragOver;
                    }
                }
                if (Grid.GetRow(child) == 2)
                {
                    child.Visibility = allowDrop ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public void StopPlaying()
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
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
                    Tag = columnName
                };

                _mediaGridInfolist.Add(new MediaGridInfo(columnName, columnIndex, string.Empty, string.Empty, string.Empty));

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

                Grid.SetRow(contentControl, 1);
                Grid.SetColumn(contentControl, columnIndex);
                MediaContentGrid.Children.Add(contentControl);

                MediaEditButtons mediaEditButtons = new MediaEditButtons();
                mediaEditButtons.button_Clear.Click += ClearButton_Click;
                mediaEditButtons.button_Accept.Click += AcceptButton_Click;
                mediaEditButtons.button_Undo.Click += UndoButton_Click;
                Grid.SetRow(mediaEditButtons, 2);
                Grid.SetColumn(mediaEditButtons, columnIndex);

                // Add the control to the grid
                MediaContentGrid.Children.Add(mediaEditButtons);

                columnIndex++;

            }
        }


    }
}

