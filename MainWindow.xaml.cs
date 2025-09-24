using GamelistManager.classes;
using GamelistManager.pages;
using Microsoft.Win32;
using Renci.SshNet;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GamelistManager
{
    public partial class MainWindow : Window
    {
        private string _parentFolderPath;
        private string _visibilityFilter;
        private string _genreFilter;
        private string _customFilter;
        bool _autosizeColumns;
        private DispatcherTimer _dataGridSelectionChangedTimer;
        private DataRowView _pendingSelectedRow;
        private MediaPage _mediaPage;
        private Scraper _scraper;
        private Dictionary<DataGridColumn, Style?> _originalCellStyles = new();
        private Brush _originalTextBoxForeground;

       
        public MainWindow()
        {
            InitializeComponent();
            // This is for a short delay between datagrid selection changes
            // In case of fast scrolling
            _dataGridSelectionChangedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Set the delay interval
            };

            _dataGridSelectionChangedTimer.Tick += DataGridSelectionChangedTimer_Tick;
            SharedData.ProgramDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            SharedData.InitializeChangeTracker();
            SharedData.ChangeTracker!.UndoRedoStateChanged += ChangeTracker_UndoRedoStateChanged!;

            _mediaPage = new MediaPage(); // Initialize MediaPage Page 
            _scraper = new Scraper(this); // Initialize Scraper Page
            _autosizeColumns = true;

            _parentFolderPath = string.Empty;
            _visibilityFilter = string.Empty;
            _genreFilter = string.Empty;
            _customFilter = string.Empty;
            _pendingSelectedRow = null!;
            _originalTextBoxForeground = SystemColors.ControlTextBrush;

    
        }

        private void ChangeTracker_UndoRedoStateChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCounters();
            });
        
            if (SharedData.ChangeTracker!.IsTrackingEnabled == false)
            {
                return;
            }

            UpdateChangeTrackerButtons();
        }
                
        private void UpdateChangeTrackerButtons()
        {
            if (SharedData.ChangeTracker!.IsTrackingEnabled == false)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                UndoButton.IsEnabled = SharedData.ChangeTracker!.UndoCount > 1;
                RedoButton.IsEnabled = SharedData.ChangeTracker!.RedoCount > 0;
            });
        }


        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.ChangeTracker!.IsTrackingEnabled == false)
            {
                return;
            }

            SharedData.ChangeTracker!.Undo();
            UpdateCounters();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.ChangeTracker!.IsTrackingEnabled == false)
            {
                return;
            }
            SharedData.ChangeTracker!.Redo();
            UpdateCounters();
        }

        private DataTemplate CreateCheckBoxTemplate(string columnName)
        {
            var xaml = $@"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
            <CheckBox IsChecked='{{Binding {columnName}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}}'
             HorizontalAlignment='Center'/>
            </DataTemplate>";

            return (DataTemplate)XamlReader.Parse(xaml);
        }

        private async Task<DataSet?> LoadGamelistFromFileAsync(string fileName)
        {            
     
            var data = await Task.Run(() => GamelistLoader.LoadGamelist(fileName));
          
            if (data == null)
            {
                return null;
            }

            return data;
        }

        private void SetupMainDataGrid()
        {
            MainDataGrid.ItemsSource = null;
            MainDataGrid.Columns.Clear();
       
            foreach (DataColumn column in SharedData.DataSet.Tables[0].Columns)
            {
                if (column.DataType == typeof(bool))
                {
                    MainDataGrid.Columns.Add(new DataGridTemplateColumn
                    {
                        Header = column.ColumnName,
                        CellTemplate = CreateCheckBoxTemplate(column.ColumnName),
                        SortMemberPath = column.ColumnName
                    });
                }
                else
                {
                    MainDataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = column.ColumnName,
                        Binding = new Binding(column.ColumnName),
                        SortMemberPath = column.ColumnName
                    });
                }
            }

            MainDataGrid.ItemsSource = SharedData.DataSet.Tables[0].DefaultView;
        }

        private void SetupGamelistUI(string systemName)
        {   
            string imageName = $"{systemName}.png";
            string imagePath = $"pack://application:,,,/Resources/systems/{imageName}";

            try
            {
                PlatformLogo.Source = new BitmapImage(new Uri(imagePath));
            }
            catch
            {
                PlatformLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/gamelistmanager.png"));
            }
            
     
            // Populate genre ComboBox
            var uniqueGenres = SharedData.DataSet.Tables[0].AsEnumerable()
                .Select(row => row.Field<string>("genre"))
                .Where(genre => !string.IsNullOrEmpty(genre))
                .Distinct()
                .OrderBy(genre => genre)
                .ToList();

            comboBox_Genre.Items.Clear();
            comboBox_Genre.Items.Add("<All Genres>");
            comboBox_Genre.Items.Add("<Empty Genres>");
            foreach (var genre in uniqueGenres)
                comboBox_Genre.Items.Add(genre);

            comboBox_Genre.SelectedIndex = 0;

            MainDataGrid.Focus();
            MainDataGrid.SelectedIndex = 0;

            stackpanel_Filters.IsEnabled = true;
            textBox_CustomFilter.Text = string.Empty;
            menuItem_Reload.IsEnabled = true;
            menuItem_Save.IsEnabled = true;
            menuItem_Restore.IsEnabled = true;
            menuItem_Export.IsEnabled = true;
            menuItem_Columns.IsEnabled = true;
            menuItem_Edit.IsEnabled = true;
            menuItem_Tools.IsEnabled = true;
            menuItem_View.IsEnabled = true;
            button_Media.IsEnabled = true;
            button_Scraper.IsEnabled = true;

            RedoButton.IsEnabled = false;
            UndoButton.IsEnabled = false;

            UpdateFilterComboboxes();
            SetEditMode(false);

            _scraper.button_Start.IsEnabled = true;
            _scraper.button_ClearCache.IsEnabled = false;

            ResetView();
            UpdateCounters();
        }

        public void UpdateFilterComboboxes()
        {
            var visibleColumns = MainDataGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible)  // Only visible columns
                .OfType<DataGridBoundColumn>() // Only work with DataGridBoundColumn
                .Where(column =>
                {
                    // Check if the column's binding is for a string type
                    var binding = column.Binding as System.Windows.Data.Binding;
                    if (binding != null && binding.Path != null)
                    {
                        // Retrieve the property name
                        var propertyName = binding.Path.Path;

                        // Exclude the "Rom Path" column
                        if (propertyName == "Rom Path")
                        {
                            return false;
                        }

                        // Check the property type in the DataTable
                        var columnInTable = SharedData.DataSet.Tables[0].Columns[propertyName];

                        // Return true if the column in the DataTable is of type string
                        return columnInTable != null && columnInTable.DataType == typeof(string);
                    }
                    return false;
                })
                .Select(column =>
                {
                    var binding = column.Binding as System.Windows.Data.Binding;
                    var propertyName = binding?.Path.Path;

                    return new
                    {
                        ColumnName = propertyName,
                        ColumnDescription = string.IsNullOrEmpty(column.Header.ToString()) ? propertyName : column.Header.ToString()
                    };
                });

            string previousTextComboboxColumns = comboBox_Columns.Text;
            string previousTextComboboxColumns1 = comboBox_Columns1.Text;
            comboBox_Columns.Items.Clear();
            comboBox_Columns1.Items.Clear();

            string previousTextCustomFilter = comboBox_CustomFilter.Text;
            comboBox_CustomFilter.Items.Clear();
            comboBox_CustomFilter.Items.Add("Rom Path"); // Added only for this combobox

            foreach (var item in visibleColumns)
            {
                comboBox_Columns.Items.Add(item.ColumnName);
                comboBox_Columns1.Items.Add(item.ColumnName);
                comboBox_CustomFilter.Items.Add(item.ColumnName);
            }
            comboBox_Columns.Items.Add("Description");
            comboBox_Columns1.Items.Add("Description");
            comboBox_CustomFilter.Items.Add("Description");


            if (comboBox_Columns.Items.Contains(previousTextComboboxColumns))
            {
                comboBox_Columns.Text = previousTextComboboxColumns;
            }
            else
            {
                comboBox_Columns.SelectedIndex = 0;
            }

            if (comboBox_Columns1.Items.Contains(previousTextComboboxColumns1))
            {
                comboBox_Columns1.Text = previousTextComboboxColumns1;
            }
            else
            {
                comboBox_Columns1.SelectedIndex = 0;
            }

            if (comboBox_CustomFilter.Items.Contains(previousTextCustomFilter))
            {
                comboBox_CustomFilter.Text = previousTextCustomFilter;
            }
            else
            {
                comboBox_CustomFilter.SelectedIndex = 0;
            }
        }

        public void SetBackground(string system)
        {
            string imageName;
            try
            {
                imageName = $"{system}.png";
                BackgroundImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/systems/{imageName}"));
            }
            catch
            {
                imageName = "gamelistmanager.png";
                BackgroundImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/{imageName}"));

            }
        }

        private void SetDefaultColumnVisibility(DataGrid dg)
        {
            // The metadata has a property 'AlwaysVisible' for columns
            // which should always be visible in the datagrid.
            // They might not have a menu item, but that is accounted for
            var alwaysVisible = GamelistMetaData.GetMetaDataDictionary().Values
            .Where(decl => decl.AlwaysVisible)
            .Select(decl => decl.Name)
            .ToList();

            // Use either a default column string or custom column string
            // Based upon a bool setting
            bool rememberColumns = Properties.Settings.Default.RememberColumns;
            List<string> visibleColumns = new List<string>();
            string jsonString;
            if (rememberColumns)
            {
                jsonString = Properties.Settings.Default.VisibleGridColumns;
            }
            else
            {
                jsonString = Properties.Settings.Default.DefaultColumns;
            }

            if (!string.IsNullOrEmpty(jsonString))
            {
                var columnVisibility = JsonSerializer.Deserialize<Dictionary<string, bool>>(jsonString)!;
                visibleColumns = columnVisibility
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .ToList();
            }

            // Start with hiding all columns
            foreach (DataGridColumn column in dg.Columns)
            {
                column.Visibility = Visibility.Collapsed;
            }
            textBox_Description.Visibility = Visibility.Collapsed;
            grid_DataDisplay.ColumnDefinitions[2].Width = new GridLength(0);
            gridSplitter_Vertical.Visibility = Visibility.Collapsed;

            // Start with everything hidden and readonly
            // AlwaysVisible columns are visible
            foreach (DataGridColumn column in dg.Columns)
            {
                string header = column.Header.ToString()!;
                column.IsReadOnly = true;
                string menuItemName = "menuItem_" + header.Replace(" ", "");
                MenuItem? menuItem = FindName("menuItem_" + header.Replace(" ", "")) as MenuItem;

                if (alwaysVisible.Contains(header) || (visibleColumns.Contains(header)))
                {
                    // Description needs special handling
                    // The column is never visible, but the textbox is
                    if (header == "Description")
                    {
                        grid_DataDisplay.ColumnDefinitions[2].Width = new GridLength(200);
                        gridSplitter_Vertical.Visibility = Visibility.Visible;
                        textBox_Description.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        column.Visibility = Visibility.Visible;
                    }
                    if (menuItem != null)
                    {
                        menuItem.IsChecked = true;
                    }
                }
                else
                {
                    // It's already collapsed, so just uncheck the menuItem
                    if (menuItem != null)
                    {
                        menuItem.IsChecked = false;
                    }
                }
            }

            if (visibleColumns.Contains("Media Paths") && rememberColumns == true)
            {
                ShowMediaPaths(true);
            }
            else
            {
                ShowMediaPaths(false);
            }

            // Set hidden and favorite isReadonly = false
            // so they can be checked or unchecked
            var hiddenColumn = MainDataGrid.Columns
            .FirstOrDefault(col => col.Header != null && col.Header.ToString() == "Hidden");
            hiddenColumn!.IsReadOnly = false;

            var favoriteColumn = MainDataGrid.Columns
            .FirstOrDefault(col => col.Header != null && col.Header.ToString() == "Favorite");
            favoriteColumn!.IsReadOnly = false;

        }


        private void ReloadFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to reload the file '{SharedData.XMLFilename}'?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (string.IsNullOrEmpty(SharedData.XMLFilename) || !File.Exists(SharedData.XMLFilename))
            {
                MessageBox.Show("No file is currently loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenFile(SharedData.XMLFilename);
        }


        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveGamelist();
        }


        private void SaveGamelist()
        {

            var result = MessageBox.Show("Do you want to save the current gamelist?", "Save Reminder", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            SharedData.ChangeTracker!.PauseTracking();

            GamelistSaver.BackupGamelist(SharedData.CurrentSystem, SharedData.XMLFilename);
            Mouse.OverrideCursor = Cursors.Wait;
            bool saveResult = GamelistSaver.SaveGamelist(SharedData.XMLFilename);
            Mouse.OverrideCursor = null;

            SharedData.ChangeTracker!.ResumeTracking();

            if (saveResult)
            {
                MessageBox.Show("File saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                SharedData.IsDataChanged = false;
            }
            else
            {
                MessageBox.Show("The file was not saved", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            bool saveReminder = Properties.Settings.Default.SaveReminder;
            if (SharedData.IsDataChanged && saveReminder)
            {
                SaveGamelist();
            }

            string selectedFolder = FolderPicker.PickFolder(this, "Select Folder for New Gamelist");

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }
                       
            DirectoryInfo dirInfo = new DirectoryInfo(selectedFolder);

            // Validate parent folder is "roms"
            if (dirInfo.Parent == null || !dirInfo.Parent.Name.Equals("roms", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Invalid folder selected!\n\nPlease select a file inside a folder within 'roms' (e.g., roms/systemname).",
                    "Invalid ROM Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Validate folder is not empty
            if (!Directory.EnumerateFiles(selectedFolder).Any())
            {
                MessageBox.Show(
                    "The selected folder does not contain any files.\nPlease select a non-empty system ROM folder.",
                    "Empty ROM Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Warn if gamelist.xml exists
            string fileName = Path.Combine(selectedFolder, "gamelist.xml");
            if (File.Exists(fileName))
            {
                var overwriteResult = MessageBox.Show(
                    $"A gamelist.xml already exists in this folder.\n\nSaving will replace the existing file.\n\nDo you want to continue?",
                    "Existing gamelist.xml",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (overwriteResult != MessageBoxResult.Yes)
                    return;
            }

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo > 0)
            {
                SharedData.ChangeTracker?.StopTracking();
                UpdateChangeTrackerButtons();
            }

            DataSet data = GamelistLoader.CreateEmptyGamelist();
            SharedData.DataSet = data;
            SharedData.XMLFilename = fileName;
            _parentFolderPath = Path.GetDirectoryName(fileName)!;
            SharedData.CurrentSystem = Path.GetFileName(_parentFolderPath)!;

            textBlock_Filename.Text = $"File: {fileName}";
            textBlock_LastModifiedTime.Text = "Modified Time: N/A";

            SetupMainDataGrid();

            FindNewItems(SharedData.CurrentSystem);
            
            SetupGamelistUI(SharedData.CurrentSystem);

            UpdateCounters();

            SharedData.IsDataChanged = true;
            Mouse.OverrideCursor = default;
            this.IsEnabled = true;

            if (maxUndo > 0)
            {
                SharedData.ChangeTracker?.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                UpdateChangeTrackerButtons();
            }

            MessageBox.Show(
                $"Gamelist creation complete!\n\nRemember to save the file.",
                "Gamelist Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            string fileName = SelectXMLFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            OpenFile(fileName);
        }
         
        private async void OpenFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            bool saveReminder = Properties.Settings.Default.SaveReminder;

            if (SharedData.IsDataChanged && saveReminder)
            {
                SaveGamelist();
            }

            if (string.IsNullOrEmpty(fileName) || !Path.Exists(fileName))
            {
                MessageBox.Show("File not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Logger.Instance.ClearLog();

            Mouse.OverrideCursor = Cursors.Wait;
            this.IsEnabled = false;

            var data = await LoadGamelistFromFileAsync(fileName);
            
            if (data == null)
            { 
                MessageBox.Show($"There was an error loading '{fileName}", "Load Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = default;
                this.IsEnabled = true;
                return;
            }

            int maxUndo = Properties.Settings.Default.MaxUndo;

            SaveLastOpenedGamelistName(fileName);

            if (maxUndo > 0)
            {
                SharedData.ChangeTracker?.StopTracking();
                UpdateChangeTrackerButtons();
            }

            SharedData.DataSet = data;
            SharedData.XMLFilename = fileName;
            
            _parentFolderPath = Path.GetDirectoryName(fileName)!;
            SharedData.CurrentSystem = new DirectoryInfo(_parentFolderPath).Name;
          
            DateTime lastModifiedTime = File.GetLastWriteTime(fileName);
            textBlock_Filename.Text = $"File: {fileName}";
            textBlock_LastModifiedTime.Text = $"Modified Time: {lastModifiedTime}";

            SetupMainDataGrid();
            SetupGamelistUI(SharedData.CurrentSystem);

            if (maxUndo > 0)
            {
                SharedData.ChangeTracker?.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                UpdateChangeTrackerButtons();
            }

            SharedData.IsDataChanged = false;
            Mouse.OverrideCursor = default;
            this.IsEnabled = true;

        }

        private void RecentFilemenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string fileName = menuItem.Tag as string ?? string.Empty;
                if (!string.IsNullOrEmpty(fileName))
                {
                    OpenFile(fileName);
                }
            }
        }

        private string SelectXMLFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Gamelist",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result != true)
            {
                return null!;
            }
            return openFileDialog.FileName;
        }

        public void UpdateCounters()
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }

            int totalItems = SharedData.DataSet.Tables[0].Rows.Count;

            int hiddenItems = SharedData.DataSet.Tables[0].AsEnumerable()
                .Count(row => row.Field<bool?>("hidden") == true);

            // Count rows where hidden is not true
            int visibleItems = SharedData.DataSet.Tables[0].AsEnumerable()
                .Count(row => row.Field<bool?>("hidden") != true);

            // Count rows where favorite is true
            int favoriteItems = SharedData.DataSet.Tables[0].AsEnumerable()
                .Count(row => row.Field<bool?>("favorite") == true);

            // Count visible rows in the datagrid
            int visibleRowCount = MainDataGrid.Items.Count;

            // Make sure labels are updated in gui thread
            Dispatcher.Invoke(() =>
            {
                textBlock_TotalCount.Text = totalItems.ToString();
                textBlock_HiddenCount.Text = hiddenItems.ToString();
                textBlock_FavoriteCount.Text = favoriteItems.ToString();
                textBlock_ShowingCount.Text = visibleRowCount.ToString();

                if (_scraper != null)
                {
                    // Update scraper cache counter
                    _scraper.UpdateCacheCount(_scraper.comboBox_SelectedScraper.Text, SharedData.CurrentSystem);

                    // Update scraper media counts
                    bool showCounts = Properties.Settings.Default.ShowCounts;
                    _scraper.ShowOrHideCounts(showCounts);

                }

            });
        }

        public void SaveLastOpenedGamelistName(string lastFileName)
        {
            if (string.IsNullOrWhiteSpace(lastFileName))
            {
                return;
            }

            string recentFiles = Properties.Settings.Default.RecentFiles;
            int maxFiles = 15; 

            List<string> recentFilesList = !string.IsNullOrEmpty(recentFiles)
                ? recentFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(f => f.Trim())
                             .ToList()
                : new List<string>();

            // Remove the existing one if it exists
            recentFilesList.RemoveAll(f => string.Equals(f, lastFileName, StringComparison.OrdinalIgnoreCase));

            // Insert at the top
            recentFilesList.Insert(0, lastFileName);

            // Trim the list
            if (recentFilesList.Count > maxFiles)
                recentFilesList = recentFilesList.Take(maxFiles).ToList();

            // Save back to settings
            Properties.Settings.Default.RecentFiles = string.Join(",", recentFilesList);
            Properties.Settings.Default.Save();

            AddRecentFilesToMenu();

        }



        private void AddRecentFilesToMenu()
        {
            // Clear existing items first
            for (int i = menuItem_File.Items.Count - 1; i >= 0; i--)
            {
                if (menuItem_File.Items[i] is MenuItem menuItem && !string.IsNullOrEmpty(menuItem.Tag?.ToString()))
                {
               
                    menuItem_File.Items.RemoveAt(i);
                }
            }

            // Get the saved recent files list
            string recentFiles = Properties.Settings.Default.RecentFiles;

            if (string.IsNullOrEmpty(recentFiles))
            {
                return;
            }

            foreach (var file in recentFiles.Split(","))
            {
                // The margin is set to match the menu template
                var menuItem = new MenuItem
                {
                    Header = file,
                    Tag = file
                };
                menuItem.Style = (Style)Application.Current.FindResource("MinimalMenuItem");

                menuItem.Click += RecentFilemenuItem_Click;

                // Add the menu item to the recent files menu
                menuItem_File.Items.Add(menuItem);
            }

        }

        

        public void VerifyAndResetSettings()
        {
            // Load saved media paths
            string mediaPathsJsonString = Properties.Settings.Default.MediaPaths;
            var mediaPaths = string.IsNullOrWhiteSpace(mediaPathsJsonString)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)!;

            // Load default media paths
            string defaultMediaPathsJsonString = Properties.Settings.Default.Properties["MediaPaths"].DefaultValue?.ToString() ?? "{}";
            var defaultMediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(defaultMediaPathsJsonString)!;

            bool needsReset = false;

            // Check if all keys exist
            foreach (var (key, defaultValue) in defaultMediaPaths)
            {
                if (!mediaPaths.ContainsKey(key))
                {
                    mediaPaths[key] = defaultValue; // Add missing key with default value
                    needsReset = true;
                }
            }

            // Reset to defaults if any path is missing
            if (needsReset)
            {
                Properties.Settings.Default.MediaPaths = Properties.Settings.Default.Properties["MediaPaths"].DefaultValue?.ToString() ?? "{}";
                Properties.Settings.Default.Save();
                MessageBox.Show("Media paths have been reset to default", "Paths Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            VerifyAndResetSettings();

            statusBar_FileInfo.Visibility = Properties.Settings.Default.ShowFileStatusBar ? Visibility.Visible : Visibility.Collapsed;

            string gridLineVisibility = Properties.Settings.Default.GridLineVisibility;
            if (Enum.TryParse(gridLineVisibility, out DataGridGridLinesVisibility visibility))
            {
                MainDataGrid.GridLinesVisibility = visibility;
            }

            string filePath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
            string fileVersion = fileVersionInfo.FileVersion!;

            this.Title = $"Gamelist Manager {fileVersion}";

            // Grid size
            int size = Properties.Settings.Default.GridFontSize;
            FontSizeSlider.Value = size;
            FontSizeValue.Text = size.ToString();

            // Only enable this event after page loads and saved value is set
            FontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;

            // Default states
            stackpanel_Filters.IsEnabled = false;

            // Populate recent files, if any
            AddRecentFilesToMenu();

            // Start with bottom grid collapsed
            MainGrid.RowDefinitions[3].Height = new GridLength(0);
            gridSplitter_Horizontal.Visibility = Visibility.Collapsed;

            // Custom filter, not enabled at this time
            comboBox_CustomFilter.SelectedIndex = 0;

            string colorName = Properties.Settings.Default.AlternatingRowColor;
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            MainDataGrid.AlternatingRowBackground = new SolidColorBrush(color);

            button_ClearGenreSelection.Visibility = Visibility.Hidden;
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            textBox_CustomFilter.Text = string.Empty;

        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int size = ((int)FontSizeSlider.Value);
            Properties.Settings.Default.GridFontSize = size;
            Properties.Settings.Default.Save();

            if (MainDataGrid != null)
            {
                // Update the font size for DataGrid
                MainDataGrid.FontSize = size;
                FontSizeValue.Text = size.ToString("0");

                // Update font size for DataGrid headers and cells
                UpdateDataGridFontSize(MainDataGrid, FontSizeSlider.Value);

                // Adjust column widths
                AdjustDataGridColumnWidths(MainDataGrid);
            }
        }

        private void UpdateDataGridFontSize(DataGrid dg, double fontSize)
        {
            // Update font size for DataGrid header and cells
            foreach (var column in dg.Columns)
            {
                if (column.Header is TextBlock header)
                {
                    header.FontSize = fontSize;
                }
            }

            dg.RowHeight = fontSize + 7; // Adjust row height for better fit
        }

        private void AdjustDataGridColumnWidths(DataGrid dg)
        {
            MainDataGrid.UpdateLayout();

            DataGridLength size = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            if (_autosizeColumns)
            {
                size = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            foreach (var column in MainDataGrid.Columns)
            {
                double headerWidth = MeasureColumnHeaderWidth(column);

                if (column is DataGridTemplateColumn templateColumn)
                {
                    // For boolean columns
                    column.CanUserResize = false;
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                }
                else
                {
                    // Measure content width
                    double contentWidth = MeasureColumnContentWidth(column);

                    // Set width to header if content width is shorter
                    if (contentWidth < headerWidth)
                    {
                        column.Width = new DataGridLength(headerWidth, DataGridLengthUnitType.SizeToHeader);
                    }
                    else
                    {
                        column.Width = size;
                    }

                    column.CanUserResize = true;
                }
            }
        }

        private double MeasureColumnContentWidth(DataGridColumn column)
        {
            double maxWidth = 0;

            foreach (var item in MainDataGrid.Items)
            {
                var cellContent = GetCellContent(column, item);

                if (cellContent != null)
                {
                    cellContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double width = cellContent.DesiredSize.Width;
                    if (width > maxWidth)
                    {
                        maxWidth = width;
                    }
                }
            }

            return maxWidth;
        }

        private double MeasureColumnHeaderWidth(DataGridColumn column)
        {
            string header = column.Header.ToString()!;
            var textBlock = new TextBlock { Text = header };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }


        private FrameworkElement GetCellContent(DataGridColumn column, object item)
        {
            if (column.GetCellContent(item) is FrameworkElement cellContent)
            {
                return cellContent;
            }

            return null!;
        }


        private void menuItem_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem? menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            string columnName = menuItem.Header.ToString()!;
            bool isMenuItemChecked = menuItem.IsChecked == true;

            SetColumnVisibility(columnName, isMenuItemChecked);
        }


        private void SetColumnVisibility(string columnName, bool isChecked)
        {
            if (columnName == "Description")
            {
                if (!isChecked)
                {
                    grid_DataDisplay.ColumnDefinitions[2].Width = new GridLength(0);
                    gridSplitter_Vertical.Visibility = Visibility.Collapsed;
                    textBox_Description.Visibility = Visibility.Collapsed;
                    menuItem_Description.IsChecked = false;
                }
                else
                {
                    grid_DataDisplay.ColumnDefinitions[2].Width = new GridLength(200);
                    gridSplitter_Vertical.Visibility = Visibility.Visible;
                    textBox_Description.Visibility = Visibility.Visible;
                    menuItem_Description.IsChecked = true;
                }
                return;
            }

            if (columnName == "Media Paths")
            {
                ShowMediaPaths(isChecked);
                return;
            }

            // Find the column with the specified header
            var column = MainDataGrid.Columns
                .FirstOrDefault(col => col.Header.ToString() == columnName);
            if (column != null)
            {
                // Show or hide the column based on the menu item state
                column.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            }

            UpdateFilterComboboxes();

            if (Properties.Settings.Default.RememberColumns == true)
            {
                SaveVisibleColumnsSetting();
            }
        }

        private void ShowMediaPaths(bool showPaths)
        {
            var mediaItems = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl => decl.DataType == MetaDataType.Image ||
                                decl.DataType == MetaDataType.Video ||
                                decl.DataType == MetaDataType.Music ||
                                decl.DataType == MetaDataType.Document)
                                .Select(decl => decl.Name)
                                .ToList();

            foreach (DataGridColumn column1 in MainDataGrid.Columns)
            {
                if (mediaItems.Contains(column1.Header.ToString()!))
                {
                    column1.Visibility = (showPaths) ? Visibility.Visible : Visibility.Collapsed;
                    column1.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                }
            }

            MainDataGrid.UpdateLayout();

            if (showPaths)
            {
                // Media paths are fairly large, so turn off autosize automatically
                menuItem_ColumnAutoSize.IsChecked = false;
                _autosizeColumns = false;
            }

            AdjustDataGridColumnWidths(MainDataGrid);
            UpdateFilterComboboxes();
        }

        private void SaveVisibleColumnsSetting()
        {
            // Get the column names from known metadata
            var metaNames = GamelistMetaData.GetMetaDataDictionary().Values
            .Select(decl => decl.Name)
            .ToList();

            Dictionary<string, bool> keyValuePairs = new Dictionary<string, bool>();

            foreach (DataGridColumn column in MainDataGrid.Columns)
            {
                string header = column.Header.ToString()!;
                if (!metaNames.Contains(header))
                {
                    continue;
                }

                bool isColumnVisible = column.Visibility == Visibility.Visible;
                if (header == "Description" && menuItem_Description.IsChecked == true)
                {
                    isColumnVisible = true;
                }

                keyValuePairs.Add(header, isColumnVisible);
            }

            bool isMediaVisible = false;
            if (menuItem_MediaPaths.IsChecked)
            {
                isMediaVisible = true;
            }
            keyValuePairs.Add("Media Paths", isMediaVisible);

            string jsonString = JsonSerializer.Serialize(keyValuePairs);
            Properties.Settings.Default.VisibleGridColumns = jsonString;
            Properties.Settings.Default.Save();
        }


        private void ApplyFilters(string[] filters)
        {
            string mergedFilter = string.Empty;

            if (SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }

            if (filters == null || filters.Length == 0)
            {
                SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
                return;
            }

            try
            {
                mergedFilter = string.Join(" AND ", filters.Where(f => !string.IsNullOrEmpty(f)));

                DataView dataView = SharedData.DataSet.Tables[0].AsDataView();

                if (!string.IsNullOrEmpty(mergedFilter))
                {
                    SharedData.DataSet.Tables[0].DefaultView.RowFilter = mergedFilter;
                }
                else
                {
                    SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The filter \"{mergedFilter}\" has an error!\n{ex.Message}", "Filter Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }


        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            radioButton_AllItems.IsChecked = true;
        }

        private void ShowAll()
        {
            menuItem_ShowAll.IsChecked = true;
            menuItem_ShowHidden.IsChecked = false;
            menuItem_ShowVisible.IsChecked = false;

            _visibilityFilter = string.Empty;
            ApplyFilters(new string[] { _visibilityFilter, _genreFilter, _customFilter });
            UpdateCounters();
        }

        private void ShowVisible_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            radioButton_VisibleItems.IsChecked = true;
        }

        private void ShowVisible()
        {
            menuItem_ShowAll.IsChecked = false;
            menuItem_ShowHidden.IsChecked = false;
            menuItem_ShowVisible.IsChecked = true;

            _visibilityFilter = "(hidden = false OR hidden IS NULL)";
            ApplyFilters(new string[] { _visibilityFilter, _genreFilter, _customFilter });
            UpdateCounters();
        }

        private void ShowHidden_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            radioButton_HiddenItems.IsChecked = true;
        }

        private void ShowHidden()
        {
            menuItem_ShowAll.IsChecked = false;
            menuItem_ShowHidden.IsChecked = true;
            menuItem_ShowVisible.IsChecked = false;

            _visibilityFilter = "(hidden = true)";
            ApplyFilters(new string[] { _visibilityFilter, _genreFilter, _customFilter });
            UpdateCounters();

        }

        private void ShowAllGenres_Click(object sender, RoutedEventArgs e)
        {
            menuItem_ShowAllGenre.IsChecked = true;
            menuItem_ShowOneGenre.IsChecked = false;
            comboBox_Genre.SelectedIndex = 0;

        }

        private void ShowGenreOnly_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItems.Count == 0 && MainDataGrid.Items.Count > 0)
            {
                // Default show empty genre because there's no selected datagrid row
                comboBox_Genre.SelectedIndex = 1;
                return;
            }

            menuItem_ShowAllGenre.IsChecked = false;
            menuItem_ShowOneGenre.IsChecked = true;

            DataRowView selectedRow = (DataRowView)MainDataGrid.SelectedItems[0]!;
            string genre = selectedRow["Genre"] != null && selectedRow["Genre"] != DBNull.Value && !string.IsNullOrEmpty(selectedRow["Genre"].ToString())
                ? selectedRow["Genre"].ToString()!
                : string.Empty;


            int index;
            if (string.IsNullOrEmpty(genre))
            {
                index = 1;
            }
            else
            {
                index = comboBox_Genre.Items.IndexOf(genre);
            }
            comboBox_Genre.SelectedIndex = index;

        }

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            if (menuItem_AlwaysOnTop.IsChecked)
            {
                menuItem_AlwaysOnTop.IsChecked = false;
                this.Topmost = false;
            }
            else
            {
                menuItem_AlwaysOnTop.IsChecked = true;
                this.Topmost = true;
            }
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            // ResetView selected from menu will also reset saved columns
            Properties.Settings.Default.VisibleGridColumns = Properties.Settings.Default.DefaultColumns;
            Properties.Settings.Default.Save();
            ResetView();
        }

        private void ResetView()
        {
            // Reset checkmarks
            menuItem_ShowOneGenre.IsChecked = false;
            menuItem_ShowAllGenre.IsChecked = true;
            menuItem_ShowAll.IsChecked = true;
            menuItem_ShowHidden.IsChecked = false;
            menuItem_ShowVisible.IsChecked = false;
            menuItem_MediaPaths.IsChecked = false;
            

            bool rememberAutoSize = Properties.Settings.Default.RememberAutoSize;
            if (rememberAutoSize)
            {
                bool autoSizeColumns = Properties.Settings.Default.AutoSizeColumns;
                menuItem_ColumnAutoSize.IsChecked = autoSizeColumns;
                _autosizeColumns = autoSizeColumns;
            }
            else
            {
                menuItem_ColumnAutoSize.IsChecked = true;
                _autosizeColumns = true;
            }

            // Clear filters
            _genreFilter = string.Empty;
            _visibilityFilter = string.Empty;
            _customFilter = string.Empty;
            SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
            UpdateCounters();

            // Reset column view
            SetDefaultColumnVisibility(MainDataGrid);

            // Adjust Column Widths
            AdjustDataGridColumnWidths(MainDataGrid);

            // Reset custom filter stuff
            comboBox_Genre.SelectedIndex = 0;
            comboBox_CustomFilter.SelectedIndex = 0;
            comboBox_FilterMode.SelectedIndex = 0;
            textBox_CustomFilter.Text = "";
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            button_ClearCustomFilter.IsEnabled = false;
            stackpanel_Custom.Visibility = Visibility.Collapsed;
            stackpanel_Genre.Visibility = Visibility.Visible;

            radioButton_AllItems.IsChecked = true;

        }

      
        private void menuItem_MapDrive_Click(object sender, RoutedEventArgs e)
        {

            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease use the Settings menu to configure this", "Missing Hostname", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string networkShareToCheck = $"\\\\{hostName}\\share";

            bool isMapped = DriveMappingChecker.IsShareMapped(networkShareToCheck);

            if (isMapped == true)
            {
                MessageBox.Show($"There already is a drive mapping: {networkShareToCheck}", "Map Network Drive", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            char driveLetter = '\0';

            // Get first letter starting at z: working backward
            for (char drive = 'Z'; drive >= 'D'; drive--)
            {
                if (!DriveInfo.GetDrives().Any(d => d.Name[0] == drive))
                {
                    driveLetter = drive;
                    break;
                }
            }

            string networkSharePath = $"\\\\{hostName}\\share";
            string exePath = "net";
            string command = $" use {driveLetter}: {networkSharePath} /user:{userName} {userPassword}";

            // Execute the net use command
            string output = CommandExecutor.ExecuteCommand(exePath, command);

            if (output != null && output != string.Empty)
            {
                MessageBox.Show(output, "Map Network Drive", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void menuItem_OpenTerminal_Click(object sender, RoutedEventArgs e)
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string sshPath = "C:\\Windows\\System32\\OpenSSH\\ssh.exe"; // path to ssh.exe on Windows

            try
            {
                ProcessStartInfo psi = new(sshPath)
                {
                    Arguments = $"-t {userName}@{hostName}",
                    UseShellExecute = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    CreateNoWindow = false // Set this to false to see the terminal window
                };

                Process process = new() { StartInfo = psi };
                process.Start();

            }
            catch (Exception)
            {
                MessageBox.Show("Could not start OpenSSH", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItem_GetVersion_Click(object sender, RoutedEventArgs e)
        {
            string command = "batocera-es-swissknife --version"; 
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"Your Batocera is version {output}", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void menuItem_ShowUpdates_Click(object sender, RoutedEventArgs e)
        {
            string command = "batocera-es-swissknife --update"; 
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"{output}", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void menuItem_StopEmulators_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to stop any running emulators?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot";
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show("Running emulators should be stopped now", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void menuItem_StopEmulationstation_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Are you sure you want to stop EmulationStation?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid"; 
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            if (!string.IsNullOrEmpty(output) && output.Length > 0 && output[0] == '0')
            {
                MessageBox.Show("EmulationStation is stopped", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("An unknown error has occured!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItem_RebootHost_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Are you sure you want to reboot your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot"; 
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show("Reboot has been sent to host", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void menuItem_ShutdownHost_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to shutdown your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;sleep 3;shutdown -h now"; 
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show("Shutdown has been sent to host", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }



        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRow = MainDataGrid.SelectedItems.OfType<DataRowView>().FirstOrDefault();

            if (selectedRow == null)
            {
                return;
            }

            _pendingSelectedRow = selectedRow;
            _dataGridSelectionChangedTimer.Stop();
            _dataGridSelectionChangedTimer.Start();
        }

        private void DataGridSelectionChangedTimer_Tick(object? sender, EventArgs e)
        {

            _dataGridSelectionChangedTimer.Stop();

            if (_pendingSelectedRow == null)
            {
                return;
            }

            textBox_Description.Text = _pendingSelectedRow["Description"] is DBNull ? string.Empty : _pendingSelectedRow?["Description"]?.ToString() ?? string.Empty;


            if (button_Media.Content.ToString() == "Hide Media" && _pendingSelectedRow != null)
            {
                _mediaPage.ShowMedia(_pendingSelectedRow);
            }
        }



        private void Export_Click(object sender, RoutedEventArgs e)
        {

            string csvFileName = Directory.GetCurrentDirectory() + "\\" + $"{SharedData.CurrentSystem}_export.csv";

            try
            {
                using (var csvContent = new StreamWriter(csvFileName))
                {
                    DataTable dataTable = SharedData.DataSet.Tables[0];

                    // Write the header (column names)
                    var columnNames = dataTable.Columns.Cast<DataColumn>()
                        .Select(col => EscapeCsvField(col.ColumnName));
                    csvContent.WriteLine(string.Join(",", columnNames));

                    // Write the data rows
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var fields = row.Table.Columns.Cast<DataColumn>()
                            .Select(col =>
                            {
                                string field = row[col]?.ToString() ?? string.Empty;

                                // Check if the field contains special characters (commas, quotes, or newlines)
                                if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                                {
                                    // Escape double quotes by doubling them
                                    field = field.Replace("\"", "\"\"");

                                    // Encapsulate the field in double quotes
                                    return $"\"{field}\"";
                                }

                                return field;
                            });

                        csvContent.WriteLine(string.Join(",", fields));
                    }


                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

                    string currentDirectory = System.IO.Directory.GetCurrentDirectory();

                    // Open File Explorer to the current directory
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = currentDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }

            }

            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"UnauthorizedAccessException: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"IOException: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EscapeCsvField(string field)
        {
            // If the field contains a comma, double-quote, or newline, enclose it in double-quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        static string ExecuteSshCommand(string command)
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The batocera hostname is not configured.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            string output = string.Empty;
            using (var client = new SshClient(hostName, userName, userPassword))
            {
                try
                {
                    client.Connect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"There was an error connecting to the host: {ex.Message}", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null!;
                }
                using (var commandRunner = client.RunCommand(command))
                {
                    output = commandRunner.Result;
                }
                client.Disconnect();
                userPassword = null!;
                return output;
            }
        }

        private void menuItem_View_SubmenuOpened(object sender, RoutedEventArgs e)
        {

            if (MainDataGrid.Items.Count == 0)
            {
                menuItem_ShowOneGenre.IsEnabled = false;
                menuItem_ShowAllGenre.IsEnabled = false;
                menuItem_ShowAll.IsEnabled = false;
                menuItem_ShowHidden.IsEnabled = false;
                menuItem_ShowVisible.IsEnabled = false;
                return;
            }
            else
            {
                menuItem_ShowOneGenre.IsEnabled = true;
                menuItem_ShowAllGenre.IsEnabled = true;
                menuItem_ShowAll.IsEnabled = true;
                menuItem_ShowHidden.IsEnabled = true;
                menuItem_ShowVisible.IsEnabled = true;

            }

            string genre = string.Empty;

            if (comboBox_Genre.SelectedIndex == 0)
            {
                if (MainDataGrid.SelectedItems.Count > 0)
                {
                    DataRowView selectedRow = (DataRowView)MainDataGrid.SelectedItems[0]!;
                    genre = selectedRow["Genre"] != null && selectedRow["Genre"] != DBNull.Value && !string.IsNullOrEmpty(selectedRow["Genre"].ToString())
                    ? selectedRow["Genre"].ToString()!
                    : "Empty";
                }
                else
                {
                    genre = "Empty";
                }
            }
            if (comboBox_Genre.SelectedIndex > 1)
            {
                genre = comboBox_Genre.SelectedItem.ToString()!;
            }

            if (comboBox_Genre.SelectedIndex == 1)
            {
                genre = "Empty";
            }


            menuItem_ShowOneGenre.Header = $"Show '{genre}' Genre Only";
            menuItem_ShowOneGenre.UpdateLayout();
        }

        private void comboBox_Genre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedText;

            ComboBox? comboBox = sender as ComboBox;

            if (comboBox == null || comboBox.SelectedItem == null)
            {
                // Should never be empty, but always check anyhow
                return;
            }

            int selectedIndex = comboBox.SelectedIndex;

            if (selectedIndex > 1)
            {
                selectedText = comboBox.Items[selectedIndex].ToString()!;
                selectedText = selectedText.Replace("'", "''");
                _genreFilter = $"Genre = '{selectedText}'";
                menuItem_ShowAllGenre.IsChecked = false;
                menuItem_ShowOneGenre.IsChecked = true;
                button_ClearGenreSelection.Visibility = Visibility.Visible;
            }

            if (selectedIndex == 0)
            {
                _genreFilter = string.Empty;
                menuItem_ShowAllGenre.IsChecked = true;
                menuItem_ShowOneGenre.IsChecked = false;
                button_ClearGenreSelection.Visibility = Visibility.Hidden;
            }

            if (selectedIndex == 1)
            {
                menuItem_ShowAllGenre.IsChecked = false;
                menuItem_ShowOneGenre.IsChecked = true;
                _genreFilter = "Genre IS NULL";
                button_ClearGenreSelection.Visibility = Visibility.Visible;
            }

            ApplyFilters(new string[] { _visibilityFilter!, _genreFilter, _customFilter });
            UpdateCounters();

        }

        private void textBox_CustomFilter_KeyUp(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrEmpty(SharedData.XMLFilename))
            {
                return;
            }
                       

            string filterText = textBox_CustomFilter.Text;
            string filterItem = GetComboBoxDisplayText(comboBox_CustomFilter);
            string selectedMode = GetComboBoxDisplayText(comboBox_FilterMode);


            if (!string.IsNullOrEmpty(filterText))
            {
                button_ClearCustomFilter.IsEnabled = true;
                button_ClearCustomFilter.Visibility = Visibility.Visible;
            }
            else
            {
                button_ClearCustomFilter.IsEnabled = false;
                button_ClearCustomFilter.Visibility = Visibility.Hidden;
            }

            if (selectedMode.Contains("EMPTY"))
            {
                button_ClearCustomFilter.IsEnabled = false;
                button_ClearCustomFilter.Visibility = Visibility.Hidden;
            }

            _customFilter = MakeFilter(filterItem, filterText, selectedMode);

            ApplyFilters(new string[] { _visibilityFilter, _genreFilter, _customFilter });
            UpdateCounters();

        }

        private string GetComboBoxDisplayText(ComboBox comboBox)
        {
            // If something is selected
            if (comboBox.SelectedItem != null)
            {
                // If it's a ComboBoxItem (manually added in XAML)
                if (comboBox.SelectedItem is ComboBoxItem cbi)
                    return cbi.Content?.ToString();

                // If it's a bound object or plain string
                return comboBox.SelectedItem.ToString();
            }

            // If nothing is selected, fallback to whatever is displayed
            return comboBox.Text;
        }

        private void FilterModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (string.IsNullOrEmpty(SharedData.XMLFilename))
            {
                return;
            }
            
            textBox_CustomFilter.IsEnabled = true;

            // Determine the selected filter mode
            string selectedMode = GetComboBoxDisplayText(comboBox_FilterMode);

            switch (selectedMode)
            {
                case "Is Empty":
                    textBox_CustomFilter.IsEnabled = false;
                    textBox_CustomFilter.Text = "";
                    break;

                case "Is Not Empty":
                    textBox_CustomFilter.IsEnabled = false;
                    textBox_CustomFilter.Text = "";
                    break;
            }

            string filterText = textBox_CustomFilter.Text;
            string filterItem = GetComboBoxDisplayText(comboBox_CustomFilter);
          
            _customFilter = MakeFilter(filterItem, filterText, selectedMode);

            ApplyFilters(new string[] { _visibilityFilter, _genreFilter, _customFilter });
            UpdateCounters();

        }

        private string MakeFilter(string filterItem, string filterText, string selectedMode)
        {
            // Sanitize input
            filterText = filterText.Trim().Replace("'", "''");
                      
            if (filterItem.Contains(" "))
            {
                filterItem = $"[{filterItem}]";
            }

            string filterExpression = string.Empty;

            switch (selectedMode)
            {
                case "Is":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterExpression = $"{filterItem} = '{filterText}'";
                    }
                    break;

                case "Is Like":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        string likePattern = filterText.Replace("*", "%");
                        likePattern = EscapeSpecialCharacters(likePattern);
                        likePattern = $"%{likePattern}%";
                        filterExpression = $"{filterItem} LIKE '{likePattern}'";
                    }
                    break;

                case "Starts With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterText = $"{filterText}%";
                        filterExpression = $"{filterItem} LIKE '{filterText}'";
                    }
                    break;

                case "Ends With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterText = $"%{filterText}";
                        filterExpression = $"{filterItem} LIKE '{filterText}'";
                    }
                    break;

                case "Is Empty":
                    filterExpression = $"{filterItem} IS NULL OR {filterItem} = ''";
                    break;

                case "Is Not Empty":
                    filterExpression = $"{filterItem} IS NOT NULL AND {filterItem} <> ''";
                    break;
            }

            return filterExpression;
        }


        private string EscapeSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Escape single quotes (RowFilter requires doubled single quotes)
            input = input.Replace("'", "''");

            // Escape double quotes by doubling them
            input = input.Replace("\"", "\"\"");

            // Escape wildcards used in filtering
            input = input.Replace("%", "[%]").Replace("_", "[_]");

            return input;
        }

        private void menuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            int originalMaxUndo = Properties.Settings.Default.MaxUndo;

            SettingsDialogWindow settingsDialog = new SettingsDialogWindow(this);
            settingsDialog.ShowDialog();

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo == 0)
            {
                SharedData.ChangeTracker!.StopTracking();
                SharedData.ChangeTracker!.UndoRedoStateChanged -= ChangeTracker_UndoRedoStateChanged!;
                RedoButton.IsEnabled = false;
                UndoButton.IsEnabled = false;
                return;
            }

            if (maxUndo != originalMaxUndo && SharedData.DataSet.Tables.Count > 0)
            {
                SharedData.ChangeTracker!.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                // SharedData.ChangeTracker!.UndoRedoStateChanged += ChangeTracker_UndoRedoStateChanged!;
            }

        }

        private void menuItem_Edit_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            // Check readonly state
            bool readOnly = MainDataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header.ToString() == "Name")?.IsReadOnly ?? false;

            menuItem_EditData.Header = (readOnly == true) ? "Edit Data" : "StopPlaying Editing Data";

            bool filtered = false;

            DataView dataView = (DataView)MainDataGrid.ItemsSource;

            if (!string.IsNullOrEmpty(dataView.RowFilter))
            {
                filtered = true;
            }

            // Adjust menuitems for view filtering
            menuItem_SetAllHidden.Header = (filtered == false) ? "Set All Items Hidden" : "Set All Filtered Items Hidden";
            menuItem_SetAllVisible.Header = (filtered == false) ? "Set All Items Visible" : "Set All Filtered Items Visible";

            // Adjust menu previousTextComboboxColumns based upon selected item count
            int selectedItemCount = MainDataGrid.SelectedItems.Count;
            menuItem_SetSelectedVisible.Header = (selectedItemCount < 2) ? "Set Item Visible" : "Set Selected Items Visible";
            menuItem_SetSelectedHidden.Header = (selectedItemCount < 2) ? "Set Item Hidden" : "Set Selected Items Hidden";
            menuItem_RemoveItem.Header = (selectedItemCount < 2) ? "Remove Item" : "Remove Selected Items";
            menuItem_ResetName.Header = (selectedItemCount < 2) ? "Reset Name" : "Reset Selected Names";

            if (selectedItemCount == 1)
            {
                menuItem_SetAllGenreVisible.IsEnabled = true;
                menuItem_SetAllGenreHidden.IsEnabled = true;

                var genreValue = MainDataGrid.SelectedItems
                 .OfType<DataRowView>()
                 .Select(item => Convert.ToString(item["Genre"]))
                 .FirstOrDefault();

                if (string.IsNullOrEmpty(genreValue))
                {
                    genreValue = "Empty Genre";
                }

                menuItem_SetAllGenreVisible.Header = $"Set All \"{genreValue}\" Visible";
                menuItem_SetAllGenreHidden.Header = $"Set All \"{genreValue}\" Hidden";
            }
            else
            {
                menuItem_SetAllGenreVisible.IsEnabled = false;
                menuItem_SetAllGenreHidden.IsEnabled = false;
            }

        }

        private void menuItem_SetSelectedVisible_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedVisibility(false);
        }

        private void menuItem_SetSelectedHidden_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedVisibility(true);
        }

        private void SetSelectedVisibility(bool visible)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            int count = MainDataGrid.SelectedItems.Count;
            if (count < 1)
            {
                return;
            }

            string item = (count > 1) ? "items" : "item";
            string visibility = visible ? "hidden" : "visible";

            if (confirmBulkChanges && count > 1)
            {
                var result = MessageBox.Show($"Do you want to set the selected {item} {visibility}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Rom Path"]?.ToString())
                .ToList();


            var matchingRows = from DataRow row in SharedData.DataSet.Tables[0].AsEnumerable()
                               let pathValue = row.Field<string>("Rom Path")
                               where selectedPaths.Contains(pathValue)
                               select row;

            SharedData.ChangeTracker!.StartBulkOperation();
            foreach (var row in matchingRows)
            {
                int rowIndex = SharedData.DataSet.Tables[0].Rows.IndexOf(row);
                row["hidden"] = visible;
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();



            UpdateCounters();
            SharedData.IsDataChanged = true;
        }

        private void menuItem_SetAllGenreVisible_Click(object sender, RoutedEventArgs e)
        {
            string? selectedGenre = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Genre"]?.ToString())
                .FirstOrDefault();

            SetVisibilityByItemValue("Genre", selectedGenre!, false);

        }

        private void menuItem_SetAllGenreHidden_Click(object sender, RoutedEventArgs e)
        {
            string? selectedGenre = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Genre"]?.ToString())
                .FirstOrDefault();

            SetVisibilityByItemValue("Genre", selectedGenre!, true);
        }

        private void SetVisibilityByItemValue(string colname, string colvalue, bool visible)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            if (confirmBulkChanges)
            {
                string visibility = visible ? "hidden" : "visible";

                var result = MessageBox.Show($"Do you want to set all items with the {colname} value of '{colvalue}' {visibility}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DataTable dataTable = SharedData.DataSet.Tables[0];

            var rowsToUpdate = SharedData.DataSet.Tables[0].AsEnumerable()
                .Where(row =>
                (string.Equals(row.Field<string>(colname), colvalue, StringComparison.OrdinalIgnoreCase) ||
                (string.IsNullOrEmpty(colvalue) && string.IsNullOrEmpty(row.Field<string>(colname))))
            );

            SharedData.ChangeTracker!.StartBulkOperation();
            foreach (var row in rowsToUpdate)
            {
                int rowIndex = SharedData.DataSet.Tables[0].Rows.IndexOf(row);
                row["hidden"] = visible;
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            UpdateCounters();
            SharedData.IsDataChanged = true;

        }

        private void menuItem_RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            if (confirmBulkChanges)
            {
                int count = MainDataGrid.SelectedItems.Count;
                string item = (count == 1) ? "item" : "items";

                var result = MessageBox.Show($"Do you want to remove the selected {item} from the gamelist?\n\nNo files are being deleted.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            int selectedIndex = MainDataGrid.SelectedIndex;

            var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Rom Path"]?.ToString())
                .ToList();

            var matchingRows = (from DataRow row in SharedData.DataSet.Tables[0].AsEnumerable()
                                let pathValue = row.Field<string>("Rom Path")
                                where selectedPaths.Contains(pathValue)
                                select row).ToList(); // Convert to a list to avoid modifying the collection during iteration


            SharedData.ChangeTracker!.StartBulkOperation();
            // Delete rows in reverse order
            for (int i = matchingRows.Count - 1; i >= 0; i--)
            {
                var row = matchingRows[i];
                row.Delete();
                _pendingSelectedRow = null!;
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            SetSelectedRowAfterChange(selectedIndex);

            UpdateCounters();
            SharedData.IsDataChanged = true;
        }

        private void SetSelectedRowAfterChange(int previousIndex)
        {
            if (MainDataGrid.Items.Count == 0)
            {
                return; // No rows left
            }

            // Try to select the next row
            int newIndex = previousIndex < MainDataGrid.Items.Count ? previousIndex : MainDataGrid.Items.Count - 1;

            MainDataGrid.SelectedIndex = newIndex;
            MainDataGrid.ScrollIntoView(MainDataGrid.Items[newIndex]); // Ensure it's visible
        }

        private void menuItem_EditData_Click(object sender, RoutedEventArgs e)
        {
            bool readOnly = MainDataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header.ToString() == "Name")?.IsReadOnly ?? false;

            bool editMode = readOnly;
            SharedData.MetaDataEditMode = editMode;
            SetEditMode(editMode);
         
            if (button_Media.Content.ToString() == "Hide Media")
            {
                if (MainDataGrid.SelectedItems.Count == 0 && MainDataGrid.Items.Count > 0)
                {
                    MainDataGrid.SelectedIndex = 0; // Select the first row
                }

                // Get the first selected row
                DataRowView? selectedRow = MainDataGrid.SelectedItems
                .OfType<DataRowView>()
                .FirstOrDefault();

                _mediaPage.ShowMedia(selectedRow!);
            }

        }

        private void SetEditMode(bool editMode)
        {

            var editableColumns = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl => decl.editible)
                .Select(decl => decl.Name)
                .ToList();

            foreach (DataGridColumn column in MainDataGrid.Columns)
            {
                var columnName = column.Header;
                if (editableColumns.Contains(columnName))
                {
                    column.IsReadOnly = !editMode;

                    if (editMode)
                    {
                        // Store original style if not already saved
                        if (!_originalCellStyles.ContainsKey(column))
                        {
                            _originalCellStyles[column] = column.CellStyle;
                        }

                        // Apply edit mode style (Black text)
                        var editStyle = new Style(typeof(DataGridCell));
                        editStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Blue));
                        column.CellStyle = editStyle;
                    }
                    else
                    {
                        // Restore original style
                        column.CellStyle = _originalCellStyles.ContainsKey(column) ? _originalCellStyles[column] : null;
                    }
                }
            }

            // Handle TextBox foreground color
            if (editMode)
            {
                // Store the original foreground color only once
                if (_originalTextBoxForeground == null)
                {
                    _originalTextBoxForeground = textBox_Description.Foreground;
                }

                textBox_Description.Foreground = Brushes.Blue; 
                               
            }
            else
            {
                // Restore original color if it was stored
                textBox_Description.Foreground = _originalTextBoxForeground ?? Brushes.Black;
            }

            textBox_Description.IsReadOnly = !editMode;

        }



        private void menuItem_VideoJukebox_Click(object sender, RoutedEventArgs e)
        {
            PlayJukeBox("video");
        }

        private void textBox_Description_LostFocus(object sender, RoutedEventArgs e)
        {
            bool readOnly = MainDataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header.ToString() == "Name")?.IsReadOnly ?? false;

            if (readOnly)
            {
                return;
            }

            if (MainDataGrid.SelectedItems.Count < 1) { return; }

            DataRowView selectedRow = (DataRowView)MainDataGrid.SelectedItems[0]!;
            string pathValue = selectedRow["Rom Path"].ToString()!;
            string textboxValue = textBox_Description.Text;

            DataRow[] rows = SharedData.DataSet.Tables[0].Select($"[Rom Path] = '{pathValue.Replace("'", "''")}'");
            DataRow tabledata = rows[0];
            tabledata["Description"] = textboxValue;
        }

        private void menuItem_ClearAllData_Click(object sender, RoutedEventArgs e)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            if (confirmBulkChanges == true)
            {
                var result = MessageBox.Show($"Do you want clear all data?\n\nRom Paths will remain unchanged\nNames will now reflect the rom name without extension", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }


            SharedData.ChangeTracker!.StartBulkOperation();
            foreach (DataRow row in SharedData.DataSet.Tables[0].Rows)
            {
                foreach (DataColumn column in SharedData.DataSet.Tables[0].Columns)
                {
                    if (column.ColumnName != "Rom Path")
                    {
                        if (column.DataType == typeof(bool))
                        {
                            row[column] = false; // Set bool columns to false
                        }
                        else if (column.ColumnName == "Name")
                        {
                            string romPath = row["Rom Path"].ToString()!;
                            string trimmedName = romPath.Substring(2);
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(trimmedName);
                            row[column] = nameWithoutExtension;
                        }
                        else
                        {
                            row[column] = DBNull.Value; // Clear other columns
                        }
                    }
                }
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            // Flip selected row to invoke selection changed event
            // It's just easier to do this way
            // If media is showing, view will be updated
            if (MainDataGrid.Items.Count > 1)
            {
                MainDataGrid.SelectedIndex = 1;
                MainDataGrid.SelectedIndex = 0;
            }
        }

        private void menuItem_ColumnAutoSize_Click(object sender, RoutedEventArgs e)
        {
            bool value = menuItem_ColumnAutoSize.IsChecked;
            menuItem_ColumnAutoSize.IsChecked = !value;
            _autosizeColumns = !value;
            AdjustDataGridColumnWidths(MainDataGrid);
            Properties.Settings.Default.AutoSizeColumns = !value;
        }

        private void button_ClearGenreSelection_Click(object sender, RoutedEventArgs e)
        {
            comboBox_Genre.SelectedIndex = 0;
            button_ClearGenreSelection.Visibility = Visibility.Hidden;
            UpdateCounters();
        }

        private void button_ClearCustomFilter_Click(object sender, RoutedEventArgs e)
        {
            textBox_CustomFilter.Text = "";
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            ApplyFilters(new string[] { _visibilityFilter, _genreFilter });
            UpdateCounters();
        }

        private void menuItem_ClearSelected_Click(object sender, RoutedEventArgs e)
        {

            if (MainDataGrid.SelectedItems.Count < 1)
            {
                MessageBox.Show("No item is selected!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            if (confirmBulkChanges == true)
            {
                var result = MessageBox.Show($"Do you want clear data for the selected items?\n\nRom Paths will remain unchanged\nNames will now reflect the rom name without extension", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            SharedData.ChangeTracker!.StartBulkOperation();

            var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
               .Select(rowView => rowView.Row["Rom Path"]?.ToString())
               .ToList();

            var matchingRows = from DataRow row in SharedData.DataSet.Tables[0].AsEnumerable()
                               let pathValue = row.Field<string>("Rom Path")
                               where selectedPaths.Contains(pathValue)
                               select row;


            foreach (var row in matchingRows)
            {
                // Clear all cell values except "Rom Path"
                foreach (DataColumn column in SharedData.DataSet.Tables[0].Columns)
                {
                    if (column.ColumnName != "Rom Path")
                    {
                        if (column.DataType == typeof(bool))
                        {
                            row[column] = false; // Set bool columns to false
                        }
                        else if (column.ColumnName == "Name")
                        {
                            string romPath = row["Rom Path"].ToString()!;
                            string trimmedName = romPath.Substring(2);
                            string nameWithoutExtension = Path.GetFileNameWithoutExtension(trimmedName);
                            row[column] = nameWithoutExtension;
                        }
                        else
                        {
                            row[column] = DBNull.Value; // Clear other columns
                        }
                    }
                }
            }

            SharedData.ChangeTracker!.EndBulkOperation();

            // For a view update by switching selected item
            // to create selection changed event

            var firstSelectedItem = MainDataGrid.SelectedItems[0];
            int index = MainDataGrid.Items.IndexOf(firstSelectedItem);
            int totalItems = MainDataGrid.Items.Count;

            if (index > 0)
            {
                MainDataGrid.SelectedIndex = 0;
                MainDataGrid.SelectedIndex = index;
            }

            if (index == 0 && totalItems > 1)
            {
                MainDataGrid.SelectedIndex = 1;
                MainDataGrid.SelectedIndex = index;
            }
        }

        private void menuItem_MusicJukebox_Click(object sender, RoutedEventArgs e)
        {
            PlayJukeBox("music");
        }

        private void PlayJukeBox(string mediaType)
        {
            string jsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;

            string filePath;
            if (mediaType == "video")
            {
                filePath = mediaPaths["video"];
            }
            else
            {
                filePath = mediaPaths["music"];
            }

            string transformed = filePath.StartsWith("./") ? filePath.Substring(2).Replace('/', '\\') : filePath.Replace('/', '\\');
            string fullPath = Path.Combine(_parentFolderPath!, transformed);

            if (!Directory.Exists(fullPath))
            {
                MessageBox.Show($"There's no {mediaType} folder for this platform!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] files = Directory.GetFiles(fullPath);

            if (files.Length == 0)
            {
                MessageBox.Show($"There's no {mediaType} files for this platform!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MediaPlayerWindow mediaPlayerWindow = new MediaPlayerWindow(files);
            mediaPlayerWindow.Show();
        }


        private void menuItem_Restore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_parentFolderPath))
            {
                return;
            }

            string backupFolder = $"{SharedData.ProgramDirectory}\\gamelist backup";

            if (!Path.Exists(backupFolder))
            {
                MessageBox.Show("A 'backups' folder does not exist yet!", "Backup Folder Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Restore a Gamelist",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml",
                InitialDirectory = backupFolder
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            // Get the selected file's path
            string selectedPath = openFileDialog.FileName;
            if (!selectedPath.EndsWith(".xml"))
            {
                return;
            }

            if (!selectedPath.Contains(backupFolder))
            {
                MessageBox.Show("Please only restore from the 'backups' folder", "Invalid Backup Location", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string fileName = Path.GetFileName(selectedPath);
            string directoryPath = Path.GetDirectoryName(selectedPath)!;
            string systemName = new DirectoryInfo(directoryPath).Name;

            if (systemName != SharedData.CurrentSystem)
            {
                MessageBox.Show($"Please only restore for the current system '{SharedData.CurrentSystem}'", "Incorrect System Choice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string pattern = @"_(\d{8}_\d{6})\.xml$";
            Match match = Regex.Match(fileName, pattern);
            if (!match.Success)
            {
                return;
            }

            string dateTime = match.Groups[1].Value;
            string dateTimeFormatted;
            string inputFormat = "yyyyMMdd_HHmmss";
            if (DateTime.TryParseExact(dateTime, inputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDateTime))
            {
                dateTimeFormatted = parsedDateTime.ToString("MMMM d, yyyy 'at' h:mmtt");
            }
            else
            {
                dateTimeFormatted = "Unknown";
            }

            var result = MessageBox.Show($"Ok to restore '{fileName}'?" +
                $"\n\nFor system: {systemName}" +
                $"\nBackup time: {dateTimeFormatted}" +
                $"\nRestore to: {SharedData.XMLFilename}" +
                "\n\nThe existing gamelist.xml will be renamed to gamelist.old",
                "Restore File?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string oldFile = SharedData.XMLFilename.Replace(".xml", ".old");
            File.Copy(SharedData.XMLFilename, oldFile, true);
            File.Copy(selectedPath, SharedData.XMLFilename, true);
            MessageBox.Show($"Restore completed!\n\nThe new gamelist will now be loaded", "Restore Completed!", MessageBoxButton.OK, MessageBoxImage.Information);
            SharedData.IsDataChanged = false;
            OpenFile (SharedData.XMLFilename);

        }

        private void button_Scraper_Click(object sender, RoutedEventArgs e)
        {

            bool show = true;

            if (MainContentFrame.Content == _mediaPage)
            {

                _mediaPage.StopPlaying();

            }

            button_Media.Content = "Show Media";

            if (button_Scraper.Content.ToString() == "Show Scraper" && button_Scraper.IsEnabled == true)
            {
                button_Scraper.Content = "Hide Scraper";
                show = true;
            }
            else
            {
                button_Scraper.Content = "Show Scraper";
                show = false;
            }

            ShowScraperPage(show);

        }

        private void ShowScraperPage(bool show)
        {
            ShowMediaPage(false);
            MainContentFrame.Navigate(_scraper);

            if (show)
            {
                MainGrid.RowDefinitions[3].Height = new GridLength(270);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainGrid.RowDefinitions[3].Height = new GridLength(0);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
            }
        }

        private void button_Media_Click(object sender, RoutedEventArgs e)
        {
            button_Scraper.Content = "Show Scraper";

            bool show = true;
            if (button_Media.Content.ToString() == "Show Media" && button_Media.IsEnabled == true)
            {
                button_Media.Content = "Hide Media";
                show = true;
            }
            else
            {
                button_Media.Content = "Show Media";
                show = false;
            }
            ShowMediaPage(show);
        }

        private void ShowMediaPage(bool show)
        {

            MainContentFrame.Navigate(_mediaPage);

            if (show)
            {
                MainGrid.RowDefinitions[3].Height = new GridLength(235);
                gridSplitter_Horizontal.Visibility = Visibility.Visible;

                if (MainDataGrid.SelectedItems.Count == 0 && MainDataGrid.Items.Count > 0)
                {
                    MainDataGrid.SelectedIndex = 0; // Select the first row
                }

                // Get the first selected row
                DataRowView? selectedRow = MainDataGrid.SelectedItems
                .OfType<DataRowView>()
                .FirstOrDefault();

                _mediaPage.ShowMedia(selectedRow!);

            }
            else
            {
                MainGrid.RowDefinitions[3].Height = new GridLength(0);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
                if (_mediaPage != null)
                {
                    _mediaPage.ClearAllImages();
                    _mediaPage.StopPlaying();
                }
            }
        }

        private void menuItem_FindNewItems_Click(object sender, RoutedEventArgs e)
        {
            FindNewItems(SharedData.CurrentSystem);
        }

        private void FindNewItems(string systemName)
        { 
            string optionsFileName = $"ini\\filetypes.ini";
            string[] fileExtensions;
            var fileTypes = IniFileReader.GetSection(optionsFileName, "Filetypes");

            if (fileTypes == null)
            {
                MessageBox.Show(
                    $"No file types found! Please check filetypes.ini file configuration.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (fileTypes.TryGetValue(systemName, out string? fileTypesForSystem))
            {
                fileExtensions = fileTypesForSystem
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim())
                    .ToArray();

                if (fileExtensions.Length == 0)
                {
                    MessageBox.Show(
                        $"No file extensions found for the system '{systemName}'. Please check your ini file configuration.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                string fileTypesList = string.Join("\n", fileExtensions);

                var result = MessageBox.Show(
                    "This will check for additional items and add them to your gamelist.\n\n" +
                    "Search criteria will be based upon these file extensions:\n" +
                    $"{fileTypesList}\n\n" +
                    "Items listed in m3u files will be ignored.\n\n" +
                    $"Search depth setting: {Properties.Settings.Default.SearchDepth}\n\n" +
                    "Do you want to continue?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;  // Exit early
                }
            }
            else
            {
                MessageBox.Show(
                    $"No file types found for the system '{systemName}'. Please check your ini file configuration.",
                    "System Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            int searchDepth = Properties.Settings.Default.SearchDepth;
            int totalNewItems = SearchForNewItems(fileExtensions, searchDepth);
            UpdateCounters();

            Mouse.OverrideCursor = null;

            if (totalNewItems == 0)
            {
                MessageBox.Show("No additional items were found", "Notice:", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var statusColumn = MainDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Status");
                if (statusColumn != null)
                {
                    statusColumn.Visibility = Visibility.Visible;
                }
                MessageBox.Show($"{totalNewItems} items were found and added to the gamelist", "Notice:", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private int SearchForNewItems(string[] fileExtensions, int searchDepth)
        {
            // Initialize the M3U contents HashSet directly
            var m3uContents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Get all .m3u files in the directory
            string[] m3uFiles = Directory.GetFiles(_parentFolderPath!, "*.m3u");

            // Populate the M3U contents HashSet with normalized paths
            foreach (var m3uFile in m3uFiles)
            {
                m3uContents.UnionWith(
                    File.ReadLines(m3uFile)
                        .Select(line => line.Trim().Replace("\\", "/"))
                );
            }

            // Get existing file paths from the DataTable, normalized
            HashSet<string> fileList;
            if (SharedData.DataSet.Tables[0].Rows.Count == 0)
            {
                fileList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                fileList = SharedData.DataSet.Tables[0].AsEnumerable()
                    .Select(row => row.Field<string>("Rom Path"))
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => (path!.StartsWith("./") ? path.Substring(2) : path).Replace("\\", "/"))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            // Determine the search option based on the search depth
            var searchOption = searchDepth > 0 ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Enumerate through the directory for files matching the extensions
            var fullPathList = fileExtensions
                .SelectMany(ext =>
                    Directory.EnumerateFiles(_parentFolderPath, $"*{ext}", searchOption)
                        .Select(file =>
                        {
                            var dirName = Path.GetDirectoryName(file)!;
                            var relativePath = Path.GetRelativePath(_parentFolderPath, dirName);

                            int depth = relativePath == "."
                                ? 0
                                : relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                              .Count(segment => !string.IsNullOrEmpty(segment)) + 1;

                            bool isIncluded = depth <= searchDepth;

                            var relativeFilePath = Path.GetRelativePath(_parentFolderPath, file)
                                                       .Replace("\\", "/"); // normalize during scan

                            return (file: relativeFilePath, isIncluded);
                        })
                        .Where(result => result.isIncluded)
                        .Select(result => result.file)
                )
                .ToList();

            // Create HashSet for faster lookups
            var fileListSet = new HashSet<string>(fileList, StringComparer.OrdinalIgnoreCase);

            // Filter new files
            var newFileList = fullPathList
                .Where(relativeFile =>
                {
                    var fileName = Path.GetFileName(relativeFile); // filter by filename only
                    return !fileListSet.Contains(fileName) && !m3uContents.Contains(fileName);
                })
                .Distinct(StringComparer.OrdinalIgnoreCase) // distinct by full relative path
                .ToList();

            int totalNewItems = newFileList.Count;

            if (totalNewItems == 0)
            {
                return 0;
            }

            // Begin bulk operations
            SharedData.ChangeTracker!.StartBulkOperation();

            foreach (string fileName in newFileList)
            {
                string newName = Path.GetFileNameWithoutExtension(fileName);
                var newRow = SharedData.DataSet.Tables[0].NewRow();
                newRow["Name"] = newName;
                newRow["Rom Path"] = $"./{fileName}";  // Store path with forward slashes
                newRow["Hidden"] = false;
                newRow["Favorite"] = false;
                newRow["Status"] = "New";

                SharedData.DataSet.Tables[0].Rows.Add(newRow);
            }

            // Finalize changes
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            return totalNewItems;
        }


        private void menuItem_AddMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaToolWindow mediaSearch = new MediaToolWindow();

            SharedData.ChangeTracker!.StopTracking();

            mediaSearch.ShowDialog();

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo > 0)
            {
                // Re-initialize Change Tracker
                SharedData.ChangeTracker!.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                UpdateChangeTrackerButtons();
            }
        }

        private void menuItem_FindMissingItems_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will quickly identify any missing items in this gamelist\n\nDo you want to continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            FindMissingItems();
        }

        private void FindMissingItems()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            int missingCount = 0;

            // Create a HashSet for fast lookups
            var filesSet = new HashSet<string>(Directory.GetFiles(_parentFolderPath!, "*", SearchOption.AllDirectories).Select(f =>
                Path.GetRelativePath(_parentFolderPath!, f).Replace("\\", "/"))); // Use relative paths with '/' separator

            var missingItems = new List<DataRowView>(); // To store rows with missing files

            // Temporarily disable DataGrid updates
            MainDataGrid.IsEnabled = false;

            try
            {
                // First loop: Find missing items
                foreach (DataRowView row in MainDataGrid.Items.OfType<DataRowView>())
                {
                    if (row["Rom Path"] is string romPath)
                    {
                        string trimmedRomPath = romPath.StartsWith("./") ? romPath.Substring(2) : romPath;
                        string relativeRomPath = trimmedRomPath.Replace("\\", "/"); // Ensure using '/' separator

                        if (!filesSet.Contains(relativeRomPath))
                        {
                            missingItems.Add(row); // Add to missing items
                            missingCount++;
                        }
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
                MainDataGrid.IsEnabled = true;
            }

            // If no missing items are found, show a message and exit
            if (missingItems.Count == 0)
            {
                MessageBox.Show("No missing items were detected", "Notice:", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show Status column if it exists
            var statusColumn = MainDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Status");
            if (statusColumn != null)
            {
                statusColumn.Visibility = Visibility.Visible;
            }

            // Ask user for confirmation before removal
            var result = MessageBox.Show($"There are {missingItems.Count} missing items in this gamelist.\n\nDo you want to remove them?",
                "Notice:", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SharedData.ChangeTracker!.StartBulkOperation();
                foreach (var row in missingItems)
                {
                    // Remove from DataGrid source safely
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        (MainDataGrid.ItemsSource as DataView)?.Table.Rows.Remove(row.Row);
                    });
                }
                SharedData.ChangeTracker!.EndBulkOperation();
            }
            else
            {
                // If not removing, mark them as missing
                foreach (var row in missingItems)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        row["Status"] = "Missing";
                    });
                }
            }
        }


        private void menuItem_MameIdentifyUnplayable_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.CurrentSystem != "mame")
            {
                MessageBox.Show("This is not a mame gamelist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string message = "This will identify games that are not playable according to the following rules:\n\n" +
                      "isbios = yes\n" +
                      "isdevice = yes\n" +
                      "ismechanical = yes\n" +
                      "driver status = preliminary\n" +
                      "disk status = nodump\n" +
                      "runnable = no\n\n" +
                      "\n\nSelect OK to continue or Cancel to abort";

            var result = MessageBox.Show(message, "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            MameIdentifyUnplayable(mamePath);
        }

        private async void MameIdentifyUnplayable(string mamePath)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            // Get unplayable game names using MameHelper asynchronously
            var gameNames = await MameHelper.GetMameUnplayable(mamePath);

            if (gameNames == null || gameNames.Count == 0)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("No data was returned!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SharedData.ChangeTracker!.PauseTracking();

            object lockObject = new object();

            // Loop through each row in the DataTable using Parallel.ForEach

            ConcurrentBag<DataRow> processedRows = new ConcurrentBag<DataRow>();
            Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
            {
                string romPath = row["Rom Path"].ToString()!.Substring(2);
                string romPathWithoutExtension = Path.GetFileNameWithoutExtension(romPath)!;

                // Check if the game path is in the list of unplayable game names
                if (gameNames.Contains(romPathWithoutExtension))
                {
                    // Lock to ensure thread safety when updating DataTable
                    lock (lockObject)
                    {
                        // Modify DataRow inside the lock
                        row["Status"] = "Unplayable";
                        processedRows.Add(row); // Store the row for later modification
                    }
                }
            });

            Mouse.OverrideCursor = null;

            List<DataRow> processedRowsList = processedRows.ToList();

            SharedData.DataSet.AcceptChanges();

            SharedData.ChangeTracker!.ResumeTracking();

            int unplayableCount = processedRowsList.Count;

            if (unplayableCount == 0)
            {
                MessageBox.Show("No unplayable games found", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MainDataGrid.Columns[0].Visibility = Visibility.Visible;
            MainDataGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

            // Show a dialog to optionally hide unplayable games
            var setHidden = MessageBox.Show($"There were {unplayableCount} unplayable items found.\nDo you want to set them hidden?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (setHidden != MessageBoxResult.Yes)
            {
                return;
            }
            
            // Mark the previously processed rows as hidden
            Mouse.OverrideCursor = Cursors.Wait;
            SharedData.ChangeTracker!.StartBulkOperation();

            foreach (DataRow row in processedRowsList)
            {
                row["Hidden"] = true;
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();
            Mouse.OverrideCursor = null;
            UpdateCounters();
        }


        private void menuItem_MameIdentifyCHDRequired_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.CurrentSystem != "mame")
            {
                MessageBox.Show("This is not a mame gamelist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string message = "This will identify games that require a CHD. " +
                "\n\nSelect OK to proceed or Cancel to abort.";

            var result = MessageBox.Show(message, "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            IdentifyRequiresCHD(mamePath);
        }

        private async void IdentifyRequiresCHD(string mamePath)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            SharedData.ChangeTracker!.PauseTracking();

            try
            {
                string mameRomPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

                // Get CHD information directly without storing in a list
                var chdInfoList = await MameHelper.GetMameRequiresCHD(mamePath, mameRomPath);

                // Lock object for synchronizing access to shared resources (DataSet)
                object lockObject = new object();

                // Update DataSet (shared resource) based on CHD information
                Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
                {
                    string romPath = row["Rom Path"].ToString()!.Substring(2);
                    string romPathWithoutExtension = Path.GetFileNameWithoutExtension(romPath)!;

                    // Check CHD requirement for the current game
                    var chdInfo = chdInfoList.FirstOrDefault(chd =>
                        chd.GameName.Equals(romPathWithoutExtension, StringComparison.OrdinalIgnoreCase));

                    if (chdInfo != null)
                    {
                        // Lock to ensure thread safety when updating shared resources (DataSet)
                        lock (lockObject)
                        {
                            // Modify DataRow inside the lock
                            string missing = !chdInfo.Present ? $", missing {chdInfo.DiskName}.chd" : "";
                            row["Status"] = $"CHD Required, status={chdInfo.Status}{missing}";
                        }
                    }
                });

                // Commit changes and update UI
                lock (lockObject)
                {
                    SharedData.DataSet.AcceptChanges();
                }

                // Refresh UI components outside of the lock
                MainDataGrid.Columns[0].Visibility = Visibility.Visible;
                MainDataGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                SharedData.ChangeTracker!.ResumeTracking();
            }
        }

        private void menuItem_MameIdentifyClones_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.CurrentSystem != "mame")
            {
                MessageBox.Show("This is not a mame gamelist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string message = "This will identify games that are bootlegs of a parent set." +
                "\n\nSelect OK to proceed or Cancel to abort.";

            var result = MessageBox.Show(message, "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            IdentifyClones(mamePath);
        }

        private async void IdentifyClones(string mamePath)
        {

            Mouse.OverrideCursor = Cursors.Wait;

            Dictionary<string, string> clones = new Dictionary<string, string>();

            try
            {
                clones = await Task.Run(() => MameHelper.GetMameClones(mamePath));
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (clones == null || clones.Count == 0)
            {
                MessageBox.Show("No data was returned!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int cloneCount = 0;

            SharedData.ChangeTracker!.PauseTracking();

            // Lock object for synchronizing access to shared resources (DataSet)
            object lockObject = new object();
            ConcurrentBag<DataRow> cloneRows = new ConcurrentBag<DataRow>();

            // Loop through each row in the DataTable
            Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
            {
                string romPath = row["Rom Path"].ToString()!.Substring(2);
                string romPathWithoutExtension = Path.GetFileNameWithoutExtension(romPath)!;

                // Set the value of the "clone" column to true or false
                if (clones.ContainsKey(romPathWithoutExtension))
                {
                    Interlocked.Increment(ref cloneCount);
                    lock (lockObject)
                    {
                        row["Status"] = $"Clone of '{clones[romPathWithoutExtension]}'";
                        cloneRows.Add(row); // Store the row for later modifications
                    }
                }
            });

            SharedData.ChangeTracker?.ResumeTracking();

            // Convert to a List if needed for further modifications
            List<DataRow> cloneRowsList = cloneRows.ToList();

            if (cloneCount == 0)
            {
                MessageBox.Show("No clone items were found.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Mouse.OverrideCursor = null;

            MainDataGrid.Columns[0].Visibility = Visibility.Visible;
            MainDataGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

            var setHidden = MessageBox.Show($"There were {cloneCount} clone items found.\nDo you want to set them hidden?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (setHidden == MessageBoxResult.Yes)
            {
                SharedData.ChangeTracker?.StartBulkOperation();

                // Mark previously identified clone rows as hidden
                foreach (DataRow row in cloneRowsList)
                {
                    row["Hidden"] = true;
                }
                SharedData.DataSet.AcceptChanges();
                SharedData.ChangeTracker?.EndBulkOperation();
            }
        }

        private void menuItem_MameIdentifyBootleg_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.CurrentSystem != "mame")
            {
                MessageBox.Show("This is not a mame gamelist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string message = "This will identify games that are known bootleg." +
             "\n\nSelect OK to proceed or Cancel to abort.";

            var result = MessageBox.Show(message, "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
            {
                return;
            }


            IdentifyBootleg(mamePath);
        }

        private async void IdentifyBootleg(string mamePath)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            List<string> bootlegs = new();

            try
            {
                bootlegs = await Task.Run(() => MameHelper.GetMameBootleg(mamePath));
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (bootlegs == null || bootlegs.Count == 0)
            {
                MessageBox.Show("No data was returned!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int bootlegCount = 0;

            SharedData.ChangeTracker!.PauseTracking();

            // Lock object for synchronizing access to shared resources (DataSet)
            object lockObject = new object();
            ConcurrentBag<DataRow> bootlegRows = new ConcurrentBag<DataRow>();

            // Loop through each row in the DataTable
            Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
            {
                string romPath = row["Rom Path"].ToString()!.Substring(2);
                string romPathWithoutExtension = Path.GetFileNameWithoutExtension(romPath)!;

                // Set the value of the "clone" column to true or false
                if (bootlegs.Contains(romPathWithoutExtension))
                {
                    Interlocked.Increment(ref bootlegCount);
                    lock (lockObject)
                    {
                        row["Status"] = "Bootleg";
                        bootlegRows.Add(row); // Store the row for later modifications
                    }
                }
            });

            // Convert to a List if needed for further modifications
            List<DataRow> bootlegRowsList = bootlegRows.ToList();

            Mouse.OverrideCursor = null;

            SharedData.ChangeTracker?.ResumeTracking();

            if (bootlegCount == 0)
            {
                MessageBox.Show("No bootleg items were found.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MainDataGrid.Columns[0].Visibility = Visibility.Visible;
            MainDataGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

            var setHidden = MessageBox.Show($"There were {bootlegCount} bootleg items found.\nDo you want make sure all are identified as bootleg?\n\nThe Publisher, Developer and Name will contain a bootleg reference.", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (setHidden == MessageBoxResult.Yes)
            {
                SharedData.ChangeTracker?.StartBulkOperation();
                // Mark previously identified clone rows as hidden
                foreach (DataRow row in bootlegRowsList)
                {
                    row["Developer"] = "bootleg";
                    row["Publisher"] = "bootleg";
                    row["Name"] = UpdateNameWithBootleg(row["Name"].ToString()!);
                }

                SharedData.DataSet.AcceptChanges();
                SharedData.ChangeTracker!.EndBulkOperation();
            }
        }

        public static string UpdateNameWithBootleg(string description)
        {
            // Check if "bootleg" is already part of the string
            if (description.IndexOf("bootleg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // If "bootleg" is already present, return the string as-is
                return description;
            }

            // Check if the string contains brackets with text
            int openBracketIndex = description.IndexOf('(');
            int closeBracketIndex = description.IndexOf(')');

            if (openBracketIndex >= 0 && closeBracketIndex > openBracketIndex)
            {
                // Insert "bootleg" before the existing text inside the parentheses
                string beforeBracket = description.Substring(0, openBracketIndex + 1);
                string insideBracket = description.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
                string afterBracket = description.Substring(closeBracketIndex);

                return $"{beforeBracket}bootleg, {insideBracket}{afterBracket}";
            }
            else
            {
                // Add "(bootleg)" to the end of the string if no brackets are present
                return $"{description} (bootleg)";
            }
        }

        private void menuItem_UseMameDescriptionsForNames_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.CurrentSystem != "mame")
            {
                MessageBox.Show("This is not a mame gamelist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string message = "This will set all name metadata values to what is used by " +
            "the internal Mame database description.  It is often more descriptive than what scrapers may provide." +
            "\n\nSelect OK to proceed or Cancel to abort.";

            var result = MessageBox.Show(message, "Notice", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            UseMameDescriptionsForNames(mamePath);
        }

        private void UseMameDescriptionsForNames(string mamePath)
        {

            Mouse.OverrideCursor = Cursors.Wait;
            string filePath = "./ini/arcadenames.ini";
            var mameNames = IniFileReader.GetSection(filePath, "ArcadeNames");

            if (mameNames == null || mameNames.Count == 0)
            {
                MessageBox.Show("No data was returned!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
                return;
            }

            SharedData.ChangeTracker?.StartBulkOperation();

            foreach (DataRow row in SharedData.DataSet.Tables[0].Rows)
            {
                string romPath = row["Rom Path"].ToString()!.Substring(2);
                string romPathWithoutExtension = Path.GetFileNameWithoutExtension(romPath)!;

                if (mameNames.ContainsKey(romPathWithoutExtension))
                {
                    row["Name"] = mameNames[romPathWithoutExtension];
                }
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

            Mouse.OverrideCursor = null;

        }


        private void button_Items_Click(object sender, RoutedEventArgs e)
        {
            button_Items.Content = button_Items.Content.ToString() == "All Items" ? "Selected Items" : "All Items";
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            string from = textBox_ChangeFrom.Text;
            string to = textBox_ChangeTo.Text;
            string column = comboBox_Columns.Text;

            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;

            if (confirmBulkChanges)
            {
                var result = MessageBox.Show($"Are you absolutely sure?\n\nReplace '{from}' with '{to}'?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            bool scrapeAll = button_Items.Content.ToString() == "All Items";
            var datagridRowSelection = scrapeAll
               ? MainDataGrid.Items.OfType<DataRowView>().ToList()
               : MainDataGrid.SelectedItems.OfType<DataRowView>().ToList();

            SharedData.ChangeTracker!.StartBulkOperation();

            foreach (DataRowView row in datagridRowSelection)
            {
                string currentValue = row[column].ToString() ?? string.Empty;

                string newValue;

                if (from.Contains("*") || from.Contains("?"))
                {
                    // If wildcard characters are present, use regex for search and replace
                    string searchPattern = Regex.Escape(from).Replace("\\*", ".*").Replace("\\?", ".");
                    newValue = Regex.Replace(currentValue, searchPattern, to, RegexOptions.IgnoreCase);
                }
                else
                {
                    // If no wildcard characters are present, use simple string replace (case-insensitive)
                    newValue = Regex.Replace(currentValue, Regex.Escape(from), to, RegexOptions.IgnoreCase);
                }

                // Handle empty strings for database or data handling purposes
                if (string.IsNullOrEmpty(newValue))
                {
                    row[column] = DBNull.Value;
                }
                else
                {
                    row[column] = newValue;
                }
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            button_Apply.IsEnabled = false;

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;

            if (textBox == null)
            {
                return;
            }

            string from = textBox_ChangeFrom.Text;
            string to = textBox_ChangeTo.Text;

            if (!string.IsNullOrEmpty(from))
            {
                button_Apply.IsEnabled = true;
            }
            else
            {
                button_Apply.IsEnabled = false;
            }
        }

        private void comboBox_Columns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            button_Apply.IsEnabled = false;

            string from = textBox_ChangeFrom.Text;

            if (!string.IsNullOrEmpty(from))
            {
                button_Apply.IsEnabled = true;
            }
        }

        
        private void menuItem_Find_Click(object sender, RoutedEventArgs e)
        {
            if (dockPanel_Find.Visibility == Visibility.Collapsed)
            {
                dockPanel_Find.Visibility = Visibility.Visible;
            }
            else
            {
                dockPanel_Find.Visibility = Visibility.Collapsed;
            }
        }

        private void button_CloseFind_Click(object sender, RoutedEventArgs e)
        {
            dockPanel_Find.Visibility = Visibility.Collapsed;
        }

        private void textBox_Find_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;

            if (textBox == null)
            {
                return;
            }

            string findText = textBox.Text;
        
            if (!string.IsNullOrEmpty(findText))
            {
                button_Find.IsEnabled = true;
            }
            else
            {
                button_Find.IsEnabled = false;
            }
        }

     
        private void comboBox_Columns1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            button_Find.IsEnabled = false;
            textBox_Find.Text = string.Empty;

        }

        private void FindNext()
        {
            string column = comboBox_Columns1.Text;
            string searchText = textBox_Find.Text;

            int currentIndex = MainDataGrid.SelectedIndex;

            int rowIndex = FindRowIndexInDataGrid(MainDataGrid, currentIndex, searchText, string.IsNullOrEmpty(column) ? null : column);

            if (rowIndex >= 0)
            {
                MainDataGrid.SelectedIndex = rowIndex;
                MainDataGrid.ScrollIntoView(MainDataGrid.Items[rowIndex]);
            }
            else
            {
                MessageBox.Show("No matching items were found.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void button_Find_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void textBox_Find_KeyDown(object sender, KeyEventArgs e)
        {
            string findText = textBox_Find.Text;
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(findText))
            {
                FindNext();
            }
        }

        private int FindRowIndexInDataGrid(DataGrid dataGrid, int currentIndex, string searchText, string columnName = null)
        {
            if (string.IsNullOrWhiteSpace(searchText) || dataGrid.Items.Count == 0)
                return -1;

            int startIndex = (currentIndex + 1) % dataGrid.Items.Count;

            // Pass 1: from startIndex → end
            for (int i = startIndex; i < dataGrid.Items.Count; i++)
            {
                if (IsMatch(dataGrid.Items[i], searchText, columnName))
                    return i;
            }

            // Pass 2: from 0 → currentIndex
            for (int i = 0; i < startIndex; i++)
            {
                if (IsMatch(dataGrid.Items[i], searchText, columnName))
                    return i;
            }

            return -1; // nothing found
        }

        private bool IsMatch(object item, string searchText, string columnName)
        {
            if (item is DataRowView rowView)
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    // Search all columns
                    foreach (var col in rowView.Row.ItemArray)
                    {
                        if (col?.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
                else if (rowView.Row.Table.Columns.Contains(columnName))
                {
                    var value = rowView.Row[columnName]?.ToString();
                    if (!string.IsNullOrEmpty(value) &&
                        value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }


        private void menuItem_SearchAndReplace_Click(object sender, RoutedEventArgs e)
        {
            if (dockPanel_SearchAndReplace.Visibility == Visibility.Collapsed)
            {
                dockPanel_SearchAndReplace.Visibility = Visibility.Visible;
            }
            else
            {
                dockPanel_SearchAndReplace.Visibility = Visibility.Collapsed;
            }
        }

        private void button_CloseSearchAndReplace_Click(object sender, RoutedEventArgs e)
        {
            dockPanel_SearchAndReplace.Visibility = Visibility.Collapsed;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/RobG66/Gamelist-Manager/issues";
            OpenPage(url);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/RobG66/Gamelist-Manager";
            OpenPage(url);
        }

        private void OpenPage(string url)
        {
            try
            {
                // Use Process.Start to open the webpage in the default browser
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // This ensures it opens the default browser
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open webpage: {ex.Message}");
            }
        }
                
        private void menuItem_ClearMediaPaths_Click(object sender, RoutedEventArgs e)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;


            MenuItem? menuItem = sender as MenuItem;

            DataTable dataTable = SharedData.DataSet.Tables[0];
            IEnumerable<DataRow> rows;

            string text = "all";

            int index = 0;

            if (menuItem?.Name == "menuItem_ClearAllMediaPaths")
            {
                if (MainDataGrid.Items.Count == 0)
                {
                    MessageBox.Show("There are no displayed items!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Get all rows
                rows = dataTable.AsEnumerable();

            }
            else
            {
                if (MainDataGrid.SelectedItems.Count < 1)
                {
                    MessageBox.Show("No item is selected!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get only the selected rows
                var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
                    .Select(rowView => rowView.Row["Rom Path"]?.ToString())
                    .ToList();

                rows = dataTable.AsEnumerable()
                    .Where(row => selectedPaths.Contains(row.Field<string>("Rom Path")));

                var firstSelectedRow = MainDataGrid.SelectedItems[0];
                index = MainDataGrid.Items.IndexOf(firstSelectedRow);
                text = "selected";
            }

            if (confirmBulkChanges == true)
            {
                var result = MessageBox.Show($"Do you want clear {text} media paths?\n\nAll other metadata remains unchanged", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            SharedData.ChangeTracker!.StartBulkOperation();

            var mediaItems = GamelistMetaData.GetMetaDataDictionary().Values
                    .Where(decl => decl.DataType == MetaDataType.Image ||
                                   decl.DataType == MetaDataType.Video ||
                                   decl.DataType == MetaDataType.Music ||
                                   decl.DataType == MetaDataType.Document)
                                   .Select(decl => decl.Name)
                                   .ToList();

            // Iterate through each column in the DataTable
            foreach (var columnName in mediaItems)
            {
                // Ensure the column exists
                if (dataTable.Columns.Contains(columnName))
                {
                    // Iterate through all rows in the table and clear the column's content
                    foreach (DataRow row in rows)
                    {
                        row[columnName] = DBNull.Value;
                    }
                }
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            // Flip selected row to invoke selection changed event
            // It's just easier to do this way
            // If media is showing, view will be updated
            if (MainDataGrid.Items.Count > 1)
            {
                int modifier = 1;
                if (MainDataGrid.Items.Count - 1 == index)
                {
                    modifier = -1;
                }

                MainDataGrid.SelectedIndex = index + modifier;
                MainDataGrid.SelectedIndex = index;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool saveReminder = Properties.Settings.Default.SaveReminder;
            if (SharedData.IsDataChanged && saveReminder)
            {
                SaveGamelist();
            }
        }

        private void menuItem_ResetName_Click(object sender, RoutedEventArgs e)
        {
            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;
            if (confirmBulkChanges)
            {
                MessageBoxResult result = MessageBox.Show($"Do you want to reset the names for the selected items?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }
            ResetNames();
        }

        private void ResetNames()
        {
            SharedData.ChangeTracker?.StartBulkOperation();
            foreach (var item in MainDataGrid.SelectedItems)
            {
                if (item is DataRowView row && row["Rom Path"] is string path && !string.IsNullOrEmpty(path))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                    row["Name"] = fileNameWithoutExtension;
                }
            }
            SharedData.ChangeTracker?.EndBulkOperation();
        }

        private void menuItem_ResetAllSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to reset all settings?\n\nSaved passwords will not be affected", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

        }

        private void menuItem_AllVisible_Click(object sender, RoutedEventArgs e)
        {
            SetAllHiddenValues(false);
        }

        private void menuItem_AllHidden_Click(object sender, RoutedEventArgs e)
        {
            SetAllHiddenValues(true);
        }

        private void SetAllHiddenValues(bool hidden)
        {

            string text = "visible";
            if (hidden == true)
            {
                text = "hidden";
            }


            DataView dataView = (DataView)MainDataGrid.ItemsSource;

            string text2 = "all";
            if (!string.IsNullOrEmpty(dataView.RowFilter))
            {
                text2 = "all filtered";
            }

            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;
            if (confirmBulkChanges)
            {
                MessageBoxResult result = MessageBox.Show($"Do you want to set {text2} items {text}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            SharedData.ChangeTracker!.StartBulkOperation();

            foreach (DataRowView rowView in dataView)
            {
                rowView.Row["Hidden"] = hidden;
            }


            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

        }

        private void menuItem_RemoveSshKey_Click(object sender, RoutedEventArgs e)
        {
            string hostname = Properties.Settings.Default.BatoceraHostName;

            // Check if the hostname is set
            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("The Batocera hostname is not set!");
                return;
            }

            // Ask the user if they want to reset the key
            var result = MessageBox.Show($"Do you want to reset the local SSH key for '{hostname}'?",
                                         "Reset Confirmation",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            // Proceed based on the user's choice
            if (result == MessageBoxResult.Yes)
            {
                string response = SshKeyManager.RemoveBatoceraKey(hostname);
                MessageBox.Show(response, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItem_CreateArcadeGamesINI_Click(object sender, RoutedEventArgs e)
        {
            string? mamePath = Properties.Settings.Default.MamePath;
            if (string.IsNullOrEmpty(mamePath))
            {
                MessageBox.Show("Please configure the mame.exe path in settings first.", "Missing Mame Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            MessageBoxResult result = MessageBox.Show("This will create a new 'arcadenames.ini' file which is used for region and language metadata during arcade game scraping.\n\nDo you want to continue?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
            {
                return;
            }


            CreateArcadeGamesINI(mamePath);

        }

        private async void CreateArcadeGamesINI(string mamePath)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var mameNames = await MameHelper.GetMameNames(mamePath);

            if (mameNames == null || mameNames.Count == 0)
            {
                MessageBox.Show("No data was returned, the file has not been created!", "Data Missing!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Mouse.OverrideCursor = null;
                return;
            }

            string filePath = "./ini/arcadenames.ini";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false)) // false to overwrite
                {
                    writer.WriteLine("[ArcadeNames]");

                    foreach (var kvp in mameNames)
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "File Creation Failed!", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
                return;
            }

            MessageBox.Show("The file 'arcadenames.ini' has been created", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            Mouse.OverrideCursor = null;
        }

        private void ToggleFilterModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (stackpanel_Genre.Visibility == Visibility.Visible)
            {
                stackpanel_Genre.Visibility = Visibility.Collapsed;
                stackpanel_Custom.Visibility = Visibility.Visible;
                comboBox_Genre.SelectedIndex = 0;
            }
            else
            {
                stackpanel_Genre.Visibility = Visibility.Visible;
                stackpanel_Custom.Visibility = Visibility.Collapsed;
                textBox_CustomFilter.Text = string.Empty;
                comboBox_CustomFilter.SelectedIndex = 0;
                button_ClearCustomFilter.Visibility = Visibility.Hidden;
                comboBox_Genre.SelectedIndex = 0;
                _customFilter = string.Empty;
                ApplyFilters([_visibilityFilter!]);
            }
        }

        private void radioButton_AllItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowAll();
        }

        private void radioButton_VisibleItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowVisible();
        }

        private void radioButton_HiddenItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowHidden();
        }

        private void button_ResetView_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
        }
    }
}
