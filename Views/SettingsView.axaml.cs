using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gamelist_Manager.Services;
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

        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;

        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            // Scale window dimensions proportionally to font size (base: 580x600 at font 12)
            var scale = SharedDataService.Instance.AppFontSize / BASE_FONT_SIZE;
            Width = Math.Round(BASE_WIDTH * scale);
            Height = Math.Round(BASE_HEIGHT * scale);

            // Handle ESC key to close window
            KeyDown += SettingsWindow_KeyDown;

            // Handle window closing to check for unsaved changes
            Closing += SettingsWindow_Closing;

            // Propagate profile list changes to MainWindowViewModel
            ViewModel.ProfilesChanged += (_, _) =>
            {
                if (Owner is MainWindow { DataContext: MainWindowViewModel mainVm })
                    mainVm.RefreshProfiles();
            };

            // Confirm save before switching profiles if dirty
            ViewModel.ConfirmSwitchProfileRequested += async (_, profileName) =>
            {
                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Switch Profile",
                    Message = $"Switch to profile '{profileName}'?",
                    DetailMessage = "You have unsaved changes. Do you want to save them first?",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                });
                var result = await dialog.ShowDialog<ThreeButtonResult>(this);
                if (result == ThreeButtonResult.Button1) return;
                ViewModel.DoSwitchProfile(saveFirst: result == ThreeButtonResult.Button3);
            };

            // Confirm before deleting a profile
            ViewModel.ConfirmDeleteProfileRequested += async (_, profileName) =>
            {
                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Delete Profile",
                    Message = $"Delete profile '{profileName}'?",
                    DetailMessage = "This cannot be undone.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "",
                    Button3Text = "Delete"
                });
                var result = await dialog.ShowDialog<ThreeButtonResult>(this);
                if (result == ThreeButtonResult.Button3)
                    ViewModel.DoDeleteProfile();
            };

            // Confirm overwrite when creating from template over existing profile
            ViewModel.DuplicateTemplateProfileRequested += async (_, profileName) =>
            {
                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Profile Already Exists",
                    Message = $"A profile named '{profileName}' already exists.",
                    DetailMessage = "Do you want to overwrite it?",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "",
                    Button3Text = "Overwrite"
                });
                var result = await dialog.ShowDialog<ThreeButtonResult>(this);
                if (result == ThreeButtonResult.Button3)
                    ViewModel.DoCreateFromTemplate(overwrite: true);
            };
        }

        private async void SettingsWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (ViewModel.IsDirty)
            {
                e.Cancel = true;

                // Show save confirmation dialog for settings
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

                if (result == ThreeButtonResult.Button1) return; // Cancel

                if (result == ThreeButtonResult.Button3) // Save
                {
                    var errors = ViewModel.ValidateAndTrimPaths();
                    if (errors.Count > 0)
                    {
                        // show error dialog
                        return; // stay open
                    }
                    ViewModel.SaveSettings();
                }

                // Reaches here only for DontSave or successful Save
                Closing -= SettingsWindow_Closing;
                Close();
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

        private async void ButtonSave_Click(object? sender, RoutedEventArgs e)
        {
            var errors = ViewModel.ValidateAndTrimPaths();
            if (errors.Count > 0)
            {
                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Validation Error",
                    Message = "Settings could not be saved due to the following issues:",
                    DetailMessage = string.Join("\n", errors),
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                await dialog.ShowDialog<ThreeButtonResult>(this);
                return;
            }

            ViewModel.SaveSettings();

            // Resize window to match the (possibly changed) font size
            var scale = ViewModel.AppFontSize / BASE_FONT_SIZE;
            Width = Math.Round(BASE_WIDTH * scale);
            Height = Math.Round(BASE_HEIGHT * scale);
        }

        private void ButtonClose_Click(object? sender, RoutedEventArgs e)
        {
            // Close will trigger the Closing event which checks for unsaved changes
            Close();
        }

        private void Button_Reset_Click(object? sender, RoutedEventArgs e)
        {
            // Reset media paths to defaults
            ViewModel.ResetFolderPaths();
        }

        private async void ButtonResetAll_Click(object? sender, RoutedEventArgs e)
        {
            // Show confirmation dialog using standard ThreeButtonDialog
            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Reset All Settings",
                Message = "Are you sure you want to reset all settings to defaults?",
                DetailMessage = "This action cannot be undone. All your customizations will be lost.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Reset"
            });

            var result = await dialog.ShowDialog<ThreeButtonResult>(this);

            if (result == ThreeButtonResult.Button3) // Reset
            {
                // Reset all settings to defaults
                ViewModel.ResetAllSettings();
                ViewModel.IsDirty = true;
            }
        }

        private async void BrowseRomsFolder_Click(object? sender, RoutedEventArgs e)
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

        private async void BrowseMameExecutable_Click(object? sender, RoutedEventArgs e)
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

        private async void BrowseMediaFolder_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Gamelist_Manager.Models.MediaFolderItem item }) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select Media Folder",
                    AllowMultiple = false
                });

            if (folders.Count > 0)
                item.Path = folders[0].Path.LocalPath;
        }

    }
}