using GamelistManager.classes;
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
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;
        Dictionary<string, string> _mediaItems;
        Dictionary<string, string> _mediaPaths;
        private ObservableCollection<MediaSearchItem> _mediaSearchCollection = new ObservableCollection<MediaSearchItem>();
        private ObservableCollection<MediaCleanupItem> _mediaCleanupCollection = new ObservableCollection<MediaCleanupItem>();
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
            // Get the selected dataGridItems

            var selectedItems = dataGrid_Media.SelectedItems.Cast<MediaSearchItem>().ToList();

            if (selectedItems.Count > 0)
            {
                // Ask for confirmation
                if (MessageBox.Show("Are you sure you want to delete the selected rows?",
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Remove each selected uniqueFolderName from the collection
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


        private async void button_ScanForNewMedia_Click(object sender, RoutedEventArgs e)
        {
            string folderToScan = textBox_SourceFolder.Text;

            if (string.IsNullOrEmpty(folderToScan))
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


            //blah check below if can be cleaned up
            // Update UI for scanning
            contextMenu_DeleteItems.IsEnabled = false;
            progressBar_ProgressBar.IsIndeterminate = true;
            button_ScanForNewMediaStart.IsEnabled = false;
            button_ScanFolder.IsEnabled = false;
            textBox_SourceFolder.IsEnabled = false;
            button_AddNewMedia.IsEnabled = false;
            button_ScanForNewMediaCancel.IsEnabled = true;
            bool isCancelled = false;

            // Clear previous results
            _mediaSearchCollection.Clear();

            // New cancel token
            _cancellationTokenSource = new CancellationTokenSource();

            bool excludeHidden = checkBox_SkipHiddenItems.IsChecked == true;
            bool recurse = checkBox_IncludeSubFolders.IsChecked == true;
            var searchOption = recurse ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;

            var romQuery = SharedData.DataSet.Tables[0].AsEnumerable();

            if (checkBox_ScanOnlyNeededMedia.IsChecked == true)
            {
                romQuery = romQuery.Where(row => string.IsNullOrEmpty(row.Field<string>(mediaType)));
            }

            var romList = romQuery
                .Where(row => !excludeHidden || !row.Field<bool>("Hidden"))
                .Select(row => (
                    RomPath: row.Field<string>("Rom Path"),
                    Name: row.Field<string>("Name")
                ))
                .ToList();

            label_ScanningMessage2.Content = $"Searching for {mediaType} media...";
            var foundMedia = new ConcurrentBag<Tuple<string, string, string>>();

            try
            {
                // Build the dictionary in a separate thread
                var newMediaFilesDictionary = await Task.Run(() =>
                    Directory.GetFiles(folderToScan, "*.*", searchOption)
                      .ToDictionary(
                          file => Path.GetFileName(file), // Key: File name
                          file => file,                  // Value: Full file path
                          StringComparer.OrdinalIgnoreCase
                      ),
                    _cancellationToken
                );

                // Create a list of keys to avoid repeatedly converting dictionary keys to a list
                var mediaFileKeys = newMediaFilesDictionary.Keys.ToList();

                // Perform fuzzy matching in parallel
                await Task.Run(() =>
                {
                    // Iterate over the ROM list sequentially
                    foreach (var romTuple in romList)
                    {
                        string romPath = romTuple.RomPath!;
                        string romName = romTuple.Name!;
                        string filteredRomPath = Path.GetFileNameWithoutExtension(romPath)?.Replace("./", "") ?? string.Empty;

                        // Perform fuzzy matching
                        string? matchedFile = TextSearch.FindTextMatch(filteredRomPath, mediaFileKeys)
                            ?? TextSearch.FindTextMatch(romName, mediaFileKeys);

                        if (!string.IsNullOrEmpty(matchedFile))
                        {
                            // Get the full path of the matched file
                            var matchedValue = newMediaFilesDictionary[matchedFile];

                            // Add to the thread-safe collection (if necessary, use locks here if this list is accessed concurrently)
                            foundMedia.Add(Tuple.Create(romPath, mediaType, matchedValue));
                        }
                    }
                }, _cancellationToken);

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
                // Graceful cancellation
                isCancelled = true;
            }
            catch (Exception ex)
            {
                // Handle errors
                MessageBox.Show($"An error occurred while scanning: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            // Reset UI
            progressBar_ProgressBar.IsIndeterminate = false;
            button_ScanForNewMediaStart.IsEnabled = true;
            button_ScanFolder.IsEnabled = true;
            textBox_SourceFolder.IsEnabled = true;
            button_ScanForNewMediaCancel.IsEnabled = false;

            if (isCancelled)
            {
                label_ScanningMessage2.Content = $"Cancelled!";
                return;
            }

            if (_mediaSearchCollection.Count > 0)
            {
                button_AddNewMedia.IsEnabled = true;
                contextMenu_DeleteItems.IsEnabled = true;
            }

            label_ScanningMessage2.Content = $"Found {_mediaSearchCollection.Count} items.";

        }


        private async void button_FindExistingMedia_Click(object sender, RoutedEventArgs e)
        {

            // GUI changes
            progressBar_ProgressBar.IsIndeterminate = true;
            button_FindExistingMediaStart.IsEnabled = false;
            button_FindExistingMediaCancel.IsEnabled = true;
            button_AddExistingMedia.IsEnabled = false;
            label_Missing.Foreground = Brushes.Black;
            contextMenu_DeleteItems.IsEnabled = false;

            // New cancel token
            _cancellationTokenSource = new CancellationTokenSource();

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
                        // Throw if cancelled
                        _cancellationToken.ThrowIfCancellationRequested();

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

                        // Make a list of roms where the column of the current media (mediaDisplayName)
                        // is empty.  Skip hidden roms if excludeHidden is true
                        // Roms with existing media are skipped
                        var romsWithoutMedia = SharedData.DataSet.Tables[0].AsEnumerable()
                            .Where(row => string.IsNullOrEmpty(row.Field<string>(mediaDisplayName)))
                            .Where(row => !excludeHidden || !row.Field<bool>("Hidden"))
                            .Select(row => (
                                RomPath: row.Field<string>("Rom Path"),
                                Name: row.Field<string>("Name")
                            ))
                            .ToList();

                        // Skip if there's no roms without media
                        if (romsWithoutMedia.Count == 0)
                        {
                            continue;
                        }

                        // Correct any element names to what is currently used
                        string correctedMediaElementName = mediaElementName == "thumbnail" ? "thumb" : mediaElementName;

                        // Make a dictionary of the existingMediaFiles in the existing media folder
                        // Filter for the current media element
                        var existingMediaFilesDictionary = Directory.GetFiles(mediaFolderPath, $"*{correctedMediaElementName}.*")
                        .ToDictionary(
                            file => Path.GetFileName(file), // Key: File name
                            file => file,                  // Value: Full file path
                            StringComparer.OrdinalIgnoreCase
                        );

                        // New concurrent bag consisting of a 3 item tuple
                        var foundMedia = new ConcurrentBag<Tuple<string, string, string>>();

                        // Process all the roms without media
                        Parallel.ForEach(romsWithoutMedia, romTuple =>
                        {

                            // Rom Variables
                            string romPath = romTuple.RomPath!;
                            string romName = romTuple.Name!;

                            string filteredRomPath = Path.GetFileNameWithoutExtension(romPath).Replace("./", "");

                            // First search
                            string searchPattern = $"{filteredRomPath}-{correctedMediaElementName}";
                            var matchedFile = existingMediaFilesDictionary.Keys
                            .FirstOrDefault(key => key.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase));

                            //Second search if first did not match
                            if (matchedFile == null)
                            {
                                string searchPatternName = $"{romName}-{correctedMediaElementName}";
                                matchedFile = existingMediaFilesDictionary.Keys
                                .FirstOrDefault(key => key.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase));
                            }

                            // Add to concurrentbag tuple
                            if (matchedFile != null)
                            {
                                var matchedValue = existingMediaFilesDictionary[matchedFile];
                                foundMedia.Add(Tuple.Create(romPath, mediaDisplayName, matchedValue));
                            }
                        });

                        // Add any found items to the datagrid
                        foreach (var media in foundMedia)
                        {
                            // Safe update in gui thread
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
                }, _cancellationToken);
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

            // Finalize UI stuff
            if (isCancelled)
            {
                label_ScanningMessage.Content = "Scan cancelled!";
                label_ScanningMessage.Foreground = Brushes.Red;
            }
            else
            {
                int count = _mediaSearchCollection.Count;
                label_ScanningMessage.Content = $"Scan completed! {count} media items were found.";
                button_AddExistingMedia.IsEnabled = count > 0;
                contextMenu_DeleteItems.IsEnabled = count > 0;
            }

            progressBar_ProgressBar.IsIndeterminate = false;
            button_FindExistingMediaStart.IsEnabled = true;
            button_FindExistingMediaCancel.IsEnabled = false;
        }


        private void button_AddExistingMedia_Click(object sender, RoutedEventArgs e)
        {
            var rowLookup = SharedData.DataSet.Tables[0].AsEnumerable()
            .ToDictionary(
                row => row.Field<string>("Rom Path")!,
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var item in _mediaSearchCollection)
            {
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string matchedFile = item.MatchedFile;

                // Convert the full path to a relative path
                string relativePath = PathConverter.ConvertToRelativePath(matchedFile);

                if (rowLookup.TryGetValue(item.RomPath, out DataRow? foundRow))

                    if (foundRow != null)
                    {
                        // Update the found row
                        foundRow[mediaType] = relativePath;
                    }
            }

            // Commit all changes to the DataSet once after processing
            SharedData.DataSet.AcceptChanges();

            button_AddExistingMedia.IsEnabled = false;

            _mediaSearchCollection.Clear();

            MessageBox.Show("Existing gamelistMediaPaths has been added back to the the gamelist.\n\n You will still need to save the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void radioButton_ExistingMedia_Checked(object sender, RoutedEventArgs e)
        {
            if (stackPanel_ExistingMediaControls == null)
            {
                return;
            }

            if (radioButton_ExistingMedia.IsChecked == true)
            {
                stackPanel_ExistingMediaControls.Visibility = Visibility.Visible;
                stackPanel_NewMediaControls.Visibility = Visibility.Collapsed;
                radioButton_ExistingMedia.FontWeight = FontWeights.SemiBold;
                radioButton_NewMedia.FontWeight = FontWeights.Regular;
            }
            else
            {
                stackPanel_NewMediaControls.Visibility = Visibility.Visible;
                stackPanel_ExistingMediaControls.Visibility = Visibility.Collapsed;
                radioButton_ExistingMedia.FontWeight = FontWeights.Regular;
                radioButton_NewMedia.FontWeight = FontWeights.SemiBold;

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
                var selectedItem = comboBox_MediaTypes.SelectedItem;
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


        private void button_ScanFolder_Click(object sender, RoutedEventArgs e)
        {

            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };
            //blahcom
            if (folderDialog.ShowDialog() == true)
            {
                ResetAddFolderControls();
                // The combobox has a default empty first item
                // Only want to see the choose text after folder is selected
                comboBox_MediaTypes.Items[0] = "<choose>";
                var folderName = folderDialog.FolderName;
                textBox_SourceFolder.Text = folderName;
                comboBox_MediaTypes.SelectedIndex = 0;
                comboBox_MediaTypes.IsEnabled = true;
            }
        }

        private void button_AddNewMedia_Click(object sender, RoutedEventArgs e)
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
            .ToDictionary(row => row.Field<string>("Rom Path")!, row => row);

            int itemsAdded = 0;
            progressBar_ProgressBar.IsIndeterminate = true;

            foreach (var item in _mediaSearchCollection)
            {
                // Extract values from the DataGrid row
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string elementName = _mediaItems[mediaType];
                string filePath = item.MatchedFile;

                string parentFolder = Path.GetDirectoryName(SharedData.XMLFilename)!;

                string destinationFolder = $"{parentFolder}\\{mediaFolder}";

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Batocera naming standard
                elementName = (elementName == "thumbnail" ? "thumb" : elementName);

                string filteredRomPath = Path.GetFileNameWithoutExtension(romPath.TrimStart('.', '/'));
                string fileName = Path.GetFileName(filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);
                string newFileName = $"{filteredRomPath}-{elementName}{fileExtension}";
                string destinationFile = $"{destinationFolder}\\{newFileName}";

                //blah fix this or check to make sure names are right

                if (System.IO.File.Exists(destinationFile))
                {
                    if (overwrite)
                    {
                        string parentDirectory = Directory.GetParent(destinationFile)!.Name;
                        string backupFolder = $"{SharedData.ProgramDirectory}\\media backup\\replaced\\{SharedData.CurrentSystem}\\{parentDirectory}";

                        if (!Directory.Exists(backupFolder))
                        {
                            Directory.CreateDirectory(backupFolder);
                        }

                        string backupFile = Path.Combine(backupFolder, fileName);
                        System.IO.File.Copy(destinationFile, backupFile, true);

                    }
                    else
                    {
                        continue;
                    }
                }

                try
                {
                    System.IO.File.Copy(filePath, destinationFile, true);
                    itemsAdded++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"File Copy Error: {ex.Message}", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Find the corresponding DataRow in the DataTable
                if (pathToRowMap.TryGetValue(romPath, out DataRow? foundRow))
                {
                    foundRow[mediaType] = $"./{mediaFolder}/{newFileName}";
                }
            }

            progressBar_ProgressBar.IsIndeterminate = false;

            // Commit all changes to the DataSet once after processing
            SharedData.DataSet.AcceptChanges();

            button_AddExistingMedia.IsEnabled = false;

            _mediaSearchCollection.Clear();
            button_AddNewMedia.IsEnabled = false;

            SharedData.DataSet.AcceptChanges();

            MessageBox.Show($"{itemsAdded} {mediaName} items have been added to the gamelist\n\n You will still need to save the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private async void button_ScanExistingMedia_Click(object sender, RoutedEventArgs e)
        {
            // Start indeterminite progressbar animation
            progressBar_ProgressBar2.IsIndeterminate = true;

            // New cancel token
            _cancellationTokenSource = new CancellationTokenSource();

            // Set button states
            button_ScanExistingMediaStart.IsEnabled = false;
            button_ScanExistingMediaCancel.IsEnabled = true;
            button_FixMissing.Visibility = Visibility.Hidden;
            button_FixMissing.IsEnabled = false;
            button_FixUnused.Visibility = Visibility.Hidden;
            button_FixUnused.IsEnabled = false;
            button_FixBad.Visibility = Visibility.Hidden;
            button_FixBad.IsEnabled = false;

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

            // Cancel flag
            bool isCancelled = false;

            // Check for missing media first
            // Using gamelistMediaPaths collection
            if (checkBox_MissingMedia.IsChecked == true)
            {
                label_Missing.Foreground = Brushes.Blue;
                label_Missing.Content = "Running scan...";
                int missingCount = 0;

                await Task.Run(() =>
                {
                    try
                    {
                        // Normalize all media paths by replacing forward slashes with backslashes
                        var allMediaNormalized = allMedia.Select(p => Path.GetFullPath(p).Replace('/', '\\')).ToHashSet(StringComparer.OrdinalIgnoreCase);

                        // Thread-safe collection to collect missing media items
                        var missingItems = new ConcurrentBag<MediaCleanupItem>();

                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = _cancellationToken
                        };

                        // Use Parallel.ForEach to loop through gamelistMediaPaths in parallel
                        Parallel.ForEach(gamelistMediaPaths, parallelOptions, (item) =>
                        {
                            string itemType = item.Key;
                            string itemPath = item.Value;

                            // Normalize the path (replace '/' with '\\')
                            string fullPath = Path.GetFullPath(Path.Combine(_parentFolderPath, itemPath)).Replace('/', '\\');

                            // Check if the media item exists in allMedia (case-insensitive and normalized comparison)
                            if (!allMediaNormalized.Contains(fullPath))
                            {
                                // Add missing item to the thread-safe collection
                                missingItems.Add(new MediaCleanupItem
                                {
                                    Status = "Missing",
                                    MediaType = itemType,
                                    FileName = itemPath
                                });

                                // Increment missingCount in a thread-safe manner
                                Interlocked.Increment(ref missingCount);
                            }
                        });

                        // Perform UI updates after the parallel loop completes
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
                        // Task was canceled, exit gracefully
                        isCancelled = true;
                    }
                }, _cancellationToken);


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
                        // Precompute gamelistMediaPaths full paths for fast lookup
                        var gamelistPathsLookup = gamelistMediaPaths
                            .ToLookup(
                                kvp => Path.GetFullPath(Path.Combine(_parentFolderPath, kvp.Value)),  // Key is the full path
                                kvp => kvp.Key, // MediaType as the value
                                StringComparer.OrdinalIgnoreCase);  // Case-insensitive comparison

                        // Concurrent collection for collecting missing items in a thread-safe manner
                        var missingItems = new ConcurrentBag<MediaCleanupItem>();

                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = _cancellationToken
                        };

                        // Process allMedia in parallel
                        Parallel.ForEach(allMedia, parallelOptions, (mediaItem) =>
                        {
                            // Check if the mediaItem exists in the precomputed lookup
                            var matchingItems = gamelistPathsLookup[mediaItem];

                            if (!matchingItems.Any())
                            {
                                // Item is not in gamelistMediaPaths, add to the missing items collection
                                missingItems.Add(new MediaCleanupItem
                                {
                                    Status = "Unused",
                                    MediaType = string.Empty, // Optionally handle media type
                                    FileName = mediaItem
                                });

                                // Increment unusedCount in a thread-safe manner
                                Interlocked.Increment(ref unusedCount);
                            }
                        });

                        // Perform UI updates after the parallel loop completes
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
                        // Task was canceled, exit gracefully
                        isCancelled = true;
                    }
                }, _cancellationToken);


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
                    // Thread-safe collection to collect missing media items
                    var badItems = new ConcurrentBag<MediaCleanupItem>();

                    try
                    {
                        var parallelOptions = new ParallelOptions
                        {
                            CancellationToken = _cancellationToken
                        };

                        Parallel.ForEach(gamelistMediaPaths, parallelOptions, (item) =>
                        {
                            string mediaPath = item.Value;
                            string mediaType = item.Key;

                            // Extract the file name from the media path
                            string fileName = Path.GetFileName(mediaPath);

                            // Extract the parent directory from the media path
                            string parentDirectory = Path.GetDirectoryName(mediaPath)!;

                            // Extract the file extension
                            string fileExtension = Path.GetExtension(fileName);

                            // Combine the _parentFolderPath with mediaPath, ensuring it resolves correctly
                            string fullPath = Path.GetFullPath(Path.Combine(_parentFolderPath, mediaPath));

                            // Filter by file extensions
                            if (fileExtension is not (".jpg" or ".png"))
                            {
                                return;
                            }

                            string result = ImageUtility.CheckImage(fullPath);

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

                        // Perform UI updates after the parallel loop completes
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
                        // The task was canceled, exit the method gracefully
                        isCancelled = true;
                    }
                }, _cancellationToken);


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
            if (button_FixMissing.IsVisible)
            {
                button_FixMissing.IsEnabled = true;
            }
            if (button_FixUnused.IsVisible)
            {
                button_FixUnused.IsEnabled = true;
            }
            if (button_FixBad.IsVisible)
            {
                button_FixBad.IsEnabled = true;
            }
        }

        private async Task<ConcurrentBag<KeyValuePair<string, string>>> GetGamelistMediaPathsAsync()
        {
            var media = new ConcurrentBag<KeyValuePair<string, string>>();

            // Task.Run is used to offload work to a background thread
            await Task.Run(() =>
            {
                // Loop over each row in parallel (thread-safe)
                Parallel.ForEach(SharedData.DataSet.Tables[0].AsEnumerable(), row =>
                {
                    // Loop through each mediaItem column in parallel
                    foreach (var mediaItem in _mediaItems.Keys)
                    {
                        string columnName = mediaItem;

                        // Get the value from the current row and column
                        object cellValue = row[columnName];

                        // Check for null, DBNull, or empty value
                        if (cellValue != null && cellValue != DBNull.Value && !string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            // Add key-value pair to the ConcurrentBag
                            media.Add(new KeyValuePair<string, string>(columnName, cellValue.ToString()!));
                        }
                    }
                });
            });

            // Return the ConcurrentBag
            return media;
        }

        private async Task<List<string>> GetExistingMediaFiles()
        {
            // Use ConcurrentBag for thread-safe access to individual existingMediaFiles
            var allFiles = new ConcurrentBag<string>();
            var uniqueFolders = new HashSet<string>(_mediaPaths.Values);

            await Task.Run(() =>
            {
                Parallel.ForEach(uniqueFolders, uniqueFolderName =>
                {
                    // Skip music folder, it's not used in gamelists
                    if (uniqueFolderName == "music")
                    {
                        return;
                    }

                    string folderPath = Path.Combine(_parentFolderPath, uniqueFolderName);

                    if (Directory.Exists(folderPath))
                    {
                        // Retrieve existingMediaFiles from the folder and add each to the ConcurrentBag
                        foreach (string file in Directory.GetFiles(folderPath))
                        {
                            allFiles.Add(file);
                        }
                    }
                });
            });

            // Return distinct existingMediaFiles as a List
            return allFiles.Distinct().ToList();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                _cancellationTokenSource.Cancel();
            }
        }

        private async void button_FixMissing_Click(object sender, RoutedEventArgs e)
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


        private void BackupMedia(string system, List<string> files, string folder, bool delete)
        {

            foreach (string filePath in files)
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    continue;
                }

                if (delete)
                {
                    // Just delete the file
                    FileSystem.DeleteFile(filePath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                }
                else
                {
                    string parentDirectory = Directory.GetParent(filePath)!.Name;
                    string backupFolder = $"{SharedData.ProgramDirectory}\\media backup\\{folder}\\{system}\\{parentDirectory}";
                    if (!Directory.Exists(backupFolder))
                    {
                        Directory.CreateDirectory(backupFolder);
                    }
                    string fileName = Path.GetFileName(filePath);
                    string backupPath = Path.Combine(backupFolder, fileName);
                    System.IO.File.Move(filePath, backupPath, true);
                }
            }
        }


        private void button_FixUnused_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Do you want to backup the unused media first?  Selecting 'No' will send them straight to the windows recycle bin.  \n\nSelect Cancel to abort",
            "Confirm",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            bool delete = true;

            if (result == MessageBoxResult.Yes)
            {
                delete = false;
            }

            // Identify items to remove based on "Unused" status
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

        private async void button_FixBad_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Do you want to backup the bad media first?  Selecting 'No' will send them straight to the windows recycle bin.  \n\nSelect Cancel to abort",
               "Confirm",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            bool delete = true;
            SharedData.IsDataChanged = true;

            if (result == MessageBoxResult.Yes)
            {

                delete = false;
            }

            // Identify items to remove based on status
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

            // Convert full paths to relative paths for cleanup
            var relativePaths = PathConverter.ConvertListToRelativePaths(fileNames);

            await ClearMediaPathsAsync(relativePaths);
            progressBar_ProgressBar2.IsIndeterminate = false;

            button_FixBad.IsEnabled = false;
            SharedData.DataSet.AcceptChanges();

            MessageBox.Show("Completed!", "Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);


        }

        private async Task ClearMediaPathsAsync(List<string> itemsToRemove)
        {
            var dataTable = SharedData.DataSet.Tables[0];
            var changes = new ConcurrentBag<(DataRow Row, DataColumn Column)>(); // Thread-safe collection

            await Task.Run(() =>
            {
                Parallel.ForEach(dataTable.AsEnumerable(), row =>
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (row[column] is string cellValue && itemsToRemove.Contains(cellValue))
                        {
                            changes.Add((row, column)); // Collect changes
                        }
                    }
                });
            });

            // Apply changes sequentially to avoid threading issues
            foreach (var change in changes)
            {
                change.Row[change.Column] = DBNull.Value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextSearch.ClearCache();
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

        private void comboBox_MediaTypes_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
    }
}



