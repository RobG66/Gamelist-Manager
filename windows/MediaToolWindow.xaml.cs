using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GamelistManager
{
    public partial class MediaToolWindow : Window
    {
        // Constants for column names
        private const string COLUMN_ROM_PATH = "Rom Path";
        private const string COLUMN_NAME = "Name";
        private const string COLUMN_HIDDEN = "Hidden";

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;
        Dictionary<string, string> _mediaItems;
        Dictionary<string, string> _mediaPaths;
        private ObservableCollection<MediaSearchItem> _mediaSearchCollection = [];
        private ObservableCollection<MediaCleanupItem> _mediaCleanupCollection = [];
        string _parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;


        public class MediaSearchItem
        {
            public required string RomPath { get; set; }
            public required string MediaType { get; set; }
            public required string MatchedFile { get; set; }
        }

        public class MediaCleanupItem
        {
            public required string Status { get; set; }
            public required string MediaType { get; set; }
            public required string FileName { get; set; }
        }

        // Title bar drag functionality
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Minimize button
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Close button
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public MediaToolWindow()
        {
            InitializeComponent();

            string jsonString = Properties.Settings.Default.MediaPaths;
            _mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;

            _mediaItems = GamelistMetaData.GetMetaDataDictionary().Values
             .Where(decl => decl.DataType == MetaDataType.Image ||
                     decl.DataType == MetaDataType.Video ||
                     decl.DataType == MetaDataType.Document)
                    .ToDictionary(decl => decl.Name, decl => decl.Type);

            dataGrid_Media.ItemsSource = _mediaSearchCollection;
            dataGrid_BadMedia.ItemsSource = _mediaCleanupCollection;

        }

        private void DeleteSelectedRows_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = dataGrid_Media.SelectedItems.Cast<MediaSearchItem>().ToList();

            if (selectedItems.Count > 0)
            {
                if (MessageBox.Show("Are you sure you want to delete the selected rows?",
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    foreach (var item in selectedItems)
                    {
                        _mediaSearchCollection.Remove(item);
                    }
                }
            }
            else
            {
                MessageBox.Show("No rows selected.", "Delete Rows", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (_mediaSearchCollection.Count == 0)
            {
                button_AddExistingMedia.IsEnabled = false;
            }
        }

        private void SetNewMediaScanningState(bool isScanning)
        {
            progressBar_ProgressBar.IsIndeterminate = isScanning;
            button_ScanForNewMediaStart.IsEnabled = !isScanning;
            button_ScanFolder.IsEnabled = !isScanning;
            textBox_SourceFolder.IsEnabled = !isScanning;
            button_ScanForNewMediaCancel.IsEnabled = isScanning;

            if (isScanning)
            {
                contextMenu_DeleteItems.IsEnabled = false;
                button_AddNewMedia.IsEnabled = false;
            }
        }

        private void SetExistingMediaScanningState(bool isScanning)
        {
            progressBar_ProgressBar.IsIndeterminate = isScanning;
            button_FindExistingMediaStart.IsEnabled = !isScanning;
            button_FindExistingMediaCancel.IsEnabled = isScanning;

            if (isScanning)
            {
                button_AddExistingMedia.IsEnabled = false;
                contextMenu_DeleteItems.IsEnabled = false;
            }
        }

        private void ResetCancellationToken()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async void Button_ScanForNewMedia_Click(object sender, RoutedEventArgs e)
        {
            string folderToScan = textBox_SourceFolder.Text;

            if (string.IsNullOrEmpty(folderToScan))
            {
                MessageBox.Show("Please specify a folder to scan.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate folder path
            try
            {
                if (!Directory.Exists(folderToScan))
                {
                    MessageBox.Show($"The folder '{folderToScan}' does not exist!", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The folder path is invalid: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get search parameters
            var mediaType = comboBox_MediaTypes.Text;
            if (!_mediaItems.TryGetValue(mediaType, out string? mediaElementName))
            {
                MessageBox.Show($"Media type '{mediaType}' is not recognized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_mediaPaths.TryGetValue(mediaElementName, out string? mediaFolderName))
            {
                MessageBox.Show($"Media path for '{mediaElementName}' is not configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update UI for scanning
            SetNewMediaScanningState(true);
            bool isCancelled = false;

            // Clear previous results
            _mediaSearchCollection.Clear();

            // Reset cancellation token
            ResetCancellationToken();

            bool excludeHidden = checkBox_SkipHiddenItems.IsChecked == true;
            bool recurse = checkBox_IncludeSubFolders.IsChecked == true;
            var searchOption = recurse ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;

            var romQuery = SharedData.DataSet.Tables[0].AsEnumerable();

            if (checkBox_ScanOnlyNeededMedia.IsChecked == true)
            {
                romQuery = romQuery.Where(row => string.IsNullOrEmpty(row.Field<string>(mediaType)));
            }

            var romList = romQuery
                .Where(row => !excludeHidden || !row.Field<bool>(COLUMN_HIDDEN))
                .Select(row => (
                    RomPath: row.Field<string>(COLUMN_ROM_PATH),
                    Name: row.Field<string>(COLUMN_NAME)
                ))
                .ToList();

            label_ScanningMessage2.Content = $"Searching for {mediaType} media...";
            var foundMedia = new ConcurrentBag<Tuple<string, string, string>>();

            try
            {
                // Build the dictionary in a separate thread
                var newMediaFilesDictionary = await Task.Run(() =>
                {
                    try
                    {
                        return Directory.GetFiles(folderToScan, "*.*", searchOption)
                          .ToDictionary(
                              file => Path.GetFileName(file),
                              file => file,
                              StringComparer.OrdinalIgnoreCase
                          );
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new InvalidOperationException("Access denied to one or more folders.");
                    }
                }, CancellationToken);

                // Create a list of keys to avoid repeatedly converting dictionary keys to a list
                var mediaFileKeys = newMediaFilesDictionary.Keys.ToList();

                // Perform fuzzy matching in parallel
                await Task.Run(() =>
                {
                    foreach (var romTuple in romList)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        string romPath = romTuple.RomPath!;
                        string romName = romTuple.Name!;
                        string normalizedName = FilePathHelper.NormalizeRomName(romPath);

                        // Perform fuzzy matching
                        string? matchedFile = TextSearchHelper.FindTextMatch(normalizedName, mediaFileKeys)
                            ?? TextSearchHelper.FindTextMatch(romName, mediaFileKeys);

                        if (!string.IsNullOrEmpty(matchedFile))
                        {
                            var matchedValue = newMediaFilesDictionary[matchedFile];
                            foundMedia.Add(Tuple.Create(romPath, mediaType, matchedValue));
                        }
                    }
                }, CancellationToken);

                // Add found media to the observable collection
                foreach (var media in foundMedia)
                {
                    _mediaSearchCollection.Add(new MediaSearchItem
                    {
                        RomPath = media.Item1,
                        MediaType = media.Item2,
                        MatchedFile = media.Item3
                    });
                }
            }
            catch (OperationCanceledException)
            {
                isCancelled = true;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while scanning: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Reset UI
            SetNewMediaScanningState(false);

            if (isCancelled)
            {
                label_ScanningMessage2.Content = $"Cancelled!";
                return;
            }

            int count = _mediaSearchCollection.Count;

            button_AddNewMedia.IsEnabled = count > 0;
            contextMenu_DeleteItems.IsEnabled = count > 0;

            label_ScanningMessage2.Content = $"Found {_mediaSearchCollection.Count} items.";
        }


        private async void Button_FindExistingMedia_Click(object sender, RoutedEventArgs e)
        {
            // GUI changes
            SetExistingMediaScanningState(true);
            label_Missing.Foreground = Brushes.Black;

            // Reset cancellation token
            ResetCancellationToken();

            // Clear results datagrid
            _mediaSearchCollection.Clear();

            // Setup bool values
            bool excludeHidden = checkBox_SkipHiddenItems.IsChecked == true;
            bool isCancelled = false;

            // Task with exception try/catch for cancelling
            try
            {
                await Task.Run(() =>
                {
                    foreach (var mediaItem in _mediaItems)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        // Media strings
                        string mediaDisplayName = mediaItem.Key;
                        string mediaElementName = mediaItem.Value;

                        // Update GUI in a safe manner
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            label_ScanningMessage.Content = mediaDisplayName;
                        });

                        // Build a full media path string
                        string mediaFolderPath = Path.Combine(
                            Path.GetDirectoryName(SharedData.XMLFilename)!,
                            _mediaPaths[mediaElementName]
                        );

                        // Skip if the directory does not exist
                        if (!Directory.Exists(mediaFolderPath)) continue;

                        // Make a list of roms where the column of the current media is empty
                        var romsWithoutMedia = SharedData.DataSet.Tables[0].AsEnumerable()
                            .Where(row => string.IsNullOrEmpty(row.Field<string>(mediaDisplayName)))
                            .Where(row => !excludeHidden || !row.Field<bool>(COLUMN_HIDDEN))
                            .Select(row => (
                                RomPath: row.Field<string>(COLUMN_ROM_PATH),
                                Name: row.Field<string>(COLUMN_NAME)
                            ))
                            .ToList();

                        if (romsWithoutMedia.Count == 0)
                        {
                            continue;
                        }

                        // Correct any element names to what is currently used
                        string correctedMediaElementName = mediaElementName == "thumbnail" ? "thumb" : mediaElementName;

                        // Make a dictionary of existing media files
                        var existingMediaFilesDictionary = Directory.GetFiles(mediaFolderPath, $"*{correctedMediaElementName}.*")
                        .ToDictionary(
                            file => Path.GetFileName(file),
                            file => file,
                            StringComparer.OrdinalIgnoreCase
                        );

                        var foundMedia = new ConcurrentBag<Tuple<string, string, string>>();

                        // Process all the roms without media
                        Parallel.ForEach(romsWithoutMedia, romTuple =>
                        {
                            string romPath = romTuple.RomPath!;
                            string romName = romTuple.Name!;
                            string normalizedName = FilePathHelper.NormalizeRomName(romPath);

                            // First search
                            string searchPattern = $"{normalizedName}-{correctedMediaElementName}";
                            var matchedFile = existingMediaFilesDictionary.Keys
                            .FirstOrDefault(key => key.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase));

                            // Second search if first did not match
                            if (matchedFile == null)
                            {
                                string searchPatternName = $"{romName}-{correctedMediaElementName}";
                                matchedFile = existingMediaFilesDictionary.Keys
                                .FirstOrDefault(key => key.StartsWith(searchPatternName, StringComparison.OrdinalIgnoreCase));
                            }

                            if (matchedFile != null)
                            {
                                var matchedValue = existingMediaFilesDictionary[matchedFile];
                                foundMedia.Add(Tuple.Create(romPath, mediaDisplayName, matchedValue));
                            }
                        });

                        // Add any found items to the datagrid
                        foreach (var media in foundMedia)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _mediaSearchCollection.Add(new MediaSearchItem
                                {
                                    RomPath = media.Item1,
                                    MediaType = media.Item2,
                                    MatchedFile = media.Item3
                                });
                            });
                        }
                    }
                }, CancellationToken);
            }
            catch (OperationCanceledException)
            {
                isCancelled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                isCancelled = true;
            }

            int count = _mediaSearchCollection.Count;

            if (isCancelled)
            {
                label_ScanningMessage.Content = "Scan cancelled!";
                label_ScanningMessage.Foreground = Brushes.Red;
            }
            else
            {
                label_ScanningMessage.Content = $"Scan completed! {count} media items were found.";
            }

            button_AddExistingMedia.IsEnabled = count > 0;
            contextMenu_DeleteItems.IsEnabled = count > 0;

            SetExistingMediaScanningState(false);
        }


        private void Button_AddExistingMedia_Click(object sender, RoutedEventArgs e)
        {
            var rowLookup = SharedData.DataSet.Tables[0].AsEnumerable()
            .ToDictionary(
                row => row.Field<string>(COLUMN_ROM_PATH)!,
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var item in _mediaSearchCollection)
            {
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string matchedFile = item.MatchedFile;

                // Convert the full path to a relative path
                string relativePath = FilePathHelper.ConvertPathToRelativePath(matchedFile, _parentFolderPath);

                if (rowLookup.TryGetValue(item.RomPath, out DataRow? foundRow))
                {
                    foundRow[mediaType] = relativePath;
                }
            }

            // Commit all changes to the DataSet once after processing
            SharedData.DataSet.AcceptChanges();

            button_AddExistingMedia.IsEnabled = false;

            _mediaSearchCollection.Clear();

            MessageBox.Show("Existing gamelistMediaPaths has been added back to the the gamelist.\n\n You will still need to save the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RadioButton_ExistingMedia_Checked(object sender, RoutedEventArgs e)
        {
            if (stackPanel_ExistingMediaControls == null)
            {
                return;
            }

            if (radioButton_ExistingMedia.IsChecked == true)
            {
                stackPanel_ExistingMediaControls.Visibility = Visibility.Visible;
                stackPanel_NewMediaControls.Visibility = Visibility.Collapsed;
                // delete radioButton_ExistingMedia.FontWeight = FontWeights.SemiBold;
                // delete radioButton_NewMedia.FontWeight = FontWeights.Regular;
            }
            else
            {
                stackPanel_NewMediaControls.Visibility = Visibility.Visible;
                stackPanel_ExistingMediaControls.Visibility = Visibility.Collapsed;
                //delete radioButton_ExistingMedia.FontWeight = FontWeights.Regular;
                //delete radioButton_NewMedia.FontWeight = FontWeights.SemiBold;
            }

            _mediaSearchCollection.Clear();

            button_AddExistingMedia.IsEnabled = false;
            button_AddNewMedia.IsEnabled = false;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mediaPaths == null || _mediaItems == null)
            {
                return;
            }

            _mediaSearchCollection.Clear();

            button_AddNewMedia.IsEnabled = false;

            int selectedIndex = comboBox_MediaTypes.SelectedIndex;
            if (selectedIndex >= 0)
            {
                string selectedText = comboBox_MediaTypes.Items[selectedIndex].ToString()!;
                string itemValue = _mediaItems[selectedText];
                string destinationFolder = _mediaPaths[itemValue];
                textBox_DestinationFolder.Text = destinationFolder;
                button_ScanForNewMediaStart.IsEnabled = true;
            }
            else
            {
                button_ScanForNewMediaStart.IsEnabled = false;
            }
        }


        private void Button_ScanFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (folderDialog.ShowDialog() == true)
            {
                ResetAddFolderControls();
                comboBox_MediaTypes.Items[0] = "<choose>";
                var folderName = folderDialog.FolderName;
                textBox_SourceFolder.Text = folderName;
                comboBox_MediaTypes.SelectedIndex = 0;
                comboBox_MediaTypes.IsEnabled = true;
            }
        }

        private void Button_AddNewMedia_Click(object sender, RoutedEventArgs e)
        {
            string mediaName = comboBox_MediaTypes.Text;
            string mediaFolder = textBox_DestinationFolder.Text;

            bool overwrite = checkBox_OverwriteExisting.IsChecked == true;

            string overwriteMessage = string.Empty;
            if (overwrite)
            {
                overwriteMessage = "Existing files will be backed up first, then overwritten\n\n";
            }

            MessageBoxResult result = MessageBox.Show(
            $"This will copy all new media items to the ./{mediaFolder} directory\n\n" +
            $"{overwriteMessage}Make sure you have picked the correct media type before proceeding or else the files will have incorrect metadata naming and possibly be in the wrong folder",
            "Confirm Action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var pathToRowMap = SharedData.DataSet.Tables[0].AsEnumerable()
            .ToDictionary(row => row.Field<string>(COLUMN_ROM_PATH)!, row => row);

            int itemsAdded = 0;
            progressBar_ProgressBar.IsIndeterminate = true;

            foreach (var item in _mediaSearchCollection)
            {
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string elementName = _mediaItems[mediaType];
                string filePath = item.MatchedFile;

                string parentFolder = Path.GetDirectoryName(SharedData.XMLFilename)!;
                string destinationFolder = Path.Combine(parentFolder, mediaFolder);

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Batocera naming standard
                elementName = (elementName == "thumbnail" ? "thumb" : elementName);

                string normalizedRomName = FilePathHelper.NormalizeRomName(romPath);
                string fileExtension = Path.GetExtension(filePath);
                string newFileName = $"{normalizedRomName}-{elementName}{fileExtension}";
                string destinationFile = Path.Combine(destinationFolder, newFileName);

                if (File.Exists(destinationFile))
                {
                    if (overwrite)
                    {
                        string parentDirectory = Directory.GetParent(destinationFile)!.Name;
                        string backupFolder = Path.Combine(
                            SharedData.ProgramDirectory,
                            "media backup",
                            "replaced",
                            SharedData.CurrentSystem,
                            parentDirectory
                        );

                        if (!Directory.Exists(backupFolder))
                        {
                            Directory.CreateDirectory(backupFolder);
                        }

                        string backupFile = Path.Combine(backupFolder, newFileName);
                        File.Copy(destinationFile, backupFile, true);
                    }
                    else
                    {
                        continue;
                    }
                }

                try
                {
                    File.Copy(filePath, destinationFile, true);
                    itemsAdded++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"File Copy Error: {ex.Message}", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (pathToRowMap.TryGetValue(romPath, out DataRow? foundRow))
                {
                    foundRow[mediaType] = $"./{mediaFolder}/{newFileName}";
                }
            }

            progressBar_ProgressBar.IsIndeterminate = false;

            SharedData.DataSet.AcceptChanges();

            button_AddExistingMedia.IsEnabled = false;

            _mediaSearchCollection.Clear();
            button_AddNewMedia.IsEnabled = false;

            MessageBox.Show($"{itemsAdded} {mediaName} items have been added to the gamelist\n\n You will still need to save the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private async void Button_ScanExistingMedia_Click(object sender, RoutedEventArgs e)
        {
            SetMediaCleanupScanningState(true);

            // Reset cancellation token
            ResetCancellationToken();

            // Reset labels            
            label_Missing.Content = string.Empty;
            label_SingleColor.Content = string.Empty;
            label_Unused.Content = string.Empty;

            // Clear datagrid
            _mediaCleanupCollection.Clear();

            // Get a list of media paths as defined in the gamelist
            var gamelistMediaPaths = await GetGamelistMediaPathsAsync();

            // Get a list of all media in the media folders
            var allMedia = await GetExistingMediaFiles();

            bool isCancelled = false;

            // Check for missing media
            if (checkBox_MissingMedia.IsChecked == true)
            {
                label_Missing.Foreground = Brushes.Blue;
                label_Missing.Content = "Running scan...";
                int missingCount = 0;

                await Task.Run(() =>
                {
                    try
                    {
                        var uniqueDirectories = gamelistMediaPaths
                            .Select(kvp => Path.GetDirectoryName(Path.Combine(_parentFolderPath, kvp.Value)))
                            .Where(dir => !string.IsNullOrEmpty(dir))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var allMediaFromGamelistDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var directory in uniqueDirectories)
                        {
                            if (Directory.Exists(directory))
                            {
                                var filesInDir = Directory.GetFiles(directory, "*.*", System.IO.SearchOption.TopDirectoryOnly)
                                    .Select(f => Path.GetFullPath(f));

                                foreach (var file in filesInDir)
                                {
                                    allMediaFromGamelistDirs.Add(file);
                                }
                            }
                        }

                        var missingItems = new ConcurrentBag<MediaCleanupItem>();

                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = CancellationToken
                        };

                        Parallel.ForEach(gamelistMediaPaths, parallelOptions, (item) =>
                        {
                            string itemType = item.Key;
                            string itemPath = item.Value;

                            string fullPath = Path.GetFullPath(Path.Combine(_parentFolderPath, itemPath));

                            if (!allMediaFromGamelistDirs.Contains(fullPath))
                            {
                                missingItems.Add(new MediaCleanupItem
                                {
                                    Status = "Missing",
                                    MediaType = itemType,
                                    FileName = itemPath
                                });

                                Interlocked.Increment(ref missingCount);
                            }
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var missingItem in missingItems)
                            {
                                _mediaCleanupCollection.Add(missingItem);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        isCancelled = true;
                    }
                }, CancellationToken);

                if (isCancelled == true)
                {
                    label_Missing.Content = "Cancelled!";
                    label_Missing.Foreground = Brushes.Red;
                }
                else
                {
                    label_Missing.Content = $"[{missingCount} missing media items]";
                    if (missingCount == 0)
                    {
                        label_Missing.Foreground = Brushes.Green;
                    }
                    else
                    {
                        button_FixMissing.Visibility = Visibility.Visible;
                        label_Missing.Foreground = Brushes.Red;
                    }
                }
            }


            if (!isCancelled && checkBox_UnusedMedia.IsChecked == true)
            {
                label_Unused.Foreground = Brushes.Blue;
                label_Unused.Content = "Running scan...";
                int unusedCount = 0;

                await Task.Run(() =>
                {
                    try
                    {
                        var gamelistPathsLookup = gamelistMediaPaths
                            .ToLookup(
                                kvp => Path.GetFullPath(Path.Combine(_parentFolderPath, kvp.Value)),
                                kvp => kvp.Key,
                                StringComparer.OrdinalIgnoreCase);

                        var missingItems = new ConcurrentBag<MediaCleanupItem>();

                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = CancellationToken
                        };

                        Parallel.ForEach(allMedia, parallelOptions, (mediaItem) =>
                        {
                            var matchingItems = gamelistPathsLookup[mediaItem];

                            if (!matchingItems.Any())
                            {
                                missingItems.Add(new MediaCleanupItem
                                {
                                    Status = "Unused",
                                    MediaType = string.Empty,
                                    FileName = mediaItem
                                });

                                Interlocked.Increment(ref unusedCount);
                            }
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var missingItem in missingItems)
                            {
                                _mediaCleanupCollection.Add(missingItem);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        isCancelled = true;
                    }
                }, CancellationToken);

                if (isCancelled == true)
                {
                    label_Unused.Content = "Cancelled!";
                    label_Unused.Foreground = Brushes.Red;
                }
                else
                {
                    label_Unused.Content = $"[{unusedCount} unused media items]";
                    if (unusedCount == 0)
                    {
                        label_Unused.Foreground = Brushes.Green;
                        button_FixBad.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        button_FixUnused.Visibility = Visibility.Visible;
                        label_Unused.Foreground = Brushes.Red;
                    }
                }
            }

            if (!isCancelled && checkBox_SingleColor.IsChecked == true)
            {
                label_SingleColor.Foreground = Brushes.Blue;
                label_SingleColor.Content = "Running scan...";
                int badCount = 0;

                await Task.Run(() =>
                {
                    var badItems = new ConcurrentBag<MediaCleanupItem>();

                    try
                    {
                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = CancellationToken
                        };

                        Parallel.ForEach(gamelistMediaPaths, parallelOptions, (item) =>
                        {
                            string mediaPath = item.Value;
                            string mediaType = item.Key;

                            string fileExtension = Path.GetExtension(mediaPath).ToLowerInvariant();
                            string fullPath = Path.GetFullPath(Path.Combine(_parentFolderPath, mediaPath));

                            // Filter by file extensions (case-insensitive)
                            if (fileExtension is not (".jpg" or ".png"))
                            {
                                return;
                            }

                            string result = ImageHelper.CheckImage(fullPath);

                            if (result is ("Corrupt" or "Single Color"))
                            {
                                badItems.Add(new MediaCleanupItem
                                {
                                    Status = result,
                                    MediaType = mediaType,
                                    FileName = fullPath
                                });
                                Interlocked.Increment(ref badCount);
                            }
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var badItem in badItems)
                            {
                                _mediaCleanupCollection.Add(badItem);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        isCancelled = true;
                    }
                }, CancellationToken);

                if (isCancelled == true)
                {
                    label_SingleColor.Content = "Cancelled!";
                    label_SingleColor.Foreground = Brushes.Red;
                }
                else
                {
                    label_SingleColor.Content = $"[{badCount} bad media items]";
                    if (badCount == 0)
                    {
                        label_SingleColor.Foreground = Brushes.Green;
                    }
                    else
                    {
                        label_SingleColor.Foreground = Brushes.Red;
                        button_FixBad.Visibility = Visibility.Visible;
                    }
                }
            }

            progressBar_ProgressBar2.IsIndeterminate = false;
            button_ScanExistingMediaStart.IsEnabled = true;
            button_ScanExistingMediaCancel.IsEnabled = false;

            // Fix buttons enabled now that scanning is done
            button_FixMissing.IsEnabled = button_FixMissing.IsVisible;
            button_FixUnused.IsEnabled = button_FixUnused.IsVisible;
            button_FixBad.IsEnabled = button_FixBad.IsVisible;

            int count = _mediaCleanupCollection.Count;
            contextMenu_DeleteItems2.IsEnabled = count > 0;
                  
        }

        private async Task<ConcurrentBag<KeyValuePair<string, string>>> GetGamelistMediaPathsAsync()
        {
            var media = new ConcurrentBag<KeyValuePair<string, string>>();

            await Task.Run(() =>
            {
                // Sequential processing is more efficient here
                foreach (DataRow row in SharedData.DataSet.Tables[0].Rows)
                {
                    foreach (var mediaItem in _mediaItems.Keys)
                    {
                        string columnName = mediaItem;
                        object cellValue = row[columnName];

                        if (cellValue != null && cellValue != DBNull.Value && !string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            media.Add(new KeyValuePair<string, string>(columnName, cellValue.ToString()!));
                        }
                    }
                }
            });

            return media;
        }

        private async Task<List<string>> GetExistingMediaFiles()
        {
            var allFiles = new ConcurrentBag<string>();
            var uniqueFolders = new HashSet<string>(_mediaPaths.Values);

            await Task.Run(() =>
            {
                Parallel.ForEach(uniqueFolders, uniqueFolderName =>
                {
                    // Skip music folder
                    if (uniqueFolderName == "music")
                    {
                        return;
                    }

                    string folderPath = Path.Combine(_parentFolderPath, uniqueFolderName);

                    if (Directory.Exists(folderPath))
                    {
                        foreach (string file in Directory.GetFiles(folderPath))
                        {
                            allFiles.Add(file);
                        }
                    }
                });
            });

            return allFiles.Distinct().ToList();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                _cancellationTokenSource?.Cancel();
            }
        }

        private async void Button_FixMissing_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to remove the paths for missing media items in the gamelist?",
                                    "Confirm",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var missingItems = _mediaCleanupCollection
                    .Where(MediaCleanupDataItem => MediaCleanupDataItem.Status == "Missing")
                    .ToList();

                var fileNames = missingItems.Select(item => item.FileName).ToList();

                Mouse.OverrideCursor = Cursors.Wait;

                await ClearMediaPathsAsync(fileNames);

                foreach (var item in missingItems)
                {
                    _mediaCleanupCollection.Remove(item);
                }

                Mouse.OverrideCursor = null;

                button_FixMissing.IsEnabled = false;

                SharedData.IsDataChanged = true;
                SharedData.DataSet.AcceptChanges();

                MessageBox.Show("Completed!", "Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static void BackupMedia(string system, List<string> files, string folder, bool delete)
        {
            foreach (string filePath in files)
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    continue;
                }

                if (delete)
                {
                    FileSystem.DeleteFile(filePath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                }
                else
                {
                    string parentDirectory = Directory.GetParent(filePath)!.Name;
                    string backupFolder = Path.Combine(
                        SharedData.ProgramDirectory,
                        "media backup",
                        folder,
                        system,
                        parentDirectory
                    );
                    if (!Directory.Exists(backupFolder))
                    {
                        Directory.CreateDirectory(backupFolder);
                    }
                    string fileName = Path.GetFileName(filePath);
                    string backupPath = Path.Combine(backupFolder, fileName);
                    File.Move(filePath, backupPath, true);
                }
            }
        }

        private void Button_FixUnused_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to backup the unused media first?  Selecting 'No' will send them straight to the windows recycle bin.  \n\nSelect Cancel to abort",
            "Confirm",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            bool delete = result != MessageBoxResult.Yes;

            var unusedItems = _mediaCleanupCollection
            .Where(item => item.Status == "Unused")
            .ToList();

            var fileNames = unusedItems.Select(item => item.FileName).ToList();

            Mouse.OverrideCursor = Cursors.Wait;

            BackupMedia(SharedData.CurrentSystem, fileNames, "unused", delete);

            Mouse.OverrideCursor = null;

            foreach (var item in unusedItems)
            {
                _mediaCleanupCollection.Remove(item);
            }

            button_FixUnused.IsEnabled = false;

            MessageBox.Show("Completed!", "Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Button_FixBad_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to backup the bad media first?  Selecting 'No' will send them straight to the windows recycle bin.  \n\nSelect Cancel to abort",
               "Confirm",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            bool delete = result != MessageBoxResult.Yes;

            var badItems = _mediaCleanupCollection
            .Where(MediaCleanupDataItem => MediaCleanupDataItem.Status == "Single Color" || MediaCleanupDataItem.Status == "Corrupt")
            .ToList();

            var fileNames = badItems.Select(item => item.FileName).ToList();

            Mouse.OverrideCursor = Cursors.Wait;

            BackupMedia(SharedData.CurrentSystem, fileNames, "bad", delete);

            Mouse.OverrideCursor = null;

            foreach (var item in badItems)
            {
                _mediaCleanupCollection.Remove(item);
            }

            var relativePaths = fileNames
                .Select(f => FilePathHelper.ConvertPathToRelativePath(f, _parentFolderPath))
                .ToList();

            await ClearMediaPathsAsync(relativePaths);
            progressBar_ProgressBar2.IsIndeterminate = false;

            button_FixBad.IsEnabled = false;
            SharedData.DataSet.AcceptChanges();

            SharedData.IsDataChanged = true;

            MessageBox.Show("Completed!", "Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static async Task ClearMediaPathsAsync(List<string> itemsToRemove)
        {
            var dataTable = SharedData.DataSet.Tables[0];
            var changes = new ConcurrentBag<(DataRow Row, DataColumn Column)>();

            await Task.Run(() =>
            {
                Parallel.ForEach(dataTable.AsEnumerable(), row =>
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (row[column] is string cellValue && itemsToRemove.Contains(cellValue))
                        {
                            changes.Add((row, column));
                        }
                    }
                });
            });

            // Apply changes sequentially
            foreach (var change in changes)
            {
                change.Row[change.Column] = DBNull.Value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if any scanning operation is in progress
            bool isScanning = button_ScanForNewMediaCancel.IsEnabled ||
                              button_FindExistingMediaCancel.IsEnabled ||
                              button_ScanExistingMediaCancel.IsEnabled;

            if (isScanning)
            {
                var result = MessageBox.Show(
                    "A scan is currently in progress. Do you want to cancel the scan and close the window?",
                    "Scan in Progress",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // Cancel the scan
                _cancellationTokenSource?.Cancel();
            }

            // Cleanup
            TextSearchHelper.ClearCache();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetAddFolderControls();
        }

        private void ResetAddFolderControls()
        {
            comboBox_MediaTypes.SelectionChanged -= ComboBox_SelectionChanged;

            textBox_DestinationFolder.Text = string.Empty;

            button_AddNewMedia.IsEnabled = false;
            button_ScanForNewMediaCancel.IsEnabled = false;
            button_ScanForNewMediaStart.IsEnabled = false;

            textBox_SourceFolder.Text = "<select a folder>";
            comboBox_MediaTypes.Items.Clear();
            comboBox_MediaTypes.Items.Add(string.Empty);
            comboBox_MediaTypes.IsEnabled = false;

            comboBox_MediaTypes.Items.Clear();
            comboBox_MediaTypes.Items.Add(string.Empty);
            foreach (var item in _mediaItems)
            {
                comboBox_MediaTypes.Items.Add(item.Key);
            }
            comboBox_MediaTypes.SelectedIndex = 0;
        }

        private void ComboBox_MediaTypes_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_MediaTypes.IsEnabled == false)
            {
                return;
            }

            if (comboBox_MediaTypes.Items[0].ToString() == "<choose>")
            {
                comboBox_MediaTypes.SelectedIndex = 1;
                comboBox_MediaTypes.Items.RemoveAt(0);
                comboBox_MediaTypes.SelectionChanged += ComboBox_SelectionChanged;
                button_ScanForNewMediaStart.IsEnabled = true;
            }
        }

        private void SetMediaCleanupScanningState(bool isScanning)
        {
            progressBar_ProgressBar2.IsIndeterminate = isScanning;
            button_ScanExistingMediaStart.IsEnabled = !isScanning;
            button_ScanExistingMediaCancel.IsEnabled = isScanning;

            // Disable checkboxes during scan
            checkBox_MissingMedia.IsEnabled = !isScanning;
            checkBox_UnusedMedia.IsEnabled = !isScanning;
            checkBox_SingleColor.IsEnabled = !isScanning;

            // Disable datagrid context menu
            contextMenu_DeleteItems2.IsEnabled = !isScanning;

            if (isScanning)
            {
                // Hide and disable all fix buttons during scan
                button_FixMissing.Visibility = Visibility.Hidden;
                button_FixMissing.IsEnabled = false;
                button_FixUnused.Visibility = Visibility.Hidden;
                button_FixUnused.IsEnabled = false;
                button_FixBad.Visibility = Visibility.Hidden;
                button_FixBad.IsEnabled = false;
            }
        }

        private void contextMenu_DeleteItems2_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = dataGrid_BadMedia.SelectedItems.Cast<MediaCleanupItem>().ToList();

            if (selectedItems.Count > 0)
            {
                if (MessageBox.Show("Are you sure you want to delete the selected rows?",
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    foreach (var item in selectedItems)
                    {
                        _mediaCleanupCollection.Remove(item);
                    }
                }
            }
            else
            {
                MessageBox.Show("No rows selected.", "Delete Rows", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (_mediaCleanupCollection.Count == 0)
            {
                button_FixBad.IsEnabled = false;
                button_FixMissing.IsEnabled = false;
                button_FixUnused.IsEnabled = false;
            }
        }
    }
}