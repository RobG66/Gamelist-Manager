using GamelistManager.classes;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.IO.Enumeration;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace GamelistManager
{
    /// <summary>
    /// Interaction logic for MediaTool.xaml
    /// </summary>
    public partial class MediaTool : Window
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;
        Dictionary<string, string> _mediaItems;
        Dictionary<string, string> _mediaPaths;
        private ObservableCollection<MediaSearchDataItem> _dataSearchItems = new ObservableCollection<MediaSearchDataItem>();
        private ObservableCollection<MediaCleanupDataItem> _dataCleanupItems = new ObservableCollection<MediaCleanupDataItem>();
        string _parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

        public class FileInfo
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }
        }

        public class MediaSearchDataItem
        {
            public required string RomPath { get; set; }
            public required string MediaType { get; set; }
            public required string MatchedItem { get; set; }
        }

        public class MediaCleanupDataItem
        {
            public required string Status { get; set; }
            public required string Folder { get; set; }
            public required string FileName { get; set; }
        }

        public MediaTool()
        {
            InitializeComponent();

            string jsonString = Properties.Settings.Default.MediaPaths;
            _mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;
      
            MetaDataList metaDataList = new MetaDataList();
            _mediaItems = metaDataList.GetMetaDataDictionary().Values
             .Where(decl => decl.DataType == MetaDataType.Image ||
                     decl.DataType == MetaDataType.Video ||
                     decl.DataType == MetaDataType.Document)
                    .ToDictionary(decl => decl.Name, decl => decl.Type);

            comboBox_MediaTypes.SelectionChanged += ComboBox_SelectionChanged;
            comboBox_MediaTypes.SelectedIndex = 0;
            dataGrid_Media.ItemsSource = _dataSearchItems;
            dataGrid_BadMedia.ItemsSource = _dataCleanupItems;
                      
        }
        private void DeleteSelectedRows_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected dataGridItems
            
            var selectedItems = dataGrid_Media.SelectedItems.Cast<MediaSearchDataItem>().ToList();

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
                        _dataSearchItems.Remove(item);
                    }
                }
            }
            else
            {
                MessageBox.Show("No rows selected.", "Delete Rows", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void button_ScanNewMedia_Click(object sender, RoutedEventArgs e)
        {
            string folderToScan = textBox_SourceFolder.Text;

            if (string.IsNullOrEmpty(folderToScan))
            {
                return;
            }

            if (!Directory.Exists(folderToScan))
            {
                MessageBox.Show($"The folder {folderToScan} does not exist!", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            contextMenu_DeleteItems.IsEnabled = false;
            progressBar_ProgressBar.IsIndeterminate = true;
            button_ScanNewMedia.IsEnabled = false;
            button_ScanFolder.IsEnabled = false;
            textBox_SourceFolder.IsEnabled = false;

            _dataSearchItems.Clear();

            var mediaType = comboBox_MediaTypes.Text;
            string mediaPath = textBox_SourceFolder.Text;
            bool excludeHidden = checkBox_SkipHiddenItems.IsChecked == true;

            var romList = SharedData.DataSet.Tables[0].AsEnumerable()
                .Where(row => string.IsNullOrEmpty(row.Field<string>(mediaType)))
                .Where(row => !excludeHidden || !row.Field<bool>("Hidden"))
                .Select(row => (
                    RomPath: row.Field<string>("Rom Path"),
                    Name: row.Field<string>("Name")
                ))
                .ToList();

            string key = _mediaItems[mediaType];
            string value = _mediaPaths[key];
            bool useFuzzy = true;
            bool recurse = checkBox_IncludeSubFolders.IsChecked == true;

            label_ScanningMessage.Content = mediaType;
            var rows = new ConcurrentBag<Tuple<string, string, string>>();

            rows = await FindMedia(romList, folderToScan, key, value, string.Empty, useFuzzy, recurse);
            foreach (var tuple in rows)
            {
                _dataSearchItems.Add(new MediaSearchDataItem { RomPath = tuple.Item1, MediaType = tuple.Item2, MatchedItem = tuple.Item3 });
            }

            progressBar_ProgressBar.IsIndeterminate = false;
            button_ScanNewMedia.IsEnabled = true;
            button_ScanFolder.IsEnabled = true;
            textBox_SourceFolder.IsEnabled = true;

            if (_dataSearchItems.Count > 0)
            {
                button_AddNewMedia.IsEnabled = true;
                contextMenu_DeleteItems.IsEnabled = true;
            }

        }


        private async void button_ScanMedia_Click(object sender, RoutedEventArgs e)
        {
            // Start progress bar animation
            progressBar_ProgressBar.IsIndeterminate = true;
            
            // Set button states
            button_ScanForExistingMedia.IsEnabled = false;
            button_ScanForExistingMediaCancel.IsEnabled = true;
            button_AddExistingMedia.IsEnabled = false;

            // Set default label color
            label_Missing.Foreground = Brushes.Black;

            // Disable context menu
            contextMenu_DeleteItems.IsEnabled = false;

            // New cancel token
            _cancellationTokenSource = new CancellationTokenSource();

            // Clear datagrid (source)
            _dataSearchItems.Clear();

            // Bool flag for excluding hidden items
            bool excludeHidden = checkBox_SkipHiddenItems.IsChecked == true;
            
            // Cancel flag starts false
            bool isCancelled = false;

            await Task.Run(async () =>
            {
                try
                {
                    foreach (var mediaItem in _mediaItems)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        string mediaName = mediaItem.Key;
                        string mediaType = mediaItem.Value;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            label_ScanningMessage.Content = mediaName;
                        });
                        string currentPath = Path.Combine(Path.GetDirectoryName(SharedData.XMLFilename)!, _mediaPaths[mediaType]);
                        if (!Directory.Exists(currentPath))
                        {
                            continue;
                        }

                        string pattern = (mediaType == "thumbnail" ? "thumb" : mediaType);
                        bool recurse = false;
                        bool useFuzzy = false;

                        var romList = SharedData.DataSet.Tables[0].AsEnumerable()
                        .Where(row => string.IsNullOrEmpty(row.Field<string>(mediaName)))
                        .Where(row => !excludeHidden || !row.Field<bool>("Hidden"))
                        .Select(row => (
                            RomPath: row.Field<string>("Rom Path"),
                            Name: row.Field<string>("Name")
                        ))
                        .ToList();

                        if (romList.Count > 0)
                        {
                            var rows = await FindMedia(romList, currentPath, mediaName, mediaType, pattern, useFuzzy, recurse);
                            foreach (var tuple in rows)
                            {
                                _cancellationToken.ThrowIfCancellationRequested();
                                _dataSearchItems.Add(new MediaSearchDataItem { RomPath = tuple.Item1, MediaType = tuple.Item2, MatchedItem = tuple.Item3 });
                            }
                        }
                    }
                }
                catch
                {
                    isCancelled = true;
                }
            }, _cancellationToken);

            if (isCancelled == true)
            {
                label_ScanningMessage.Content = "Cancelled!";
                label_ScanningMessage.Foreground = Brushes.Red;
                button_ScanForExistingMedia.IsEnabled = true;
                progressBar_ProgressBar.IsIndeterminate = false;
                return;
            }

            string count = "0";
            if (_dataSearchItems.Count > 0)
            {
                button_AddExistingMedia.IsEnabled = true;
                count = _dataSearchItems.Count.ToString();
                contextMenu_DeleteItems.IsEnabled = true;
            }

            string message = $" {count} items were found.";
            label_ScanningMessage.Content = $"Scan completed! {message}";

            progressBar_ProgressBar.IsIndeterminate = false;
            button_ScanForExistingMedia.IsEnabled = true;

        }

        private async Task<ConcurrentBag<Tuple<string, string, string>>> FindMedia(
           List<(string? RomPath, string? Name)> romList,
           string folder,
           string mediaName,
           string mediaType,
           string dirPattern,
           bool useFuzzy,
           bool recurse)
        {
            // Determine the search option based on the recurse flag
            SearchOption searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Get the list of files matching the directory pattern
            List<FileInfo> files = await Task.Run(() =>
                Directory.GetFiles(folder, $"*{dirPattern}.*", searchOption)
                .Select(file => new FileInfo { FileName = Path.GetFileName(file), FullPath = file })
                .ToList()
            );

            var foundMedia = new ConcurrentBag<Tuple<string, string, string>>();

            // Use Task.Run for parallel processing to keep the UI responsive
            await Task.Run(() =>
            {
                Parallel.ForEach(romList, romTuple =>
                {
                    string romPath = romTuple.RomPath;
                    string romName = romTuple.Name;
                    string filteredRomPath = Path.GetFileNameWithoutExtension(romPath);
                    filteredRomPath = romName.Replace("./", "");
                    string? firstMatch = null;

                    // Check for match using RomPath
                    if (!useFuzzy)
                    {
                        string abbreviatedType = mediaType == "thumbnail" ? "thumb" : mediaType;
                        string searchPatternPath = $"{filteredRomPath}-{abbreviatedType}";

                        var matchedFile = files.FirstOrDefault(file =>
                            file.FileName.StartsWith(searchPatternPath, StringComparison.OrdinalIgnoreCase));

                        if (matchedFile != null)
                        {
                            firstMatch = matchedFile.FullPath;
                        }
                    }
                    else
                    {
                        FuzzySearchHelper fuzzySearchHelper = new FuzzySearchHelper();
                        var matchedFileNamePath = fuzzySearchHelper.FuzzySearch(filteredRomPath, files.Select(file => file.FileName).ToList());

                        if (matchedFileNamePath != null)
                        {
                            var matchedFile = files.FirstOrDefault(file =>
                                file.FileName.Equals(matchedFileNamePath, StringComparison.OrdinalIgnoreCase));

                            firstMatch = matchedFile?.FullPath;
                        }
                    }

                    // If no match found using RomPath, check using RomName
                    if (firstMatch == null)
                    {
                        if (!useFuzzy)
                        {
                            string abbreviatedType = mediaType == "thumbnail" ? "thumb" : mediaType;
                            string searchPatternName = $"{romName}-{abbreviatedType}";

                            var matchedFile = files.FirstOrDefault(file =>
                                file.FileName.StartsWith(searchPatternName, StringComparison.OrdinalIgnoreCase));

                            if (matchedFile != null)
                            {
                                firstMatch = matchedFile.FullPath;
                            }
                        }
                        else
                        {
                            FuzzySearchHelper fuzzySearchHelper = new FuzzySearchHelper();
                            var matchedFileNameName = fuzzySearchHelper.FuzzySearch(romName, files.Select(file => file.FileName).ToList());

                            if (matchedFileNameName != null)
                            {
                                var matchedFile = files.FirstOrDefault(file =>
                                    file.FileName.Equals(matchedFileNameName, StringComparison.OrdinalIgnoreCase));

                                firstMatch = matchedFile?.FullPath;
                            }
                        }
                    }

                    // If a match is found, add it to the result list
                    if (!string.IsNullOrEmpty(firstMatch))
                    {
                        var newMediaItem = Tuple.Create(romPath, mediaName, firstMatch);
                        foundMedia.Add(newMediaItem);
                    }
                });
            });

            return foundMedia;
        }



        private void button_AddMedia_Click(object sender, RoutedEventArgs e)
        {
            SharedData.ChangeTracker!.StartBulkOperation();

            var pathToRowMap = SharedData.DataSet.Tables[0].AsEnumerable()
            .ToDictionary(row => row.Field<string>("Rom Path")!, row => row);

            foreach (var item in _dataSearchItems)
            {
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string matchedItem = item.MatchedItem;

                // Find the corresponding DataRow in the DataTable
                if (pathToRowMap.TryGetValue(romPath, out DataRow? foundRow))
                {
                    // Update the found row
                    foundRow[mediaType] = matchedItem;
                }
            }

            // Commit all changes to the DataSet once after processing
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker.EndBulkOperation();

            button_AddExistingMedia.IsEnabled = false;

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

            _dataSearchItems.Clear();

            button_AddExistingMedia.IsEnabled = false;
            button_AddNewMedia.IsEnabled = false;

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mediaPaths == null || _mediaItems == null)
            {
                return;
            }

            _dataSearchItems.Clear();

            button_AddNewMedia.IsEnabled = false;

            int selectedIndex = comboBox_MediaTypes.SelectedIndex;
            if (selectedIndex >= 0)
            {
                var selectedItem = comboBox_MediaTypes.SelectedItem;
                string selectedText = (selectedItem as ComboBoxItem)?.Content.ToString() ?? selectedItem.ToString();
                string textValue = _mediaItems[selectedText];
                string destinationFolder = _mediaPaths[textValue];
                textBox_DestinationFolder.Text = destinationFolder;

            }
        }


        private void button_ScanFolder_Click(object sender, RoutedEventArgs e)
        {

            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (folderDialog.ShowDialog() == true)
            {
                var folderName = folderDialog.FolderName;
                textBox_SourceFolder.Text = folderName;
                button_ScanNewMedia.IsEnabled = true;
            }
        }

        private void button_AddNewMedia_Click(object sender, RoutedEventArgs e)
        {
            string mediaName = comboBox_MediaTypes.Text;
            string mediaFolder = textBox_DestinationFolder.Text;

            MessageBoxResult result = MessageBox.Show(
            $"This will copy all found gamelistMediaPaths dataGridItems to the ./{mediaFolder} directory\n\n" +
            $"The files will be renamed and then added to the gamelist",
            "Confirm Action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
          );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }


            SharedData.ChangeTracker?.StartBulkOperation();

            var pathToRowMap = SharedData.DataSet.Tables[0].AsEnumerable()
            .ToDictionary(row => row.Field<string>("Rom Path")!, row => row);

            foreach (var item in _dataSearchItems)
            {
                // Extract values from the DataGrid row
                string romPath = item.RomPath;
                string mediaType = item.MediaType;
                string matchedItem = item.MatchedItem;

                string parentFolder = Path.GetDirectoryName(SharedData.XMLFilename)!;

                string destinationFolder = $"{parentFolder}\\{mediaFolder}";

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Batocera naming standard
                mediaType = (mediaType == "thumbnail" ? "thumb" : mediaType);

                string fileExtension = Path.GetExtension(matchedItem);
                string newFileName = romPath.Replace("./", "");
                newFileName = Path.GetFileNameWithoutExtension(newFileName);
                newFileName = $"{newFileName}-{mediaType}{fileExtension}";
                File.Copy(matchedItem, $"{destinationFolder}\\{newFileName}", overwrite: false);

                // Find the corresponding DataRow in the DataTable
                if (pathToRowMap.TryGetValue(romPath, out DataRow? foundRow))
                {
                    // Update the found row
                    foundRow[mediaType] = $"./{mediaFolder}/{newFileName}";
                }
            }


            // Commit all changes to the DataSet once after processing
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

            button_AddExistingMedia.IsEnabled = false;

            MessageBox.Show("Existing gamelistMediaPaths has been added back to the the gamelist.\n\n You will still need to save the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private async void button_ScanForProblems_Click(object sender, RoutedEventArgs e)
        {
            // Start indeterminite progressbar animation
            progressBar_ProgressBar2.IsIndeterminate = true;

            // Set button states
            button_ScanForProblemsStart.IsEnabled = false;
            button_ScanForProblemsStop.IsEnabled = true;
            
            // New cancel token
            _cancellationTokenSource = new CancellationTokenSource();

            label_Missing.Content = string.Empty;
            label_SingleColor.Content = string.Empty;
            label_Unused.Content = string.Empty;
            label_Missing.Foreground = Brushes.Black;

            _dataCleanupItems.Clear();

            var gamelistMediaPaths = await GetGamelistMediaPathsAsync();
            var allMedia = await GetExistingMediaFiles();

            if (checkBox_MissingMedia.IsChecked == true)
            {
                bool isCancelled = false;
                label_Missing.Content = "Running scan...";
                int missingCount = 0;

                await Task.Run(() =>
                {
                    try
                    {
                        foreach (var file in gamelistMediaPaths)
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                            string fullPath = Path.Combine(_parentFolderPath, file);
                            if (!allMedia.Contains(fullPath))
                            {
                                string fileName = Path.GetFileName(fullPath);
                                string parentDirectory = Path.GetFileName(Path.GetDirectoryName(fullPath));
                                _dataCleanupItems.Add(new MediaCleanupDataItem { Status = "Missing", Folder = parentDirectory, FileName = fileName });
                                missingCount++;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // The task was canceled, exit the method gracefully
                        isCancelled = true;
                    }
                }, _cancellationToken);

                if (isCancelled == true)
                {
                    label_Missing.Content = "Cancelled!";
                    label_Missing.Foreground = Brushes.Red;
                    button_ScanForProblemsStart.IsEnabled = true;
                    progressBar_ProgressBar2.IsIndeterminate = false;
                    return;
                }

                label_Missing.Content = $"[{missingCount} missing media items]";
                if (missingCount == 0)
                {
                    label_Missing.Foreground = Brushes.LightGreen;
                }
                else
                {
                    label_Missing.Foreground = Brushes.Red;
                }
            }

            if (checkBox_UnusedMedia.IsChecked == true)
            {
                label_Unused.Content = "Running scan...";
                int unusedCount = 0;

            
                foreach (var file in allMedia)
                {
                    string fileName = Path.GetFileName(file);
                    string parentDirectory = Path.GetFileName(Path.GetDirectoryName(file));
                    string relativePath = Path.Combine(parentDirectory,fileName);
          
                    if (!gamelistMediaPaths.Contains(relativePath)) 
                    {
                        _dataCleanupItems.Add(new MediaCleanupDataItem { Status = "Unused", Folder = parentDirectory, FileName = fileName  });
                        unusedCount++;
                    }
                }

                label_Unused.Content = $"[{unusedCount} unused media items]";
                if (unusedCount == 0)
                {
                    label_Unused.Foreground = Brushes.LightGreen;
                }
                else
                {
                    label_Unused.Foreground = Brushes.Red;
                }

            }

            progressBar_ProgressBar2.IsIndeterminate = false;

            button_ScanForProblemsStart.IsEnabled = true;

        }

        private async Task<List<string>> GetGamelistMediaPathsAsync()
        {
            var media = new ConcurrentBag<string>();

            await Task.Run(() =>
            {
                Parallel.ForEach(SharedData.DataSet.Tables[0].AsEnumerable(), row =>
                {
                    // Loop through each gamelistMediaPaths type column in parallel
                    foreach (var mediaItem in _mediaItems.Keys)
                    {
                        string columnName = mediaItem;
                   
                        // Get the mediaType from the current row and column
                        var cellValue = row[columnName];
                        if (cellValue != null && cellValue != DBNull.Value && !string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            string convertedName = (cellValue.ToString().Substring(2)).Replace("/", "\\");
                            media.Add(convertedName);
                        }
                    }
                });
            });

            return  media.ToList();
        }

        private async Task<List<string>> GetExistingMediaFiles()
        {
            // Use ConcurrentBag for thread-safe access
            var allFiles = new List<string>();
            var uniqueFolders = new HashSet<string>(_mediaPaths.Values);

            await Task.Run(() =>
            {
                foreach(var uniqueFolderName in uniqueFolders)
                {
                    // Skip music folder, it's not used in gamelists
                    if (uniqueFolderName == "music")
                    {
                        return;         
                    }

                    string folderPath = $@"{_parentFolderPath}\{uniqueFolderName}";

                    if (Directory.Exists(folderPath))
                    {
                        // Retrieve files from the folder
                        List<string> files = new List<string>(Directory.GetFiles(folderPath));
                        allFiles.AddRange(files);
                      }
                }
            });

            // Return distinct files as a List
            return allFiles.Distinct().ToList();
        }

        private void button_CancelScan_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                _cancellationTokenSource.Cancel();
            }
        }
    }
}



