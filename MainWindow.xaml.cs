using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using GamelistManager.controls;
using GamelistManager.pages;
using Microsoft.Win32;
using Renci.SshNet;
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
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private bool _autosizeColumns;
        private bool _isUpdatingSelection = false;
        private bool _confirmBulkChange;
        private DispatcherTimer _dataGridSelectionChangedTimer;
        private DataRowView _currentSelectedRow;
        private int _currentSelectedRowIndex;
        private MediaPage _mediaPage;
        private Scraper _scraper;
        private Dictionary<DataGridColumn, Style?> _originalCellStyles = [];
        private DatToolPage? _datToolPage;

        public MainWindow()
        {
            InitializeComponent();
            // This is for a short delay between datagrid selection changes
            // In case of fast scrolling
            _dataGridSelectionChangedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // Set the delay interval
            };

            _dataGridSelectionChangedTimer.Tick += DataGridSelectionChangedTimer_Tick;
            SharedData.ProgramDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            SharedData.InitializeChangeTracker();
            SharedData.ChangeTracker.UndoRedoStateChanged += ChangeTracker_UndoRedoStateChanged!;

            _autosizeColumns = true;
            _parentFolderPath = string.Empty;
            _visibilityFilter = string.Empty;
            _genreFilter = string.Empty;
            _customFilter = string.Empty;
            _currentSelectedRow = null!;
            _scraper = new Scraper(this);
            _mediaPage = new MediaPage();
        }


        private void ChangeTracker_UndoRedoStateChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCounters();

                if (SharedData.ChangeTracker?.IsTrackingEnabled == false)
                {
                    return;
                }

                UpdateChangeTrackerButtons();
            });
        }

        private void UpdateChangeTrackerButtons()
        {
            if (SharedData.ChangeTracker?.IsTrackingEnabled == false)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                UndoButton.IsEnabled = SharedData.ChangeTracker?.UndoCount > 1;
                RedoButton.IsEnabled = SharedData.ChangeTracker?.RedoCount > 0;
            });
        }


        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.ChangeTracker?.IsTrackingEnabled == false)
            {
                return;
            }

            SharedData.ChangeTracker?.Undo();
            UpdateCounters();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.ChangeTracker?.IsTrackingEnabled == false)
            {
                return;
            }
            SharedData.ChangeTracker?.Redo();
            UpdateCounters();
        }


        private async Task<DataSet?> LoadGamelistFromFileAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            try
            {
                textBlock_Filename.Text = $"Loading {fileName}...";
                textBlock_LastModifiedTime.Text = "";

                bool ignoreDuplicates = Properties.Settings.Default.IgnoreDuplicates;
                var data = await Task.Run(() => GamelistLoader.LoadGamelist(fileName, ignoreDuplicates));

                if (data == null)
                {
                    textBlock_Filename.Text = "Failed to load gamelist.";
                    return null;
                }

                textBlock_Filename.Text = $"Loaded {System.IO.Path.GetFileName(fileName)}";
                return data;
            }
            catch (Exception ex)
            {
                textBlock_Filename.Text = $"Error loading gamelist: {ex.Message}";
                return null;
            }
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
                        CellTemplate = CheckBoxTemplateHelper.CreateCheckbox(column.ColumnName),
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
            string imagePath = $"pack://application:,,,/resources/images/systems/{imageName}";

            try
            {
                PlatformLogo.Source = new BitmapImage(new Uri(imagePath));
            }
            catch
            {
                PlatformLogo.Source = new BitmapImage(new Uri("pack://application:,,,/resources/images/logos/gamelistmanager.png"));
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
            ribbonMenu_Reload.IsEnabled = true;
            menuItem_Save.IsEnabled = true;
            ribbonMenu_Save.IsEnabled = true;
            menuItem_Restore.IsEnabled = true;
            ribbonMenu_Restore.IsEnabled = true;
            menuItem_Export.IsEnabled = true;
            ribbonMenu_ExportToCSV .IsEnabled = true;
            menuItem_Columns.IsEnabled = true;
            ribbonTab_Columns.IsEnabled = true;
            menuItem_Edit.IsEnabled = true;
            ribbonTab_Edit.IsEnabled = true;
            menuItem_Tools.IsEnabled = true;
            ribbonTab_Tools.IsEnabled = true;
            menuItem_View.IsEnabled = true;
            button_Media.IsEnabled = true;
            button_Scraper.IsEnabled = true;
            ribbonTab_Home.IsEnabled = true;


            RedoButton.IsEnabled = false;
            UndoButton.IsEnabled = false;

            UpdateFilterComboboxes();
            bool editMode = false;
            SetEditMode(editMode);
            toggleButton_EditData.IsChecked = editMode;
            toggleButton_EditData.Label = editMode ? "Edit Stop" : "Edit Data";
            toggleButton_EditData.IsChecked = false;
            menuItem_EditData.Header = editMode ? "Stop Editing" : "Edit Data";

            if (_scraper != null)
            {
                _scraper.button_Start.IsEnabled = true;
                _scraper.button_ClearCache.IsEnabled = false;
            }

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
                    if (column.Binding is System.Windows.Data.Binding binding && binding.Path != null)
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

            string previousTextComboboxColumns = comboBox_FindAndReplaceColumns.Text;
            string previousTextComboboxColumns1 = comboBox_FindColumns.Text;
            comboBox_FindAndReplaceColumns.Items.Clear();
            comboBox_FindColumns.Items.Clear();

            string previousTextCustomFilter = comboBox_CustomFilter.Text;
            comboBox_CustomFilter.Items.Clear();
            comboBox_CustomFilter.Items.Add("Rom Path"); // Added only for this combobox

            foreach (var item in visibleColumns)
            {
                comboBox_FindAndReplaceColumns.Items.Add(item.ColumnName);
                comboBox_FindColumns.Items.Add(item.ColumnName);
                comboBox_CustomFilter.Items.Add(item.ColumnName);
            }
            comboBox_FindAndReplaceColumns.Items.Add("Description");
            comboBox_FindColumns.Items.Add("Description");
            comboBox_CustomFilter.Items.Add("Description");

            if (comboBox_FindAndReplaceColumns.Items.Contains(previousTextComboboxColumns))
            {
                comboBox_FindAndReplaceColumns.Text = previousTextComboboxColumns;
            }
            else
            {
                comboBox_FindAndReplaceColumns.SelectedIndex = 0;
            }

            if (comboBox_FindColumns.Items.Contains(previousTextComboboxColumns1))
            {
                comboBox_FindColumns.Text = previousTextComboboxColumns1;
            }
            else
            {
                comboBox_FindColumns.SelectedIndex = 0;
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
                BackgroundImage.Source = new BitmapImage(new Uri($"pack://application:,,,/resources/images/systems/{imageName}"));
            }
            catch
            {
                imageName = "gamelistmanager.png";
                BackgroundImage.Source = new BitmapImage(new Uri($"pack://application:,,,/resources/images/logos/{imageName}"));

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
            List<string> visibleColumns = [];
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
            column_Description.Width = new GridLength(0);
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
                        column_Description.Width = new GridLength(200);
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

            SyncColumnCheckboxes(true); // menu to ribbon

        }


        private async void ReloadFile_Click(object sender, RoutedEventArgs e)
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

            await OpenFileAsync(SharedData.XMLFilename);
        }


        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveGamelist();
        }


        private void SaveGamelist()
        {

            string currentSystem =  char.ToUpper(SharedData.CurrentSystem[0]) + SharedData.CurrentSystem.Substring(1); 
            var result = MessageBox.Show($"Do you want to save the '{currentSystem}' gamelist?", "Save Reminder", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            SharedData.ChangeTracker?.PauseTracking();

            GamelistSaver.BackupGamelist(SharedData.CurrentSystem, SharedData.XMLFilename);
            Mouse.OverrideCursor = Cursors.Wait;
            bool saveResult = GamelistSaver.SaveGamelist(SharedData.XMLFilename);
            Mouse.OverrideCursor = null;

            SharedData.ChangeTracker?.ResumeTracking();

            if (saveResult)
            {
                MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                SharedData.IsDataChanged = false;
                SaveLastOpenedGamelistName(SharedData.XMLFilename);
            }
            else
            {
                MessageBox.Show("The file was not saved.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            DirectoryInfo dirInfo = new(selectedFolder);

            // Validate parent folder is "roms"
            if (dirInfo.Parent == null || !dirInfo.Parent.Name.Equals("roms", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Invalid folder selected.\n\nPlease select a file inside a folder within 'roms' (e.g., roms/systemname).",
                    "Invalid ROM Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Validate folder is not empty
            if (!Directory.EnumerateFiles(selectedFolder).Any())
            {
                MessageBox.Show(
                    "The selected folder does not contain any files.\n\nPlease select a non-empty system ROM folder.",
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

            SetupMainDataGrid();

            if (SharedData.CurrentSystem == "mame")
            {
                GenerateMameNames();
            }

            textBlock_Filename.Text = $"File: {fileName}";
            textBlock_LastModifiedTime.Text = "Modified Time: N/A";

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
                $"Gamelist creation complete.\n\nRemember to save the file.",
                "Gamelist Created",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static async void GenerateMameNames()
        {
            string mameFilePath = Properties.Settings.Default.MamePath;

            if (string.IsNullOrWhiteSpace(mameFilePath) || !File.Exists(mameFilePath))
            {
                return;
            }

            if (MameNamesHelper.Names.Count > 0)
            {
                return;
            }

            try
            {
                await MameNamesHelper.GenerateAsync(mameFilePath);
            }
            catch
            {
                // Log or show an error
            }
        }


        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            
            string fileName = SelectXMLFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
                    
            await OpenFileAsync(fileName);
        }

        private async Task OpenFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            bool saveReminder = Properties.Settings.Default.SaveReminder;

            if (SharedData.IsDataChanged && saveReminder)
                SaveGamelist();

            if (!Path.Exists(fileName))
            {
                MessageBox.Show("File not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_scraper != null)
                LogHelper.Instance.ClearLog();
                     
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

            SaveLastOpenedGamelistName(fileName);

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo > 0)
            {
                SharedData.ChangeTracker?.StopTracking();
                UpdateChangeTrackerButtons();
            }

            SharedData.DataSet = data;
            SharedData.XMLFilename = fileName;

            _parentFolderPath = Path.GetDirectoryName(fileName)!;
            SharedData.CurrentSystem = new DirectoryInfo(_parentFolderPath).Name;

            if (SharedData.CurrentSystem == "mame")
                GenerateMameNames();

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

            if (ribbonApplicationMenu.IsDropDownOpen == true)
            {
                ribbonApplicationMenu.IsDropDownOpen = false;
            }

            // Reset dattool page if showing
            if (_datToolPage != null && MainContentFrame.Content == _datToolPage)
            {
                _datToolPage.ResetDatToolPage();
            }

        }


        private async void RecentFilemenuItem_Click(object sender, RoutedEventArgs e)
        {            
            if (sender is MenuItem menuItem && menuItem.Tag is string fileName && !string.IsNullOrEmpty(fileName))
            {
                await OpenFileAsync(fileName);
            }

        }

        private static string SelectXMLFile()
        {
            OpenFileDialog openFileDialog = new()
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
            lastFileName = lastFileName.Trim();
            if (string.IsNullOrEmpty(lastFileName))
            {
                return;
            }

            string recentFiles = Properties.Settings.Default.RecentFiles;
            int maxFiles = Properties.Settings.Default.RecentFilesCount;

            // Deserialize from JSON
            List<string> recentFilesList;
            if (!string.IsNullOrEmpty(recentFiles))
            {
                try
                {
                    recentFilesList = JsonSerializer.Deserialize<List<string>>(recentFiles) ?? [];
                }
                catch
                {
                    recentFilesList = [];
                }
            }
            else
            {
                recentFilesList = [];
            }

            // Remove the existing one if it exists
            recentFilesList.RemoveAll(f => string.Equals(f, lastFileName, StringComparison.OrdinalIgnoreCase));

            // Insert at the top
            recentFilesList.Insert(0, lastFileName);

            // Trim the list
            if (recentFilesList.Count > maxFiles)
                recentFilesList = recentFilesList.Take(maxFiles).ToList();

            // Serialize to JSON and save
            Properties.Settings.Default.RecentFiles = JsonSerializer.Serialize(recentFilesList);
            Properties.Settings.Default.Save();
            AddRecentFilesToMenu();
        }


        private void AddRecentFilesToMenu()
        {
            // Clear existing items first (Classic Menu)
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
                // Clear ribbon recent files if no files
                if (RecentFilesRibbon != null)
                {
                    RecentFilesRibbon.ItemsSource = null;
                }
                return;
            }

            // Deserialize from JSON
            List<string> recentFilesList;
            try
            {
                recentFilesList = JsonSerializer.Deserialize<List<string>>(recentFiles);
                if (recentFilesList == null || recentFilesList.Count == 0)
                {
                    if (RecentFilesRibbon != null)
                    {
                        RecentFilesRibbon.ItemsSource = null;
                    }
                    return;
                }
            }
            catch
            {
                // Invalid JSON, skip
                if (RecentFilesRibbon != null)
                {
                    RecentFilesRibbon.ItemsSource = null;
                }
                return;
            }

            // Populate Classic Menu
            foreach (var file in recentFilesList)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

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

            // Populate Ribbon Recent Files
            // This will bind to the ListBox in the Ribbon's AuxiliaryPaneContent
            if (RecentFilesRibbon != null)
            {
                RecentFilesRibbon.ItemsSource = recentFilesList;
            }
        }

        private async void RecentFilesRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string fileName)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    await OpenFileAsync(fileName);
                    // Clear selection so the same file can be selected again
                    listBox.SelectedIndex = -1;
                }
            }
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            string filePath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
            string fileVersion = fileVersionInfo.FileVersion!;

            this.Title = $"Gamelist Manager {fileVersion}";

            if (Properties.Settings.Default.Version != fileVersion)
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Version = fileVersion;
                Properties.Settings.Default.Save();

                MessageBox.Show("Settings have been reset.", "Settings Reset", MessageBoxButton.OK, MessageBoxImage.Information);

            }

            statusBar_FileInfo.Visibility = Properties.Settings.Default.ShowFileStatusBar ? Visibility.Visible : Visibility.Collapsed;

            string gridLineVisibility = Properties.Settings.Default.GridLineVisibility;
            if (Enum.TryParse(gridLineVisibility, out DataGridGridLinesVisibility visibility))
            {
                MainDataGrid.GridLinesVisibility = visibility;
            }

            // Apply saved grid size
            int size = Properties.Settings.Default.GridFontSize;
            RibbonFontSizeSlider.Value = size;
            ClassicFontSizeSlider.Value = size;
            RibbonFontSizeValue.Text = size.ToString();
            ClassicFontSizeValue.Text = size.ToString();

            // Update the font size for DataGrid
            MainDataGrid.FontSize = size;

            // Update font size for DataGrid headers and cells
            UpdateDataGridFontSize(MainDataGrid, size);

            // Adjust column widths
            AdjustDataGridColumnWidths(MainDataGrid);

            // Default states
            stackpanel_Filters.IsEnabled = false;

            // Populate recent files, if any
            AddRecentFilesToMenu();

            // Start with bottom
            MainGrid.RowDefinitions[4].Height = new GridLength(0);

            // Custom filter, not enabled at this time
            comboBox_CustomFilter.SelectedIndex = 0;

            string colorName = Properties.Settings.Default.AlternatingRowColor;
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            MainDataGrid.AlternatingRowBackground = new SolidColorBrush(color);

            button_ClearGenreSelection.Visibility = Visibility.Hidden;
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            textBox_CustomFilter.Text = string.Empty;

            _confirmBulkChange = Properties.Settings.Default.ConfirmBulkChange;

            bool useRibbonMenu = Properties.Settings.Default.UseRibbonMenu;
            bool autoHideRibbon = Properties.Settings.Default.AutoHideRibbon;
            ribbon_AutoHide.IsChecked = autoHideRibbon;

            bool alwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
            ribbon_AlwaysOnTop.IsChecked = alwaysOnTop;
            menuItem_AlwaysOnTop.IsChecked = alwaysOnTop;
            this.Topmost = alwaysOnTop;

            SetMenuStyle(useRibbonMenu);

        }

        private void ribbon_AutoHide_Click(object sender, RoutedEventArgs e)
        {
            bool autoHideRibbon = ribbon_AutoHide.IsChecked == true;
            SetRibbonAutoHide(autoHideRibbon);
        }

        private void SetRibbonAutoHide(bool autoHideRibbon)
        {
            if (autoHideRibbon)
            {
                RibbonHotZone.MouseEnter += RibbonHotZone_MouseEnter;
                RibbonMenu.MouseLeave += RibbonMenu_MouseLeave; // Also hook leave event
            }
            else
            {
                RibbonHotZone.MouseEnter -= RibbonHotZone_MouseEnter;
                RibbonMenu.MouseLeave -= RibbonMenu_MouseLeave;
            }

            Properties.Settings.Default.AutoHideRibbon = autoHideRibbon;
            Properties.Settings.Default.Save();
        }

        private void RibbonHotZone_MouseEnter(object sender, MouseEventArgs e)
        {
            bool useRibbonMenu = Properties.Settings.Default.UseRibbonMenu;
            if (RibbonMenu.Visibility is Visibility.Collapsed && useRibbonMenu)
            {
                ShowRibbonWithAnimation();
            }
        }

        private void RibbonMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            // Only hide if mouse is truly outside the ribbon
            if (!RibbonMenu.IsMouseOver && !string.IsNullOrEmpty(SharedData.XMLFilename))
            {
                HideRibbonWithAnimation();
            }
        }

        private void ShowRibbonWithAnimation()
        {
            RibbonMenu.Visibility = Visibility.Visible;

            // Slide down animation
            var slideDown = new DoubleAnimation
            {
                From = -RibbonMenu.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            var transform = new TranslateTransform();
            RibbonMenu.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.YProperty, slideDown);
            RibbonMenu.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void HideRibbonWithAnimation()
        {
            // Slide up animation
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -RibbonMenu.ActualHeight,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            slideUp.Completed += (s, e) =>
            {
                RibbonMenu.Visibility = Visibility.Collapsed;
                RibbonMenu.RenderTransform = null;
            };

            var transform = RibbonMenu.RenderTransform as TranslateTransform ?? new TranslateTransform();
            RibbonMenu.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
            RibbonMenu.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {        
            if (MainDataGrid.ItemsSource == null)
            {
                return;
            }

            var slider = sender as Slider;

            if (slider == null)
            {
                return;
            }

            int size = ((int)slider.Value);
            Properties.Settings.Default.GridFontSize = size;
            Properties.Settings.Default.Save();

            ClassicFontSizeValue.Text = size.ToString("0");
            RibbonFontSizeValue.Text = size.ToString("0");

            // Update the font size for DataGrid
            MainDataGrid.FontSize = size;
          
            // Update font size for DataGrid headers and cells
            UpdateDataGridFontSize(MainDataGrid, size);

            // Adjust column widths
            AdjustDataGridColumnWidths(MainDataGrid);

       }

        private static void UpdateDataGridFontSize(DataGrid dg, double fontSize)
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

            DataGridLength size = new(1, DataGridLengthUnitType.SizeToCells);
            if (_autosizeColumns)
            {
                size = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            foreach (var column in dg.Columns)
            {
                double headerWidth = MeasureColumnHeaderWidth(column);

                if (column is DataGridTemplateColumn)
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

        private static double MeasureColumnHeaderWidth(DataGridColumn column)
        {
            string header = column.Header.ToString()!;
            var textBlock = new TextBlock { Text = header };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }


        private static FrameworkElement GetCellContent(DataGridColumn column, object item)
        {
            if (column.GetCellContent(item) is FrameworkElement cellContent)
            {
                return cellContent;
            }

            return null!;
        }


        private void MenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            // Try reading the Tag from either control type
            string? columnName = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrEmpty(columnName))
                return;

            bool isChecked;

            if (sender is MenuItem menuItem)
            {
                isChecked = menuItem.IsChecked == true;
                SyncColumnCheckboxes(true);   // menu → ribbon
            }
            else if (sender is System.Windows.Controls.Ribbon.RibbonCheckBox ribbonCheckBox)
            {
                isChecked = ribbonCheckBox.IsChecked == true;
                SyncColumnCheckboxes(false);  // ribbon → menu
            }
            else
            {
                return; // unexpected sender
            }

            SetColumnVisibility(columnName, isChecked);
        }


        private void SetColumnVisibility(string columnName, bool isChecked)
        {
            if (columnName == "Description")
            {
                if (!isChecked)
                {
                    gridSplitter_Vertical.Visibility = Visibility.Collapsed;
                    column_Description.Width = new GridLength(0);
                    menuItem_Description.IsChecked = false;
                }
                else
                {
                    gridSplitter_Vertical.Visibility = Visibility.Visible;
                    column_Description.Width = new GridLength(2, GridUnitType.Star);
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

            foreach (DataGridColumn column in MainDataGrid.Columns)
            {
                if (mediaItems.Contains(column.Header.ToString()!))
                {
                    column.Visibility = (showPaths) ? Visibility.Visible : Visibility.Collapsed;
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
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

            Dictionary<string, bool> keyValuePairs = [];

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


        private static void ApplyFilters(string[] filters)
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
            toggleButton_ShowAll.IsChecked = true;
            menuItem_ShowHidden.IsChecked = false;
            toggleButton_HiddenOnly.IsChecked = false;
            menuItem_ShowVisible.IsChecked = false;
            toggleButton_VisibleOnly.IsChecked = false;

            _visibilityFilter = string.Empty;
            ApplyFilters([_visibilityFilter, _genreFilter, _customFilter]);
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
            toggleButton_ShowAll.IsChecked= false;
            menuItem_ShowHidden.IsChecked = false;
            toggleButton_HiddenOnly.IsChecked = false;
            menuItem_ShowVisible.IsChecked = true;
            toggleButton_VisibleOnly.IsChecked = true;

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
            toggleButton_ShowAll.IsChecked = false;
            menuItem_ShowHidden.IsChecked = true;
            toggleButton_HiddenOnly.IsChecked = true;
            menuItem_ShowVisible.IsChecked = false;
            toggleButton_VisibleOnly.IsChecked = false;

            _visibilityFilter = "(hidden = true)";
            ApplyFilters([_visibilityFilter, _genreFilter, _customFilter]);
            UpdateCounters();

        }

        private void ShowAllGenres_Click(object sender, RoutedEventArgs e)
        {
            menuItem_ShowAllGenre.IsChecked = true;
            toggleButton_AllGenres.IsChecked = true;
            menuItem_ShowOneGenre.IsChecked = false;
            toggleButton_SelectedGenre.IsChecked = false;
            comboBox_Genre.SelectedIndex = 0;
        }

        private void ShowGenreOnly_Click(object sender, RoutedEventArgs e)
        {            
            menuItem_ShowAllGenre.IsChecked = false;
            toggleButton_AllGenres.IsChecked = false;
            menuItem_ShowOneGenre.IsChecked = true;
            toggleButton_SelectedGenre.IsChecked = true;

            if (MainDataGrid.SelectedItems.Count == 0 && MainDataGrid.Items.Count > 0)
            {
                // Default show empty genre because there's no selected datagrid row
                comboBox_Genre.SelectedIndex = 1;
                return;
            }


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
            bool alwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
            this.Topmost = !alwaysOnTop;
            menuItem_AlwaysOnTop.IsChecked = !alwaysOnTop;
            ribbon_AlwaysOnTop.IsChecked = !alwaysOnTop;
            Properties.Settings.Default.AlwaysOnTop = !alwaysOnTop;
            Properties.Settings.Default.Save();
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            // ResetView selected from menu will also reset saved columns
            Properties.Settings.Default.VisibleGridColumns = Properties.Settings.Default.DefaultColumns;
            Properties.Settings.Default.Save();
            ResetView();
        }

        private void ResetAllFilters()
        {
            // Reset checkmarks
            menuItem_ShowOneGenre.IsChecked = false;
            toggleButton_SelectedGenre.IsChecked = false;
            
            menuItem_ShowAllGenre.IsChecked = true;
            toggleButton_AllGenres.IsChecked = true;
            
            menuItem_ShowAll.IsChecked = true;
            toggleButton_ShowAll.IsChecked = true;
            
            menuItem_ShowHidden.IsChecked = false;
            toggleButton_HiddenOnly.IsChecked = false;
            
            menuItem_ShowVisible.IsChecked = false;
            toggleButton_VisibleOnly.IsChecked = false;
            
            menuItem_MediaPaths.IsChecked = false;

            // Clear filters
            _genreFilter = string.Empty;
            _visibilityFilter = string.Empty;
            _customFilter = string.Empty;

            // Reset custom filter stuff
            comboBox_Genre.SelectedIndex = 0;
            comboBox_CustomFilter.SelectedIndex = 0;
            comboBox_FilterMode.SelectedIndex = 0;
            textBox_CustomFilter.Text = "";
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            button_ClearCustomFilter.IsEnabled = false;
            //stackpanel_Custom.Visibility = Visibility.Collapsed;
            //stackpanel_Genre.Visibility = Visibility.Visible;

            radioButton_AllItems.IsChecked = true;

            SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;

        }

        private void ResetView()
        {
            ResetAllFilters();

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
                                
            // Reset column view
            SetDefaultColumnVisibility(MainDataGrid);

            // Adjust Column Widths
            AdjustDataGridColumnWidths(MainDataGrid);

            UpdateCounters();

        }


        private async void MenuItem_MapDrive_Click(object sender, RoutedEventArgs e)
        {

            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The Batocera hostname is not set.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The Batocera credentials are missing.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string networkShareToCheck = $"\\\\{hostName}\\share";

            bool isMapped = DriveMappingHelper.IsShareMapped(networkShareToCheck);

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
            string output = await CommandHelper.ExecuteCommandAsync(exePath, command);

            if (output != null && output != string.Empty)
            {
                MessageBox.Show(output, "Map Network Drive", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuItem_OpenTerminal_Click(object sender, RoutedEventArgs e)
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);

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
                MessageBox.Show("Could not start OpenSSH.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_GetVersion_Click(object sender, RoutedEventArgs e)
        {
            string command = "batocera-es-swissknife --version";
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"Your Batocera is version {output}.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void MenuItem_ShowUpdates_Click(object sender, RoutedEventArgs e)
        {
            string command = "batocera-es-swissknife --update";
            string output = ExecuteSshCommand(command) as string;

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"{output}", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void MenuItem_StopEmulators_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to stop any running emulators?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot";
            string output = ExecuteSshCommand(command) as string;

            MessageBox.Show("Running emulators should now be stopped.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_StopEmulationstation_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Are you sure you want to stop EmulationStation?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid";
            string output = ExecuteSshCommand(command) as string;

            if (!string.IsNullOrEmpty(output) && output.Contains('0'))
            {
                MessageBox.Show("EmulationStation is stopped.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Debug.WriteLine(output);
                MessageBox.Show("An unknown error has occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private void MenuItem_RebootHost_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("Are you sure you want to reboot your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot";
            string output = ExecuteSshCommand(command) as string;

            MessageBox.Show("A reboot command has been sent to the host.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_ShutdownHost_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to shutdown your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;sleep 3;shutdown -h now";
            string output = ExecuteSshCommand(command) as string;

            MessageBox.Show("A shutdown command has been sent to the host.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }


        private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDataGrid.Items.Count == 0)
                return;

            _isUpdatingSelection = true;

            // Always restart the timer to handle null-selection edge case
            _dataGridSelectionChangedTimer.Stop();
            _dataGridSelectionChangedTimer.Start();

            // Update current row/index only if a valid row is selected
            var selectedRow = MainDataGrid.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                _currentSelectedRow = selectedRow;
                _currentSelectedRowIndex = MainDataGrid.SelectedIndex;
            }

            _isUpdatingSelection = false;
        }

        private void DataGridSelectionChangedTimer_Tick(object? sender, EventArgs e)
        {
            if (_isUpdatingSelection) return;
            _dataGridSelectionChangedTimer.Stop();

            if (MainDataGrid.Items.Count == 0)
            {
                textBox_Description.Text = string.Empty;
                return;
            }

            // Filter out any selected rows that are no longer visible
            var visibleSelectedRows = MainDataGrid.SelectedItems
                .OfType<DataRowView>()
                .Where(r => MainDataGrid.Items.Contains(r))
                .ToList();

            // If nothing is left selected, pick the next available row
            if (!visibleSelectedRows.Any())
            {
                int nextIndex = _currentSelectedRowIndex;
                if (nextIndex >= MainDataGrid.Items.Count)
                    nextIndex = MainDataGrid.Items.Count - 1;

                var nextRow = MainDataGrid.Items[nextIndex] as DataRowView
                    ?? MainDataGrid.Items.OfType<DataRowView>().FirstOrDefault();

                if (nextRow != null)
                {
                    _currentSelectedRow = nextRow;
                    _currentSelectedRowIndex = nextIndex;
                    MainDataGrid.SelectedItem = nextRow;
                    MainDataGrid.ScrollIntoView(nextRow);
                }
            }
            else
            {
                // Keep only the visible rows selected
                MainDataGrid.SelectedItems.Clear();
                foreach (var row in visibleSelectedRows)
                    MainDataGrid.SelectedItems.Add(row);

                // Update the “current” row to the first visible selected row
                _currentSelectedRow = visibleSelectedRows.First();
                _currentSelectedRowIndex = MainDataGrid.Items.IndexOf(_currentSelectedRow);
            }

            // Update description and media for the current row
            if (_currentSelectedRow != null)
            {
                textBox_Description.Text =
                    _currentSelectedRow["Description"] is DBNull
                        ? string.Empty
                        : _currentSelectedRow["Description"]?.ToString() ?? string.Empty;

                if (_mediaPage != null && button_Media.Content?.ToString() == "Hide Media")
                {
                    _mediaPage.ShowMedia(_currentSelectedRow);
                }
            }
        }


        private void Export_Click(object sender, RoutedEventArgs e)
        {
            string csvFileName = Directory.GetCurrentDirectory() + "\\" + $"{SharedData.CurrentSystem}_export.csv";
            ExportCsv(SharedData.DataSet.Tables[0], csvFileName);
        }

        public static void ExportCsv(DataTable dataTable, string csvFileName)
        {
            try
            {
                using var csvContent = new StreamWriter(csvFileName);

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
                            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
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


                MessageBox.Show($"The file '{csvFileName}' was successfully saved.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

                string currentDirectory = System.IO.Directory.GetCurrentDirectory();

                // Open File Explorer to the current directory
                Process.Start(new ProcessStartInfo()
                {
                    FileName = currentDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });

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

        private static string EscapeCsvField(string field)
        {
            // If the field contains a comma, double-quote, or newline, enclose it in double-quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\r') || field.Contains('\n'))
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
                MessageBox.Show("The batocera hostname is not configured.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            string output = string.Empty;
            using var client = new SshClient(hostName, userName, userPassword);
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

        private void MenuItem_View_SubmenuOpened(object sender, RoutedEventArgs e)
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

        private void ComboBox_Genre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null)
            {
                // Should never be empty, but always check anyhow
                return;
            }


            string selectedText;

            int selectedIndex = comboBox.SelectedIndex;

            if (selectedIndex > 1)
            {
                selectedText = comboBox.Items[selectedIndex].ToString()!;
                selectedText = FilterHelper.EscapeSpecialCharacters(selectedText);
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

        private void TextBox_CustomFilter_KeyUp(object sender, KeyEventArgs e)
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

        private static string GetComboBoxDisplayText(ComboBox comboBox)
        {
            // If something is selected
            if (comboBox.SelectedItem != null)
            {
                // If it's a ComboBoxItem (manually added in XAML)
                if (comboBox.SelectedItem is ComboBoxItem cbi)
                {
                    return cbi.Content.ToString()!;
                }
                // If it's a bound object or plain string
                {
                    return comboBox.SelectedItem.ToString()!;
                }
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

        private static string MakeFilter(string filterItem, string filterText, string selectedMode)
        {
            string filterExpression = string.Empty;

            if (filterItem.Contains(' '))
            {
                filterItem = $"[{filterItem}]";
            }

            switch (selectedMode)
            {
                case "Is":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = FilterHelper.EscapeSpecialCharacters(filterText);
                        filterExpression = $"{filterItem} = '{filterText}'";
                    }
                    break;

                case "Is Like":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        string likePattern = filterText.Replace("*", "%");
                        likePattern = FilterHelper.EscapeSpecialCharacters(likePattern);
                        likePattern = $"%{likePattern}%";
                        filterExpression = $"{filterItem} LIKE '{likePattern}'";
                    }
                    break;

                case "Is Not Like":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        string likePattern = filterText.Replace("*", "%");
                        likePattern = FilterHelper.EscapeSpecialCharacters(likePattern);
                        likePattern = $"%{likePattern}%";
                        filterExpression = $"{filterItem} NOT LIKE '{likePattern}'";
                    }
                    break;

                case "Starts With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = FilterHelper.EscapeSpecialCharacters(filterText);
                        filterText = $"{filterText}%";
                        filterExpression = $"{filterItem} LIKE '{filterText}'";
                    }
                    break;

                case "Ends With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = FilterHelper.EscapeSpecialCharacters(filterText);
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


        private void MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            int originalMaxUndo = Properties.Settings.Default.MaxUndo;

            SettingsDialogWindow settingsDialog = new(this);
            settingsDialog.ShowDialog();

            // Reload settings
            _confirmBulkChange = Properties.Settings.Default.ConfirmBulkChange;

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo == 0)
            {
                SharedData.ChangeTracker?.StopTracking();
                if (SharedData.ChangeTracker != null)
                {
                    SharedData.ChangeTracker.UndoRedoStateChanged -= ChangeTracker_UndoRedoStateChanged!;
                }

                RedoButton.IsEnabled = false;
                UndoButton.IsEnabled = false;
                return;
            }

            if (maxUndo != originalMaxUndo && SharedData.DataSet.Tables.Count > 0)
            {
                SharedData.ChangeTracker?.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                // SharedData.ChangeTracker?.UndoRedoStateChanged += ChangeTracker_UndoRedoStateChanged!;
            }

        }

        private void MenuItem_Edit_SubmenuOpened(object sender, RoutedEventArgs e)
        {
                                  
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
            menuItem_DeleteSelectedItems.Header = (selectedItemCount < 2) ? "Delete Item" : "Delete Selected Items";
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

        private void MenuItem_SetSelectedVisible_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedVisibility(false);
        }

        private void MenuItem_SetSelectedHidden_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedVisibility(true);
        }

        private void SetSelectedVisibility(bool visible)
        {
            int count = MainDataGrid.SelectedItems.Count;
            if (count < 1)
            {
                return;
            }

            string item = (count > 1) ? "items" : "item";
            string visibility = visible ? "hidden" : "visible";

            if (_confirmBulkChange && count > 1)
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

            SharedData.ChangeTracker?.StartBulkOperation();
            foreach (var row in matchingRows)
            {
                int rowIndex = SharedData.DataSet.Tables[0].Rows.IndexOf(row);
                row["hidden"] = visible;
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();



            UpdateCounters();
            SharedData.IsDataChanged = true;
        }

        private void MenuItem_SetAllGenreVisible_Click(object sender, RoutedEventArgs e)
        {
            string? selectedGenre = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Genre"]?.ToString())
                .FirstOrDefault();

            SetVisibilityByItemValue("Genre", selectedGenre!, false);

        }

        private void MenuItem_SetAllGenreHidden_Click(object sender, RoutedEventArgs e)
        {
            string? selectedGenre = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Genre"]?.ToString())
                .FirstOrDefault();

            SetVisibilityByItemValue("Genre", selectedGenre!, true);
        }

        private void SetVisibilityByItemValue(string colname, string colvalue, bool visible)
        {
            if (_confirmBulkChange)
            {
                string visibility = visible ? "hidden" : "visible";

                var result = MessageBox.Show($"Do you want to set all items with the {colname} Value of '{colvalue}' {visibility}?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

            SharedData.ChangeTracker?.StartBulkOperation();
            foreach (var row in rowsToUpdate)
            {
                int rowIndex = SharedData.DataSet.Tables[0].Rows.IndexOf(row);
                row["hidden"] = visible;
            }
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

            UpdateCounters();
            SharedData.IsDataChanged = true;

        }

        private void ribbonButton_DeleteItem_Click(object sender, RoutedEventArgs e)
        {

            if (!Properties.Settings.Default.EnableDelete)
            {
                MessageBox.Show("The delete function is currently disabled in Settings.", "Delete Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedIndex = MainDataGrid.SelectedIndex;

            var selectedItems = MainDataGrid.SelectedItems.Cast<DataRowView>().ToList();
            int itemCount = selectedItems.Count;
            
            var result = MessageBoxWithCheckbox.Show(
                owner: this,
                message: $"Are you sure you want to delete {itemCount} item{(itemCount > 1 ? "s" : "")} from the gamelist?",
                checkboxChecked: out bool deleteMedia,  // Must be 3rd parameter
                title: "Confirm Deletion",
                buttons: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Warning,
                checkboxText: "Also delete associated media files (images, videos, etc.)",
                checkboxDefaultChecked: false,
                infoText: "ROM files will be deleted from disk.",
                warningText: "⚠️ This operation cannot be undone."
            );
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            SharedData.ChangeTracker?.StartBulkOperation();
            DeleteSelectedItems(selectedItems, deleteMedia);
            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();
            SetSelectedRowAfterChange(selectedIndex);
            UpdateCounters();
            SharedData.IsDataChanged = true;

        }

        private void DeleteSelectedItems(List<DataRowView> selectedItems, bool deleteMedia)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }

            string? logPath = null;

            // Create logs folder and set up log path
            try
            {
                string logsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }
                logPath = Path.Combine(logsFolder, "deletions.log");
            }
            catch
            {
                // If we can't set up logging, continue without it
            }

            var mediaColumns = GamelistMetaData.GetMetaDataDictionary()
                .Where(entry => entry.Value.DataType == MetaDataType.Image ||
                    entry.Value.DataType == MetaDataType.Video ||
                    entry.Value.DataType == MetaDataType.Document ||
                    entry.Value.DataType == MetaDataType.Music)
                .Select(entry => entry.Value.Name)
                .ToList();

            var selectedPaths = selectedItems
                .Select(rowView => rowView.Row["Rom Path"]?.ToString())
                .ToList();

            var matchingRows = (from DataRow row in SharedData.DataSet.Tables[0].AsEnumerable()
                                let pathValue = row.Field<string>("Rom Path")
                                where selectedPaths.Contains(pathValue)
                                select row).ToList();

            if (matchingRows.Count == 0)
            {
                return;
            }

            // Delete rows in reverse order
            for (int i = matchingRows.Count - 1; i >= 0; i--)
            {
                var row = matchingRows[i];

                try
                {
                    string romPath = row["Rom Path"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(romPath))
                    {
                        string filename = PathHelper.ConvertGamelistPathToFullPath(romPath, _parentFolderPath);

                        // Delete the ROM file
                        try
                        {
                            if (File.Exists(filename))
                            {
                                File.Delete(filename);
                                if (!string.IsNullOrEmpty(logPath))
                                {
                                    try
                                    {
                                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        File.AppendAllText(logPath, $"[{timestamp}] Successfully deleted ROM: {filename}{Environment.NewLine}");
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(logPath))
                                {
                                    try
                                    {
                                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        File.AppendAllText(logPath, $"[{timestamp}] ROM file not found: {filename}{Environment.NewLine}");
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!string.IsNullOrEmpty(logPath))
                            {
                                try
                                {
                                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    File.AppendAllText(logPath, $"[{timestamp}] ERROR deleting ROM {filename}: {ex.Message}{Environment.NewLine}");
                                }
                                catch { }
                            }
                        }
                    }

                    // Delete associated media files if checkbox was checked
                    if (deleteMedia)
                    {
                        foreach (var column in mediaColumns)
                        {
                            try
                            {
                                if (row.Table.Columns.Contains(column) && row[column] != DBNull.Value)
                                {
                                    string? mediaPath = row[column]?.ToString();
                                    if (!string.IsNullOrEmpty(mediaPath))
                                    {
                                        string fullMediaPath = PathHelper.ConvertGamelistPathToFullPath(mediaPath, _parentFolderPath);
                                        if (File.Exists(fullMediaPath))
                                        {
                                            File.Delete(fullMediaPath);
                                            if (!string.IsNullOrEmpty(logPath))
                                            {
                                                try
                                                {
                                                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                                    File.AppendAllText(logPath, $"[{timestamp}] Successfully deleted media ({column}): {fullMediaPath}{Environment.NewLine}");
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!string.IsNullOrEmpty(logPath))
                                {
                                    try
                                    {
                                        string mediaPath = row[column]?.ToString() ?? "unknown";
                                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        File.AppendAllText(logPath, $"[{timestamp}] ERROR deleting media ({column}) {mediaPath}: {ex.Message}{Environment.NewLine}");
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    row.Delete();
                    if (!string.IsNullOrEmpty(logPath))
                    {
                        try
                        {
                            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            File.AppendAllText(logPath, $"[{timestamp}] Successfully deleted database row for: {romPath}{Environment.NewLine}");
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(logPath))
                    {
                        try
                        {
                            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            File.AppendAllText(logPath, $"[{timestamp}] ERROR processing row deletion: {ex.Message}{Environment.NewLine}");
                        }
                        catch { }
                    }
                }
            }

            _currentSelectedRow = null!;
        }

        private void MenuItem_RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (_confirmBulkChange)
            {
                var result = MessageBox.Show($"Do you want to remove the selected items from the gamelist?\n\nNo files are being deleted.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

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


            SharedData.ChangeTracker?.StartBulkOperation();
            // Delete rows in reverse order
            for (int i = matchingRows.Count - 1; i >= 0; i--)
            {
                var row = matchingRows[i];
                row.Delete();
                _currentSelectedRow = null!;
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

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

        private void MenuItem_EditData_Click(object sender, RoutedEventArgs e)
        {
            bool readOnly = MainDataGrid.Columns
            .OfType<DataGridTextColumn>()
            .FirstOrDefault(c => c.Header.ToString() == "Name")?.IsReadOnly ?? false;

            bool editMode = readOnly;
            SharedData.MetaDataEditMode = editMode;
            SetEditMode(editMode);
            toggleButton_EditData.IsChecked = editMode;
            toggleButton_EditData.Label = editMode ? "Edit Stop" : "Edit Data";
            menuItem_EditData.Header = editMode ? "Stop Editing" : "Edit Data";
            
            //if (button_Media.Content.ToString() == "Hide Media")
            //{
            //    if (MainDataGrid.SelectedItems.Count == 0 && MainDataGrid.Items.Count > 0)
            //    {
            //        MainDataGrid.SelectedIndex = 0; // Select the first row
            //    }

            //    // Get the first selected row
            //    DataRowView? selectedRow = MainDataGrid.SelectedItems
            //    .OfType<DataRowView>()
            //    .FirstOrDefault();

            //    _mediaPage?.ShowMedia(selectedRow!);
            //}

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
                textBox_Description.Foreground = Brushes.Blue;
                textBox_Description.IsReadOnly = false;
            }
            else
            {
                // Restore original color if it was stored
                var brush = (Brush)FindResource("PrimaryTextBrush");
                textBox_Description.Foreground = brush;
                textBox_Description.IsReadOnly = true;
            }
              
        }



        private void MenuItem_VideoJukebox_Click(object sender, RoutedEventArgs e)
        {
            PlayJukeBox("video");
        }

        private void TextBox_Description_LostFocus(object sender, RoutedEventArgs e)
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

            //DataRow[] rows = SharedData.DataSet.Tables[0].Select($"[Rom Path] = '{pathValue.Replace("'", "''")}'");
            DataRow[] rows = SharedData.DataSet.Tables[0].AsEnumerable()
                .Where(row => string.Equals(row.Field<string>("Rom Path"), pathValue, StringComparison.Ordinal))
                .ToArray();

            DataRow tabledata = rows[0];
            tabledata["Description"] = textboxValue;

        }

        private void MenuItem_ClearSelected_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItems.Count < 1)
            {
                MessageBox.Show("No item is selected!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ConfirmClearOperation("selected items"))
                return;

            var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
                .Select(rowView => rowView.Row["Rom Path"]?.ToString())
                .Where(path => !string.IsNullOrEmpty(path))
                .ToHashSet(StringComparer.Ordinal);

            var rowsToClear = SharedData.DataSet.Tables[0].AsEnumerable()
                .Where(row => selectedPaths.Contains(row.Field<string>("Rom Path")))
                .ToList();

            ClearRows(rowsToClear, useMameNames: SharedData.CurrentSystem == "mame");
        }

        private void MenuItem_ClearAllData_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmClearOperation("all data"))
            {
                return;
            }

            var rowsToClear = SharedData.DataSet.Tables[0].AsEnumerable().ToList();
            ClearRows(rowsToClear, useMameNames: SharedData.CurrentSystem == "mame");
        }

        private bool ConfirmClearOperation(string targetDescription)
        {
            if (_confirmBulkChange == false)
            {
                return true;
            }

            var result = MessageBox.Show(
                $"Do you want to clear {targetDescription}?\n\n" +
                "Rom Paths will remain unchanged.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private void ClearRows(List<DataRow> rows, bool useMameNames)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var table = SharedData.DataSet.Tables[0];

            // Cache column metadata once
            var boolColumns = table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName != "Rom Path" && c.DataType == typeof(bool))
                .ToArray();

            var nameColumn = table.Columns["Name"];
            var romPathColumn = table.Columns["Rom Path"];

            var otherColumns = table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName != "Rom Path" &&
                            c.ColumnName != "Name" &&
                            c.DataType != typeof(bool))
                .ToArray();

            SharedData.ChangeTracker?.StartBulkOperation();

            table.BeginLoadData();

            foreach (var row in rows)
            {
                // Set Name based on Rom Path
                if (nameColumn != null && romPathColumn != null)
                {
                    string romPath = row[romPathColumn]?.ToString() ?? string.Empty;
                    string newName = NameHelper.NormalizeRomName(romPath);

                    if (useMameNames && MameNamesHelper.Names.TryGetValue(newName, out string? value) && !string.IsNullOrEmpty(value))
                    {
                        newName = value;
                    }

                    row[nameColumn] = newName;
                }

                // Clear boolean columns
                foreach (var col in boolColumns)
                {
                    row[col] = false;
                }

                // Clear other columns
                foreach (var col in otherColumns)
                {
                    row[col] = DBNull.Value;
                }
            }

            SharedData.ChangeTracker?.EndBulkOperation();

            table.EndLoadData();

            if (MainDataGrid.Items.Count > 2)
            {
                int currentIndex = MainDataGrid.SelectedIndex;

                if (currentIndex > 0)
                {
                    MainDataGrid.SelectedIndex = 0;
                    MainDataGrid.SelectedIndex = currentIndex;
                }
                else
                {
                    MainDataGrid.SelectedIndex = 1;
                    MainDataGrid.SelectedIndex = 0;
                }
            }
        }


        private void MenuItem_ColumnAutoSize_Click(object sender, RoutedEventArgs e)
        {
            bool value = menuItem_ColumnAutoSize.IsChecked;
            menuItem_ColumnAutoSize.IsChecked = !value;
            _autosizeColumns = !value;
            AdjustDataGridColumnWidths(MainDataGrid);
            Properties.Settings.Default.AutoSizeColumns = !value;
        }

        private void Button_ClearGenreSelection_Click(object sender, RoutedEventArgs e)
        {
            comboBox_Genre.SelectedIndex = 0;
            button_ClearGenreSelection.Visibility = Visibility.Hidden;
            UpdateCounters();
        }

        private void Button_ClearCustomFilter_Click(object sender, RoutedEventArgs e)
        {
            textBox_CustomFilter.Text = "";
            button_ClearCustomFilter.Visibility = Visibility.Hidden;
            ApplyFilters(new string[] { _visibilityFilter, _genreFilter });
            UpdateCounters();
        }
        
        private void Button_ClearAllFilters_Click(object sender, RoutedEventArgs e)
        {
            ResetAllFilters();
            UpdateCounters();
        }

        private void MenuItem_MusicJukebox_Click(object sender, RoutedEventArgs e)
        {
            PlayJukeBox("music");
        }

        private void PlayJukeBox(string mediaType)
        {
            string jsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;

            string filePath = mediaType == "video"
            ? mediaPaths["video"]
            : mediaPaths["music"];

            string fullPath = PathHelper.ConvertGamelistPathToFullPath(filePath, _parentFolderPath!);

            if (!Directory.Exists(fullPath))
            {
                MessageBox.Show($"There is no {mediaType} folder for this platform.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] files = Directory.GetFiles(fullPath);

            if (files.Length == 0)
            {
                MessageBox.Show($"There are no {mediaType} files for this platform.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MediaPlayerWindow mediaPlayerWindow = new(files);
            
            mediaPlayerWindow.Show();
            
        }


        private async void MenuItem_Restore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_parentFolderPath))
            {
                return;
            }

            string backupFolder = $"{SharedData.ProgramDirectory}\\gamelist backup";

            if (!Path.Exists(backupFolder))
            {
                MessageBox.Show("A 'backups' folder does not exist yet.", "Backup Folder Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenFileDialog openFileDialog = new()
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
                MessageBox.Show("Please only restore from the 'backups' folder.", "Invalid Backup Location", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string fileName = Path.GetFileName(selectedPath);
            string directoryPath = Path.GetDirectoryName(selectedPath)!;
            string systemName = new DirectoryInfo(directoryPath).Name;

            if (systemName != SharedData.CurrentSystem)
            {
                MessageBox.Show($"Please only restore for the current system '{SharedData.CurrentSystem}'.", "Incorrect System Choice", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Restore completed.\n\nThe new gamelist will now be loaded.", "Restore Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            SharedData.IsDataChanged = false;
            await OpenFileAsync(SharedData.XMLFilename);
        }

        private void Button_Scraper_Click(object sender, RoutedEventArgs e)
        {

            if (_mediaPage != null && MainContentFrame.Content == _mediaPage)
            {
                _mediaPage.StopPlaying();
            }

            button_Media.Content = "Show Media";

            bool show = button_Scraper.Content?.ToString() == "Show Scraper" && button_Scraper.IsEnabled;

            button_Scraper.Content = show ? "Hide Scraper" : "Show Scraper";

            ShowScraperPage(show);

        }

        public void ShowScraperPage(bool show)
        {

            ShowMediaPage(false);

            if (_datToolPage != null && MainContentFrame.Content == _datToolPage)
            {
                _datToolPage.CloseDatToolPage();
            }

            MainContentFrame.Navigate(_scraper);

            if (show)
            {
                MainGrid.RowDefinitions[4].Height = new GridLength(220);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainGrid.RowDefinitions[4].Height = new GridLength(0);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Media_Click(object sender, RoutedEventArgs e)
        {
            button_Scraper.Content = "Show Scraper";

            bool show = button_Media.Content?.ToString() == "Show Media";
            button_Media.Content = show ? "Hide Media" : "Show Media";

            ShowMediaPage(show);
        }


        public void ShowMediaPage(bool show)
        {
            if (_mediaPage == null)
            {
                return;
            }

            if (_datToolPage != null && MainContentFrame.Content == _datToolPage)
            {
                _datToolPage.CloseDatToolPage();
            }

            MainContentFrame.Navigate(_mediaPage);

            if (show)
            {
                MainGrid.RowDefinitions[4].Height = new GridLength(235);
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
                MainGrid.RowDefinitions[4].Height = new GridLength(0);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
                if (_mediaPage != null)
                {
                    _mediaPage.ClearAllImages();
                    _mediaPage.StopPlaying();
                }
            }
        }

        private void MenuItem_FindNewItems_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("No additional items were found", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var statusColumn = MainDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Status");
                if (statusColumn != null)
                {
                    statusColumn.Visibility = Visibility.Visible;
             
                    if (!comboBox_CustomFilter.Items.Contains("Status"))
                        comboBox_CustomFilter.Items.Add("Status");
            }

                MessageBox.Show($"{totalNewItems} items were found and added to the gamelist", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
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
                fileList = SharedData.DataSet.Tables[0]
             .AsEnumerable()
             .Select(row => row.Field<string>("Rom Path"))
             .Where(path => !string.IsNullOrWhiteSpace(path))
             .Select(path => Path.GetFileName(path!))
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
                .Distinct(StringComparer.OrdinalIgnoreCase) // distinct by fullPath relative path
                .ToList();

            int totalNewItems = newFileList.Count;

            if (totalNewItems == 0)
            {
                return 0;
            }

            // Begin bulk operations
            SharedData.ChangeTracker?.StartBulkOperation();
            SharedData.DataSet.Tables[0].BeginLoadData();

            foreach (string fileName in newFileList)
            {
                string newName = Path.GetFileNameWithoutExtension(fileName);
                if (SharedData.CurrentSystem == "mame")
                {
                    newName = MameNamesHelper.Names.TryGetValue(newName, out string? value) ? value : newName;
                }

                var newRow = SharedData.DataSet.Tables[0].NewRow();
                newRow["Name"] = newName;
                newRow["Rom Path"] = $"./{fileName}";  // Store path with forward slashes
                newRow["Hidden"] = false;
                newRow["Favorite"] = false;
                newRow["Status"] = "New";

                SharedData.DataSet.Tables[0].Rows.Add(newRow);
            }

            // Finalize changes
            SharedData.DataSet.Tables[0].EndLoadData();
            SharedData.ChangeTracker?.EndBulkOperation();

            return totalNewItems;
        }


        private void MenuItem_AddMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaToolWindow mediaSearch = new();

            // Stop Change Tracker
            SharedData.ChangeTracker?.StopTracking();

            mediaSearch.ShowDialog();

            int maxUndo = Properties.Settings.Default.MaxUndo;

            if (maxUndo > 0)
            {
                // Re-initialize Change Tracker
                SharedData.ChangeTracker?.StartTracking(SharedData.DataSet.Tables[0], maxUndo);
                UpdateChangeTrackerButtons();
            }
        }

        private void MenuItem_FindMissingItems_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will quickly identify any missing items from this gamelist\n\nDo you want to continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

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
            MainDataGrid.IsEnabled = false;

            var usedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRowView row in MainDataGrid.Items.OfType<DataRowView>())
            {
                if (row["Rom Path"] is string romPath && !string.IsNullOrWhiteSpace(romPath))
                {
                    string cleaned = romPath.TrimStart('.', '\\', '/');
                    string dir = Path.GetDirectoryName(cleaned) ?? string.Empty;

                    string fullFolder = Path.GetFullPath(
                        Path.Combine(_parentFolderPath!, dir)
                    );

                    usedFolders.Add(fullFolder);
                }
            }

            usedFolders.Add(Path.GetFullPath(_parentFolderPath!));

            // 🔹 Track BOTH files and directories
            var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in usedFolders)
            {
                if (!Directory.Exists(folder))
                    continue;

                try
                {
                    foreach (var file in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly))
                        existingPaths.Add(Path.GetFullPath(file));

                    foreach (var dir in Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly))
                        existingPaths.Add(Path.GetFullPath(dir));
                }
                catch
                {
                    // Skip inaccessible folders
                }
            }

            var missingItems = new List<DataRowView>();

            try
            {
                foreach (DataRowView row in MainDataGrid.Items.OfType<DataRowView>())
                {
                    if (row["Rom Path"] is not string romPath || string.IsNullOrWhiteSpace(romPath))
                        continue;

                    string fullPath = Path.GetFullPath(
                        PathHelper.ConvertGamelistPathToFullPath(romPath, _parentFolderPath!)
                    );

                    if (!existingPaths.Contains(fullPath))
                    {
                        missingItems.Add(row);
                        missingCount++;
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
                MainDataGrid.IsEnabled = true;
            }

            if (missingItems.Count == 0)
            {
                MessageBox.Show(
                    "No missing items were detected",
                    "Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            var statusColumn = MainDataGrid.Columns
                .FirstOrDefault(c => c.Header?.ToString() == "Status");

            if (statusColumn != null)
            {
                statusColumn.Visibility = Visibility.Visible;
               
                if (!comboBox_CustomFilter.Items.Contains("Status"))
                    comboBox_CustomFilter.Items.Add("Status");

            }

            var result = MessageBox.Show(
                $"There are {missingItems.Count} missing items in this gamelist.\n\nDo you want to remove them?",
                "Notice:",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            SharedData.ChangeTracker.StartBulkOperation();

            if (result == MessageBoxResult.Yes)
            {
                foreach (var row in missingItems)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (MainDataGrid.ItemsSource is DataView dataView)
                            dataView.Table?.Rows.Remove(row.Row);
                    });
                }
            }
            else
            {
                SharedData.ChangeTracker.StopTracking();

                foreach (var row in missingItems)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        row["Status"] = "Missing";
                    });
                }

                SharedData.ChangeTracker.ResumeTracking();
            }

            SharedData.ChangeTracker.EndBulkOperation();
        }



        private void MenuItem_DatTools_Click(object sender, RoutedEventArgs e)
        {
            ShowDatTools(true);
        }

        private void ShowDatTools(bool show)
        {
            _datToolPage ??= new DatToolPage(this);

            ShowScraperPage(false);
            ShowMediaPage(false);
            button_Scraper.Content = "Show Scraper";
            button_Media.Content = "Show Media";

            if (show)
            {
                MainGrid.RowDefinitions[4].Height = new GridLength(210);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
                _datToolPage.ResetDatToolPage();
                MainContentFrame.Navigate(_datToolPage);
            }
            else
            {
                MainGrid.RowDefinitions[4].Height = new GridLength(0);
                gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
            }
        }


        private void Button_Items_Click(object sender, RoutedEventArgs e)
        {
            button_Items.Content = button_Items.Content.ToString() == "All Items" ? "Selected Items" : "All Items";
            if (!string.IsNullOrEmpty(textBox_ChangeFrom.Text))
            {
                button_Apply.IsEnabled = true;
            }
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e)
        {
            string from = textBox_ChangeFrom.Text;
            string to = textBox_ChangeTo.Text;
            string column = comboBox_FindAndReplaceColumns.Text;

            if (_confirmBulkChange)
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

            SharedData.ChangeTracker?.StartBulkOperation();

            foreach (DataRowView row in datagridRowSelection)
            {
                string currentValue = row[column].ToString() ?? string.Empty;

                string newValue;

                if (from.Contains('*') || from.Contains('?'))
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
            SharedData.ChangeTracker?.EndBulkOperation();

            button_Apply.IsEnabled = false;

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox)
            {
                return;
            }

            string from = textBox_ChangeFrom.Text;

            button_Apply.IsEnabled = !string.IsNullOrEmpty(from);

        }

        private void ComboBox_Columns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            button_Apply.IsEnabled = false;

            string from = textBox_ChangeFrom.Text;

            if (!string.IsNullOrEmpty(from))
            {
                button_Apply.IsEnabled = true;
            }
        }


        private void MenuItem_Find_Click(object sender, RoutedEventArgs e)
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

        private void Button_CloseFind_Click(object sender, RoutedEventArgs e)
        {
            dockPanel_Find.Visibility = Visibility.Collapsed;
        }

        private void TextBox_Find_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            button_Find.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);

        }


        private void FindNext()
        {
            string column = comboBox_FindColumns.Text;
            string searchText = textBox_Find.Text;

            int currentIndex = MainDataGrid.SelectedIndex;

            int rowIndex = FindRowIndexInDataGrid(MainDataGrid, currentIndex, searchText, column);

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

        private void Button_Find_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void TextBox_Find_KeyDown(object sender, KeyEventArgs e)
        {
            string findText = textBox_Find.Text;
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(findText))
            {
                FindNext();
            }
        }

        private static int FindRowIndexInDataGrid(DataGrid dataGrid, int currentIndex, string searchText, string columnName)
        {
            if (string.IsNullOrWhiteSpace(searchText) || dataGrid.Items.Count == 0)
            {
                return -1;
            }

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

        private static bool IsMatch(object item, string searchText, string columnName)
        {
            if (item is DataRowView rowView)
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    // Search all columns
                    return rowView.Row.ItemArray
                              .Cast<object?>()
                              .Any(col =>
                              {
                                  var s = col?.ToString();
                                  return !string.IsNullOrEmpty(s) &&
                                         s.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                              });
                }
                else if (rowView.Row.Table.Columns.Contains(columnName))
                {
                    var value = rowView.Row[columnName]?.ToString();
                    return !string.IsNullOrEmpty(value) &&
                           value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            return false;
        }

        private void MenuItem_SearchAndReplace_Click(object sender, RoutedEventArgs e)
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

        private void Button_CloseSearchAndReplace_Click(object sender, RoutedEventArgs e)
        {
            dockPanel_SearchAndReplace.Visibility = Visibility.Collapsed;
        }
                
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/RobG66/Gamelist-Manager/issues";
            OpenPage(url);
        }

        private void MenuItem_AboutClick(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }

        private static void OpenPage(string url)
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

        private void MenuItem_ClearSelectedMediaPaths_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataTable = SharedData.DataSet.Tables[0];
            IEnumerable<DataRow> rows;

            var selectedPaths = MainDataGrid.SelectedItems.Cast<DataRowView>()
            .Select(rowView => rowView.Row["Rom Path"]?.ToString())
            .ToList();

            rows = dataTable.AsEnumerable()
                 .Where(row => selectedPaths.Contains(row.Field<string>("Rom Path")));

            ClearMediaPaths(rows,"selected");

        }

        private void MenuItem_ClearAllMediaPaths_Click(object sender, RoutedEventArgs e)
        {
            DataTable dataTable = SharedData.DataSet.Tables[0];
            IEnumerable<DataRow> rows;
            rows = dataTable.AsEnumerable();

            ClearMediaPaths(rows,"all");
        }


        public void ClearMediaPaths(IEnumerable<DataRow> rows, string text)
        {
            int index = 0;
            DataTable dataTable = SharedData.DataSet.Tables[0];

            if (_confirmBulkChange)
            {
                var result = MessageBox.Show(
                    $"Do you want clear {text} media paths?\n\nAll other metadata remains unchanged",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            SharedData.ChangeTracker?.StartBulkOperation();

            var mediaItems = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl =>
                    decl.DataType == MetaDataType.Image ||
                    decl.DataType == MetaDataType.Video ||
                    decl.DataType == MetaDataType.Music ||
                    decl.DataType == MetaDataType.Document)
                .Select(decl => decl.Name)
                .ToList();

            foreach (var columnName in mediaItems)
            {
                if (!dataTable.Columns.Contains(columnName))
                    continue;

                foreach (var row in rows)
                    row[columnName] = DBNull.Value;
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker?.EndBulkOperation();

            // Force selection refresh to update media view
            if (MainDataGrid.Items.Count > 1)
            {
                int modifier = MainDataGrid.Items.Count - 1 == index ? -1 : 1;
                MainDataGrid.SelectedIndex = index + modifier;
                MainDataGrid.SelectedIndex = index;
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_scraper._isScraping)
            {
                _ = MessageBox.Show("A scraping operation is currently in progress. Please wait for it to finish before closing the application.", "Operation In Progress", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            // Last chance to save, always ask!
            if (SharedData.IsDataChanged)
            {
                SaveGamelist();
            }

            // Unsubscribe from event handlers to prevent memory leaks
            if (_dataGridSelectionChangedTimer != null)
            {
                _dataGridSelectionChangedTimer.Tick -= DataGridSelectionChangedTimer_Tick;
                _dataGridSelectionChangedTimer.Stop();
            }

            if (SharedData.ChangeTracker != null)
            {
                SharedData.ChangeTracker.UndoRedoStateChanged -= ChangeTracker_UndoRedoStateChanged;
            }

            // Unsubscribe ComboBox selection changed
            if (_scraper != null)
            {
                _scraper.comboBox_SelectedScraper.SelectionChanged -= _scraper.ComboBox_SelectedScraper_SelectionChanged;
            }

            // Unsubscribe Ribbon auto-hide events if they were attached
            RibbonHotZone.MouseEnter -= RibbonHotZone_MouseEnter;
            RibbonMenu.MouseLeave -= RibbonMenu_MouseLeave;

            // Unsubscribe font size sliders
            RibbonFontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
            ClassicFontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;

            // Stop media playback if active
            if (_mediaPage != null)
            {
                _mediaPage.StopPlaying();
                _mediaPage.ClearAllImages();
            }

            // Clean up scraper resources
            if (_scraper != null)
            {
                // Any scraper-specific cleanup if needed
            }

            // Clean up dat tool page
            if (_datToolPage != null && MainContentFrame.Content == _datToolPage)
            {
                _datToolPage.CloseDatToolPage();
            }
        }

        private void MenuItem_ResetName_Click(object sender, RoutedEventArgs e)
        {
            if (_confirmBulkChange)
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
            SharedData.DataSet.Tables[0].BeginLoadData();
            foreach (var item in MainDataGrid.SelectedItems)
            {
                if (item is DataRowView row && row["Rom Path"] is string path && !string.IsNullOrEmpty(path))
                {
                    string newName = NameHelper.NormalizeRomName(path);
                    if (SharedData.CurrentSystem == "mame")
                    {
                        newName = MameNamesHelper.Names.ContainsKey(newName) ? MameNamesHelper.Names[newName] : newName;
                    }
                    row["Name"] = newName;
                }
            }
            SharedData.DataSet.Tables[0].EndLoadData();
            SharedData.ChangeTracker?.EndBulkOperation();
        }

        private void MenuItem_ResetAllSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to reset all settings?\n\nSaved passwords will not be affected", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string filePath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
            string fileVersion = fileVersionInfo.FileVersion!;

            Properties.Settings.Default.Reset();

            Properties.Settings.Default.Version = fileVersion;

            Properties.Settings.Default.Save();

            string gridLineVisibility = Properties.Settings.Default.GridLineVisibility;
            if (Enum.TryParse(gridLineVisibility, out DataGridGridLinesVisibility visibility))
            {
                MainDataGrid.GridLinesVisibility = visibility;
            }

            var colorName = Properties.Settings.Default.AlternatingRowColor;
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            MainDataGrid.AlternatingRowBackground = new SolidColorBrush(color);
            Properties.Settings.Default.AlternatingRowColor = colorName;

            statusBar_FileInfo.Visibility = Properties.Settings.Default.ShowFileStatusBar ? Visibility.Visible : Visibility.Collapsed;

            MessageBox.Show("All settings have been reset.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        private void MenuItem_AllVisible_Click(object sender, RoutedEventArgs e)
        {
            SetAllHiddenValues(false);
        }

        private void MenuItem_AllHidden_Click(object sender, RoutedEventArgs e)
        {
            SetAllHiddenValues(true);
        }

        private void SetAllHiddenValues(bool hidden)
        {
            if (MainDataGrid.ItemsSource is not DataView dataView)
            {
                return;
            }

            if (_confirmBulkChange)
            {
                string scope = string.IsNullOrEmpty(dataView.RowFilter) ? "all" : "all filtered";
                string visibility = hidden ? "hidden" : "visible";

                MessageBoxResult result = MessageBox.Show(
                    $"Do you want to set {scope} items {visibility}?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            DataTable? table = dataView.Table;
            if (table == null)
            {
                return;
            }

            SharedData.ChangeTracker?.StartBulkOperation();

            table.BeginLoadData();

            foreach (DataRowView rowView in dataView)
            {
                rowView.Row["Hidden"] = hidden;
            }

            table.EndLoadData();
            SharedData.ChangeTracker?.EndBulkOperation();
        }
        private void MenuItem_RemoveSshKey_Click(object sender, RoutedEventArgs e)
        {
            string hostname = Properties.Settings.Default.BatoceraHostName;

            // Check if the hostname is set
            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("The Batocera hostname is not set.");
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
                string response = SshKeyHelper.RemoveBatoceraKey(hostname);
                MessageBox.Show(response, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void RadioButton_AllItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowAll();
        }

        private void RadioButton_VisibleItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowVisible();
        }

        private void RadioButton_HiddenItems_Checked(object sender, RoutedEventArgs e)
        {
            if (SharedData.DataSet == null || SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }
            ShowHidden();
        }

        private void Button_ResetView_Click(object sender, RoutedEventArgs e)
        {
            // ResetView will also reset saved columns
            Properties.Settings.Default.VisibleGridColumns = Properties.Settings.Default.DefaultColumns;
            Properties.Settings.Default.Save();
            ResetView();
        }
              
        private void ToggleMenuStyle_Click(object sender, RoutedEventArgs e)
        {
            SetMenuStyle(!Properties.Settings.Default.UseRibbonMenu);
        }

        private void SetMenuStyle(bool useRibbon)
        { 
            if (!useRibbon)
            {
                // Switch to Classic Menu
                RibbonMenu.Visibility = Visibility.Collapsed;
                ClassicMenu.Visibility = Visibility.Visible;
                RibbonHotZone.Visibility = Visibility.Collapsed;

                // Sync Sliders
                var fontSize = RibbonFontSizeSlider.Value;
                RibbonFontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
                ClassicFontSizeSlider.Value = fontSize;
                ClassicFontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;

                // Remove event handlers
                RibbonHotZone.MouseEnter -= RibbonHotZone_MouseEnter;
                RibbonMenu.MouseLeave -= RibbonMenu_MouseLeave;
                             
                // Sync enabled states
                menuItem_View.IsEnabled = ribbonTab_Home.IsEnabled;
                menuItem_Edit.IsEnabled = ribbonTab_Edit.IsEnabled;
                menuItem_Columns.IsEnabled = ribbonTab_Columns.IsEnabled;
                menuItem_Tools.IsEnabled = ribbonTab_Tools.IsEnabled;

                // Sync column checkboxes
                SyncColumnCheckboxes(false); // false = ribbon to menu

                Properties.Settings.Default.UseRibbonMenu = false;
                Properties.Settings.Default.AutoHideRibbon = false;
                ribbon_AutoHide.IsChecked = false;
                Properties.Settings.Default.Save();
            }
            else
            {
                // Switch to Ribbon
                ClassicMenu.Visibility = Visibility.Collapsed;
                RibbonMenu.Visibility = Visibility.Visible;
                RibbonHotZone.Visibility = Visibility.Visible;

                // Sync Sliders
                var fontSize = ClassicFontSizeSlider.Value;
                ClassicFontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
                RibbonFontSizeSlider.Value = fontSize;
                RibbonFontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;

                bool autoHideRibbon = Properties.Settings.Default.AutoHideRibbon;
                // Add event handlers if autohide is enabled
                if (autoHideRibbon)
                {
                    RibbonHotZone.MouseEnter += RibbonHotZone_MouseEnter;
                    RibbonMenu.MouseLeave += RibbonMenu_MouseLeave;
                }
                else
                {
                    RibbonHotZone.MouseEnter -= RibbonHotZone_MouseEnter;
                    RibbonMenu.MouseLeave -= RibbonMenu_MouseLeave;
                }

                // Sync enabled states
                ribbonTab_Home.IsEnabled = menuItem_View.IsEnabled;
                ribbonTab_Edit.IsEnabled = menuItem_Edit.IsEnabled;
                ribbonTab_Columns.IsEnabled = menuItem_Columns.IsEnabled;
                ribbonTab_Tools.IsEnabled = menuItem_Tools.IsEnabled;

                // Sync column checkboxes
                SyncColumnCheckboxes(true); // true = menu to ribbon

                Properties.Settings.Default.UseRibbonMenu = true;
                if (!string.IsNullOrEmpty(SharedData.XMLFilename))
                {
                    ribbon_AutoHide.IsChecked = false;
                    Properties.Settings.Default.AutoHideRibbon = false;
                    ShowRibbonWithAnimation(); // make sure ribbon is shown
                }
                Properties.Settings.Default.Save();
            }
        }

        private void SyncColumnCheckboxes(bool menuToRibbon)
        {
            if (menuToRibbon)
            {
                ribbon_Description.IsChecked = menuItem_Description.IsChecked;
                ribbon_Genre.IsChecked = menuItem_Genre.IsChecked;
                ribbon_GameId.IsChecked = menuItem_GameId.IsChecked;
                ribbon_Rating.IsChecked = menuItem_Rating.IsChecked;
                ribbon_ReleaseDate.IsChecked = menuItem_ReleaseDate.IsChecked;
                ribbon_Players.IsChecked = menuItem_Players.IsChecked;
                ribbon_Favorite.IsChecked = menuItem_Favorite.IsChecked;
                ribbon_Developer.IsChecked = menuItem_Developer.IsChecked;
                ribbon_Publisher.IsChecked = menuItem_Publisher.IsChecked;
                ribbon_ArcadeSystemName.IsChecked = menuItem_ArcadeSystemName.IsChecked;
                ribbon_Family.IsChecked = menuItem_Family.IsChecked;
                ribbon_Region.IsChecked = menuItem_Region.IsChecked;
                ribbon_Language.IsChecked = menuItem_Language.IsChecked;
                ribbon_PlayCount.IsChecked = menuItem_PlayCount.IsChecked;
                ribbon_LastPlayed.IsChecked = menuItem_LastPlayed.IsChecked;
                ribbon_GameTime.IsChecked = menuItem_GameTime.IsChecked;
                ribbon_MediaPaths.IsChecked = menuItem_MediaPaths.IsChecked;
            }
            else
            {
                menuItem_Description.IsChecked = ribbon_Description.IsChecked ?? false;
                menuItem_Genre.IsChecked = ribbon_Genre.IsChecked ?? false;
                menuItem_GameId.IsChecked = ribbon_GameId.IsChecked ?? false;
                menuItem_Rating.IsChecked = ribbon_Rating.IsChecked ?? false;
                menuItem_ReleaseDate.IsChecked = ribbon_ReleaseDate.IsChecked ?? false;
                menuItem_Players.IsChecked = ribbon_Players.IsChecked ?? false;
                menuItem_Favorite.IsChecked = ribbon_Favorite.IsChecked ?? false;
                menuItem_Developer.IsChecked = ribbon_Developer.IsChecked ?? false;
                menuItem_Publisher.IsChecked = ribbon_Publisher.IsChecked ?? false;
                menuItem_ArcadeSystemName.IsChecked = ribbon_ArcadeSystemName.IsChecked ?? false;
                menuItem_Family.IsChecked = ribbon_Family.IsChecked ?? false;
                menuItem_Region.IsChecked = ribbon_Region.IsChecked ?? false;
                menuItem_Language.IsChecked = ribbon_Language.IsChecked ?? false;
                menuItem_PlayCount.IsChecked = ribbon_PlayCount.IsChecked ?? false;
                menuItem_LastPlayed.IsChecked = ribbon_LastPlayed.IsChecked ?? false;
                menuItem_GameTime.IsChecked = ribbon_GameTime.IsChecked ?? false;
                menuItem_MediaPaths.IsChecked = ribbon_MediaPaths.IsChecked ?? false;
            }
        }

        // Hide the ribbon's quick access toolbar
        private void MainRibbon_Loaded(object sender, RoutedEventArgs e)
        {
            Grid child = System.Windows.Media.VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                child.RowDefinitions[0].Height = new GridLength(0);
            }
        }

        private void RibbonButton_ClearAllFilters_Click(object sender, RoutedEventArgs e)
        {
            ResetAllFilters();
        }

  
        private void menuItem_FindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will quickly identify any duplicate items in this gamelist, regardless of text case.\n\nDo you want to continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            FindDuplicateItems();

        }

        private void FindDuplicateItems()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            // Temporarily disable DataGrid
            MainDataGrid.IsEnabled = false;

            var duplicateItems = new List<DataRowView>();

            // Track seen ROM paths (case-insensitive)
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Detect duplicates
                foreach (DataRowView row in MainDataGrid.Items.OfType<DataRowView>())
                {
                    if (row["Rom Path"] is string romPath && !string.IsNullOrWhiteSpace(romPath))
                    {
                        string fullPath = PathHelper.ConvertGamelistPathToFullPath(romPath, _parentFolderPath!);

                        // Check for duplicates - if already seen, it's a duplicate
                        if (seenPaths.Contains(fullPath))
                        {
                            duplicateItems.Add(row);
                        }
                        else
                        {
                            seenPaths.Add(fullPath);
                        }
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
                MainDataGrid.IsEnabled = true;
            }

            // --- No duplicate items ---
            if (duplicateItems.Count == 0)
            {
                MessageBox.Show("No duplicate items were detected", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // --- Show Status column ---
            var statusColumn = MainDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Status");
            if (statusColumn != null)
            {
                statusColumn.Visibility = Visibility.Visible;
      
                if (!comboBox_CustomFilter.Items.Contains("Status"))
                    comboBox_CustomFilter.Items.Add("Status");

            }

            // --- Ask user ---
            var result = MessageBox.Show(
                $"There are {duplicateItems.Count} duplicate items in this gamelist.\n\nDo you want to remove them?",
                "Notice:",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SharedData.ChangeTracker!.StartBulkOperation();

                foreach (var row in duplicateItems)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (MainDataGrid.ItemsSource is DataView dataView && dataView.Table != null)
                        {
                            dataView.Table.Rows.Remove(row.Row);
                        }
                    });
                }

                SharedData.ChangeTracker!.EndBulkOperation();
            }
            else
            {
                // Mark duplicates
                foreach (var row in duplicateItems)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        row["Status"] = "Duplicate";
                    });
                }
            }
        }

        private async void MainDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            if (_currentSelectedRow != null)
            {
                MainDataGrid.SelectedItem = _currentSelectedRow;
                MainDataGrid.UpdateLayout(); // important for virtualization
                MainDataGrid.ScrollIntoView(_currentSelectedRow);
            }
        }

        // In InitializeComponent() or Window_Loaded, add:
        // MainDataGrid.RowEditEnding += MainDataGrid_RowEditEnding;

        private void MainDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            var dataView = (DataView)MainDataGrid.ItemsSource;
            if (string.IsNullOrEmpty(dataView?.RowFilter))
                return; // No filter, nothing to worry about

            int currentIndex = MainDataGrid.SelectedIndex;
            var editedRow = e.Row.Item as DataRowView;

            // Check after the edit completes
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Is the edited row still visible?
                if (!MainDataGrid.Items.Contains(editedRow))
                {
                    // Row filtered out, select next or previous visible row
                    if (MainDataGrid.Items.Count == 0)
                    {
                        // No rows left, do nothing
                        return;
                    }

                    // Try next row at same index, or previous if at end
                    int newIndex = currentIndex < MainDataGrid.Items.Count
                        ? currentIndex
                        : MainDataGrid.Items.Count - 1;

                    MainDataGrid.SelectedIndex = newIndex;
                    MainDataGrid.ScrollIntoView(MainDataGrid.Items[newIndex]);
                }
            }), DispatcherPriority.Background);
        }

    }
}