using GamelistManager.classes.helpers;
using Microsoft.Win32;
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
            string searchDepth = textBox_SearchDepth.Text;

            if (string.IsNullOrEmpty(hostName) || string.IsNullOrEmpty(searchDepth) || string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("One or more fields are empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string theme = comboBox_Theme.Text;
            Properties.Settings.Default.Theme = theme;

            string gridLineVisibility = comboBox_GridLinesVisibility.Text;
            if (Enum.TryParse(gridLineVisibility, out DataGridGridLinesVisibility visibility))
            {
                _mainDataGrid.GridLinesVisibility = visibility;
            }

            Properties.Settings.Default.GridLineVisibility = gridLineVisibility;

            Properties.Settings.Default.BatoceraHostName = hostName;
            Properties.Settings.Default.ConfirmBulkChange = (bool)checkBox_ConfirmBulkChanges.IsChecked!;
            Properties.Settings.Default.RememberColumns = (bool)checkBox_RememberColumns.IsChecked!;
            Properties.Settings.Default.SaveReminder = (bool)checkBox_EnableSaveReminder.IsChecked!;
            Properties.Settings.Default.ShowFileStatusBar = (bool)checkBox_ShowFileStatusBar.IsChecked!;
            Properties.Settings.Default.VerifyDownloadedImages = (bool)checkBox_VerifyImageDownloads.IsChecked!;
            Properties.Settings.Default.VideoAutoplay = (bool)checkBox_VideoAutoplay.IsChecked!;
            Properties.Settings.Default.RememberAutoSize = (bool)checkBox_RememberAutosize.IsChecked!;
            Properties.Settings.Default.Volume = (int)sliderVolumeSetting.Value;
            Properties.Settings.Default.AutoExpandLogger = (bool)checkBox_AutoExpandLogger.IsChecked!;

            string searchDepthString = textBox_SearchDepth.Text;
            int searchDepthInt = !int.TryParse(searchDepthString, out searchDepthInt) ? 0 : searchDepthInt;
            Properties.Settings.Default.SearchDepth = searchDepthInt;

            string changeTrackerValue = textBox_ChangeCount.Text;
            int maxUndo = string.IsNullOrEmpty(changeTrackerValue) || !int.TryParse(changeTrackerValue, out maxUndo) ? 0 : maxUndo;
            Properties.Settings.Default.MaxUndo = maxUndo;

            string MamePath = textBox_MamePath.Text;
            Properties.Settings.Default.MamePath = MamePath;

            bool result = CredentialHelper.SaveCredentials(hostName, userID, password);

            if (!result)
            {
                MessageBox.Show("Failed to save credentials!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var textBoxes = GamelistManager.classes.helpers.VisualTreeHelper.GetAllVisualChildren<TextBox>(Paths);
            Dictionary<string, string> mediaPaths = [];
            foreach (TextBox textBox in textBoxes)
            {
                // Clean the text to keep only alphanumeric characters
                string cleanedText = Regex.Replace(textBox.Text, "[^a-zA-Z0-9]", "");
                textBox.Text = cleanedText;
                string? name = textBox.Tag.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    mediaPaths[name.ToString()] = cleanedText;
                }
            }

            string jsonString = JsonSerializer.Serialize(mediaPaths);
            Properties.Settings.Default.MediaPaths = jsonString;


            if (comboBox_AlternatingRowColor.SelectedItem is ComboBoxItem selectedItem)
            {
                var colorName = selectedItem.Content.ToString();
                var color = (Color)ColorConverter.ConvertFromString(colorName);
                _mainDataGrid.AlternatingRowBackground = new SolidColorBrush(color);
                Properties.Settings.Default.AlternatingRowColor = colorName;
            }

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
            var mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)!;
            var textBoxes = GamelistManager.classes.helpers.VisualTreeHelper.GetAllVisualChildren<TextBox>(Paths);
            foreach (TextBox textBox in textBoxes)
            {
                var tagValue = textBox.Tag;
                if (tagValue != null)
                {
                    string value = mediaPaths[tagValue.ToString()!];
                    textBox.Text = value;
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

            comboBox_AlternatingRowColor.SelectedIndex = 0;

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

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                return;
            }

            textBox_HostName.Text = hostName;
            textBox_UserID.Text = userName;
            textBox_Password.Text = userPassword;

            bool confirmBulkChanges = Properties.Settings.Default.ConfirmBulkChange;
            checkBox_ConfirmBulkChanges.IsChecked = confirmBulkChanges;

            bool rememberAutoSize = Properties.Settings.Default.RememberAutoSize;
            checkBox_RememberAutosize.IsChecked = rememberAutoSize;

            bool showFileStatusBar = Properties.Settings.Default.ShowFileStatusBar;
            checkBox_ShowFileStatusBar.IsChecked = showFileStatusBar;

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

            int searchDepth = Properties.Settings.Default.SearchDepth;
            if (searchDepth < 0 || searchDepth > 9)
            {
                searchDepth = 0;
            }
            textBox_SearchDepth.Text = searchDepth.ToString();

            int volume = Properties.Settings.Default.Volume;
            if (volume > 100 || volume < 0)
            {
                volume = 75;
            }
            sliderVolumeSetting.Value = volume;

            int maxUndo = Properties.Settings.Default.MaxUndo;
            if (maxUndo == 0)
            {
                checkBox_TrackChanges.IsChecked = false;
                textBox_ChangeCount.IsEnabled = false;
                textBox_ChangeCount.Text = "0";
            }
            else
            {
                checkBox_TrackChanges.IsChecked = true;
                textBox_ChangeCount.IsEnabled = true;
                textBox_ChangeCount.Text = maxUndo.ToString();
            }

            string theme = Properties.Settings.Default.Theme;
            comboBox_Theme.SelectedItem = comboBox_Theme.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content?.ToString() == theme);

        }

        private void SetTextBoxDefaults()
        {
            string mediaPathsJsonString = SettingsHelper.GetDefaultSetting("MediaPaths")!;
            var mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaPathsJsonString)!;
            var textBoxes = GamelistManager.classes.helpers.VisualTreeHelper.GetAllVisualChildren<TextBox>(Paths);
            foreach (TextBox textBox in textBoxes)
            {
                var tagValue = textBox.Tag;
                if (tagValue != null)
                {
                    string value = mediaPaths[tagValue.ToString()!];
                    textBox.Text = value;
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            button_Save.IsEnabled = true;
            if (checkBox_TrackChanges.IsChecked == true)
            {
                textBox_ChangeCount.IsEnabled = true;
                textBox_ChangeCount.Text = "15";
            }
            else
            {
                textBox_ChangeCount.IsEnabled = false;
                textBox_ChangeCount.Text = "0";
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input is a digit
            e.Handled = !IsTextNumeric(e.Text);
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
    }
}

