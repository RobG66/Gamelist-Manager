using GamelistManager.classes.helpers;
using Microsoft.Win32;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GamelistManager
{
    public partial class SettingsDialogWindow : Window
    {
        private DataGrid _mainDataGrid;
        private StatusBar statusBar;

        // Constants for validation ranges
        private const int MIN_SEARCH_DEPTH = 0;
        private const int MAX_SEARCH_DEPTH = 9;
        private const int DEFAULT_SEARCH_DEPTH = 2;
               
        private const int MIN_MAX_UNDO = 0;
        private const int MAX_MAX_UNDO = 15;
        private const int DEFAULT_MAX_UNDO = 5;

        private const int MIN_RECENT_FILES = 1;
        private const int MAX_RECENT_FILES = 50;
        private const int DEFAULT_RECENT_FILES = 15;

        private const int MIN_BATCH_SIZE = 50;
        private const int MAX_BATCH_SIZE = 300;
        private const int DEFAULT_BATCH_SIZE = 300;

        public SettingsDialogWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainDataGrid = mainWindow.MainDataGrid;
            statusBar = mainWindow.statusBar_FileInfo;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string hostName = textBox_HostName.Text;
            string userID = textBox_UserID.Text;
            string password = textBox_Password.Text;

            string theme = comboBox_Theme.Text;
            Properties.Settings.Default.Theme = theme;

            string gridLineVisibility = comboBox_GridLinesVisibility.Text;
            if (Enum.TryParse(gridLineVisibility, out DataGridGridLinesVisibility visibility))
            {
                _mainDataGrid.GridLinesVisibility = visibility;
            }

            Properties.Settings.Default.LogVerbosity = comboBox_LogVerbosity.SelectedIndex;
            Properties.Settings.Default.GridLineVisibility = gridLineVisibility;
            Properties.Settings.Default.BatoceraHostName = hostName;
            Properties.Settings.Default.ConfirmBulkChange = (bool)checkBox_ConfirmBulkChanges.IsChecked!;
            Properties.Settings.Default.RememberColumns = (bool)checkBox_RememberColumns.IsChecked!;
            Properties.Settings.Default.SaveReminder = (bool)checkBox_EnableSaveReminder.IsChecked!;
            Properties.Settings.Default.EnableDelete = (bool)checkBox_EnableDelete.IsChecked!;
            Properties.Settings.Default.ShowFileStatusBar = (bool)checkBox_ShowFileStatusBar.IsChecked!;
            Properties.Settings.Default.VerifyDownloadedImages = (bool)checkBox_VerifyImageDownloads.IsChecked!;
            Properties.Settings.Default.VideoAutoplay = (bool)checkBox_VideoAutoplay.IsChecked!;
            Properties.Settings.Default.RememberAutoSize = (bool)checkBox_RememberAutosize.IsChecked!;
            Properties.Settings.Default.Volume = (int)sliderVolumeSetting.Value;
            Properties.Settings.Default.AutoExpandLogger = (bool)checkBox_AutoExpandLogger.IsChecked!;
            Properties.Settings.Default.IgnoreDuplicates = (bool)checkBox_IgnoreDuplicates.IsChecked!;
            Properties.Settings.Default.BatchProcessing = (bool)checkBox_BatchProcessing.IsChecked!;
            Properties.Settings.Default.DisableScrapeNotifications = (bool)checkBox_DisableScrapeNotifications.IsChecked!;

            // Search Depth validation
            string searchDepth = textBox_SearchDepth.Text;
            if (!int.TryParse(searchDepth, out int searchDepthInt) || searchDepthInt < MIN_SEARCH_DEPTH || searchDepthInt > MAX_SEARCH_DEPTH)
            {
                searchDepthInt = DEFAULT_SEARCH_DEPTH;
            }
            textBox_SearchDepth.Text = searchDepthInt.ToString();
            Properties.Settings.Default.SearchDepth = searchDepthInt;

            // Max Undo validation
            string maxUndo = textBox_MaxUndo.Text;
            if (!int.TryParse(maxUndo, out int maxUndoInt) || maxUndoInt < MIN_MAX_UNDO || maxUndoInt > MAX_MAX_UNDO)
            {
                maxUndoInt = DEFAULT_MAX_UNDO;
            }
            textBox_MaxUndo.Text = maxUndoInt.ToString();
            Properties.Settings.Default.MaxUndo = maxUndoInt;

            // Recent Files Count validation
            string recentFilesCount = textBox_RecentFilesCount.Text;
            if (!int.TryParse(recentFilesCount, out int recentFilesInt) || recentFilesInt < MIN_RECENT_FILES || recentFilesInt > MAX_RECENT_FILES)
            {
                recentFilesInt = DEFAULT_RECENT_FILES;
            }
            textBox_RecentFilesCount.Text = recentFilesInt.ToString();
            Properties.Settings.Default.RecentFilesCount = recentFilesInt;

            // Max Batch validation
            string maxBatch = textBox_MaxBatch.Text;
            if (!int.TryParse(maxBatch, out int maxBatchInt) || maxBatchInt < MIN_BATCH_SIZE || maxBatchInt > MAX_BATCH_SIZE)
            {
                maxBatchInt = DEFAULT_BATCH_SIZE;
            }
            textBox_MaxBatch.Text = maxBatchInt.ToString();
            Properties.Settings.Default.BatchProcessingMaximum = maxBatchInt;
            
            string MamePath = textBox_MamePath.Text;
            Properties.Settings.Default.MamePath = MamePath;

            bool result = CredentialHelper.SaveCredentials(hostName, userID, password);

            if (!result)
            {
                MessageBox.Show("Failed to save credentials!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var textBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<TextBox>(Paths);
            Dictionary<string, string> mediaPaths = [];
            foreach (TextBox textBox in textBoxes)
            {
                string text = textBox.Text.Trim();

                // Remove drive letters (e.g., "C:", "D:")
                text = Regex.Replace(text, @"^[a-zA-Z]:", "");

                // Replace backslashes with forward slashes
                text = text.Replace('\\', '/');

                // Remove invalid filename characters except forward slash
                // Windows invalid chars: < > : " | ? * and control characters
                text = Regex.Replace(text, @"[<>:""|?*\x00-\x1F]", "");

                // Remove leading/trailing slashes and consolidate multiple slashes
                text = Regex.Replace(text, @"/+", "/").Trim('/');

                text = text.Trim();

                textBox.Text = text;
                string? name = textBox.Tag.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    mediaPaths[name] = text;
                }
            }

            var checkBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<CheckBox>(Paths);
                        
            foreach (CheckBox checkBox in checkBoxes)
            {
                string? name = checkBox.Name; // Use checkbox name, not tag
                if (!string.IsNullOrEmpty(name) && name.StartsWith("checkBox_Disable"))
                {
                    // Extract media type from checkbox name (e.g., "checkBox_DisableImage" -> "image")
                    string mediaType = name.Replace("checkBox_Disable", "").ToLower();
                    string key = $"{mediaType}_enabled";
                    // Store as "enabled" or "disabled"
                    mediaPaths[key] = checkBox.IsChecked == true ? "true" : "false";
                }
            }

            string jsonString = JsonSerializer.Serialize(mediaPaths);
            Properties.Settings.Default.MediaPaths = jsonString;

            var colorName = comboBox_AlternatingRowColor.Text;
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            Properties.Settings.Default.AlternatingRowColor = colorName;

            statusBar.Visibility = Properties.Settings.Default.ShowFileStatusBar ? Visibility.Visible : Visibility.Collapsed;

            button_Save.Content = "Saved";
            button_Save.IsEnabled = false;

            Properties.Settings.Default.Save();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            button_Save.IsEnabled = false;

            // Populate saved or default mediapaths from a serialized json string
            string mediaPathsJsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths;

            try
            {
                mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)
                             ?? new Dictionary<string, string>();
            }
            catch
            {
                mediaPaths = new Dictionary<string, string>();
            }

            var textBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<TextBox>(Paths);
            foreach (TextBox textBox in textBoxes)
            {
                var tagValue = textBox.Tag;
                if (tagValue != null && mediaPaths.TryGetValue(tagValue.ToString()!, out string? value))
                {
                    textBox.Text = value;
                }
            }

            // Load checkbox states
            var checkBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<CheckBox>(Paths);
            foreach (CheckBox checkBox in checkBoxes)
            {
                string? name = checkBox.Name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("checkBox_Disable"))
                {
                    // Extract media type from checkbox name
                    string mediaType = name.Replace("checkBox_Disable", "").ToLower();
                    string key = $"{mediaType}_enabled";

                    if (mediaPaths.TryGetValue(key, out string? value))
                    {
                        checkBox.IsChecked = value == "true";
                    }
                    else
                    {
                        checkBox.IsChecked = true; // Default to enabled
                    }
                }
            }

            string currentColorName = Properties.Settings.Default.AlternatingRowColor;

            var colors = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static)
                                     .Select(prop => new { Name = prop.Name, Color = (Color)prop.GetValue(null)! })
                                     .ToList();

            foreach (var color in colors)
            {
                ComboBoxItem item = new()
                {
                    Content = color.Name,
                    Background = new SolidColorBrush(color.Color),
                    Foreground = new SolidColorBrush(Colors.Black) // Ensure text is visible
                };
                comboBox_AlternatingRowColor.Items.Add(item);
            }

            //comboBox_AlternatingRowColor.SelectedIndex = 0;

            var item2 = comboBox_AlternatingRowColor.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Content.ToString() == currentColorName);
            if (item2 != null)
            {
                comboBox_AlternatingRowColor.SelectedItem = item2;
            }

            string? mamePath = Properties.Settings.Default.MamePath;
            if (!string.IsNullOrEmpty(mamePath))
            {
                textBox_MamePath.Text = mamePath;
            }

            string hostName = Properties.Settings.Default.BatoceraHostName;
            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword))
            {
                textBox_HostName.Text = hostName;
                textBox_UserID.Text = userName;
                textBox_Password.Text = userPassword;
            }

            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;
            checkBox_ConfirmBulkChanges.IsChecked = confirmBulkChanges;

            bool rememberAutoSize = Properties.Settings.Default.RememberAutoSize;
            checkBox_RememberAutosize.IsChecked = rememberAutoSize;

            bool disableNotifications = Properties.Settings.Default.DisableScrapeNotifications;
            checkBox_DisableScrapeNotifications.IsChecked = disableNotifications;

            bool showFileStatusBar = Properties.Settings.Default.ShowFileStatusBar;
            checkBox_ShowFileStatusBar.IsChecked = showFileStatusBar;

            bool enableDelete = Properties.Settings.Default.EnableDelete;
            checkBox_EnableDelete.IsChecked = enableDelete;

            bool ignoreDuplicates = Properties.Settings.Default.IgnoreDuplicates;
            checkBox_IgnoreDuplicates.IsChecked = ignoreDuplicates;

            bool batchProcessing = Properties.Settings.Default.BatchProcessing;
            checkBox_BatchProcessing.IsChecked = batchProcessing;
            textBox_MaxBatch.IsEnabled = batchProcessing;

            bool rememberColumns = Properties.Settings.Default.RememberColumns;
            checkBox_RememberColumns.IsChecked = rememberColumns;

            bool autoExpandLogger = Properties.Settings.Default.AutoExpandLogger;
            checkBox_AutoExpandLogger.IsChecked = autoExpandLogger;

            bool saveReminder = Properties.Settings.Default.SaveReminder;
            checkBox_EnableSaveReminder.IsChecked = saveReminder;
                    
            bool verifyImages = Properties.Settings.Default.VerifyDownloadedImages;
            checkBox_VerifyImageDownloads.IsChecked = verifyImages;
                 
            bool videoAutoplay = Properties.Settings.Default.VideoAutoplay;
            checkBox_VideoAutoplay.IsChecked = videoAutoplay;

            int recentFilesCount = Properties.Settings.Default.RecentFilesCount;
            if (recentFilesCount < MIN_RECENT_FILES || recentFilesCount > MAX_RECENT_FILES)
            {
                recentFilesCount = DEFAULT_RECENT_FILES;
            }
            textBox_RecentFilesCount.Text = recentFilesCount.ToString();

            int searchDepth = Properties.Settings.Default.SearchDepth;
            if (searchDepth < MIN_SEARCH_DEPTH || searchDepth > MAX_SEARCH_DEPTH)
            {
                searchDepth = DEFAULT_SEARCH_DEPTH;
            }
            textBox_SearchDepth.Text = searchDepth.ToString();

            int volume = Properties.Settings.Default.Volume;
            if (volume > 100 || volume < 0)
            {
                volume = 75;
            }
            sliderVolumeSetting.Value = volume;

            int maxUndo = Properties.Settings.Default.MaxUndo;
            if (maxUndo < 1 || maxUndo > MAX_MAX_UNDO)
            {
                checkBox_TrackChanges.IsChecked = false;
                textBox_MaxUndo.IsEnabled = false;
                textBox_MaxUndo.Text = "0";
            }
            else
            {
                checkBox_TrackChanges.IsChecked = true;
                textBox_MaxUndo.IsEnabled = true;
                textBox_MaxUndo.Text = maxUndo.ToString();
            }

            string theme = Properties.Settings.Default.Theme;
            comboBox_Theme.SelectedItem = comboBox_Theme.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == theme);

            int logVerbosity = Properties.Settings.Default.LogVerbosity;    
            comboBox_LogVerbosity.SelectedIndex = logVerbosity;

            // Hook up event handlers for comboboxes
            comboBox_AlternatingRowColor.SelectionChanged += ComboBox_AlternatingRowColor_SelectionChanged;
            comboBox_GridLinesVisibility.SelectionChanged += ComboBox_GridLinesSelectionChanged;
            comboBox_Theme.SelectionChanged += ComboBox_Theme_SelectionChanged;

        }

        private void SetTextBoxDefaults()
        {
            string mediaPathsJsonString = GetDefaultSetting("MediaPaths")!;
            Dictionary<string, string> mediaPaths = new();

            if (!string.IsNullOrEmpty(mediaPathsJsonString))
            { 
                try
                {
                    mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)
                                 ?? new Dictionary<string, string>();
                }
                catch
                {
                    // Ignore errors and use empty dictionary
                }
            }

            var textBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<TextBox>(Paths);
            foreach (TextBox textBox in textBoxes)
            {
                var tagValue = textBox.Tag;
                if (tagValue != null && mediaPaths.TryGetValue(tagValue.ToString()!, out string? value))
                {
                    textBox.Text = value;
                }
            }

            // Reset checkboxes to default (all enabled)
            var checkBoxes = GamelistManager.classes.helpers.TreeHelper.GetAllVisualChildren<CheckBox>(Paths);
            foreach (CheckBox checkBox in checkBoxes)
            {
                string? name = checkBox.Name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("checkBox_Disable"))
                {
                    string mediaType = name.Replace("checkBox_Disable", "").ToLower();
                    string key = $"{mediaType}_enabled";

                    if (mediaPaths.TryGetValue(key, out string? value))
                    {
                        checkBox.IsChecked = value == "true";
                    }
                    else
                    {
                        checkBox.IsChecked = true; // Default to enabled
                    }
                }
            }

        }

        private static string? GetDefaultSetting(string propertyName)
        {
            // Get the property from the Settings class by its name
            PropertyInfo? property = typeof(Properties.Settings).GetProperty(propertyName);

            if (property != null)
            {
                // Get the DefaultSettingValue attribute if it exists
                var defaultValueAttribute = (DefaultSettingValueAttribute?)property
                    .GetCustomAttributes(typeof(DefaultSettingValueAttribute), false)
                    .FirstOrDefault();

                if (defaultValueAttribute != null)
                {
                    // Return the default Value as a string
                    return defaultValueAttribute.Value;
                }
            }

            // Return null if the property or default Value is not found
            return null;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            button_Save.IsEnabled = true;
            button_Save.Content = "Save";

            if (checkBox_TrackChanges.IsChecked == true)
            {
                textBox_MaxUndo.IsEnabled = true;
                textBox_MaxUndo.Text = MAX_MAX_UNDO.ToString();
            }
            else
            {
                textBox_MaxUndo.IsEnabled = false;
                textBox_MaxUndo.Text = "0";
            }

            if (checkBox_BatchProcessing.IsChecked == true)
            {
                textBox_MaxBatch.IsEnabled = true;
                textBox_MaxBatch.Text = DEFAULT_BATCH_SIZE.ToString();
            }
            else
            {
                textBox_MaxBatch.IsEnabled = false;
            }
                       
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input is a digit
            e.Handled = !IsTextNumeric(e.Text);
            button_Save.IsEnabled = true;
            button_Save.Content = "Save";
        }

        private void NumericTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));

                if (!IsTextNumeric(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextNumeric(string text)
        {
            // Use regex to match only digits 0-9
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void ComboBox_AlternatingRowColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null || _mainDataGrid == null)
            {
                return;
            }

            if (comboBox.Items[comboBox.SelectedIndex] is ComboBoxItem selectedItem)
            {
                string? colorName = selectedItem.Content?.ToString();
                if (string.IsNullOrEmpty(colorName))
                {
                    return;
                }

                var color = (Color)ColorConverter.ConvertFromString(colorName);
                _mainDataGrid.AlternatingRowBackground = new SolidColorBrush(color);

                if (button_Save != null)
                {
                    button_Save.Content = "Save";
                    button_Save.IsEnabled = true;
                }
            }
        }

        private void ComboBox_GridLinesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null || _mainDataGrid == null)
            {
                return;
            }

            if (comboBox.Items[comboBox.SelectedIndex] is ComboBoxItem selectedItem)
            {
                string? gridLineVisibility = selectedItem.Content?.ToString();
                if (string.IsNullOrEmpty(gridLineVisibility))
                {
                    return;
                }

                _mainDataGrid.GridLinesVisibility = gridLineVisibility switch
                {
                    "None" => DataGridGridLinesVisibility.None,
                    "Horizontal" => DataGridGridLinesVisibility.Horizontal,
                    "Vertical" => DataGridGridLinesVisibility.Vertical,
                    "All" => DataGridGridLinesVisibility.All,
                    _ => DataGridGridLinesVisibility.None,
                };

                if (button_Save != null)
                {
                    button_Save.Content = "Save";
                    button_Save.IsEnabled = true;
                }
            }
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            SetTextBoxDefaults();
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void SliderVolumeSetting_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // Get the mouse position relative to the slider
                var position = e.GetPosition(slider);

                // Calculate the ratio of the mouse position to the slider width
                var relativePosition = position.X / slider.ActualWidth;

                // Determine the new Value
                var newValue = relativePosition * (slider.Maximum - slider.Minimum) + slider.Minimum;

                // Set the slider Value
                slider.Value = newValue;
            }
        }

        private void ComboBox_Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Theme.SelectedItem is ComboBoxItem selectedItem)
            {
                var themeContent = selectedItem.Content.ToString();
                var themeTag = selectedItem.Tag.ToString();

                if (Enum.TryParse<ThemeHelper.Theme>(themeContent, out var theme))
                {
                    ThemeHelper.Instance.ApplyTheme(theme);
                    ThemeHelper.Instance.SaveThemePreference(theme);
                }

                var item = comboBox_AlternatingRowColor.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == themeTag);
                if (item != null)
                {
                    comboBox_AlternatingRowColor.SelectedItem = item;
                }

                Properties.Settings.Default.AlternatingRowColor = themeTag;
                Properties.Settings.Default.Save();
            }
        }

        private void Button_FindMame_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Locate mame.exe",
                Filter = "MAME Executable (mame.exe)|mame.exe",
                CheckFileExists = true,
                FileName = "mame.exe"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true && File.Exists(openFileDialog.FileName))
            {
                textBox_MamePath.Text = openFileDialog.FileName;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unhook event handlers to prevent memory leaks
            comboBox_AlternatingRowColor.SelectionChanged -= ComboBox_AlternatingRowColor_SelectionChanged;
            comboBox_GridLinesVisibility.SelectionChanged -= ComboBox_GridLinesSelectionChanged;
            comboBox_Theme.SelectionChanged -= ComboBox_Theme_SelectionChanged;

        }
    }
}