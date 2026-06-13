using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gamelist_Manager.Models;
using Gamelist_Manager.ViewModels;
using System;
using System.ComponentModel;

namespace Gamelist_Manager.Views
{
    public partial class SettingsView : Window
    {
        private const double BASE_WIDTH = 580;
        private const double BASE_HEIGHT = 600;
        private const double BASE_FONT_SIZE = 12.0;
        private readonly SettingsState _settingsState = SettingsState.Instance;
        private readonly SessionState _sessionState = SessionState.Instance;

        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;

        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            // Scale window dimensions proportionally to font size (base: 580x600 at font 12)
            var scale = SettingsState.Instance.AppFontSize / BASE_FONT_SIZE;
            Width = Math.Round(BASE_WIDTH * scale);
            Height = Math.Round(BASE_HEIGHT * scale);

            Opened += OnOpened;
            KeyDown += SettingsWindow_KeyDown;
            Closing += SettingsWindow_Closing;
        }

        public void NavigateTo(int tabIndex, int scraperIndex)
        {
            MainTabControl.SelectedIndex = tabIndex;
            ViewModel.SelectedSetupScraperIndex = scraperIndex;
        }

        #region Event Handlers - Window

        private void OnOpened(object? sender, EventArgs e)
        {
            if (Owner is not MainWindow { DataContext: MainWindowViewModel }) return;
        }

        private async void SettingsWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                if (!ViewModel.SettingsChanged)
                {
                    KeyDown -= SettingsWindow_KeyDown;
                    Closing -= SettingsWindow_Closing;
                    return;
                }

                e.Cancel = true;

                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Save Settings",
                    Message = "Do you want to save changes to settings?",
                    DetailMessage = "You have unsaved changes that will be lost if you don't save.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                });
                var result = await dialog.ShowDialog<ThreeButtonResult>(this);

                if (result == ThreeButtonResult.Button1) return;

                if (result == ThreeButtonResult.Button3)
                {
                    var errors = ViewModel.ValidateAndTrimPaths();
                    if (errors.Count > 0)
                    {
                        await ThreeButtonDialogView.ShowErrorAsync(
                            "Validation Error",
                            "Settings could not be saved due to the following issues:",
                            detail: string.Join("\n", errors),
                            owner: this);
                        return;
                    }
                    ViewModel.SaveSettings();
                }

                KeyDown -= SettingsWindow_KeyDown;
                Closing -= SettingsWindow_Closing;
                Close();
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("Error", "An unexpected error occurred while closing settings.", detail: ex.Message, owner: this);
            }
        }

        private void SettingsWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        #endregion

        #region Event Handlers - Buttons

        private async void ButtonSave_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var errors = ViewModel.ValidateAndTrimPaths();
                if (errors.Count > 0)
                {
                    await ThreeButtonDialogView.ShowErrorAsync(
                        "Validation Error",
                        "Settings could not be saved due to the following issues:",
                        detail: string.Join("\n", errors),
                        owner: this);
                    return;
                }

                ViewModel.SaveSettings();

                // Resize window to match the (possibly changed) font size
                var scale = ViewModel.AppFontSize / BASE_FONT_SIZE;
                Width = Math.Round(BASE_WIDTH * scale);
                Height = Math.Round(BASE_HEIGHT * scale);
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("Save Error", "An error occurred while saving settings.", detail: ex.Message, owner: this);
            }
        }

        private void ButtonClose_Click(object? sender, RoutedEventArgs e) => Close();

        private void Button_Reset_Click(object? sender, RoutedEventArgs e) => ViewModel.ResetFolderPaths();

        private async void ButtonResetAll_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ThreeButtonDialogView.ShowConfirmAsync(
                    "Reset All Settings",
                    "Are you sure you want to reset all settings to defaults?",
                    confirmText: "Reset",
                    cancelText: "Cancel",
                    icon: DialogIconTheme.Warning,
                    detail: "This action cannot be undone. All your customizations will be lost.",
                    owner: this);

                if (result)
                {
                    ViewModel.ResetAllSettings();
                    ViewModel.SettingsChanged = true;
                }
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("Reset Error", "An error occurred while resetting settings.", detail: ex.Message, owner: this);
            }
        }

        private async void BrowseRomsFolder_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Select ROMs Folder",
                        AllowMultiple = false
                    });

                if (folders.Count > 0)
                    ViewModel.RomsPath = folders[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("Folder Selection Error", "An error occurred while opening the folder picker.", detail: ex.Message, owner: this);
            }
        }

        private async void BrowseMameExecutable_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Select MAME Executable",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("Executable Files")
                            {
                                Patterns = new[] { "*.exe", "*" }
                            }
                        }
                    });

                if (files.Count > 0)
                    ViewModel.MamePath = files[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("File Selection Error", "An error occurred while opening the file picker.", detail: ex.Message, owner: this);
            }
        }

        private async void BrowseMediaFolder_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button { DataContext: Gamelist_Manager.Models.MediaFolderItem item }) return;

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var locationPath = _sessionState.CurrentRomFolder
                    ?? _settingsState.RootRomFolder;

                IStorageFolder? startLocation = null;
                if (locationPath != null)
                {
                    startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(locationPath);
                }

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        SuggestedStartLocation = startLocation,
                        Title = "Select Media Folder",
                        AllowMultiple = false
                    });

                if (folders.Count > 0)
                    item.Path = folders[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                await ThreeButtonDialogView.ShowErrorAsync("Folder Selection Error", "An error occurred while opening the folder picker.", detail: ex.Message, owner: this);
            }
        }

        #endregion
    }
}